using System.Text.RegularExpressions;
using VMTranslator.Abstractions;
namespace VMTranslator.Services;

public partial class HackVmLineTranslator : IVmLineTranslator
{
    
    private static readonly IDictionary<string, string> RegisterMap = new Dictionary<string, string>
    {
        {
            "local", "@LCL"
        },
        {
            "argument", "@ARG"
        },
        {
            "this", "@THIS"
        },
        {
            "that", "@THAT"
        },
    };
    
    public string[] Translate(string line, int lineNumber = 0)
    {
        var match = PushPopInstructionRegex().Match(line);
        return [
            $"// {line}",
            ..MapTranslate(line, match, lineNumber)
        ];
    }
    private static string[] MapTranslate(string line, Match match, int lineNumber)
    {

        if (!match.Success)
            return line switch
            {
                "add" => ProcessAddInstruction("+"),
                "sub" => ProcessAddInstruction("-"),
                "or" => ProcessAddInstruction("|"),
                "and" => ProcessAddInstruction("&"),
                "eq" => ProcessCompareInstruction("=", lineNumber),
                "gt" => ProcessCompareInstruction(">", lineNumber),
                "lt" => ProcessCompareInstruction("<", lineNumber),
                "neg" => [
                    "@SP",
                    "A=M-1",
                    "M=-M"
                ],
                "not" => [
                    "@SP",
                    "A=M-1",
                    "M=!M"
                ],
                _ => throw new InvalidOperationException($"line {lineNumber} Invalid instruction for : {line}")
            };
        var segment = match.Groups["segment"].Value;
        var indexStr = match.Groups["index"].Value;
        var command =  match.Groups["command"].Value;
        if (!int.TryParse(indexStr, out var index))
        {
            throw new InvalidOperationException($"Line {lineNumber}, Invalid instruction for : {line}");
        }
        try
        {
            return command switch
            {
                "push" => ProcessPushInstructions(segment, index),
                "pop" => ProcessPopInstructions(segment, index),
                _ => throw new InvalidOperationException($"Line  {lineNumber}, Incorrect instruction for : {line}")
            };

        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Line  {lineNumber}, Invalid instruction for : {line}", e);
        }
    }
    private static string[] ProcessCompareInstruction(string symbol, int lineNumber)
    {
        var endLabel = $"END_{lineNumber}";

        var jumpCondition = symbol switch
        {
            "=" => "JEQ",
            ">" => "JGT",
            "<" => "JLT",
            _ => throw new InvalidOperationException($"Invalid comparison instruction: {symbol}")
        };

        return
        [
            "@SP",         // SP--
            "AM=M-1",
            "D=M",         // D = y

            "A=A-1",       // A = SP-1
            "D=M-D",       // D = x - y
            "M=-1",        // assume true (-1)

            $"@{endLabel}",
            $"D;{jumpCondition}",

            "@SP",         // if condition is false
            "A=M-1",
            "M=0",         // set false (0)

            $"({endLabel})"
        ];
    }
    private static string[] ProcessAddInstruction(string op)
    {
        var l = op == "+" ? "M=D+M" : $"M=M{op}D";
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            l
        ];
    }

    private static string[] ProcessPushInstructions(string segment, int index)
    {
        return segment switch
        {
            "constant" => PushConstantInstructions(index),
            "argument" or "local" or "this" or "that" => PushLocalOrArgumentInstructions(segment, index),
            "temp" => PushTempInstructions(index),
            "pointer" => PushPointerInstructions(index),
            "static" => PushStaticInstructions(index),
            _ =>
            [
            ]
        };

    }
    
    private static string[] ProcessPopInstructions(string segment, int index)
    {
        return segment switch
        {
            "constant" => throw new InvalidOperationException($"Invalid instruction for : {segment}"),
            "argument" or "local" or "this" or "that" => PopLocalOrArgumentInstructions(segment, index),
            "temp" => PopTempInstructions(index),
            "pointer" => PopPointerInstructions(index),
            "static" => PopStaticInstructions(index),
            _ =>
            [
            ]
        };

    }
    private static string[] PopStaticInstructions(int index)
    {
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            
            $"@FileName.{index}",
            "M=D"
        ];
    }
    private static string[] PopPointerInstructions(int index)
    {
        if (index is > 1 or < 0)
        {
            throw new InvalidOperationException($"Invalid pointer instruction for : {index}");
        }
        var addr = index == 0 ? "@THIS" : "@THAT";
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            
            addr,
            "M=D"
        ];
    }
    private static string[] PopTempInstructions(int index)
    {
        if (index is > 7 or < 0)
        {
            throw new InvalidOperationException($"Invalid instruction for : {index}");
        }
        return
        [
            $"@{index}",
            "D=A",
            "@R5",
            "D=D+A",
            "@R13",
            "M=D",
            
            "@SP",
            "AM=M-1",
            "D=M",
            
            "@R13",
            "A=M",
            "M=D"
,        ];
    }
    private static string[] PopLocalOrArgumentInstructions(string segment, int index)
    {
        if (RegisterMap.TryGetValue(segment, out var seg))
        {
            return
            [
                $"@{index}",
                "D=A",
                $"{seg}",
                "D=D+M",
                "@R13",
                "M=D",
                
                "@SP",
                "AM=M-1",
                "D=M",
                
                "@R13",
                "A=M",
                "M=D"
            ];
        }
        throw new InvalidOperationException();
    }

    private static string[] PushStaticInstructions(int index)
    {
        return
        [
            $"@FileName.{index}",
            "D=M",
            ..LoadDStack()
        ];
    }
    
    private static string[] PushPointerInstructions(int index)
    {
        var addr = index == 0 ? "@THIS" : "@THAT";
        if (index is < 0 or > 1)
        {
            throw new InvalidOperationException();
        }
        return
        [
            addr,
            "D=M",
            ..LoadDStack()
        ];
    }
    
    private static string[] PushTempInstructions(int index)
    {
        if (index > 7)
        {
            throw new InvalidOperationException();
        }

        return
        [
            $"@{index}",
            "D=A",
            "@5",
            "A=D+A",
            "D=M",
            ..LoadDStack()
        ];
    }
    
    private static string[] IncrementSp => [
        "@SP",
        "M=M+1"
    ];
    
    private static string[] DecrementSp => [
        "@SP",
        "M=M-1"
    ];

    private static string[] LoadDStack(bool isIncrement = true)
    {
        var lastOp = isIncrement ? IncrementSp : DecrementSp;
        return
        [
            "@SP",
            "A=M",
            "M=D",
            ..lastOp
        ];
    }

    private static string[] PushConstantInstructions(int index)
    {
        return
        [
            $"@{index}",
            "D=A",
            ..LoadDStack()
        ];
    }

    private static string[] PushLocalOrArgumentInstructions(string segment, int index)
    {
        if (RegisterMap.TryGetValue(segment, out var seg))
        {
            return
            [
                $"@{index}",
                "D=A",
                seg,
                "A=D+M",
                "D=M",
                ..LoadDStack()
            ];
        }
        throw new InvalidOperationException();
    }
    
 
    [GeneratedRegex(@"^(?<command>push|pop)\s+(?<segment>constant|local|argument|this|that|temp|pointer|static)\s+(?<index>\d+)$")]
    private partial Regex PushPopInstructionRegex();
    
}

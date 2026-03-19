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

    public string[] Translate(string line, int lineNumber = 0, string fileName = "FileName")
    {
        return [
            $"// {line}",
            ..MapTranslate(line, lineNumber, fileName)
        ];
    }
    private string[] MapTranslate(string line, int lineNumber, string fileName)
    {

        var matchPushPop = PushPopInstructionRegex().Match(line);
        if (matchPushPop.Success)
        {
            var segment = matchPushPop.Groups["segment"].Value;
            var indexStr = matchPushPop.Groups["index"].Value;
            var command =  matchPushPop.Groups["command"].Value;
            if (!int.TryParse(indexStr, out var index))
            {
                throw new InvalidOperationException($"Line {lineNumber}, Invalid instruction for : {line}");
            }
            try
            {
                return command switch
                {
                    "push" => ProcessPushInstructions(segment, index, fileName),
                    "pop" => ProcessPopInstructions(segment, index, fileName),
                    _ => throw new InvalidOperationException($"Line  {lineNumber}, Incorrect instruction for : {line}")
                };

            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Line  {lineNumber}, Invalid instruction for : {line}", e);
            }
        }

        var matchFuncCall = CallFunctionInstructionRegex().Match(line);
        if (!matchFuncCall.Success)
        {
            return line switch
            {
                "add" => ProcessAddInstruction("+"),
                "sub" => ProcessAddInstruction("-"),
                "or" => ProcessAddInstruction("|"),
                "and" => ProcessAddInstruction("&"),
                "eq" => ProcessCompareInstruction("=", lineNumber),
                "gt" => ProcessCompareInstruction(">", lineNumber),
                "lt" => ProcessCompareInstruction("<", lineNumber),
                "neg" =>
                [
                    "@SP",
                    "A=M-1",
                    "M=-M"
                ],
                "not" =>
                [
                    "@SP",
                    "A=M-1",
                    "M=!M"
                ],
                "return" => ProcessReturnInstruction(),
                var l when GotoAndLabelInstructionRegex().IsMatch(l) => GotoAndLabelInstruction(line, lineNumber),
                var l when IfGotoInstructionRegex().IsMatch(l) => IfGotoInstruction(line, lineNumber),
                _ => throw new InvalidOperationException($"line {lineNumber} Invalid instruction for : {line}")
            };
        }

        var type = matchFuncCall.Groups["type"].Value;
        var name = matchFuncCall.Groups["name"].Value;
        var arg = matchFuncCall.Groups["arg"].Value;
        if (!int.TryParse(arg, out var number))
        {
            throw new InvalidOperationException($"Line {lineNumber}, Invalid instruction for : {line}");
        }
        try
        {
            return type switch
            {
                "call" => ProcessCallInstruction(line, name, number, lineNumber),
                "function" => ProcessFunctionInstruction(line, name, number, lineNumber),
                _ => throw new InvalidOperationException($"Line  {lineNumber}, Incorrect instruction for : {line}")
            };
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Line  {lineNumber}, Invalid instruction for : {line}", e);
        }
    }

    private string[] IfGotoInstruction(string line, int lineNumber)
    {
        var match = IfGotoInstructionRegex().Match(line);
        var dest = match.Groups["dest"].Value;
        return [
            $"// begin [{line}]",
            Indent("@SP"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent($"@{dest}"),
            Indent("D;JNE"),
            $"// end [{line}]",
        ];
    }

    private string[] GotoAndLabelInstruction(string line, int lineNumber)
    {
        var match = GotoAndLabelInstructionRegex().Match(line);
        var type = match.Groups["type"].Value;
        var dest = match.Groups["dest"].Value;
        if (type == "goto")
        {

            return [
                Indent($"// begin [{line}]"),
                Indent($"@{dest}"),
                Indent("0;JMP"),
                Indent($"// end [{line}]"),
            ];
        }
        return [
            Indent($"// begin [{line}]"),
            Indent($"({dest})", 2),
            Indent($"// end [{line}]"),
        ];
    }

    private string[] ProcessFunctionInstruction(string line, string name, int number, int lineNumber)
    {
        var result = new List<string>
        {
            $"// begin [{line}]",
            Indent("// Declare the function to be able to jump in it"),
            Indent($"({name})"),
            Indent($"// Push 0 n-tine (n={number})")
        };

        for (var i = 0; i < number; i++)
        {
            result.Add(Indent("@0"));
            result.Add(Indent("D=A"));
            result.AddRange(LoadDStack()
                .Select(r => Indent(r)));
        }

        result.Add($"// end [{line}]");

        return result.ToArray();
    }

    private string[] ProcessCallInstruction(string line, string name, int number, int lineNumber)
    {
        var returnLabel = $"{name}.ret.{lineNumber}";

        return
        [
            $"// begin [{line}]",

            Indent("// push return addr"),
            Indent($"@{returnLabel}"),
            Indent("D=A"),
            ..LoadDStack()
                .Select(r => Indent(r)),

            Indent("// push LCL"),
            Indent("@LCL"),
            Indent("D=M"),
            ..LoadDStack()
                .Select(r => Indent(r)),

            Indent("// push ARG"),
            Indent("@ARG"),
            Indent("D=M"),
            ..LoadDStack()
                .Select(r => Indent(r)),

            Indent("// push THIS"),
            Indent("@THIS"),
            Indent("D=M"),
            ..LoadDStack()
                .Select(r => Indent(r)),

            Indent("// push THAT"),
            Indent("@THAT"),
            Indent("D=M"),
            ..LoadDStack()
                .Select(r => Indent(r)),

            Indent("// LCL = SP"),
            Indent("@SP"),
            Indent("D=M"),
            Indent("@LCL"),
            Indent("M=D"),

            Indent($"// ARG = SP - (5 + {number})"),
            Indent($"@{number}"),
            Indent("D=A"),
            Indent("@5"),
            Indent("D=D+A"),
            Indent("@SP"),
            Indent("D=M-D"),
            Indent("@ARG"),
            Indent("M=D"),

            Indent($"// goto {name}"),
            Indent($"@{name}"),
            Indent("0;JMP"),

            Indent("// declare the return here"),
            Indent($"({returnLabel})"),

            $"// end [{line}]"
        ];
    }

    private static string Indent(string line, int depth = 1)
    {
        return $"{new string('\t', depth)}{line}";
    }

    private static string[] ProcessReturnInstruction()
    {
        return
        [
            "// begin [return]",

            Indent("// FRAME = LCL"),
            Indent("@LCL"),
            Indent("D=M"),
            Indent("@R13"),
            Indent("M=D"),

            Indent("// RET = *(FRAME - 5)"),
            Indent("@5"),
            Indent("A=D-A"),
            Indent("D=M"),
            Indent("@R14"),
            Indent("M=D"),

            Indent("// *ARG = pop()"),
            Indent("@SP"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent("@ARG"),
            Indent("A=M"),
            Indent("M=D"),

            Indent("// SP = ARG + 1"),
            Indent("@ARG"),
            Indent("D=M+1"),
            Indent("@SP"),
            Indent("M=D"),

            Indent("// THAT = *(FRAME - 1)"),
            Indent("@R13"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent("@THAT"),
            Indent("M=D"),

            Indent("// THIS = *(FRAME - 2)"),
            Indent("@R13"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent("@THIS"),
            Indent("M=D"),

            Indent("// ARG = *(FRAME - 3)"),
            Indent("@R13"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent("@ARG"),
            Indent("M=D"),

            Indent("// LCL = *(FRAME - 4)"),
            Indent("@R13"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent("@LCL"),
            Indent("M=D"),

            Indent("// goto RET"),
            Indent("@R14"),
            Indent("A=M"),
            Indent("0;JMP"),

            "// end [return]",
        ];
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

    private static string[] ProcessPushInstructions(string segment, int index, string fileName)
    {
        return segment switch
        {
            "constant" => PushConstantInstructions(index),
            "argument" or "local" or "this" or "that" => PushLocalOrArgumentInstructions(segment, index),
            "temp" => PushTempInstructions(index),
            "pointer" => PushPointerInstructions(index),
            "static" => PushStaticInstructions(index, fileName),
            _ =>
            [
            ]
        };

    }
    
    private static string[] ProcessPopInstructions(string segment, int index, string fileName)
    {
        return segment switch
        {
            "constant" => throw new InvalidOperationException($"Invalid instruction for : {segment}"),
            "argument" or "local" or "this" or "that" => PopLocalOrArgumentInstructions(segment, index),
            "temp" => PopTempInstructions(index),
            "pointer" => PopPointerInstructions(index),
            "static" => PopStaticInstructions(index, fileName),
            _ =>
            [
            ]
        };

    }
    private static string[] PopStaticInstructions(int index, string fileName)
    {
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",

            $"@{fileName}.{index}",
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

    private static string[] PushStaticInstructions(int index, string fileName)
    {
        return
        [
            $"@{fileName}.{index}",
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

    [GeneratedRegex(@"^(?<type>function|call)\s+(?<name>\S+)\s+(?<arg>\d+)$")]
    private partial Regex CallFunctionInstructionRegex();

    [GeneratedRegex(@"^(?<type>goto|label)\s+(?<dest>\S+)$")]
    private partial Regex GotoAndLabelInstructionRegex();

    [GeneratedRegex(@"^if-goto\s+(?<dest>\S+)$")]
    private partial Regex IfGotoInstructionRegex();
}

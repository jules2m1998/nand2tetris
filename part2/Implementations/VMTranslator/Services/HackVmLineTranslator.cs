using System.Text.RegularExpressions;
using VMTranslator.Abstractions;

namespace VMTranslator.Services;

public partial class HackVmLineTranslator : IVmLineTranslator
{
    private static readonly IDictionary<string, string> BasePointerBySegment = new Dictionary<string, string>
    {
        { "local", "@LCL" },
        { "argument", "@ARG" },
        { "this", "@THIS" },
        { "that", "@THAT" }
    };

    public string[] Translate(string line, int lineNumber = 0, string fileName = "FileName")
    {
        return
        [
            $"// {line}",
            .. TranslateCommand(line, lineNumber, fileName)
        ];
    }

    private string[] TranslateCommand(string line, int lineNumber, string fileName)
    {
        if (TryParsePushPop(line, lineNumber, out var pushPopCommand))
        {
            return TranslatePushPop(pushPopCommand, lineNumber, fileName);
        }

        if (TryParseFunctionCommand(line, lineNumber, out var functionCommand))
        {
            return TranslateFunctionCommand(functionCommand, lineNumber);
        }

        if (TryParseBranchCommand(line, out var branchCommand))
        {
            return TranslateBranchCommand(branchCommand);
        }

        return TranslateSimpleCommand(line, lineNumber);
    }

    private static bool TryParsePushPop(string line, int lineNumber, out PushPopCommand command)
    {
        var match = PushPopInstructionRegex().Match(line);
        if (!match.Success)
        {
            command = default;
            return false;
        }

        var operation = match.Groups["command"].Value;
        var segment = match.Groups["segment"].Value;
        var indexText = match.Groups["index"].Value;

        if (!int.TryParse(indexText, out var index))
        {
            throw new InvalidOperationException($"Line {lineNumber}, Invalid instruction for : {line}");
        }

        command = new PushPopCommand(operation, segment, index);
        return true;
    }

    private static bool TryParseFunctionCommand(string line, int lineNumber, out FunctionCommand command)
    {
        var match = CallFunctionInstructionRegex().Match(line);
        if (!match.Success)
        {
            command = default;
            return false;
        }

        var operation = match.Groups["type"].Value;
        var name = match.Groups["name"].Value;
        var valueText = match.Groups["arg"].Value;

        if (!int.TryParse(valueText, out var value))
        {
            throw new InvalidOperationException($"Line {lineNumber}, Invalid instruction for : {line}");
        }

        command = new FunctionCommand(operation, name, value, line);
        return true;
    }

    private static bool TryParseBranchCommand(string line, out BranchCommand command)
    {
        var gotoOrLabelMatch = GotoAndLabelInstructionRegex().Match(line);
        if (gotoOrLabelMatch.Success)
        {
            command = new BranchCommand(
                Type: gotoOrLabelMatch.Groups["type"].Value,
                Destination: gotoOrLabelMatch.Groups["dest"].Value,
                SourceLine: line);
            return true;
        }

        var ifGotoMatch = IfGotoInstructionRegex().Match(line);
        if (ifGotoMatch.Success)
        {
            command = new BranchCommand(
                Type: "if-goto",
                Destination: ifGotoMatch.Groups["dest"].Value,
                SourceLine: line);
            return true;
        }

        command = default;
        return false;
    }

    private static string[] TranslatePushPop(PushPopCommand command, int lineNumber, string fileName)
    {
        try
        {
            return command.Operation switch
            {
                "push" => TranslatePush(command.Segment, command.Index, fileName),
                "pop" => TranslatePop(command.Segment, command.Index, fileName),
                _ => throw new InvalidOperationException($"Line  {lineNumber}, Incorrect instruction for : {command.Operation}")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Line  {lineNumber}, Invalid instruction for : {command.Operation} {command.Segment} {command.Index}",
                ex);
        }
    }

    private static string[] TranslateFunctionCommand(FunctionCommand command, int lineNumber)
    {
        try
        {
            return command.Operation switch
            {
                "call" => BuildCallInstruction(command.SourceLine, command.Name, command.Value, lineNumber),
                "function" => BuildFunctionInstruction(command.SourceLine, command.Name, command.Value),
                _ => throw new InvalidOperationException($"Line  {lineNumber}, Incorrect instruction for : {command.SourceLine}")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Line  {lineNumber}, Invalid instruction for : {command.SourceLine}", ex);
        }
    }

    private static string[] TranslateBranchCommand(BranchCommand command)
    {
        return command.Type switch
        {
            "if-goto" => BuildIfGotoInstruction(command.SourceLine, command.Destination),
            "goto" => BuildGotoInstruction(command.SourceLine, command.Destination),
            "label" => BuildLabelInstruction(command.SourceLine, command.Destination),
            _ => throw new InvalidOperationException($"Invalid branch instruction: {command.SourceLine}")
        };
    }

    private static string[] TranslateSimpleCommand(string line, int lineNumber)
    {
        return line switch
        {
            "add" => BuildBinaryArithmetic("M=D+M"),
            "sub" => BuildBinaryArithmetic("M=M-D"),
            "or" => BuildBinaryArithmetic("M=M|D"),
            "and" => BuildBinaryArithmetic("M=M&D"),
            "eq" => BuildComparison("JEQ", lineNumber),
            "gt" => BuildComparison("JGT", lineNumber),
            "lt" => BuildComparison("JLT", lineNumber),
            "neg" => ["@SP", "A=M-1", "M=-M"],
            "not" => ["@SP", "A=M-1", "M=!M"],
            "return" => BuildReturnInstruction(),
            _ => throw new InvalidOperationException($"line {lineNumber} Invalid instruction for : {line}")
        };
    }

    private static string[] TranslatePush(string segment, int index, string fileName)
    {
        return segment switch
        {
            "constant" => PushConstant(index),
            "argument" or "local" or "this" or "that" => PushFromBasePointer(segment, index),
            "temp" => PushTemp(index),
            "pointer" => PushPointer(index),
            "static" => PushStatic(index, fileName),
            _ => []
        };
    }

    private static string[] TranslatePop(string segment, int index, string fileName)
    {
        return segment switch
        {
            "constant" => throw new InvalidOperationException($"Invalid instruction for : {segment}"),
            "argument" or "local" or "this" or "that" => PopToBasePointer(segment, index),
            "temp" => PopTemp(index),
            "pointer" => PopPointer(index),
            "static" => PopStatic(index, fileName),
            _ => []
        };
    }

    private static string[] PushConstant(int index)
    {
        return
        [
            $"@{index}",
            "D=A",
            .. PushDToStack()
        ];
    }

    private static string[] PushFromBasePointer(string segment, int index)
    {
        if (!BasePointerBySegment.TryGetValue(segment, out var basePointer))
        {
            throw new InvalidOperationException();
        }

        return
        [
            $"@{index}",
            "D=A",
            basePointer,
            "A=D+M",
            "D=M",
            .. PushDToStack()
        ];
    }

    private static string[] PushTemp(int index)
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
            .. PushDToStack()
        ];
    }

    private static string[] PushPointer(int index)
    {
        if (index is < 0 or > 1)
        {
            throw new InvalidOperationException();
        }

        var target = index == 0 ? "@THIS" : "@THAT";
        return
        [
            target,
            "D=M",
            .. PushDToStack()
        ];
    }

    // Static variables must be namespaced by VM file so different files do not share the same symbols.
    private static string[] PushStatic(int index, string fileName)
    {
        return
        [
            $"@{fileName}.{index}",
            "D=M",
            .. PushDToStack()
        ];
    }

    private static string[] PopToBasePointer(string segment, int index)
    {
        if (!BasePointerBySegment.TryGetValue(segment, out var basePointer))
        {
            throw new InvalidOperationException();
        }

        return
        [
            $"@{index}",
            "D=A",
            basePointer,
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

    private static string[] PopTemp(int index)
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
        ];
    }

    private static string[] PopPointer(int index)
    {
        if (index is > 1 or < 0)
        {
            throw new InvalidOperationException($"Invalid pointer instruction for : {index}");
        }

        var target = index == 0 ? "@THIS" : "@THAT";
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            target,
            "M=D"
        ];
    }

    // Static variables must be namespaced by VM file so different files do not share the same symbols.
    private static string[] PopStatic(int index, string fileName)
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

    private static string[] BuildBinaryArithmetic(string finalInstruction)
    {
        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            finalInstruction
        ];
    }

    private static string[] BuildComparison(string jumpInstruction, int lineNumber)
    {
        var endLabel = $"END_{lineNumber}";

        return
        [
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            "D=M-D",
            "M=-1",
            $"@{endLabel}",
            $"D;{jumpInstruction}",
            "@SP",
            "A=M-1",
            "M=0",
            $"({endLabel})"
        ];
    }

    private static string[] BuildIfGotoInstruction(string line, string destination)
    {
        return
        [
            $"// begin [{line}]",
            Indent("@SP"),
            Indent("AM=M-1"),
            Indent("D=M"),
            Indent($"@{destination}"),
            Indent("D;JNE"),
            $"// end [{line}]"
        ];
    }

    private static string[] BuildGotoInstruction(string line, string destination)
    {
        return
        [
            Indent($"// begin [{line}]"),
            Indent($"@{destination}"),
            Indent("0;JMP"),
            Indent($"// end [{line}]")
        ];
    }

    private static string[] BuildLabelInstruction(string line, string destination)
    {
        return
        [
            Indent($"// begin [{line}]"),
            Indent($"({destination})", 2),
            Indent($"// end [{line}]")
        ];
    }

    private static string[] BuildFunctionInstruction(string line, string name, int localCount)
    {
        var result = new List<string>
        {
            $"// begin [{line}]",
            Indent("// Declare the function to be able to jump in it"),
            Indent($"({name})"),
            Indent($"// Push 0 n-tine (n={localCount})")
        };

        for (var i = 0; i < localCount; i++)
        {
            result.Add(Indent("@0"));
            result.Add(Indent("D=A"));
            result.AddRange(PushDToStack().Select(line => Indent(line)));
        }

        result.Add($"// end [{line}]");
        return [.. result];
    }

    private static string[] BuildCallInstruction(string line, string name, int argumentCount, int lineNumber)
    {
        var returnLabel = $"{name}.ret.{lineNumber}";
        var result = new List<string>
        {
            $"// begin [{line}]"
        };

        // A VM call saves the caller frame, repositions ARG/LCL, then jumps to the callee.
        AppendCallFramePush(result, "// push return addr", $"@{returnLabel}", "D=A");
        AppendCallFramePush(result, "// push LCL", "@LCL", "D=M");
        AppendCallFramePush(result, "// push ARG", "@ARG", "D=M");
        AppendCallFramePush(result, "// push THIS", "@THIS", "D=M");
        AppendCallFramePush(result, "// push THAT", "@THAT", "D=M");

        result.Add(Indent("// LCL = SP"));
        result.Add(Indent("@SP"));
        result.Add(Indent("D=M"));
        result.Add(Indent("@LCL"));
        result.Add(Indent("M=D"));

        result.Add(Indent($"// ARG = SP - (5 + {argumentCount})"));
        result.Add(Indent($"@{argumentCount}"));
        result.Add(Indent("D=A"));
        result.Add(Indent("@5"));
        result.Add(Indent("D=D+A"));
        result.Add(Indent("@SP"));
        result.Add(Indent("D=M-D"));
        result.Add(Indent("@ARG"));
        result.Add(Indent("M=D"));

        result.Add(Indent($"// goto {name}"));
        result.Add(Indent($"@{name}"));
        result.Add(Indent("0;JMP"));
        result.Add(Indent("// declare the return here"));
        result.Add(Indent($"({returnLabel})"));
        result.Add($"// end [{line}]");

        return [.. result];
    }

    private static void AppendCallFramePush(List<string> result, string comment, string address, string valueInstruction)
    {
        result.Add(Indent(comment));
        result.Add(Indent(address));
        result.Add(Indent(valueInstruction));
        result.AddRange(PushDToStack().Select(line => Indent(line)));
    }

    private static string[] BuildReturnInstruction()
    {
        // Return restores the caller frame from the current LCL-based frame snapshot.
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
            "// end [return]"
        ];
    }

    private static string[] PushDToStack()
    {
        return
        [
            "@SP",
            "A=M",
            "M=D",
            "@SP",
            "M=M+1"
        ];
    }

    private static string Indent(string line, int depth = 1)
    {
        return $"{new string('\t', depth)}{line}";
    }

    [GeneratedRegex(@"^(?<command>push|pop)\s+(?<segment>constant|local|argument|this|that|temp|pointer|static)\s+(?<index>\d+)$")]
    private static partial Regex PushPopInstructionRegex();

    [GeneratedRegex(@"^(?<type>function|call)\s+(?<name>\S+)\s+(?<arg>\d+)$")]
    private static partial Regex CallFunctionInstructionRegex();

    [GeneratedRegex(@"^(?<type>goto|label)\s+(?<dest>\S+)$")]
    private static partial Regex GotoAndLabelInstructionRegex();

    [GeneratedRegex(@"^if-goto\s+(?<dest>\S+)$")]
    private static partial Regex IfGotoInstructionRegex();

    private readonly record struct PushPopCommand(string Operation, string Segment, int Index);

    private readonly record struct FunctionCommand(string Operation, string Name, int Value, string SourceLine);

    private readonly record struct BranchCommand(string Type, string Destination, string SourceLine);
}

using VMTranslator.Services;

namespace VMTranslator.Tests;

public class HackVmLineTranslatorTests
{
    [Theory]
    [MemberData(nameof(PushInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForPushCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = sut.Translate(line);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(PopInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForPopCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = sut.Translate(line);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(BinaryArithmeticInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForBinaryArithmeticCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = sut.Translate(line);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(UnaryArithmeticInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForUnaryArithmeticCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = sut.Translate(line);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(ComparisonInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForComparisonCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = sut.Translate(line);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Translate_IncrementsComparisonLabels_OnRepeatedCalls()
    {
        var sut = new HackVmLineTranslator();

        var first = sut.Translate("eq");
        var second = sut.Translate("eq", 1);

        Assert.Equal(Comparison("eq", "JEQ", 0), first);
        Assert.Equal(Comparison("eq", "JEQ", 1), second);
    }

    [Fact]
    public void Translate_ReturnsExpectedAssembly_ForReturnCommand()
    {
        var sut = new HackVmLineTranslator();

        var result = NormalizeIndentation(sut.Translate("return"));

        Assert.Equal(ReturnCommand(), result);
    }

    [Theory]
    [MemberData(nameof(FunctionInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForFunctionCommands(string line, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = NormalizeIndentation(sut.Translate(line));

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(CallInstructions))]
    public void Translate_ReturnsExpectedAssembly_ForCallCommands(string line, int lineNumber, string[] expected)
    {
        var sut = new HackVmLineTranslator();

        var result = NormalizeIndentation(sut.Translate(line, lineNumber));

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void Translate_ThrowsArgumentException_ForInvalidInput(string line)
    {
        var sut = new HackVmLineTranslator();

        Assert.ThrowsAny<InvalidOperationException>(() => sut.Translate(line));
    }

    public static TheoryData<string, string[]> PushInstructions =>
        new()
        {
            { "push constant 0", PushConstant(0) },
            { "push constant 7", PushConstant(7) },
            { "push constant 100", PushConstant(100) },
            { "push constant 32767", PushConstant(32767) },
            { "push local 0", PushFromSegment("local", "LCL", 0) },
            { "push local 7", PushFromSegment("local", "LCL", 7) },
            { "push local 10", PushFromSegment("local", "LCL", 10) },
            { "push argument 0", PushFromSegment("argument", "ARG", 0) },
            { "push argument 7", PushFromSegment("argument", "ARG", 7) },
            { "push argument 100", PushFromSegment("argument", "ARG", 100) },
            { "push this 0", PushFromSegment("this", "THIS", 0) },
            { "push this 1", PushFromSegment("this", "THIS", 1) },
            { "push this 3", PushFromSegment("this", "THIS", 3) },
            { "push that 0", PushFromSegment("that", "THAT", 0) },
            { "push that 1", PushFromSegment("that", "THAT", 1) },
            { "push that 3", PushFromSegment("that", "THAT", 3) },
            { "push temp 0", PushTemp(0) },
            { "push temp 1", PushTemp(1) },
            { "push temp 2", PushTemp(2) },
            { "push pointer 0", PushPointer(0) },
            { "push pointer 1", PushPointer(1) },
            { "push static 0", PushStatic(0) },
            { "push static 1", PushStatic(1) },
            { "push static 3", PushStatic(3) }
        };

    public static TheoryData<string, string[]> PopInstructions =>
        new()
        {
            { "pop local 0", PopToSegment("local", "LCL", 0) },
            { "pop local 2", PopToSegment("local", "LCL", 2) },
            { "pop local 7", PopToSegment("local", "LCL", 7) },
            { "pop argument 0", PopToSegment("argument", "ARG", 0) },
            { "pop argument 3", PopToSegment("argument", "ARG", 3) },
            { "pop argument 7", PopToSegment("argument", "ARG", 7) },
            { "pop this 0", PopToSegment("this", "THIS", 0) },
            { "pop this 1", PopToSegment("this", "THIS", 1) },
            { "pop this 3", PopToSegment("this", "THIS", 3) },
            { "pop that 0", PopToSegment("that", "THAT", 0) },
            { "pop that 3", PopToSegment("that", "THAT", 3) },
            { "pop that 5", PopToSegment("that", "THAT", 5) },
            { "pop temp 0", PopTemp(0) },
            { "pop temp 1", PopTemp(1) },
            { "pop temp 4", PopTemp(4) },
            { "pop pointer 0", PopPointer(0) },
            { "pop pointer 1", PopPointer(1) },
            { "pop static 0", PopStatic(0) },
            { "pop static 1", PopStatic(1) },
            { "pop static 5", PopStatic(5) }
        };

    public static TheoryData<string, string[]> BinaryArithmeticInstructions =>
        new()
        {
            { "add", BinaryOperation("add", "M=D+M") },
            { "sub", BinaryOperation("sub", "M=M-D") },
            { "and", BinaryOperation("and", "M=M&D") },
            { "or", BinaryOperation("or", "M=M|D") }
        };

    public static TheoryData<string, string[]> UnaryArithmeticInstructions =>
        new()
        {
            { "neg", UnaryOperation("neg", "M=-M") },
            { "not", UnaryOperation("not", "M=!M") }
        };

    public static TheoryData<string, string[]> ComparisonInstructions =>
        new()
        {
            { "eq", Comparison("eq", "JEQ", 0) },
            { "gt", Comparison("gt", "JGT", 0) },
            { "lt", Comparison("lt", "JLT", 0) }
        };

    public static TheoryData<string, string[]> FunctionInstructions =>
        new()
        {
            { "function Sys.init 0", FunctionDeclaration("Sys.init", 0) },
            { "function Main.main 2", FunctionDeclaration("Main.main", 2) }
        };

    public static TheoryData<string, int, string[]> CallInstructions =>
        new()
        {
            { "call Sys.init 0", 66, CallCommand("Sys.init", 0, 66) },
            { "call Math.add 2", 177, CallCommand("Math.add", 2, 177) }
        };

    public static TheoryData<string> InvalidCommands =>
    [
        "pop constant 0",

        "pop constant 1",

        "pop constant 7",

        "push pointer -1",

        "push pointer 2",

        "push pointer 999",

        "pop pointer -1",

        "pop pointer 2",

        "pop pointer 999",

        "push temp -1",

        "push temp 8",

        "push temp 999",

        "pop temp -1",

        "pop temp 8",

        "pop temp 999",

        "push local -1",

        "pop local -1",

        "push argument -1",

        "pop argument -1",

        "push this -1",

        "pop this -1",

        "push that -1",

        "pop that -1",

        "push static -1",

        "pop static -1",

        "push lol 0",

        "pop lol 0",

        "push stack 0",

        "pop stack 0",

        "push pointers 0",

        "pop temps 0",

        "poop constant 7",

        "peek constant 7",

        "move local 0",

        "push",

        "pop",

        "push constant",

        "pop constant",

        "push local",

        "pop local",

        "push pointer",

        "pop pointer",

        "push temp",

        "pop temp",

        "push constant 7 8",

        "pop local 0 1",

        "push pointer 0 extra",

        "pop temp 3 extra",

        "push constant x",

        "pop constant x",

        "push local x",

        "pop local x",

        "push argument x",

        "pop argument x",

        "push this x",

        "pop this x",

        "push that x",

        "pop that x",

        "push temp x",

        "pop temp x",

        "push pointer x",

        "pop pointer x",

        "push static x",

        "pop static x",

        "push constant 1.5",

        "pop local 1.5",

        "push pointer 0.0",

        "pop temp 2.2",

        "push constant +",

        "pop local --1",

        "push temp 1-",

        "Push constant 7",

        "POP local 0",

        "push Constant 7",

        "pop Local 0",

        "push POINTER 0",

        "pushconstant7",

        "push,constant,7",

        "push\t\t",

        "pop\tpointer",

        "return 0",

        "function",

        "function Main.main",

        "function Main.main -1",

        "function Main.main x",

        "call",

        "call Math.add",

        "call Math.add -1",

        "call Math.add x"
    ];

    private static string[] PushConstant(int index)
    {
        var line = $"push constant {index}";
        return
        [
            $"// {line}",
            $"@{index}",
            "D=A",
            .. PushFromD()
        ];
    }

    private static string[] PushFromSegment(string segmentName, string segmentSymbol, int index)
    {
        var line = $"push {segmentName} {index}";
        return
        [
            $"// {line}",
            $"@{index}",
            "D=A",
            $"@{segmentSymbol}",
            "A=D+M",
            "D=M",
            .. PushFromD()
        ];
    }

    private static string[] PushTemp(int index)
    {
        var line = $"push temp {index}";
        return
        [
            $"// {line}",
            $"@{index}",
            "D=A",
            "@5",
            "A=D+A",
            "D=M",
            .. PushFromD()
        ];
    }

    private static string[] PushPointer(int index)
    {
        var line = $"push pointer {index}";
        var symbol = index == 0 ? "THIS" : "THAT";
        return
        [
            $"// {line}",
            $"@{symbol}",
            "D=M",
            .. PushFromD()
        ];
    }

    private static string[] PushStatic(int index)
    {
        var line = $"push static {index}";
        return
        [
            $"// {line}",
            $"@FileName.{index}",
            "D=M",
            .. PushFromD()
        ];
    }

    private static string[] PopToSegment(string segmentName, string segmentSymbol, int index)
    {
        var line = $"pop {segmentName} {index}";
        
        return
        [
            $"// {line}",
            $"@{index}",
            "D=A",
            $"@{segmentSymbol}",
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
        var line = $"pop temp {index}";
        return
        [
            $"// {line}",
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
        var line = $"pop pointer {index}";
        var symbol = index == 0 ? "THIS" : "THAT";
        return
        [
            $"// {line}",
            "@SP",
            "AM=M-1",
            "D=M",
            $"@{symbol}",
            "M=D"
        ];
    }

    private static string[] PopStatic(int index)
    {
        var line = $"pop static {index}";
        return
        [
            $"// {line}",
            "@SP",
            "AM=M-1",
            "D=M",
            $"@FileName.{index}",
            "M=D"
        ];
    }

    private static string[] BinaryOperation(string command, string finalInstruction)
    {
        return
        [
            $"// {command}",
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            finalInstruction
        ];
    }

    private static string[] UnaryOperation(string command, string finalInstruction)
    {
        return
        [
            $"// {command}",
            "@SP",
            "A=M-1",
            finalInstruction
        ];
    }

    private static string[] Comparison(string command, string jumpInstruction, int labelIndex)
    {
        var endLabel = $"END_{labelIndex}";
        return 
        [
            $"// {command}",
            "@SP",         // SP--
            "AM=M-1",
            "D=M",         // D = y

            "A=A-1",       // A = SP-1
            "D=M-D",       // D = x - y
            "M=-1",        // assume true (-1)

            $"@{endLabel}",
            $"D;{jumpInstruction}",

            "@SP",         // if condition is false
            "A=M-1",
            "M=0",         // set false (0)

            $"({endLabel})"
        ];
    }

    private static string[] ReturnCommand()
    {
        return
        [
            "// return",
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

    private static string[] FunctionDeclaration(string functionName, int localCount)
    {
        var line = $"function {functionName} {localCount}";
        var result = new List<string>
        {
            $"// {line}",
            $"// begin [{line}]",
            Indent("// Declare the function to be able to jump in it"),
            Indent($"({functionName})"),
            Indent($"// Push 0 n-tine (n={localCount})")
        };

        for (var i = 0; i < localCount; i++)
        {
            result.Add(Indent("@0"));
            result.Add(Indent("D=A"));
            result.AddRange(IndentLines(PushFromD()));
        }

        result.Add($"// end [{line}]");

        return result.ToArray();
    }

    private static string[] CallCommand(string functionName, int argumentCount, int lineNumber)
    {
        var line = $"call {functionName} {argumentCount}";
        var returnLabel = $"{functionName}.ret.{lineNumber}";

        return
        [
            $"// {line}",
            $"// begin [{line}]",
            Indent("// push return addr"),
            Indent($"@{returnLabel}"),
            Indent("D=A"),
            .. IndentLines(PushFromD()),
            Indent("// push LCL"),
            Indent("@LCL"),
            Indent("D=M"),
            .. IndentLines(PushFromD()),
            Indent("// push ARG"),
            Indent("@ARG"),
            Indent("D=M"),
            .. IndentLines(PushFromD()),
            Indent("// push THIS"),
            Indent("@THIS"),
            Indent("D=M"),
            .. IndentLines(PushFromD()),
            Indent("// push THAT"),
            Indent("@THAT"),
            Indent("D=M"),
            .. IndentLines(PushFromD()),
            Indent("// LCL = SP"),
            Indent("@SP"),
            Indent("D=M"),
            Indent("@LCL"),
            Indent("M=D"),
            Indent($"// ARG = SP - (5 + {argumentCount})"),
            Indent($"@{argumentCount}"),
            Indent("D=A"),
            Indent("@5"),
            Indent("D=D+A"),
            Indent("@SP"),
            Indent("D=M-D"),
            Indent("@ARG"),
            Indent("M=D"),
            Indent($"// goto {functionName}"),
            Indent($"@{functionName}"),
            Indent("0;JMP"),
            Indent("// declare the return here"),
            Indent($"({returnLabel})"),
            $"// end [{line}]"
        ];
    }

    private static string[] NormalizeIndentation(string[] lines)
    {
        return lines
            .Select(line => line.Replace("\t", "    ", StringComparison.Ordinal))
            .ToArray();
    }

    private static string[] IndentLines(string[] lines, int depth = 1)
    {
        return lines.Select(line => Indent(line, depth)).ToArray();
    }

    private static string Indent(string line, int depth = 1)
    {
        return $"{new string(' ', depth * 4)}{line}";
    }

    private static string[] PushFromD()
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
}

using System.Reflection;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;

namespace SyntaxAnalyser.Tests;

public class CodeGeneratorImplementationTests
{
    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForFunction()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            function void main() {
                return;
            }
            }
            """);
        SetCurrentClassName(generator, "Main");

        var actual = generator.CompileSubroutine();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Main.main 0
            push constant 0
            return
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForMethod()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            method void setX(int value) {
                let x = value;
                return;
            }
            }
            """);
        SetCurrentClassName(generator, "Point");
        GetSymbolTable(generator).Define("x", "int", SymbolKind.Field);

        var actual = generator.CompileSubroutine();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Point.setX 0
            push argument 0
            pop pointer 0
            push argument 1
            pop this 0
            push constant 0
            return
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForConstructor()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            constructor Point new() {
                return this;
            }
            }
            """);
        SetCurrentClassName(generator, "Point");
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "int", SymbolKind.Field);
        symbolTable.Define("y", "int", SymbolKind.Field);

        var actual = generator.CompileSubroutine();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Point.new 0
            push constant 2
            call Memory.alloc 1
            pop pointer 0
            push pointer 0
            return
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForFunctionWithParametersAndMultipleLocals()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            function int max(int left, int right) {
                var int winner, margin;
                return right;
            }
            }
            """);
        SetCurrentClassName(generator, "Main");

        var actual = generator.CompileSubroutine();
        var symbolTable = GetSymbolTable(generator);

        Project11TestSupport.AssertVmEquivalent(
            """
            function Main.max 2
            push argument 1
            return
            """,
            actual);
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("left"));
        Assert.Equal(0, symbolTable.IndexOf("left"));
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("right"));
        Assert.Equal(1, symbolTable.IndexOf("right"));
        Assert.Equal(SymbolKind.Var, symbolTable.KindOf("winner"));
        Assert.Equal(0, symbolTable.IndexOf("winner"));
        Assert.Equal(SymbolKind.Var, symbolTable.KindOf("margin"));
        Assert.Equal(1, symbolTable.IndexOf("margin"));
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForMethodWithParametersAndLocals()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            method int translate(int dx, int dy) {
                var int oldX;
                return dy;
            }
            }
            """);
        SetCurrentClassName(generator, "Point");

        var actual = generator.CompileSubroutine();
        var symbolTable = GetSymbolTable(generator);

        Project11TestSupport.AssertVmEquivalent(
            """
            function Point.translate 1
            push argument 0
            pop pointer 0
            push argument 2
            return
            """,
            actual);
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("this"));
        Assert.Equal("Point", symbolTable.TypeOf("this"));
        Assert.Equal(0, symbolTable.IndexOf("this"));
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("dx"));
        Assert.Equal(1, symbolTable.IndexOf("dx"));
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("dy"));
        Assert.Equal(2, symbolTable.IndexOf("dy"));
        Assert.Equal(SymbolKind.Var, symbolTable.KindOf("oldX"));
        Assert.Equal(0, symbolTable.IndexOf("oldX"));
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ReturnsExpectedVm_ForConstructorWithParametersAndLocals()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            constructor Point new(int ax, int ay) {
                var int area;
                return this;
            }
            }
            """);
        SetCurrentClassName(generator, "Point");
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "int", SymbolKind.Field);
        symbolTable.Define("y", "int", SymbolKind.Field);
        symbolTable.Define("size", "int", SymbolKind.Field);

        var actual = generator.CompileSubroutine();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Point.new 1
            push constant 3
            call Memory.alloc 1
            pop pointer 0
            push pointer 0
            return
            """,
            actual);
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("ax"));
        Assert.Equal(0, symbolTable.IndexOf("ax"));
        Assert.Equal(SymbolKind.Argument, symbolTable.KindOf("ay"));
        Assert.Equal(1, symbolTable.IndexOf("ay"));
        Assert.Equal(SymbolKind.Var, symbolTable.KindOf("area"));
        Assert.Equal(0, symbolTable.IndexOf("area"));
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ThrowsForInvalidSubroutineKeyword()
    {
        var (generator, _) = Project11TestSupport.CreateCodeGenerator(
            """
            field void broken() {
                return;
            }
            }
            """);
        SetCurrentClassName(generator, "Broken");

        Assert.ThrowsAny<Exception>(() => generator.CompileSubroutine());
    }

    [Fact]
    public void CompileSubroutine_ThrowsForInvalidReturnTypeKeyword()
    {
        var (generator, _) = Project11TestSupport.CreateCodeGenerator(
            """
            function return broken() {
                return;
            }
            }
            """);
        SetCurrentClassName(generator, "Broken");

        Assert.ThrowsAny<Exception>(() => generator.CompileSubroutine());
    }

    [Fact]
    public void CompileParameterList_ThrowsForInvalidTypeKeyword()
    {
        var (generator, _) = Project11TestSupport.CreateCodeGenerator("return value)");

        Assert.ThrowsAny<Exception>(() => generator.CompileParameterList());
    }

    [Fact]
    public void CompileStatements_ReturnsExpectedVm_AndStopsBeforeClosingBrace()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            let x = 1;
            do Output.printInt(x);
            return;
            }
            """);
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "int", SymbolKind.Var);

        var actual = generator.CompileStatements();

        Project11TestSupport.AssertVmEquivalent(
            """
            push constant 1
            pop local 0
            push local 0
            call Output.printInt 1
            pop temp 0
            push constant 0
            return
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileDo_ReturnsExpectedVm_ForQualifiedClassCall()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("do Output.printInt(2); return");

        var actual = generator.CompileDo();

        Project11TestSupport.AssertVmEquivalent(
            """
            push constant 2
            call Output.printInt 1
            pop temp 0
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileDo_ReturnsExpectedVm_ForCurrentObjectMethodCall()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("do move(1, 2); return");
        SetCurrentClassName(generator, "Point");

        var actual = generator.CompileDo();

        Project11TestSupport.AssertVmEquivalent(
            """
            push pointer 0
            push constant 1
            push constant 2
            call Point.move 3
            pop temp 0
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnsExpectedVm_ForLocalAssignment()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("let x = 2; return");
        GetSymbolTable(generator).Define("x", "int", SymbolKind.Var);

        var actual = generator.CompileLet();

        Project11TestSupport.AssertVmEquivalent(
            """
            push constant 2
            pop local 0
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnsExpectedVm_ForArrayAssignment()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("let arr[i] = value; return");
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("arr", "Array", SymbolKind.Var);
        symbolTable.Define("i", "int", SymbolKind.Var);
        symbolTable.Define("value", "int", SymbolKind.Var);

        var actual = generator.CompileLet();

        Project11TestSupport.AssertVmEquivalent(
            """
            push local 1
            push local 0
            add
            push local 2
            pop temp 0
            pop pointer 1
            push temp 0
            pop that 0
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileReturn_ReturnsExpectedVm_ForIdentifier()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("return x; }");
        GetSymbolTable(generator).Define("x", "int", SymbolKind.Var);

        var actual = generator.CompileReturn();

        Project11TestSupport.AssertVmEquivalent(
            """
            push local 0
            return
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileIf_EmitsBranchingVm_ForIfElseStatement()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            if (x) {
                let y = 1;
            } else {
                let y = 2;
            }
            return
            """);
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "boolean", SymbolKind.Var);
        symbolTable.Define("y", "int", SymbolKind.Var);

        var actual = generator.CompileIf();
        var lines = Project11TestSupport.NormalizeVm(actual);

        Assert.Contains(lines, line => line.StartsWith("label IF_TRUE", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("label IF_FALSE", StringComparison.Ordinal));
        Assert.Contains(lines, line => line == "push local 0");
        Assert.Contains(lines, line => line.StartsWith("if-goto ", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("goto ", StringComparison.Ordinal));
        Assert.True(lines.Count(line => line.StartsWith("label ", StringComparison.Ordinal)) >= 2);
        Assert.Contains(lines, line => line == "push constant 1");
        Assert.Contains(lines, line => line == "push constant 2");
        Assert.Equal(2, lines.Count(line => line == "pop local 1"));
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileWhile_EmitsLoopingVm_ForSimpleCondition()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(
            """
            while (x) {
                let x = 0;
            }
            return
            """);
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "boolean", SymbolKind.Var);

        var actual = generator.CompileWhile();
        var lines = Project11TestSupport.NormalizeVm(actual);

        Assert.True(lines.Count(line => line.StartsWith("label ", StringComparison.Ordinal)) >= 2);
        Assert.Contains(lines, line => line.StartsWith("label WHILE_EXP", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("label WHILE_END", StringComparison.Ordinal));
        Assert.Contains(lines, line => line == "push local 0");
        Assert.Contains(lines, line => line.StartsWith("if-goto ", StringComparison.Ordinal));
        Assert.Contains(lines, line => line == "push constant 0");
        Assert.Contains(lines, line => line == "pop local 0");
        Assert.Contains(lines, line => line.StartsWith("goto ", StringComparison.Ordinal));
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Theory]
    [MemberData(nameof(CompileExpressionCases))]
    public void CompileExpression_ReturnsExpectedVm_ForEachBinaryOperation(
        string source,
        string expectedVm)
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);
        var symbolTable = GetSymbolTable(generator);
        symbolTable.Define("x", "int", SymbolKind.Var);
        symbolTable.Define("y", "int", SymbolKind.Var);

        var actual = generator.CompileExpression();

        Project11TestSupport.AssertVmEquivalent(expectedVm, actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, ";");
    }

    [Fact]
    public void CompileExpressionList_ReturnsExpectedVm_ForMultipleExpressions()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("1, x + 2, true)");
        GetSymbolTable(generator).Define("x", "int", SymbolKind.Var);

        var actual = generator.CompileExpressionList();

        Project11TestSupport.AssertVmEquivalent(
            """
            push constant 1
            push local 0
            push constant 2
            add
            push constant 0
            not
            """,
            actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, ")");
    }

    [Fact]
    public void CompileExpressionList_ReturnsEmptyVm_ForEmptyList()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(")");

        var actual = generator.CompileExpressionList();

        Project11TestSupport.AssertVmEquivalent(string.Empty, actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, ")");
    }

    [Theory]
    [MemberData(nameof(CompileTermCases))]
    public void CompileTerm_ReturnsExpectedVm_ForSupportedTermShapes(
        string source,
        Action<ISymbolTable>? seedSymbols,
        string? currentClassName,
        string expectedVm)
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);
        if (currentClassName is not null)
        {
            SetCurrentClassName(generator, currentClassName);
        }

        seedSymbols?.Invoke(GetSymbolTable(generator));

        var actual = generator.CompileTerm();

        Project11TestSupport.AssertVmEquivalent(expectedVm, actual);
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, ";");
    }

    public static TheoryData<string, string> CompileExpressionCases => new()
    {
        { "x + y ;", "push local 0\npush local 1\nadd" },
        { "x - y ;", "push local 0\npush local 1\nsub" },
        { "x * y ;", "push local 0\npush local 1\ncall Math.multiply 2" },
        { "x / y ;", "push local 0\npush local 1\ncall Math.divide 2" },
        { "x & y ;", "push local 0\npush local 1\nand" },
        { "x | y ;", "push local 0\npush local 1\nor" },
        { "x < y ;", "push local 0\npush local 1\nlt" },
        { "x > y ;", "push local 0\npush local 1\ngt" },
        { "x = y ;", "push local 0\npush local 1\neq" },
    };

    public static TheoryData<string, Action<ISymbolTable>?, string?, string> CompileTermCases => new()
    {
        { "2 ;", null, null, "push constant 2" },
        {
            "\"OK\" ;",
            null,
            null,
            """
            push constant 2
            call String.new 1
            push constant 79
            call String.appendChar 2
            push constant 75
            call String.appendChar 2
            """
        },
        { "true ;", null, null, "push constant 0\nnot" },
        { "false ;", null, null, "push constant 0" },
        { "null ;", null, null, "push constant 0" },
        { "this ;", null, null, "push pointer 0" },
        {
            "x ;",
            symbols => symbols.Define("x", "int", SymbolKind.Var),
            null,
            "push local 0"
        },
        {
            "arr[i] ;",
            symbols =>
            {
                symbols.Define("arr", "Array", SymbolKind.Var);
                symbols.Define("i", "int", SymbolKind.Var);
            },
            null,
            """
            push local 1
            push local 0
            add
            pop pointer 1
            push that 0
            """
        },
        {
            "Math.multiply(2, 3) ;",
            null,
            null,
            """
            push constant 2
            push constant 3
            call Math.multiply 2
            """
        },
        {
            "move(1, 2) ;",
            null,
            "Point",
            """
            push pointer 0
            push constant 1
            push constant 2
            call Point.move 3
            """
        },
        {
            "-x ;",
            symbols => symbols.Define("x", "int", SymbolKind.Var),
            null,
            """
            push local 0
            neg
            """
        },
        {
            "~x ;",
            symbols => symbols.Define("x", "boolean", SymbolKind.Var),
            null,
            """
            push local 0
            not
            """
        },
        {
            "(x + 1) ;",
            symbols => symbols.Define("x", "int", SymbolKind.Var),
            null,
            """
            push local 0
            push constant 1
            add
            """
        },
    };

    private static ISymbolTable GetSymbolTable(ICodeGenerator generator)
    {
        var field = generator.GetType().GetField("_symbolTable", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsAssignableFrom<ISymbolTable>(field!.GetValue(generator));
    }

    private static void SetCurrentClassName(ICodeGenerator generator, string className)
    {
        var field = generator.GetType().GetField("_currentClassName", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field!.SetValue(generator, className);
    }
}

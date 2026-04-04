using System.Reflection;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using SyntaxAnalyser.Services;

namespace SyntaxAnalyser.Tests;

public class CodeGeneratorFoundationsTests
{
    [Fact]
    public void VmWriter_ExposesBookVmCommandApi()
    {
        AssertMethodSignature(typeof(VmWriter), "WritePush", parameterCount: 2);
        AssertMethodSignature(typeof(VmWriter), "WritePop", parameterCount: 2);
        AssertMethodSignature(typeof(VmWriter), "WriteArithmetic", parameterCount: 1);
        AssertMethodSignature(typeof(VmWriter), "WriteLabel", parameterCount: 1);
        AssertMethodSignature(typeof(VmWriter), "WriteGoto", parameterCount: 1);
        AssertMethodSignature(typeof(VmWriter), "WriteIf", parameterCount: 1);
        AssertMethodSignature(typeof(VmWriter), "WriteCall", parameterCount: 2);
        AssertMethodSignature(typeof(VmWriter), "WriteFunction", parameterCount: 2);
        AssertMethodSignature(typeof(VmWriter), "WriteReturn", parameterCount: 0);
    }

    [Fact]
    public void SymbolKind_DefinesArgument()
    {
        Assert.Contains("Argument", Enum.GetNames<SymbolKind>());
    }

    [Fact]
    public void SymbolTable_ExposesBookScopeOperations()
    {
        AssertMethodSignature(typeof(SymbolTable), "StartSubroutine", parameterCount: 0);
        AssertMethodSignature(typeof(SymbolTable), "Define", parameterCount: 3);
        AssertMethodSignature(typeof(SymbolTable), "VarCount", parameterCount: 1);
        AssertMethodSignature(typeof(SymbolTable), "KindOf", parameterCount: 1);
        AssertMethodSignature(typeof(SymbolTable), "TypeOf", parameterCount: 1);
        AssertMethodSignature(typeof(SymbolTable), "IndexOf", parameterCount: 1);
    }

    [Fact]
    public void SymbolTable_AllowsSubroutineScopeToShadowClassScope()
    {
        var table = new SymbolTable();
        table.Define("size", "int", SymbolKind.Field);
        table.StartSubroutine();
        table.Define("size", "boolean", SymbolKind.Var);

        Assert.Equal(SymbolKind.Var, table.KindOf("size"));
        Assert.Equal("boolean", table.TypeOf("size"));
        Assert.Equal(0, table.IndexOf("size"));
    }

    [Fact]
    public void CompileClassVarDec_RegistersEveryField_AndConsumesWholeDeclaration()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("field int x, y; function");

        generator.CompileClassVarDec();

        var symbols = GetSymbolTable(generator);

        Assert.Collection(
            symbols,
            symbol =>
            {
                Assert.Equal("x", symbol.Name);
                Assert.Equal("int", symbol.Type);
                Assert.Equal(SymbolKind.Field, symbol.Kind);
                Assert.Equal(0, symbol.Index);
            },
            symbol =>
            {
                Assert.Equal("y", symbol.Name);
                Assert.Equal("int", symbol.Type);
                Assert.Equal(SymbolKind.Field, symbol.Kind);
                Assert.Equal(1, symbol.Index);
            });

        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "function");
    }

    [Fact]
    public void CompileVarDec_RegistersEveryLocal_AndConsumesWholeDeclaration()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("var int x, y; return");

        generator.CompileVarDec();

        var symbols = GetSymbolTable(generator);

        Assert.Collection(
            symbols,
            symbol =>
            {
                Assert.Equal("x", symbol.Name);
                Assert.Equal("int", symbol.Type);
                Assert.Equal(SymbolKind.Var, symbol.Kind);
                Assert.Equal(0, symbol.Index);
            },
            symbol =>
            {
                Assert.Equal("y", symbol.Name);
                Assert.Equal("int", symbol.Type);
                Assert.Equal(SymbolKind.Var, symbol.Kind);
                Assert.Equal(1, symbol.Index);
            });

        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void SymbolTable_StartSubroutine_ClearsOnlyArgumentAndVarSymbols()
    {
        var table = new SymbolTable();
        table.Define("screenWidth", "int", SymbolKind.Static);
        table.Define("x", "int", SymbolKind.Field);
        table.Define("dx", "int", SymbolKind.Argument);
        table.Define("sum", "int", SymbolKind.Var);

        table.StartSubroutine();

        Assert.Equal(1, table.VarCount(SymbolKind.Static));
        Assert.Equal(1, table.VarCount(SymbolKind.Field));
        Assert.Equal(0, table.VarCount(SymbolKind.Argument));
        Assert.Equal(0, table.VarCount(SymbolKind.Var));
        Assert.Equal(SymbolKind.Static, table.KindOf("screenWidth"));
        Assert.Equal(SymbolKind.Field, table.KindOf("x"));
        Assert.Null(table.KindOf("dx"));
        Assert.Null(table.KindOf("sum"));
    }

    [Fact]
    public void SymbolTable_Define_AssignsSequentialIndexes_PerKind()
    {
        var table = new SymbolTable();

        table.Define("x", "int", SymbolKind.Field);
        table.Define("y", "int", SymbolKind.Field);
        table.Define("arg0", "int", SymbolKind.Argument);
        table.Define("arg1", "boolean", SymbolKind.Argument);
        table.Define("local0", "int", SymbolKind.Var);
        table.Define("local1", "char", SymbolKind.Var);

        Assert.Equal(0, table.IndexOf("x"));
        Assert.Equal(1, table.IndexOf("y"));
        Assert.Equal(0, table.IndexOf("arg0"));
        Assert.Equal(1, table.IndexOf("arg1"));
        Assert.Equal(0, table.IndexOf("local0"));
        Assert.Equal(1, table.IndexOf("local1"));
    }

    [Fact]
    public void SymbolTable_Define_ThrowsForDuplicateNameInSameScope()
    {
        var table = new SymbolTable();
        table.Define("x", "int", SymbolKind.Field);

        Assert.ThrowsAny<Exception>(() => table.Define("x", "boolean", SymbolKind.Field));
    }

    [Theory]
    [MemberData(nameof(VmWriterCommandCases))]
    public void VmWriter_CommandMethod_EmitsExpectedVm(
        string methodName,
        object?[] arguments,
        string expectedVm)
    {
        var writer = new VmWriter();
        var method = FindVmWriterMethod(methodName, arguments.Length);

        Assert.NotNull(method);

        method!.Invoke(writer, arguments);

        Project11TestSupport.AssertVmEquivalent(expectedVm, writer.ToString());
    }

    [Fact]
    public void CompileParameterList_DefinesArguments_AndStopsBeforeClosingParenthesis()
    {
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator("int dx, boolean enabled)");

        var actual = generator.CompileParameterList();
        var symbols = GetSymbolTable(generator);

        Project11TestSupport.AssertVmEquivalent(string.Empty, actual);
        Assert.Collection(
            symbols,
            symbol =>
            {
                Assert.Equal("dx", symbol.Name);
                Assert.Equal("int", symbol.Type);
                Assert.Equal(SymbolKind.Argument, symbol.Kind);
                Assert.Equal(0, symbol.Index);
            },
            symbol =>
            {
                Assert.Equal("enabled", symbol.Name);
                Assert.Equal("boolean", symbol.Type);
                Assert.Equal(SymbolKind.Argument, symbol.Kind);
                Assert.Equal(1, symbol.Index);
            });
        Project11TestSupport.AssertCurrentToken(tokenizer, TokenType.Symbol, ")");
    }

    [Fact]
    public void CompileClass_EmitsFunctionHeader_WithLocalCount_AndVoidReturnSequence()
    {
        const string source = """
                              class Main {
                                  function void main() {
                                      var int x, y;
                                      var boolean ready;
                                      return;
                                  }
                              }
                              """;
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);

        var actual = generator.CompileClass();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Main.main 3
            push constant 0
            return
            """,
            actual);
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void CompileClass_EmitsMethodThisSetup_AndMapsParametersToArgumentSegment()
    {
        const string source = """
                              class Math {
                                  method int identity(int value) {
                                      return value;
                                  }
                              }
                              """;
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);

        var actual = generator.CompileClass();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Math.identity 0
            push argument 0
            pop pointer 0
            push argument 1
            return
            """,
            actual);
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void CompileClass_EmitsConstructorAllocation_BasedOnFieldCount()
    {
        const string source = """
                              class Point {
                                  field int x, y;

                                  constructor Point new() {
                                      return this;
                                  }
                              }
                              """;
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);

        var actual = generator.CompileClass();

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
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void CompileClass_ReturnStatementWithoutExpression_EmitsPushZeroThenReturn()
    {
        const string source = """
                              class Main {
                                  function void noop() {
                                      return;
                                  }
                              }
                              """;
        var (generator, _) = Project11TestSupport.CreateCodeGenerator(source);

        var actual = generator.CompileClass();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Main.noop 0
            push constant 0
            return
            """,
            actual);
    }

    [Fact]
    public void CompileClass_ReturnThis_EmitsPushPointerZeroThenReturn()
    {
        const string source = """
                              class Main {
                                  field int x;

                                  constructor Main new() {
                                      return this;
                                  }
                              }
                              """;
        var (generator, _) = Project11TestSupport.CreateCodeGenerator(source);

        var actual = generator.CompileClass();

        Project11TestSupport.AssertVmEquivalent(
            """
            function Main.new 0
            push constant 1
            call Memory.alloc 1
            pop pointer 0
            push pointer 0
            return
            """,
            actual);
    }

    public static TheoryData<string, object?[], string> VmWriterCommandCases => new()
    {
        { "WritePush", [ "constant", 7 ], "push constant 7" },
        { "WritePop", [ "local", 2 ], "pop local 2" },
        { "WriteArithmetic", [ "add" ], "add" },
        { "WriteLabel", [ "LOOP_START" ], "label LOOP_START" },
        { "WriteGoto", [ "LOOP_START" ], "goto LOOP_START" },
        { "WriteIf", [ "BRANCH_TRUE" ], "if-goto BRANCH_TRUE" },
        { "WriteCall", [ "Math.multiply", 2 ], "call Math.multiply 2" },
        { "WriteFunction", [ "Main.main", 3 ], "function Main.main 3" },
        { "WriteReturn", [], "return" },
    };

    private static SymbolTable GetSymbolTable(ICodeGenerator generator)
    {
        var field = generator.GetType().GetField("_symbolTable", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        var value = field.GetValue(generator);

        return Assert.IsType<SymbolTable>(value);
    }

    private static MethodInfo? FindVmWriterMethod(string name, int parameterCount)
    {
        return typeof(VmWriter)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method =>
                method.DeclaringType == typeof(VmWriter) &&
                method.Name == name &&
                method.GetParameters().Length == parameterCount);
    }

    private static void AssertMethodSignature(Type type, string name, int parameterCount)
    {
        var method = FindMethod(type, name, parameterCount);

        Assert.True(method != null, $"{type.Name} should define a public instance method named {name}.");
        Assert.Equal(parameterCount, method!.GetParameters().Length);
    }

    private static MethodInfo? FindMethod(Type type, string name, int parameterCount)
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method =>
                method.DeclaringType == type &&
                method.Name == name &&
                method.GetParameters().Length == parameterCount);
    }
}

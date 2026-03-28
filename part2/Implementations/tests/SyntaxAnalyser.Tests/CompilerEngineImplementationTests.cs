using System.Xml.Linq;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using SyntaxAnalyser.Services;

namespace SyntaxAnalyser.Tests;

public class CompilerEngineImplementationTests
{
    [Fact]
    public void CompileClass_ReturnExpectedXml_AndConsumesCompleteClass()
    {
        const string source = """
                              class Main {
                                  function void main() {
                                      return;
                                  }
                              }
                              """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileClass();

        AssertXmlEquivalent(
            """
            <class>
              <keyword> class </keyword>
              <identifier> Main </identifier>
              <symbol> { </symbol>
              <subroutineDec>
                <keyword> function </keyword>
                <keyword> void </keyword>
                <identifier> main </identifier>
                <symbol> ( </symbol>
                <parameterList>
                </parameterList>
                <symbol> ) </symbol>
                <subroutineBody>
                  <symbol> { </symbol>
                  <statements>
                    <returnStatement>
                      <keyword> return </keyword>
                      <symbol> ; </symbol>
                    </returnStatement>
                  </statements>
                  <symbol> } </symbol>
                </subroutineBody>
              </subroutineDec>
              <symbol> } </symbol>
            </class>
            """,
            actual);
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void CompileClass_ReturnMultipleClassVarDecBlocks_ForMultipleFieldAndStaticDeclarations()
    {
        const string source = """
                              class Point {
                                  field int x, y;
                                  static int count;
                                  field boolean visible;
                              }
                              """;
        var (engine, _) = CreateCompilerEngine(source);

        var actual = engine.CompileClass();

        AssertXmlEquivalent(
            """
            <class>
              <keyword> class </keyword>
              <identifier> Point </identifier>
              <symbol> { </symbol>
              <classVarDec>
                <keyword> field </keyword>
                <keyword> int </keyword>
                <identifier> x </identifier>
                <symbol> , </symbol>
                <identifier> y </identifier>
                <symbol> ; </symbol>
              </classVarDec>
              <classVarDec>
                <keyword> static </keyword>
                <keyword> int </keyword>
                <identifier> count </identifier>
                <symbol> ; </symbol>
              </classVarDec>
              <classVarDec>
                <keyword> field </keyword>
                <keyword> boolean </keyword>
                <identifier> visible </identifier>
                <symbol> ; </symbol>
              </classVarDec>
              <symbol> } </symbol>
            </class>
            """,
            actual);

        var classElement = XElement.Parse(actual);
        var classVarDecCount = classElement.Elements("classVarDec").Count();

        Assert.Equal(3, classVarDecCount);
    }

    [Fact]
    public void CompileClassVarDec_ReturnExpectedXml_AndStopsAtNextClassElement()
    {
        var (engine, tokenizer) = CreateCompilerEngine("field int x, y; function");

        var actual = engine.CompileClassVarDec();

        AssertXmlEquivalent(
            """
            <classVarDec>
              <keyword> field </keyword>
              <keyword> int </keyword>
              <identifier> x </identifier>
              <symbol> , </symbol>
              <identifier> y </identifier>
              <symbol> ; </symbol>
            </classVarDec>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "function");
    }

    [Fact]
    public void CompileSubroutine_ReturnExpectedXml_AndStopsAtClassClosingBrace()
    {
        const string source = """
                              method void move(int dx, int dy) {
                                  var int x;
                                  return;
                              }
                              }
                              """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileSubroutine();

        AssertXmlEquivalent(
            """
            <subroutineDec>
              <keyword> method </keyword>
              <keyword> void </keyword>
              <identifier> move </identifier>
              <symbol> ( </symbol>
              <parameterList>
                <keyword> int </keyword>
                <identifier> dx </identifier>
                <symbol> , </symbol>
                <keyword> int </keyword>
                <identifier> dy </identifier>
              </parameterList>
              <symbol> ) </symbol>
              <subroutineBody>
                <symbol> { </symbol>
                <varDec>
                  <keyword> var </keyword>
                  <keyword> int </keyword>
                  <identifier> x </identifier>
                  <symbol> ; </symbol>
                </varDec>
                <statements>
                  <returnStatement>
                    <keyword> return </keyword>
                    <symbol> ; </symbol>
                  </returnStatement>
                </statements>
                <symbol> } </symbol>
              </subroutineBody>
            </subroutineDec>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileSubroutine_ThrowsForInvalidReturnTypeKeyword()
    {
        var (engine, _) = CreateCompilerEngine("function return main() { return; } }");

        Assert.ThrowsAny<Exception>(() => engine.CompileSubroutine());
    }

    [Fact]
    public void CompileParameterList_ReturnExpectedXml_AndStopsBeforeClosingParenthesis()
    {
        var (engine, tokenizer) = CreateCompilerEngine("int dx, boolean ready )");

        var actual = engine.CompileParameterList();

        AssertXmlEquivalent(
            """
            <parameterList>
              <keyword> int </keyword>
              <identifier> dx </identifier>
              <symbol> , </symbol>
              <keyword> boolean </keyword>
              <identifier> ready </identifier>
            </parameterList>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, ")");
    }

    [Fact]
    public void CompileParameterList_ThrowsForInvalidTypeKeyword()
    {
        var (engine, _) = CreateCompilerEngine("return value )");

        Assert.ThrowsAny<Exception>(() => engine.CompileParameterList());
    }

    [Fact]
    public void CompileVarDec_ReturnExpectedXml_AndStopsAtNextStatement()
    {
        var (engine, tokenizer) = CreateCompilerEngine("var Square game, next; let");

        var actual = engine.CompileVarDec();

        AssertXmlEquivalent(
            """
            <varDec>
              <keyword> var </keyword>
              <identifier> Square </identifier>
              <identifier> game </identifier>
              <symbol> , </symbol>
              <identifier> next </identifier>
              <symbol> ; </symbol>
            </varDec>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "let");
    }

    [Fact]
    public void CompileVarDec_ThrowsForInvalidTypeKeyword()
    {
        var (engine, _) = CreateCompilerEngine("var return value;");

        Assert.ThrowsAny<Exception>(() => engine.CompileVarDec());
    }

    [Fact]
    public void CompileStatements_ReturnExpectedXml_AndStopsBeforeClosingBrace()
    {
        var source =
            """
            let x = 1;
            do Output.printInt(x);
            return;
            }
            """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileStatements();

        AssertXmlEquivalent(
            """
            <statements>
              <letStatement>
                <keyword> let </keyword>
                <identifier> x </identifier>
                <symbol> = </symbol>
                <expression>
                  <term>
                    <integerConstant> 1 </integerConstant>
                  </term>
                </expression>
                <symbol> ; </symbol>
              </letStatement>
              <doStatement>
                <keyword> do </keyword>
                <identifier> Output </identifier>
                <symbol> . </symbol>
                <identifier> printInt </identifier>
                <symbol> ( </symbol>
                <expressionList>
                  <expression>
                    <term>
                      <identifier> x </identifier>
                    </term>
                  </expression>
                </expressionList>
                <symbol> ) </symbol>
                <symbol> ; </symbol>
              </doStatement>
              <returnStatement>
                <keyword> return </keyword>
                <symbol> ; </symbol>
              </returnStatement>
            </statements>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileDo_ReturnExpectedXml_AndStopsAtNextStatement()
    {
        var (engine, tokenizer) = CreateCompilerEngine("do Output.printInt(x); return");

        var actual = engine.CompileDo();

        AssertXmlEquivalent(
            """
            <doStatement>
              <keyword> do </keyword>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> printInt </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
              <symbol> ; </symbol>
            </doStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileDo_ReturnExpectedXml_AndSupportsMultipleArguments()
    {
        var (engine, tokenizer) = CreateCompilerEngine("do Output.draw(x, y + 1); return");

        var actual = engine.CompileDo();

        AssertXmlEquivalent(
            """
            <doStatement>
              <keyword> do </keyword>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> draw </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
                <symbol> , </symbol>
                <expression>
                  <term>
                    <identifier> y </identifier>
                  </term>
                  <symbol> + </symbol>
                  <term>
                    <integerConstant> 1 </integerConstant>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
              <symbol> ; </symbol>
            </doStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_AndSupportsArrayAssignment()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let arr[i] = value; return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> arr </identifier>
              <symbol> [ </symbol>
              <expression>
                <term>
                  <identifier> i </identifier>
                </term>
              </expression>
              <symbol> ] </symbol>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> value </identifier>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForBinaryAssignmentExpression()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let size = size + 2; return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> size </identifier>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> size </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <integerConstant> 2 </integerConstant>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForSubtractionAssignmentExpression()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let size = size - 2; return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> size </identifier>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> size </identifier>
                </term>
                <symbol> - </symbol>
                <term>
                  <integerConstant> 2 </integerConstant>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileWhile_ReturnExpectedXml_AndStopsAtNextStatement()
    {
        var source =
            """
            while (x < 10) {
                let x = x + 1;
            }
            return
            """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileWhile();

        AssertXmlEquivalent(
            """
            <whileStatement>
              <keyword> while </keyword>
              <symbol> ( </symbol>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
                <symbol> &lt; </symbol>
                <term>
                  <integerConstant> 10 </integerConstant>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <letStatement>
                  <keyword> let </keyword>
                  <identifier> x </identifier>
                  <symbol> = </symbol>
                  <expression>
                    <term>
                      <identifier> x </identifier>
                    </term>
                    <symbol> + </symbol>
                    <term>
                      <integerConstant> 1 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ; </symbol>
                </letStatement>
              </statements>
              <symbol> } </symbol>
            </whileStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileReturn_ReturnExpectedXml_AndStopsAtNextToken()
    {
        var (engine, tokenizer) = CreateCompilerEngine("return x; }");

        var actual = engine.CompileReturn();

        AssertXmlEquivalent(
            """
            <returnStatement>
              <keyword> return </keyword>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
              </expression>
              <symbol> ; </symbol>
            </returnStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    [Fact]
    public void CompileIf_ReturnExpectedXml_AndSupportsElseBranch()
    {
        var source =
            """
            if (x) {
                let x = 1;
            } else {
                let x = 2;
            }
            return
            """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileIf();

        AssertXmlEquivalent(
            """
            <ifStatement>
              <keyword> if </keyword>
              <symbol> ( </symbol>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <letStatement>
                  <keyword> let </keyword>
                  <identifier> x </identifier>
                  <symbol> = </symbol>
                  <expression>
                    <term>
                      <integerConstant> 1 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ; </symbol>
                </letStatement>
              </statements>
              <symbol> } </symbol>
              <keyword> else </keyword>
              <symbol> { </symbol>
              <statements>
                <letStatement>
                  <keyword> let </keyword>
                  <identifier> x </identifier>
                  <symbol> = </symbol>
                  <expression>
                    <term>
                      <integerConstant> 2 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ; </symbol>
                </letStatement>
              </statements>
              <symbol> } </symbol>
            </ifStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileIf_ReturnExpectedXml_WithoutElseBranch()
    {
        var source =
            """
            if (x) {
                return;
            }
            return
            """;
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileIf();

        AssertXmlEquivalent(
            """
            <ifStatement>
              <keyword> if </keyword>
              <symbol> ( </symbol>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <returnStatement>
                  <keyword> return </keyword>
                  <symbol> ; </symbol>
                </returnStatement>
              </statements>
              <symbol> } </symbol>
            </ifStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Theory]
    [MemberData(nameof(CompileExpressionCases))]
    public void CompileExpression_ReturnExpectedXml_ForEachBinaryOperation(
        string operation,
        string source,
        string expectedXml)
    {
        Assert.False(string.IsNullOrWhiteSpace(operation));
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileExpression();

        AssertXmlEquivalent(expectedXml, actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, ")");
    }

    public static TheoryData<string, string, string> CompileExpressionCases => new()
    {
        {
            "addition",
            "x + y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> + </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "subtraction",
            "x - y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> - </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "multiplication",
            "x * y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> * </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "division",
            "x / y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> / </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "and",
            "x & y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> &amp; </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "or",
            "x | y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> | </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "less than",
            "x < y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> &lt; </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "greater than",
            "x > y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> &gt; </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "equals",
            "x = y )",
            """
            <expression>
              <term>
                <identifier> x </identifier>
              </term>
              <symbol> = </symbol>
              <term>
                <identifier> y </identifier>
              </term>
            </expression>
            """
        },
        {
            "neg",
            "-x )",
            """
            <expression>
                <term>
                    <symbol> - </symbol>
                    <term>
                        <identifier> x </identifier>
                    </term>
                </term>
            </expression>
            """
        }
    };

    [Theory]
    [MemberData(nameof(CompileExpressionListCases))]
    public void CompileExpressionList_ReturnExpectedXml_ForSupportedListShapes(
        string scenario,
        string source,
        string expectedXml,
        TokenType nextTokenType,
        string nextTokenValue)
    {
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileExpressionList();

        AssertXmlEquivalent(expectedXml, actual);
        AssertCurrentToken(tokenizer, nextTokenType, nextTokenValue);
    }

    public static TheoryData<string, string, string, TokenType, string> CompileExpressionListCases => new()
    {
        {
            "empty list",
            ")",
            """
            <expressionList>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "single identifier expression",
            "x )",
            """
            <expressionList>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "single binary expression",
            "x + 1 )",
            """
            <expressionList>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "multiple identifier expressions",
            "x, y )",
            """
            <expressionList>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
              </expression>
              <symbol> , </symbol>
              <expression>
                <term>
                  <identifier> y </identifier>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "mixed arithmetic and array expressions",
            "x + 1, arr[i] )",
            """
            <expressionList>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
              <symbol> , </symbol>
              <expression>
                <term>
                  <identifier> arr </identifier>
                  <symbol> [ </symbol>
                  <expression>
                    <term>
                      <identifier> i </identifier>
                    </term>
                  </expression>
                  <symbol> ] </symbol>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "mixed literal and subroutine call expressions",
            "\"done\", Output.printInt(x) )",
            """
            <expressionList>
              <expression>
                <term>
                  <stringConstant> done </stringConstant>
                </term>
              </expression>
              <symbol> , </symbol>
              <expression>
                <term>
                  <identifier> Output </identifier>
                  <symbol> . </symbol>
                  <identifier> printInt </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                    <expression>
                      <term>
                        <identifier> x </identifier>
                      </term>
                    </expression>
                  </expressionList>
                  <symbol> ) </symbol>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
        {
            "keyword constant and parenthesized expression",
            "true, (x + 1) )",
            """
            <expressionList>
              <expression>
                <term>
                  <keyword> true </keyword>
                </term>
              </expression>
              <symbol> , </symbol>
              <expression>
                <term>
                  <symbol> ( </symbol>
                  <expression>
                    <term>
                      <identifier> x </identifier>
                    </term>
                    <symbol> + </symbol>
                    <term>
                      <integerConstant> 1 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ) </symbol>
                </term>
              </expression>
            </expressionList>
            """,
            TokenType.Symbol,
            ")"
        },
    };

    [Theory]
    [MemberData(nameof(CompileTermCases))]
    public void CompileTerm_ReturnExpectedXml_ForAllJackTermShapes(
        string source,
        string expectedXml,
        TokenType nextTokenType,
        string nextTokenValue)
    {
        var (engine, tokenizer) = CreateCompilerEngine(source);

        var actual = engine.CompileTerm();

        AssertXmlEquivalent(expectedXml, actual);
        AssertCurrentToken(tokenizer, nextTokenType, nextTokenValue);
    }

    [Fact]
    public void CompileTerm_StopsBeforeBinaryOperator()
    {
        var (engine, tokenizer) = CreateCompilerEngine("x - y");

        var actual = engine.CompileTerm();

        AssertXmlEquivalent(
            """
            <term>
              <identifier> x </identifier>
            </term>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, "-");
    }

    [Fact]
    public void CompileTerm_EscapesXmlSensitiveCharactersInStringConstant()
    {
        var (engine, tokenizer) = CreateCompilerEngine("\"a < b & c > d\" ;");

        var actual = engine.CompileTerm();

        AssertXmlEquivalent(
            """
            <term>
              <stringConstant> a &lt; b &amp; c &gt; d </stringConstant>
            </term>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, ";");
    }

    public static TheoryData<string, string, TokenType, string> CompileTermCases => new()
    {
        {
            "123 ;",
            """
            <term>
              <integerConstant> 123 </integerConstant>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "\"hello\" ;",
            """
            <term>
              <stringConstant> hello </stringConstant>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "true ;",
            """
            <term>
              <keyword> true </keyword>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "false ;",
            """
            <term>
              <keyword> false </keyword>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "null ;",
            """
            <term>
              <keyword> null </keyword>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "this ;",
            """
            <term>
              <keyword> this </keyword>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "value ;",
            """
            <term>
              <identifier> value </identifier>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "arr[i] ;",
            """
            <term>
              <identifier> arr </identifier>
              <symbol> [ </symbol>
              <expression>
                <term>
                  <identifier> i </identifier>
                </term>
              </expression>
              <symbol> ] </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "arr[i + 1] ;",
            """
            <term>
              <identifier> arr </identifier>
              <symbol> [ </symbol>
              <expression>
                <term>
                  <identifier> i </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
              <symbol> ] </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "move() ;",
            """
            <term>
              <identifier> move </identifier>
              <symbol> ( </symbol>
              <expressionList>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "move(x) ;",
            """
            <term>
              <identifier> move </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "move(x, y + 1) ;",
            """
            <term>
              <identifier> move </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
                <symbol> , </symbol>
                <expression>
                  <term>
                    <identifier> y </identifier>
                  </term>
                  <symbol> + </symbol>
                  <term>
                    <integerConstant> 1 </integerConstant>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "Output.printInt() ;",
            """
            <term>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> printInt </identifier>
              <symbol> ( </symbol>
              <expressionList>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "Output.printInt(x) ;",
            """
            <term>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> printInt </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "Output.printInt(x, y + 1) ;",
            """
            <term>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> printInt </identifier>
              <symbol> ( </symbol>
              <expressionList>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                </expression>
                <symbol> , </symbol>
                <expression>
                  <term>
                    <identifier> y </identifier>
                  </term>
                  <symbol> + </symbol>
                  <term>
                    <integerConstant> 1 </integerConstant>
                  </term>
                </expression>
              </expressionList>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "(x < y) ;",
            """
            <term>
              <symbol> ( </symbol>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
                <symbol> &lt; </symbol>
                <term>
                  <identifier> y </identifier>
                </term>
              </expression>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "(x + 1) ;",
            """
            <term>
              <symbol> ( </symbol>
              <expression>
                <term>
                  <identifier> x </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
              <symbol> ) </symbol>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        // {
        //     "-x ;",
        //     """
        //     <expression>
        //         <symbol> - </symbol>
        //         <term>
        //             <integerConstant> 2 </integerConstant>
        //         </term>
        //                       
        //     </expression>
        //     """,
        //     TokenType.Symbol,
        //     ";"
        // },
        {
            "~x ;",
            """
            <term>
              <symbol> ~ </symbol>
              <term>
                <identifier> x </identifier>
              </term>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
        {
            "~(x + 1) ;",
            """
            <term>
              <symbol> ~ </symbol>
              <term>
                <symbol> ( </symbol>
                <expression>
                  <term>
                    <identifier> x </identifier>
                  </term>
                  <symbol> + </symbol>
                  <term>
                    <integerConstant> 1 </integerConstant>
                  </term>
                </expression>
                <symbol> ) </symbol>
              </term>
            </term>
            """,
            TokenType.Symbol,
            ";"
        },
    };

    private static (ICompilerEngine Engine, ITokenizer Tokenizer) CreateCompilerEngine(string source)
    {
        ITokenizer tokenizer = new Tokenizer(source);
        var implementationTypes = typeof(ICompilerEngine).Assembly
            .GetTypes()
            .Where(type =>
                typeof(ICompilerEngine).IsAssignableFrom(type) &&
                !type.IsInterface &&
                !type.IsAbstract)
            .ToArray();

        Assert.True(
            implementationTypes.Length == 1,
            "Add one concrete CompilationEngine implementation that implements ICompilerEngine.");

        var constructor = implementationTypes[0].GetConstructor([typeof(ITokenizer)]);
        Assert.NotNull(constructor);

        var engine = constructor.Invoke([tokenizer]) as ICompilerEngine;

        Assert.NotNull(engine);
        return (engine, tokenizer);
    }

    private static void AssertCurrentToken(ITokenizer tokenizer, TokenType expectedType, string expectedValue)
    {
        Assert.True(tokenizer.HasMoreTokens);
        Assert.Equal(new Token(expectedType, expectedValue), tokenizer.CurrentToken);
    }

    private static void AssertXmlEquivalent(string expectedXml, string actualXml)
    {
        var expected = XElement.Parse(expectedXml);
        var actual = XElement.Parse(actualXml);

        AssertElementEquivalent(expected, actual);
    }

    private static void AssertElementEquivalent(XElement expected, XElement actual)
    {
        Assert.Equal(expected.Name.LocalName, actual.Name.LocalName);

        var expectedChildren = expected.Elements().ToArray();
        var actualChildren = actual.Elements().ToArray();

        Assert.Equal(expectedChildren.Length, actualChildren.Length);

        if (expectedChildren.Length == 0)
        {
            Assert.Equal(expected.Value.Trim(), actual.Value.Trim());
            return;
        }

        for (var i = 0; i < expectedChildren.Length; i++)
        {
            AssertElementEquivalent(expectedChildren[i], actualChildren[i]);
        }
    }
}

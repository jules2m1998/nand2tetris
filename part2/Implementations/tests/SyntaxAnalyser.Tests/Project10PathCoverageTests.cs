using System.Xml.Linq;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using SyntaxAnalyser.Services;

namespace SyntaxAnalyser.Tests;

public class Project10PathCoverageTests
{
    [Fact]
    public void CompileDo_ReturnExpectedXml_ForProject10_UnqualifiedCallWithoutArguments()
    {
        var (engine, tokenizer) = CreateCompilerEngine("do draw(); return");

        var actual = engine.CompileDo();

        AssertXmlEquivalent(
            """
            <doStatement>
              <keyword> do </keyword>
              <identifier> draw </identifier>
              <symbol> ( </symbol>
              <expressionList>
              </expressionList>
              <symbol> ) </symbol>
              <symbol> ; </symbol>
            </doStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileDo_ReturnExpectedXml_ForProject10_QualifiedCallWithoutArguments()
    {
        var (engine, tokenizer) = CreateCompilerEngine("do Output.println(); return");

        var actual = engine.CompileDo();

        AssertXmlEquivalent(
            """
            <doStatement>
              <keyword> do </keyword>
              <identifier> Output </identifier>
              <symbol> . </symbol>
              <identifier> println </identifier>
              <symbol> ( </symbol>
              <expressionList>
              </expressionList>
              <symbol> ) </symbol>
              <symbol> ; </symbol>
            </doStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForProject10_AssignmentFromQualifiedCallWithoutArguments()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let game = SquareGame.new(); return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> game </identifier>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> SquareGame </identifier>
                  <symbol> . </symbol>
                  <identifier> new </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForProject10_AssignmentFromQualifiedCallWithArgument()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let a = Array.new(length); return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> a </identifier>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> Array </identifier>
                  <symbol> . </symbol>
                  <identifier> new </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                    <expression>
                      <term>
                        <identifier> length </identifier>
                      </term>
                    </expression>
                  </expressionList>
                  <symbol> ) </symbol>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForProject10_ArrayAssignmentWithArrayAccessValue()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let a[1] = a[2]; return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> a </identifier>
              <symbol> [ </symbol>
              <expression>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
              <symbol> ] </symbol>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> a </identifier>
                  <symbol> [ </symbol>
                  <expression>
                    <term>
                      <integerConstant> 2 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ] </symbol>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileLet_ReturnExpectedXml_ForProject10_AdditionWithArrayAccess()
    {
        var (engine, tokenizer) = CreateCompilerEngine("let sum = sum + a[i]; return");

        var actual = engine.CompileLet();

        AssertXmlEquivalent(
            """
            <letStatement>
              <keyword> let </keyword>
              <identifier> sum </identifier>
              <symbol> = </symbol>
              <expression>
                <term>
                  <identifier> sum </identifier>
                </term>
                <symbol> + </symbol>
                <term>
                  <identifier> a </identifier>
                  <symbol> [ </symbol>
                  <expression>
                    <term>
                      <identifier> i </identifier>
                    </term>
                  </expression>
                  <symbol> ] </symbol>
                </term>
              </expression>
              <symbol> ; </symbol>
            </letStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileIf_ReturnExpectedXml_ForProject10_EqualityCondition()
    {
        var source =
            """
            if (direction = 1) {
                do square.moveUp();
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
                  <identifier> direction </identifier>
                </term>
                <symbol> = </symbol>
                <term>
                  <integerConstant> 1 </integerConstant>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <doStatement>
                  <keyword> do </keyword>
                  <identifier> square </identifier>
                  <symbol> . </symbol>
                  <identifier> moveUp </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                  <symbol> ; </symbol>
                </doStatement>
              </statements>
              <symbol> } </symbol>
            </ifStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileIf_ReturnExpectedXml_ForProject10_AndConditionWithNestedComparisons()
    {
        var source =
            """
            if (((y + size) < 254) & ((x + size) < 510)) {
                do erase();
                let size = size + 2;
                do draw();
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
                  <symbol> ( </symbol>
                  <expression>
                    <term>
                      <symbol> ( </symbol>
                      <expression>
                        <term>
                          <identifier> y </identifier>
                        </term>
                        <symbol> + </symbol>
                        <term>
                          <identifier> size </identifier>
                        </term>
                      </expression>
                      <symbol> ) </symbol>
                    </term>
                    <symbol> &lt; </symbol>
                    <term>
                      <integerConstant> 254 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ) </symbol>
                </term>
                <symbol> &amp; </symbol>
                <term>
                  <symbol> ( </symbol>
                  <expression>
                    <term>
                      <symbol> ( </symbol>
                      <expression>
                        <term>
                          <identifier> x </identifier>
                        </term>
                        <symbol> + </symbol>
                        <term>
                          <identifier> size </identifier>
                        </term>
                      </expression>
                      <symbol> ) </symbol>
                    </term>
                    <symbol> &lt; </symbol>
                    <term>
                      <integerConstant> 510 </integerConstant>
                    </term>
                  </expression>
                  <symbol> ) </symbol>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <doStatement>
                  <keyword> do </keyword>
                  <identifier> erase </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                  <symbol> ; </symbol>
                </doStatement>
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
                <doStatement>
                  <keyword> do </keyword>
                  <identifier> draw </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                  <symbol> ; </symbol>
                </doStatement>
              </statements>
              <symbol> } </symbol>
            </ifStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileWhile_ReturnExpectedXml_ForProject10_UnaryNotIdentifierCondition()
    {
        var source =
            """
            while (~exit) {
                do moveSquare();
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
                  <symbol> ~ </symbol>
                  <term>
                    <identifier> exit </identifier>
                  </term>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <doStatement>
                  <keyword> do </keyword>
                  <identifier> moveSquare </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                  <symbol> ; </symbol>
                </doStatement>
              </statements>
              <symbol> } </symbol>
            </whileStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileWhile_ReturnExpectedXml_ForProject10_UnaryNotParenthesizedEqualityCondition()
    {
        var source =
            """
            while (~(key = 0)) {
                let key = Keyboard.keyPressed();
                do moveSquare();
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
                  <symbol> ~ </symbol>
                  <term>
                    <symbol> ( </symbol>
                    <expression>
                      <term>
                        <identifier> key </identifier>
                      </term>
                      <symbol> = </symbol>
                      <term>
                        <integerConstant> 0 </integerConstant>
                      </term>
                    </expression>
                    <symbol> ) </symbol>
                  </term>
                </term>
              </expression>
              <symbol> ) </symbol>
              <symbol> { </symbol>
              <statements>
                <letStatement>
                  <keyword> let </keyword>
                  <identifier> key </identifier>
                  <symbol> = </symbol>
                  <expression>
                    <term>
                      <identifier> Keyboard </identifier>
                      <symbol> . </symbol>
                      <identifier> keyPressed </identifier>
                      <symbol> ( </symbol>
                      <expressionList>
                      </expressionList>
                      <symbol> ) </symbol>
                    </term>
                  </expression>
                  <symbol> ; </symbol>
                </letStatement>
                <doStatement>
                  <keyword> do </keyword>
                  <identifier> moveSquare </identifier>
                  <symbol> ( </symbol>
                  <expressionList>
                  </expressionList>
                  <symbol> ) </symbol>
                  <symbol> ; </symbol>
                </doStatement>
              </statements>
              <symbol> } </symbol>
            </whileStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Keyword, "return");
    }

    [Fact]
    public void CompileReturn_ReturnExpectedXml_ForProject10_KeywordConstantThis()
    {
        var (engine, tokenizer) = CreateCompilerEngine("return this; }");

        var actual = engine.CompileReturn();

        AssertXmlEquivalent(
            """
            <returnStatement>
              <keyword> return </keyword>
              <expression>
                <term>
                  <keyword> this </keyword>
                </term>
              </expression>
              <symbol> ; </symbol>
            </returnStatement>
            """,
            actual);
        AssertCurrentToken(tokenizer, TokenType.Symbol, "}");
    }

    private static (ICompilerEngine Engine, ITokenizer Tokenizer) CreateCompilerEngine(string source)
    {
        ITokenizer tokenizer = new Tokenizer(source);
        var engine = new CompilerEngine(tokenizer);
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

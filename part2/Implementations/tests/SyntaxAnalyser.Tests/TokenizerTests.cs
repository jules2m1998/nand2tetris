using SyntaxAnalyser.Absttractions;
using SyntaxAnalyser.Models;
using SyntaxAnalyser.Services;
namespace SyntaxAnalyser.Tests;

public class TokenizerTests
{
    [Theory]
    [MemberData(nameof(IntegerConstant))]
    public void Tokenize_ReturnExpectedIntegerConstantToken_ForAnIntegerString(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }
    
    [Theory]
    [MemberData(nameof(StringConstant))]
    public void Tokenize_ReturnExpectedStringConstantToken_ForAString(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }
    
    [Theory]
    [MemberData(nameof(Identifier))]
    public void Tokenize_ReturnExpectedIdentifierToken_ForAnIdentifier(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }
    
    [Theory]
    [MemberData(nameof(Symbol))]
    public void Tokenize_ReturnExpectedSymbolToken_ForASymbol(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }
    
    [Theory]
    [MemberData(nameof(Keyword))]
    public void Tokenize_ReturnExpectedKeywordToken_ForAKeyword(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }

    [Theory]
    [InlineData("className")]
    [InlineData("returnValue")]
    [InlineData("trueCount")]
    public void Tokenize_ReturnIdentifier_WhenIdentifierStartsWithAKeyword(string code)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(new Token(TokenType.Identifier, code), token);
    }
    
    [Theory]
    [MemberData(nameof(Comments))]
    public void Tokenize_ReturnExpectedCommentToken_ForAComment(string code, Token expectedToken)
    {
        var tokenizer = new Tokenizer(code);
        var token = tokenizer.CurrentToken;
        Assert.Equal(expectedToken, token);
    }

    [Theory]
    [InlineData("32768")]
    [InlineData("999999999")]
    public void Tokenize_ThrowInvalidOperationException_WhenIntegerConstantExceedsJackRange(string code)
    {
        var tokenizer = new Tokenizer(code);
        Assert.Throws<InvalidOperationException>(() => _ = tokenizer.CurrentToken);
    }

    [Fact]
    public void Tokenize_ThrowInvalidOperationException_WhenIdentifierContainsNonAsciiCharacters()
    {
        var tokenizer = new Tokenizer("naïve");
        Assert.Throws<InvalidOperationException>(() => _ = tokenizer.CurrentToken);
    }

    [Fact]
    public void Tokenize_ThrowInvalidOperationException_WhenStringContainsANewLine()
    {
        var tokenizer = new Tokenizer(
            """
            "hello
            world"
            """);

        Assert.Throws<InvalidOperationException>(() => _ = tokenizer.CurrentToken);
    }

    [Theory]
    [InlineData("   let")]
    [InlineData("/* comment */ let")]
    [InlineData("// comment\nlet")]
    public void HasMoreTokens_ReturnTrue_WhenATokenExistsAfterIgnoredContent(string code)
    {
        ITokenizer tokenizer = new Tokenizer(code);

        Assert.True(tokenizer.HasMoreTokens);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/* comment */   ")]
    [InlineData("// comment")]
    public void HasMoreTokens_ReturnFalse_WhenInputContainsOnlyIgnoredContent(string code)
    {
        ITokenizer tokenizer = new Tokenizer(code);

        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void Advance_ReturnTokensInOrder_AndSkipsIgnoredContent()
    {
        ITokenizer tokenizer = new Tokenizer("  let /* comment */ count = 10; // trailing");

        var tokens = new[]
        {
            tokenizer.Advance(),
            tokenizer.Advance(),
            tokenizer.Advance(),
            tokenizer.Advance(),
            tokenizer.Advance(),
        };

        Assert.Equal(
            [
                new Token(TokenType.Keyword, "let"),
                new Token(TokenType.Identifier, "count"),
                new Token(TokenType.Symbol, "="),
                new Token(TokenType.IntegerConstant, "10"),
                new Token(TokenType.Symbol, ";"),
            ],
            tokens);
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Fact]
    public void CurrentToken_ReturnsSameToken_UntilAdvanceIsCalled()
    {
        ITokenizer tokenizer = new Tokenizer("let count;");

        Assert.Equal(new Token(TokenType.Keyword, "let"), tokenizer.CurrentToken);
        Assert.Equal(new Token(TokenType.Keyword, "let"), tokenizer.CurrentToken);

        tokenizer.Advance();

        Assert.Equal(new Token(TokenType.Identifier, "count"), tokenizer.CurrentToken);
    }

    [Fact]
    public void Eat_ReturnMatchingToken_AndAdvanceToNextToken()
    {
        ITokenizer tokenizer = new Tokenizer("let count;");

        var token = tokenizer.Eat("let");

        Assert.Equal(new Token(TokenType.Keyword, "let"), token);
        Assert.Equal(new Token(TokenType.Identifier, "count"), tokenizer.CurrentToken);
    }

    [Fact]
    public void Eat_ThrowInvalidOperationException_WhenCurrentTokenDoesNotMatch()
    {
        ITokenizer tokenizer = new Tokenizer("return;");

        Assert.Throws<InvalidOperationException>(() => tokenizer.Eat("let"));
    }

    public static TheoryData<string, Token> IntegerConstant => new()
    {
        {
            "42", new Token(TokenType.IntegerConstant, "42")
        },
        {
            "10", new Token(TokenType.IntegerConstant, "10")
        },
        {
            "0", new Token(TokenType.IntegerConstant, "0")
        },
        {
            "32767", new Token(TokenType.IntegerConstant, "32767")
        },
    };

    public static TheoryData<string, Token> StringConstant => new()
    {
        {
            """
            "Hello world"
            """, new Token(TokenType.StringConstant, "\"Hello world\"")
        },
        {
            """
            "Hi"
            """, new Token(TokenType.StringConstant, "\"Hi\"")
        },
        {
            """
            "This a text"
            """, new Token(TokenType.StringConstant, "\"This a text\"")
        },
        {
            """
            "f ebwbkjbwebjwvfbj jw d hwd chsk dcksdc"
            """, new Token(TokenType.StringConstant, "\"f ebwbkjbwebjwvfbj jw d hwd chsk dcksdc\"")
        },
    };

    public static TheoryData<string, Token> Identifier => new()
    {
        {
            "name", new Token(TokenType.Identifier, "name")
        },
        {
            "age", new Token(TokenType.Identifier, "age")
        },
        {
            "size", new Token(TokenType.Identifier, "size")
        },
        {
            "_name", new Token(TokenType.Identifier, "_name")
        },
        {
            "_age", new Token(TokenType.Identifier, "_age")
        },
        {
            "one_age", new Token(TokenType.Identifier, "one_age")
        },
    };

    public static TheoryData<string, Token> Symbol => new()
    {
        {
            "{", new Token(TokenType.Symbol, "{")
        },
        {
            "}", new Token(TokenType.Symbol, "}")
        },
        {
            "(", new Token(TokenType.Symbol, "(")
        },
        {
            ")", new Token(TokenType.Symbol, ")")
        },
        {
            "[", new Token(TokenType.Symbol, "[")
        },
        {
            "]", new Token(TokenType.Symbol, "]")
        },
        {
            ".", new Token(TokenType.Symbol, ".")
        },
        {
            ",", new Token(TokenType.Symbol, ",")
        },
        {
            ";", new Token(TokenType.Symbol, ";")
        },
        {
            "+", new Token(TokenType.Symbol, "+")
        },
        {
            "-", new Token(TokenType.Symbol, "-")
        },
        {
            "*", new Token(TokenType.Symbol, "*")
        },
        {
            "/", new Token(TokenType.Symbol, "/")
        },
        {
            "&", new Token(TokenType.Symbol, "&")
        },
        {
            "|", new Token(TokenType.Symbol, "|")
        },
        {
            "<", new Token(TokenType.Symbol, "<")
        },
        {
            ">", new Token(TokenType.Symbol, ">")
        },
        {
            "=", new Token(TokenType.Symbol, "=")
        },
        {
            "~", new Token(TokenType.Symbol, "~")
        },
    };

    public static TheoryData<string, Token> Keyword => new()
    {
        {
            "class", new Token(TokenType.Keyword, "class")
        },
        {
            "constructor", new Token(TokenType.Keyword, "constructor")
        },
        {
            "function", new Token(TokenType.Keyword, "function")
        },
        {
            "method", new Token(TokenType.Keyword, "method")
        },
        {
            "field", new Token(TokenType.Keyword, "field")
        },
        {
            "static", new Token(TokenType.Keyword, "static")
        },
        {
            "var", new Token(TokenType.Keyword, "var")
        },
        {
            "int", new Token(TokenType.Keyword, "int")
        },
        {
            "char", new Token(TokenType.Keyword, "char")
        },
        {
            "boolean", new Token(TokenType.Keyword, "boolean")
        },
        {
            "void", new Token(TokenType.Keyword, "void")
        },
        {
            "true", new Token(TokenType.Keyword, "true")
        },
        {
            "false", new Token(TokenType.Keyword, "false")
        },
        {
            "null", new Token(TokenType.Keyword, "null")
        },
        {
            "this", new Token(TokenType.Keyword, "this")
        },
        {
            "let", new Token(TokenType.Keyword, "let")
        },
        {
            "do", new Token(TokenType.Keyword, "do")
        },
        {
            "if", new Token(TokenType.Keyword, "if")
        },
        {
            "else", new Token(TokenType.Keyword, "else")
        },
        {
            "while", new Token(TokenType.Keyword, "while")
        },
        {
            "return", new Token(TokenType.Keyword, "return")
        },
    };

    public static TheoryData<string, Token> Comments => new()
    {
        {"// comment\n name", new Token(TokenType.Identifier, "name")},
        {"/* comment\n other line*/ 10", new Token(TokenType.IntegerConstant, "10")},
        {"/* comment\\n other line\n*/ class", new Token(TokenType.Keyword, "class")},
        {"/* comment\\n other line*/ {", new Token(TokenType.Symbol, "{")},
        {"/* comment\\n other line*/\"test\"", new Token(TokenType.StringConstant, "\"test\"")}
    };

}

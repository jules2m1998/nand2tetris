using System.Xml.Linq;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using SyntaxAnalyser.Services;

namespace SyntaxAnalyser.Tests;

public class Project10ReferenceIntegrationTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../SyntaxAnalyser.Tests/Fixtures/Project10"));

    [Theory]
    [MemberData(nameof(TokenizerReferenceCases))]
    public void Tokenizer_MatchesOfficialProject10TokenXml(
        string scenario,
        string jackPath,
        string tokenXmlPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        Assert.True(File.Exists(jackPath), $"Missing Jack fixture: {jackPath}");
        Assert.True(File.Exists(tokenXmlPath), $"Missing token XML fixture: {tokenXmlPath}");

        var tokenizer = new Tokenizer(File.ReadAllText(jackPath));

        var actualXml = SerializeTokens(tokenizer);
        var expectedXml = File.ReadAllText(tokenXmlPath);

        AssertXmlEquivalent(expectedXml, actualXml);
        Assert.False(tokenizer.HasMoreTokens);
    }

    [Theory]
    [MemberData(nameof(CompilerEngineReferenceCases))]
    public void CompileClass_MatchesOfficialProject10ParseXml(
        string scenario,
        string jackPath,
        string parseXmlPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        Assert.True(File.Exists(jackPath), $"Missing Jack fixture: {jackPath}");
        Assert.True(File.Exists(parseXmlPath), $"Missing parse XML fixture: {parseXmlPath}");

        var tokenizer = new Tokenizer(File.ReadAllText(jackPath));
        var engine = new CompilerEngine(tokenizer);

        var actualXml = engine.CompileClass();
        var expectedXml = File.ReadAllText(parseXmlPath);

        AssertXmlEquivalent(expectedXml, actualXml);
        Assert.False(tokenizer.HasMoreTokens);
    }

    public static TheoryData<string, string, string> TokenizerReferenceCases => new()
    {
        { "Square/Main tokens", FixturePath("Square", "Main.jack"), FixturePath("Square", "MainT.xml") },
        { "Square/Square tokens", FixturePath("Square", "Square.jack"), FixturePath("Square", "SquareT.xml") },
        { "Square/SquareGame tokens", FixturePath("Square", "SquareGame.jack"), FixturePath("Square", "SquareGameT.xml") },
        { "ExpressionLessSquare/Main tokens", FixturePath("ExpressionLessSquare", "Main.jack"), FixturePath("ExpressionLessSquare", "MainT.xml") },
        { "ExpressionLessSquare/Square tokens", FixturePath("ExpressionLessSquare", "Square.jack"), FixturePath("ExpressionLessSquare", "SquareT.xml") },
        { "ExpressionLessSquare/SquareGame tokens", FixturePath("ExpressionLessSquare", "SquareGame.jack"), FixturePath("ExpressionLessSquare", "SquareGameT.xml") },
        { "ArrayTest/Main tokens", FixturePath("ArrayTest", "Main.jack"), FixturePath("ArrayTest", "MainT.xml") },
    };

    public static TheoryData<string, string, string> CompilerEngineReferenceCases => new()
    {
        { "Square/Main parse", FixturePath("Square", "Main.jack"), FixturePath("Square", "Main.xml") },
        { "Square/Square parse", FixturePath("Square", "Square.jack"), FixturePath("Square", "Square.xml") },
        { "Square/SquareGame parse", FixturePath("Square", "SquareGame.jack"), FixturePath("Square", "SquareGame.xml") },
        { "ExpressionLessSquare/Main parse", FixturePath("ExpressionLessSquare", "Main.jack"), FixturePath("ExpressionLessSquare", "Main.xml") },
        { "ExpressionLessSquare/Square parse", FixturePath("ExpressionLessSquare", "Square.jack"), FixturePath("ExpressionLessSquare", "Square.xml") },
        { "ExpressionLessSquare/SquareGame parse", FixturePath("ExpressionLessSquare", "SquareGame.jack"), FixturePath("ExpressionLessSquare", "SquareGame.xml") },
        { "ArrayTest/Main parse", FixturePath("ArrayTest", "Main.jack"), FixturePath("ArrayTest", "Main.xml") },
    };

    private static string FixturePath(string project, string fileName) => Path.Combine(FixtureRoot, project, fileName);

    private static string SerializeTokens(ITokenizer tokenizer)
    {
        var root = new XElement("tokens");

        while (tokenizer.HasMoreTokens)
        {
            var token = tokenizer.CurrentToken;
            var value = token.Type == TokenType.StringConstant
                ? tokenizer.StringValue
                : token.Value;

            root.Add(new XElement(GetTagName(token.Type), $" {value} "));
            tokenizer.Advance();
        }

        return root.ToString();
    }

    private static string GetTagName(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Keyword => "keyword",
            TokenType.Identifier => "identifier",
            TokenType.Symbol => "symbol",
            TokenType.IntegerConstant => "integerConstant",
            TokenType.StringConstant => "stringConstant",
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
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

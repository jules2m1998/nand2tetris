using SyntaxAnalyser.Absttractions;
using SyntaxAnalyser.Models;
using System.Text.RegularExpressions;
namespace SyntaxAnalyser.Services;

public partial class Tokenizer(string code) : ITokenizer
{
    private int _index = 0;

    private readonly IDictionary<Regex, TokenType?> _tokenMap = new Dictionary<Regex, TokenType?>
    {
        [OnelineCommentRegex()] = null,
        [MultilinesCommentRegex()] = null,
        [WhiteSpaceRegex()] = null,
        [KeywordRegex()] = TokenType.Keyword,
        [SymbolRegex()] = TokenType.Symbol,
        [IntegerConstantRegex()] = TokenType.IntegerConstant,
        [StringConstantRegex()] = TokenType.StringConstant,
        [IdentifierRegex()] = TokenType.Identifier,
    };

    public Token CurrentToken
    {
        get
        {
            foreach (var (key, value) in _tokenMap)
            {
                var current = code[_index..];
                var match = key.Match(current);
                if (!match.Success)
                    continue;
                var token = match.Value;
                if (value != null)
                    return new Token(value.Value, token);
                _index += token.Length;
                return CurrentToken;
            }

            throw new InvalidOperationException($"Invalid token {code[_index..]}");
        }
    }

    [GeneratedRegex(@"^\d+")]
    private static partial Regex IntegerConstantRegex();

    [GeneratedRegex(@"^(?!\d)\w+")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex("""
                    ^"[^"]*"
                    """)]
    private static partial Regex StringConstantRegex();

    [GeneratedRegex(@"^(?:[{}()\[\].,;+\-*&|<>=~]|/(?![/*]))")]
    private static partial Regex SymbolRegex();

    [GeneratedRegex("^(class|constructor|function|method|field|static|var|int|char|boolean|void|true|false|null|this|let|do|if|else|while|return)")]
    private static partial Regex KeywordRegex();

    [GeneratedRegex(@"^//[^\r\n]*")]
    private static partial Regex OnelineCommentRegex();

    [GeneratedRegex(@"^/\*[\s\S]*?\*/\s*")]
    private static partial Regex MultilinesCommentRegex();

    [GeneratedRegex(@"^\s+")]
    private static partial Regex WhiteSpaceRegex();
}

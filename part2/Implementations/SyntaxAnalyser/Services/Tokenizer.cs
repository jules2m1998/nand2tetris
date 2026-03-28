using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using System.Text.RegularExpressions;
namespace SyntaxAnalyser.Services;

public partial class Tokenizer(string code) : ITokenizer
{
    private int _index;

    private readonly Regex[] _ignoredRules =
    [
        OnelineCommentRegex(),
        MultilinesCommentRegex(),
        WhiteSpaceRegex(),
    ];

    private readonly (Regex Regex, TokenType Type)[] _tokenRules =
    [
        (KeywordRegex(), TokenType.Keyword),
        (SymbolRegex(), TokenType.Symbol),
        (IntegerConstantRegex(), TokenType.IntegerConstant),
        (StringConstantRegex(), TokenType.StringConstant),
        (IdentifierRegex(), TokenType.Identifier),
    ];

    public Token CurrentToken
    {
        get
        {
            _index = SkipIgnored(_index);

            foreach (var (regex, type) in _tokenRules)
            {
                var current = code[_index..];
                var match = regex.Match(current);
                if (!match.Success)
                    continue;

                var token = match.Value;
                ValidateToken(type, token);
                return new Token(type, token);
            }

            var remaining = code[_index..];
            var preview = remaining.Length > 20 ? remaining[..20] : remaining;
            throw new InvalidOperationException($"Invalid token near '{preview}' at index {_index}");
        }
    }
    
    private int SkipIgnored(int index)
    {
        while (index < code.Length)
        {
            var current = code[index..];
            var matchedIgnoredToken = false;

            foreach (var ignoredRule in _ignoredRules)
            {
                var match = ignoredRule.Match(current);
                if (!match.Success)
                    continue;

                index += match.Length;
                matchedIgnoredToken = true;
                break;
            }

            if (!matchedIgnoredToken)
                break;
        }

        return index;
    }

    public bool HasMoreTokens => SkipIgnored(_index) < code.Length;
    
    public Token Advance()
    {
        var token = CurrentToken;
        _index += token.Value.Length;
        return token;
    }
    
    public Token Eat(string token)
    {
        if (HasMoreTokens &&  CurrentToken.Value == token)
        {
            return Advance();
        }
        throw new InvalidOperationException($"Invalid token near '{token}' at index {_index}");
    }
    public TokenType TokenType => CurrentToken.Type;
    public string StringValue => CurrentToken.Type == TokenType.StringConstant ?  CurrentToken.Value.Replace("\"", "") : throw new InvalidOperationException("You can't perform this action on non string value");
    public int IntValue => CurrentToken.Type == TokenType.IntegerConstant ?  int.Parse(CurrentToken.Value) : throw new InvalidOperationException("You can't perform this action on non int value");
    public string KeyWord => CurrentToken.Type == TokenType.Keyword ?  CurrentToken.Value : throw new InvalidOperationException("You can't perform this action on non keyword value");
    public char Symbol => CurrentToken.Type == TokenType.Symbol ? CurrentToken.Value.ElementAt(0) : throw new InvalidOperationException("You can't perform this action on symbol value");
    public string Identifier => CurrentToken.Type == TokenType.Identifier ? CurrentToken.Value : throw new InvalidOperationException("You can't perform this action on non identifier");

    [GeneratedRegex(@"^\d+\b")]
    private static partial Regex IntegerConstantRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*\b")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex("""
                    ^"[^"\r\n]*"
                    """)]
    private static partial Regex StringConstantRegex();

    [GeneratedRegex(@"^(?:[{}()\[\].,;+\-*&|<>=~]|/(?![/*]))")]
    private static partial Regex SymbolRegex();

    [GeneratedRegex(@"^(class|constructor|function|method|field|static|var|int|char|boolean|void|true|false|null|this|let|do|if|else|while|return)\b")]
    private static partial Regex KeywordRegex();

    [GeneratedRegex(@"^//[^\r\n]*")]
    private static partial Regex OnelineCommentRegex();

    [GeneratedRegex(@"^/\*[\s\S]*?\*/\s*")]
    private static partial Regex MultilinesCommentRegex();

    [GeneratedRegex(@"^\s+")]
    private static partial Regex WhiteSpaceRegex();

    private void ValidateToken(TokenType type, string token)
    {
        if (type != TokenType.IntegerConstant)
            return;

        if (!int.TryParse(token, out var number) || number > 32767)
            throw new InvalidOperationException($"Integer constant out of range near '{token}' at index {_index}");
    }
}

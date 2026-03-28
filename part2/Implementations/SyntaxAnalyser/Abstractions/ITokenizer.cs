using SyntaxAnalyser.Models;
namespace SyntaxAnalyser.Abstractions;

public interface ITokenizer
{
    Token CurrentToken { get; }
    bool HasMoreTokens { get; }
    Token Advance();
    Token Eat(string token);
    TokenType TokenType { get; }
    string KeyWord { get; }
    char Symbol { get; }
    string Identifier { get; }
    int IntValue { get; }
    string StringValue { get; }
}

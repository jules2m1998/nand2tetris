using SyntaxAnalyser.Models;
namespace SyntaxAnalyser.Absttractions;

public interface ITokenizer
{
    Token CurrentToken { get; }
    bool HasMoreTokens { get; }
    Token Advance();
    Token Eat(string token);
}

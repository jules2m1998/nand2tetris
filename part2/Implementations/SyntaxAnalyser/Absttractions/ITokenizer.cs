using SyntaxAnalyser.Models;
namespace SyntaxAnalyser.Absttractions;

public interface ITokenizer
{
    Token CurrentToken { get; }
}

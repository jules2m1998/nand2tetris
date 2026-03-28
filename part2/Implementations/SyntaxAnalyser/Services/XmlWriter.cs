using System.Text;
namespace SyntaxAnalyser.Services;

public class XmlWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void Indent() => _indent++;
    public void Unindent() => _indent--;

    public void AppendLine(string text)
    {
        _sb.Append(new string('\t', _indent));
        _sb.AppendLine(text);
    }
    
    public void Append(string text) => _sb.Append(text);
    public void AppendLines(string text)
    {
        foreach (var line in text.Split("\n"))
        {
            AppendLine(line);
        }
    }

    public override string ToString() => _sb.ToString();
}


using System.Text;

namespace SyntaxAnalyser.Services;

public class VmWriter
{
    private readonly StringBuilder _builder = new();

    public void WritePush(string segment, int index) => AppendCommand($"push {segment} {index}");

    public void WritePop(string segment, int index) => AppendCommand($"pop {segment} {index}");

    public void WriteArithmetic(string command) => AppendCommand(command);

    public void WriteLabel(string label) => AppendCommand($"label {label}");

    public void WriteGoto(string label) => AppendCommand($"goto {label}");

    public void WriteIf(string label) => AppendCommand($"if-goto {label}");

    public void WriteCall(string name, int argCount) => AppendCommand($"call {name} {argCount}");

    public void WriteFunction(string name, int localCount) => AppendCommand($"function {name} {localCount}");

    public void WriteReturn() => AppendCommand("return");

    public void AppendLine(string text) => AppendCommand(text);

    public void AppendLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var line in text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            AppendCommand(line.Trim());
        }
    }

    public override string ToString() => _builder.ToString();

    private void AppendCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _builder.AppendLine(text.Trim());
    }
}

using HackAssembler.Abstractions;
using System.Text.RegularExpressions;
namespace HackAssembler.Implementations.PipelineSteps;

public partial class ProcessLabels : IPipelineStep<string[]>
{

    public int Order => 1;
    public string[] Process(string[] input)
    {
        var inputOut = input;
        var i = 0;
        foreach (var line in input)
        {
            var match = LabelRegex().Match(line);
            if (match.Success)
            {
                var label = match.Groups[1].Value;
                inputOut = RemoveLabel(inputOut, label, i);
            }
            else
            {
                i++;
            }
        }
        return inputOut;
    }

    private static string[] RemoveLabel(string[] inputs, string label, int place)
    {
        List<string> result = [];
        foreach (var line in inputs)
        {
            if (line == $"({label})")
            {
                continue;
            }
            if (line == $"@{label}")
            {
                result.Add($"@{place}");
                continue;
            }
            result.Add(line);
        }
        return result.ToArray();
    }
    
    

    [GeneratedRegex(@"^\((.*)\)")]
    private static partial Regex LabelRegex();
}

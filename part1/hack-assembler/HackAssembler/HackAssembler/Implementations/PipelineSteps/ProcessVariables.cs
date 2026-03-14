using HackAssembler.Abstractions;
namespace HackAssembler.Implementations.PipelineSteps;

public class ProcessVariables : IPipelineStep<string[]>
{

    public int Order => 2;
    public string[] Process(string[] input)
    {
        var knownVariables = new Dictionary<string, int>
        {
            ["SP"] = 0,
            ["LCL"] = 1,
            ["ARG"] = 2,
            ["THIS"] = 3,
            ["THAT"] = 4,
            ["SCREEN"] = 16384,
            ["KBD"] = 24576,
            ["R0"] = 0,
            ["R1"] = 1,
            ["R2"] = 2,
            ["R3"] = 3,
            ["R4"] = 4,
            ["R5"] = 5,
            ["R6"] = 6,
            ["R7"] = 7,
            ["R8"] = 8,
            ["R9"] = 9,
            ["R10"] = 10,
            ["R11"] = 11,
            ["R12"] = 12,
            ["R13"] = 13,
            ["R14"] = 14,
            ["R15"] = 15
        };

        var unknownVariables = new List<string>();
        var replacedVar = new List<string>();
        var inputOut = input;
        foreach (var line in input)
        {
            if (!line.StartsWith('@'))
                continue;
            
            var key = line.Replace("@", "");
            if (int.TryParse(key, out _))
            {
                continue;
            }
            if (replacedVar.Contains(key) || unknownVariables.Contains(key))
            {
                continue;
            }
            
            if (!knownVariables.ContainsKey(key) && !unknownVariables.Contains(key))
            {
                unknownVariables.Add(key);
            }
            else if (knownVariables.TryGetValue(key, out var value) &&  !replacedVar.Contains(key))
            {
                replacedVar.Add(key);
                inputOut = RemoveLabel(inputOut, key, value);
                continue;
            }
            inputOut = RemoveLabel(inputOut, key, unknownVariables.Count + 15);
        }

        return inputOut;
    }
    private static string[] RemoveLabel(string[] inputOut, string label, int place)
    {
        List<string> result = [];
        foreach (var line in inputOut)
        {
            if (line == $"@{label}")
            {
                result.Add($"@{place}");
                continue;
            }
            result.Add(line);
        }
        return result.ToArray();
    }
}

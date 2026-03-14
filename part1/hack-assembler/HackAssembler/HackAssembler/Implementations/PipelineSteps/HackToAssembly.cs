using HackAssembler.Abstractions;
using System.Text.RegularExpressions;
namespace HackAssembler.Implementations.PipelineSteps;

public partial class HackToAssembly : IPipelineStep<string[]>
{

    public int Order => 3;
    public string[] Process(string[] input)
    {
        var result = new List<string>();
        foreach (var line in input)
        {
            if (line.StartsWith('@'))
            {
                result.Add(ProcessAInstruction(line));
                continue;
            }
            result.Add(ProcessCInstruction(line));
        }
        return  result.ToArray();
    }
    private static string ProcessCInstruction(string cInstruction)
    {
        var match = CInstruction().Match(cInstruction);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid instruction: {cInstruction}");
        }
        string[] instruction = ["111", ProcessCompPart(match.Groups["comp"].Value), ProcessDestPart(match.Groups["dest"].Value),  ProcessJumpPart(match.Groups["jump"].Value)];
        return string.Join("", instruction);
    }
    private static string ProcessJumpPart(string value)
    {
        var jumpMap = new Dictionary<string, int>
        {
            [string.Empty] = 0,
            ["JGT"] = 1,
            ["JEQ"] = 2,
            ["JGE"] = 3,
            ["JLT"] = 4,
            ["JNE"] = 5,
            ["JLE"] = 6,
            ["JMP"] = 7,
        };
        return !jumpMap.TryGetValue(value, out var index) ? 
            throw new ArgumentException($"Unknown value for jump: {value}") : 
            Convert.ToString(index, 2).PadLeft(3, '0');
    }
    private static string ProcessCompPart(string value)
    {
        var a = '0';
        var cleanedVal = value.Replace(" ", "");
        if (value.Contains('M'))
        {
            a = '1';
        }
        var compMap = new Dictionary<string, string>
        {
            ["0"]   = "101010",
            ["1"]   = "111111",
            ["-1"]  = "111010",
            ["D"]   = "001100",
            ["A"]   = "110000",
            ["!D"]  = "001101",
            ["!A"]  = "110001",
            ["-D"]  = "001111",
            ["-A"]  = "110011",
            ["D+1"] = "011111",
            ["A+1"] = "110111",
            ["D-1"] = "001110",
            ["A-1"] = "110010",
            ["D+A"] = "000010",
            ["D-A"] = "010011",
            ["A-D"] = "000111",
            ["D&A"] = "000000",
            ["D|A"] = "010101",
            ["M"] = "110000",
            ["!M"] = "110001",
            ["-M"] = "110011",
            ["M+1"] = "110111",
            ["M-1"] = "110010",
            ["D+M"] = "000010",
            ["D-M"] = "010011",
            ["M-D"] = "000111",
            ["D&M"] = "000000",
            ["D|M"] = "010101",
        };
        return !compMap.TryGetValue(cleanedVal, out var compBits)
            ? throw new ArgumentException($"Unknown value for computation: {value}")
            : $"{a}{compBits}";
    }
    private static string ProcessDestPart(string value)
    {
        var destMap = new Dictionary<string, int>()
        {
            [string.Empty] = 0,
            ["M"] = 1,
            ["D"] = 2,
            ["MD"] = 3,
            ["A"] = 4,
            ["AM"] = 5,
            ["AD"] = 6,
            ["AMD"] = 7,
            
        };
        return !destMap.TryGetValue(value, out var destBin) ? 
            throw new ArgumentException($"Unknown value for destination: {value}") : 
            Convert.ToString(destBin, 2).PadLeft(3, '0');
    }
    private static string ProcessAInstruction(string aInstruction)
    {
        if (!int.TryParse(aInstruction.Replace('@', ' ').Trim(), out var value))
            throw new InvalidOperationException($"{aInstruction} is not a number.");
        
        var binary = Convert.ToString(value, 2);
        return binary.Length > 15 ? throw new InvalidOperationException($"{aInstruction} is too long") : binary.PadLeft(16, '0');
    }

    [GeneratedRegex(@"^(?:(?<dest>[^=;]+)=)?(?<comp>[^;]+)(?:;(?<jump>[A-Z]{3}))?$")]
    private static partial Regex CInstruction();
}

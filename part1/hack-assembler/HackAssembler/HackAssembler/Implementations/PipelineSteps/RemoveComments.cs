using HackAssembler.Abstractions;
using System.Text.RegularExpressions;
namespace HackAssembler.Implementations.PipelineSteps;

public partial class RemoveComments : IPipelineStep<string[]>
{

    public int Order => 0;
    public string[] Process(string[] input)
    {
        return input
            .Select(line => CommentRegex().Replace(line, string.Empty).Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    [GeneratedRegex(@"\/\/.*$")]
    private static partial Regex CommentRegex();
}

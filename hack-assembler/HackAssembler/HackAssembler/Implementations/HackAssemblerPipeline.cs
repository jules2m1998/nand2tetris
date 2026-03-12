using HackAssembler.Abstractions;
namespace HackAssembler.Implementations;

public class HackAssemblerPipeline(ICollection<IPipelineStep<string[]>> steps) : IPipeline
{

    public async Task<string> ExecuteAsync(string filePath, string outputDirectory, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        var current = lines;
        var orderedSteps = steps.OrderBy(x => x.Order).ToList();
        current = orderedSteps.Aggregate(current, (current1, step) => step.Process(current1));
        
        var outputFileName = Path.GetFileNameWithoutExtension(filePath) + ".bin";
        var outputFilePath = Path.Combine(outputDirectory, outputFileName);
        var content = string.Join(Environment.NewLine, current);

        await File.WriteAllTextAsync(outputFilePath, content, cancellationToken);
        
        return outputFilePath;
    }
}

namespace HackAssembler.Abstractions;

public interface IPipeline
{
    Task<string> ExecuteAsync(string filePath, string outputDirectory, CancellationToken cancellationToken);
}

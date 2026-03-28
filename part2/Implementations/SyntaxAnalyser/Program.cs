using SyntaxAnalyser.Services;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    if (arguments.Length != 1 || arguments[0] is "--help" or "-h")
    {
        PrintUsage();
        return arguments.Length == 1 ? 0 : 1;
    }

    var inputPath = Path.GetFullPath(arguments[0]);
    var writtenFiles = new List<string>();

    try
    {
        var jackFiles = ResolveInputFiles(inputPath);

        foreach (var jackFile in jackFiles)
        {
            var destinationPath = Path.ChangeExtension(jackFile, ".xml");

            Console.WriteLine($"Analyzing {Path.GetFileName(jackFile)}");

            var source = await File.ReadAllTextAsync(jackFile);
            var engine = new CompilerEngine(new Tokenizer(source));
            var xml = engine.CompileClass();

            await File.WriteAllTextAsync(destinationPath, xml);
            writtenFiles.Add(destinationPath);

            Console.WriteLine($"Created {Path.GetFileName(destinationPath)}");
        }

        Console.WriteLine($"Analysis completed: {jackFiles.Length} file(s).");
        return 0;
    }
    catch (Exception exception)
    {
        foreach (var writtenFile in writtenFiles)
        {
            try
            {
                if (File.Exists(writtenFile))
                {
                    File.Delete(writtenFile);
                }
            }
            catch
            {
                // ignored
            }
        }

        Console.Error.WriteLine($"Analysis failed: {exception.Message}");
        return 1;
    }
}

static string[] ResolveInputFiles(string inputPath)
{
    if (File.Exists(inputPath))
    {
        return !string.Equals(Path.GetExtension(inputPath), ".jack", StringComparison.OrdinalIgnoreCase) ? throw new InvalidOperationException("Input file must have a .jack extension.") : [inputPath];

    }

    if (!Directory.Exists(inputPath))
        throw new InvalidOperationException($"Input path not found: {inputPath}");
    var jackFiles = Directory
        .GetFiles(inputPath, "*.jack", SearchOption.TopDirectoryOnly)
        .OrderBy(Path.GetFileName)
        .ToArray();

    return jackFiles.Length == 0 ? throw new InvalidOperationException("Input directory does not contain any .jack files.") : jackFiles;

}

static void PrintUsage()
{
    Console.WriteLine("syntax-analyser");
    Console.WriteLine("Usage:");
    Console.WriteLine("  syntax-analyser <path-to-jack-file-or-directory>");
    Console.WriteLine("Analyze Jack source and generate sibling XML parse files.");
}

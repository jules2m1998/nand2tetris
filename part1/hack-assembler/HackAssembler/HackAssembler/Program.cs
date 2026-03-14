using HackAssembler;
using HackAssembler.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;

var services = new ServiceCollection();
services.AddLogging();
services.AddPipeline();
await using var serviceProvider = services.BuildServiceProvider();

try
{
    var options = ParseArguments(args);
    if (options.ShowHelp)
    {
        WriteHelp();
        return;
    }

    var inputPath = Path.GetFullPath(options.InputPath!);
    if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
    {
        Console.Error.WriteLine($"Input path not found: {inputPath}");
        Environment.ExitCode = 1;
        return;
    }

    var outputDirectory = options.OutputDirectory is not null
        ? Path.GetFullPath(options.OutputDirectory)
        : ResolveDefaultOutputDirectory(inputPath);

    Directory.CreateDirectory(outputDirectory);

    var app = serviceProvider.GetRequiredService<IPipeline>();
    var inputFiles = ResolveInputFiles(inputPath);
    if (inputFiles.Count == 0)
    {
        Console.Error.WriteLine($"No .asm files found in directory: {inputPath}");
        Environment.ExitCode = 1;
        return;
    }

    var outputPaths = new List<string>(inputFiles.Count);
    var failures = new List<AssemblyFailure>();

    using (var loader = new ConsoleLoader("Preparing assembly..."))
    {
        for (var index = 0; index < inputFiles.Count; index++)
        {
            var inputFile = inputFiles[index];
            loader.Update($"Assembling {index + 1}/{inputFiles.Count}: {Path.GetFileName(inputFile)}");

            try
            {
                outputPaths.Add(await app.ExecuteAsync(inputFile, outputDirectory, CancellationToken.None));
            }
            catch (Exception ex)
            {
                failures.Add(new AssemblyFailure(inputFile, ex));
                if (!options.ContinueOnError)
                {
                    break;
                }
            }
        }
    }

    if (failures.Count == 0)
    {
        WriteSuccess(outputPaths);
        return;
    }

    WriteFailure(outputPaths, failures, options.ContinueOnError);
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Assembly failed.");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("Reason: ");
    Console.ResetColor();
    Console.WriteLine(ex.Message);

    Environment.ExitCode = 1;
}
return;

static CliOptions ParseArguments(string[] args)
{
    if (args.Length == 0)
    {
        return new CliOptions(null, null, false, ShowHelp: true);
    }

    var positionals = new List<string>(capacity: 2);
    var continueOnError = false;

    foreach (var arg in args)
    {
        if (IsHelpCommand(arg))
        {
            return new CliOptions(null, null, continueOnError, ShowHelp: true);
        }

        if (arg == "--continue-on-error")
        {
            continueOnError = true;
            continue;
        }

        if (arg.StartsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown option: {arg}");
        }

        positionals.Add(arg);
    }

    if (positionals.Count == 0)
    {
        return new CliOptions(null, null, continueOnError, ShowHelp: true);
    }

    if (positionals.Count > 2)
    {
        throw new ArgumentException("Too many positional arguments were provided.");
    }

    return new CliOptions(
        positionals[0],
        positionals.Count == 2 ? positionals[1] : null,
        continueOnError,
        ShowHelp: false);
}

static void WriteSuccess(List<string> outputPaths)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(
        outputPaths.Count == 1
            ? "Assembly completed successfully."
            : $"Assembly completed successfully for {outputPaths.Count} files.");
    Console.ResetColor();

    if (outputPaths.Count == 1)
    {
        Console.Write("Output: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(outputPaths[0]);
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("Outputs:");
        foreach (var outputPath in outputPaths)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {outputPath}");
            Console.ResetColor();
        }
    }
}

static void WriteFailure(List<string> outputPaths, List<AssemblyFailure> failures, bool continuedOnError)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(
        continuedOnError && outputPaths.Count > 0
            ? "Assembly completed with errors."
            : "Assembly failed.");
    Console.ResetColor();

    if (outputPaths.Count > 0)
    {
        Console.WriteLine("Successful outputs:");
        foreach (var outputPath in outputPaths)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {outputPath}");
            Console.ResetColor();
        }
    }

    Console.WriteLine("Failures:");
    foreach (var failure in failures)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  File: ");
        Console.ResetColor();
        Console.WriteLine(failure.InputFile);

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Reason: ");
        Console.ResetColor();
        Console.WriteLine(failure.Exception.Message);
    }
}

static bool IsHelpCommand(string value)
{
    return value is "-h" or "--help" or "help";
}

static List<string> ResolveInputFiles(string inputPath)
{
    if (File.Exists(inputPath))
    {
        return [inputPath];
    }

    return Directory
        .EnumerateFiles(inputPath)
        .Where(file => string.Equals(Path.GetExtension(file), ".asm", StringComparison.OrdinalIgnoreCase))
        .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static string ResolveDefaultOutputDirectory(string inputPath)
{
    if (Directory.Exists(inputPath))
    {
        return inputPath;
    }

    return Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
}

static void WriteHelp()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Hack Assembler");
    Console.ResetColor();

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Usage:");
    Console.ResetColor();
    Console.WriteLine("  hack-assembler [--continue-on-error] <path-to-file-or-folder> [output-directory]");

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Arguments:");
    Console.ResetColor();
    Console.WriteLine("  <path-to-file-or-folder>  Path to an input .asm file or a folder containing .asm files.");
    Console.WriteLine("  [output-directory]  Optional output directory. Defaults to the input file directory.");

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Options:");
    Console.ResetColor();
    Console.WriteLine("  --continue-on-error  Continue assembling remaining .asm files after a failure.");

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Help:");
    Console.ResetColor();
    Console.WriteLine("  hack-assembler --help");
    Console.WriteLine("  hack-assembler -h");
    Console.WriteLine("  hack-assembler help");
}

internal sealed record CliOptions(
    string? InputPath,
    string? OutputDirectory,
    bool ContinueOnError,
    bool ShowHelp);

internal sealed record AssemblyFailure(string InputFile, Exception Exception);

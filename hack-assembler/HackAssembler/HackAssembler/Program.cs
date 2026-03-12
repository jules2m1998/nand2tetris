using HackAssembler;
using HackAssembler.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;

var services = new ServiceCollection();
services.AddLogging();
services.AddPipeline();
using var serviceProvider = services.BuildServiceProvider();

if (args.Length > 0 && IsHelpCommand(args[0]) || args.Length == 0)
{
    WriteHelp();
    return;
}

var inputPath = Path.GetFullPath(args[0]);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"File not found: {inputPath}");
    return;
}

var outputDirectory = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();

Directory.CreateDirectory(outputDirectory);

var app = serviceProvider.GetRequiredService<IPipeline>();
try
{
    var outputPath = await app.ExecuteAsync(inputPath, outputDirectory, CancellationToken.None);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assembly completed successfully.");
    Console.ResetColor();

    Console.Write("Output: ");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(outputPath);
    Console.ResetColor();
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

static bool IsHelpCommand(string value)
{
    return value is "-h" or "--help" or "help";
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
    Console.WriteLine("  hack-assembler <path-to-file> [output-directory]");

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Arguments:");
    Console.ResetColor();
    Console.WriteLine("  <path-to-file>      Path to the input .asm file. Supports relative and absolute paths.");
    Console.WriteLine("  [output-directory]  Optional output directory. Defaults to the input file directory.");

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Help:");
    Console.ResetColor();
    Console.WriteLine("  hack-assembler --help");
    Console.WriteLine("  hack-assembler -h");
    Console.WriteLine("  hack-assembler help");
}

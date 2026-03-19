using VMTranslator.Services;

return VmTranslatorProgram.Run(args);

internal static class VmTranslatorProgram
{
    public static int Run(string[] args)
    {
        if (args.Length == 1 && IsHelpCommand(args[0]))
        {
            PrintHelp();
            return 0;
        }

        if (args.Length != 1)
        {
            WriteColoredLine(Console.Error, "Expected exactly one .vm file path.", ConsoleColor.Red);
            PrintHelp();
            return 1;
        }

        var inputPath = Path.GetFullPath(args[0]);
        var translator = new HackVmLineTranslator();

        try
        {
            var plan = CreateTranslationPlan(inputPath);
            return ExecutePlan(plan, translator);
        }
        catch (TranslationSetupException ex)
        {
            WriteColoredLine(Console.Error, ex.Message, ConsoleColor.Red);
            return 1;
        }
    }

    private static TranslationPlan CreateTranslationPlan(string inputPath)
    {
        if (Directory.Exists(inputPath))
        {
            return CreateDirectoryPlan(inputPath);
        }

        if (!File.Exists(inputPath))
        {
            throw new TranslationSetupException($"Source file was not found: {inputPath}");
        }

        if (!UsesVmExtension(inputPath))
        {
            throw new TranslationSetupException("The source file must use the .vm extension.");
        }

        return CreateSingleFilePlan(inputPath);
    }

    private static TranslationPlan CreateDirectoryPlan(string directoryPath)
    {
        var sourceFiles = GetOrderedVmFiles(directoryPath);
        if (sourceFiles.Length == 0)
        {
            throw new TranslationSetupException($"No .vm files were found in: {directoryPath}");
        }

        var directoryName = GetDirectoryName(directoryPath);
        var destinationPath = Path.Combine(directoryPath, $"{directoryName}.asm");

        return new TranslationPlan(
            DisplayName: directoryName,
            DestinationPath: destinationPath,
            SourceFiles: sourceFiles,
            BootstrapMode: BootstrapMode.DirectoryWithSysInit);
    }

    private static TranslationPlan CreateSingleFilePlan(string filePath)
    {
        return new TranslationPlan(
            DisplayName: Path.GetFileName(filePath),
            DestinationPath: Path.ChangeExtension(filePath, ".asm"),
            SourceFiles: [filePath],
            BootstrapMode: BootstrapMode.None);
    }

    private static int ExecutePlan(TranslationPlan plan, HackVmLineTranslator translator)
    {
        using var loader = new ConsoleLoader($"Translating {plan.DisplayName}");

        try
        {
            TranslateSources(plan, translator);
            loader.Complete();
            WriteColoredLine(Console.Out, $"Translation completed: {plan.DestinationPath}", ConsoleColor.Green);
            return 0;
        }
        catch (Exception ex)
        {
            TryDeleteDestination(plan.DestinationPath);
            loader.Complete();
            WriteColoredLine(Console.Error, $"Translation failed: {ex.Message}", ConsoleColor.Red);
            return 1;
        }
    }

    private static void TranslateSources(TranslationPlan plan, HackVmLineTranslator translator)
    {
        using var outputStream = new FileStream(plan.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(outputStream);

        WriteBootstrapIfNeeded(writer, translator, plan.BootstrapMode);

        foreach (var sourcePath in plan.SourceFiles)
        {
            TranslateFile(sourcePath, writer, translator);
        }
    }

    private static void WriteBootstrapIfNeeded(StreamWriter writer, HackVmLineTranslator translator, BootstrapMode bootstrapMode)
    {
        if (bootstrapMode == BootstrapMode.None)
        {
            return;
        }

        // Directory translation must initialize the VM stack before any VM code runs.
        writer.WriteLine("// bootstrap");
        writer.WriteLine("@256");
        writer.WriteLine("D=A");
        writer.WriteLine("@SP");
        writer.WriteLine("M=D");

        if (bootstrapMode != BootstrapMode.DirectoryWithSysInit)
        {
            return;
        }

        foreach (var line in translator.Translate("call Sys.init 0", 0))
        {
            writer.WriteLine(line);
        }
    }

    private static void TranslateFile(string sourcePath, StreamWriter writer, HackVmLineTranslator translator)
    {
        using var inputStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(inputStream);

        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string? rawLine;
        var lineNumber = 1;

        while ((rawLine = reader.ReadLine()) is not null)
        {
            lineNumber++;

            var vmCommand = SanitizeLine(rawLine);
            if (vmCommand is null)
            {
                continue;
            }

            var translatedLines = translator.Translate(vmCommand, lineNumber, fileName);
            foreach (var translatedLine in translatedLines)
            {
                writer.WriteLine(translatedLine);
            }
        }
    }

    private static string[] GetOrderedVmFiles(string directoryPath)
    {
        return [.. Directory
            .GetFiles(directoryPath, "*.vm", SearchOption.TopDirectoryOnly)
            .OrderByDescending(path => string.Equals(Path.GetFileName(path), "Sys.vm", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => Path.GetFileName(path), StringComparer.Ordinal)];
    }

    private static string GetDirectoryName(string directoryPath)
    {
        return Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static bool UsesVmExtension(string path)
    {
        return string.Equals(Path.GetExtension(path), ".vm", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHelpCommand(string arg)
    {
        return arg is "-h" or "--help" or "help";
    }

    private static void PrintHelp()
    {
        WriteColoredLine(Console.Out, "vm-translator", ConsoleColor.Cyan);
        Console.Out.WriteLine("Translate a single Hack VM (.vm) file into Hack assembly (.asm).");
        Console.Out.WriteLine("Supports memory access, arithmetic, comparison, and function commands.");
        Console.Out.WriteLine();
        Console.Out.WriteLine("Usage:");
        Console.Out.WriteLine("  vm-translator <path-to-file.vm>");
        Console.Out.WriteLine("  vm-translator --help");
        Console.Out.WriteLine();
        Console.Out.WriteLine("What It Does:");
        Console.Out.WriteLine("  - Translates one .vm file into a sibling .asm file");
        Console.Out.WriteLine("  - Supports push/pop, arithmetic, comparison, function, call, and return commands");
        Console.Out.WriteLine("  - Emits readable assembly comments for each translated command");
        Console.Out.WriteLine();
        Console.Out.WriteLine("Examples:");
        Console.Out.WriteLine("  vm-translator ./StackArithmetic/SimpleAdd.vm");
        Console.Out.WriteLine("  vm-translator /absolute/path/MemoryAccess/BasicTest.vm");
        Console.Out.WriteLine();
        Console.Out.WriteLine("Behavior:");
        Console.Out.WriteLine("  - Accepts relative or absolute .vm paths");
        Console.Out.WriteLine("  - Creates the destination next to the source with an .asm extension");
        Console.Out.WriteLine("  - Initializes SP to 256 before directory translation");
        Console.Out.WriteLine("  - Streams the source line by line");
        Console.Out.WriteLine("  - Deletes the destination file if translation fails");
    }

    private static string? SanitizeLine(string line)
    {
        var commentStartIndex = line.IndexOf("//", StringComparison.Ordinal);
        var content = commentStartIndex >= 0 ? line[..commentStartIndex] : line;
        var trimmed = content.Trim();

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static void TryDeleteDestination(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
        catch
        {
            // Preserve the original translation error for the caller.
        }
    }

    private static void WriteColoredLine(TextWriter writer, string message, ConsoleColor color)
    {
        if (Console.IsOutputRedirected && ReferenceEquals(writer, Console.Out))
        {
            writer.WriteLine(message);
            return;
        }

        if (Console.IsErrorRedirected && ReferenceEquals(writer, Console.Error))
        {
            writer.WriteLine(message);
            return;
        }

        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        writer.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    private sealed class ConsoleLoader : IDisposable
    {
        private static readonly char[] Frames = ['|', '/', '-', '\\'];
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Task? animationTask;
        private readonly string message;
        private readonly bool enabled;

        public ConsoleLoader(string message)
        {
            this.message = message;
            enabled = !Console.IsOutputRedirected && Environment.UserInteractive;

            if (enabled)
            {
                animationTask = Task.Run(AnimateAsync);
            }
            else
            {
                WriteColoredLine(Console.Out, message, ConsoleColor.DarkCyan);
            }
        }

        public void Complete()
        {
            cancellationTokenSource.Cancel();

            if (animationTask is not null)
            {
                try
                {
                    animationTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (enabled)
            {
                ClearCurrentLine();
            }
        }

        public void Dispose()
        {
            Complete();
            cancellationTokenSource.Dispose();
        }

        private async Task AnimateAsync()
        {
            var frameIndex = 0;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var frame = Frames[frameIndex++ % Frames.Length];
                Console.Write($"\r{frame} {message}");

                try
                {
                    await Task.Delay(120, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static void ClearCurrentLine()
        {
            var width = 80;
            try
            {
                width = Math.Max(Console.WindowWidth - 1, 1);
            }
            catch
            {
            }

            Console.Write("\r" + new string(' ', width) + "\r");
        }
    }

    private sealed record TranslationPlan(
        string DisplayName,
        string DestinationPath,
        IReadOnlyList<string> SourceFiles,
        BootstrapMode BootstrapMode);

    private enum BootstrapMode
    {
        None,
        DirectoryWithSysInit
    }

    private sealed class TranslationSetupException(string message) : Exception(message);
}

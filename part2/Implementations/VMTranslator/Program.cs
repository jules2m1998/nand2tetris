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

        if (Directory.Exists(inputPath))
        {
            var sourceFiles = GetOrderedVmFiles(inputPath);

            if (sourceFiles.Length == 0)
            {
                WriteColoredLine(Console.Error, $"No .vm files were found in: {inputPath}", ConsoleColor.Red);
                return 1;
            }

            var directoryName = Path.GetFileName(inputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var destinationPath = Path.Combine(inputPath, $"{directoryName}.asm");
            using var loader = new ConsoleLoader($"Translating {directoryName}");

            try
            {
                TranslateFiles(sourceFiles, destinationPath, translator, includeBootstrap: true, includeSysInitCall: true);
                loader.Complete();
                WriteColoredLine(Console.Out, $"Translation completed: {destinationPath}", ConsoleColor.Green);
                return 0;
            }
            catch (Exception ex)
            {
                TryDeleteDestination(destinationPath);
                loader.Complete();
                WriteColoredLine(Console.Error, $"Translation failed: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        if (!File.Exists(inputPath))
        {
            WriteColoredLine(Console.Error, $"Source file was not found: {inputPath}", ConsoleColor.Red);
            return 1;
        }

        if (!string.Equals(Path.GetExtension(inputPath), ".vm", StringComparison.OrdinalIgnoreCase))
        {
            WriteColoredLine(Console.Error, "The source file must use the .vm extension.", ConsoleColor.Red);
            return 1;
        }

        var destinationFilePath = Path.ChangeExtension(inputPath, ".asm");
        using var fileLoader = new ConsoleLoader($"Translating {Path.GetFileName(inputPath)}");

        try
        {
            TranslateFiles([inputPath], destinationFilePath, translator, includeBootstrap: false, includeSysInitCall: false);
            fileLoader.Complete();
            WriteColoredLine(Console.Out, $"Translation completed: {destinationFilePath}", ConsoleColor.Green);
            return 0;
        }
        catch (Exception ex)
        {
            TryDeleteDestination(destinationFilePath);
            fileLoader.Complete();
            WriteColoredLine(Console.Error, $"Translation failed: {ex.Message}", ConsoleColor.Red);
            return 1;
        }
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

    private static string[] GetOrderedVmFiles(string directoryPath)
    {
        return Directory
            .GetFiles(directoryPath, "*.vm", SearchOption.TopDirectoryOnly)
            .OrderByDescending(path => string.Equals(Path.GetFileName(path), "Sys.vm", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();
    }

    private static void TranslateFiles(
        IEnumerable<string> sourcePaths,
        string destinationPath,
        HackVmLineTranslator translator,
        bool includeBootstrap,
        bool includeSysInitCall)
    {
        using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(outputStream);

        if (includeBootstrap)
        {
            WriteBootstrap(writer, translator, includeSysInitCall);
        }

        foreach (var sourcePath in sourcePaths)
        {
            TranslateFile(sourcePath, writer, translator);
        }
    }

    private static void WriteBootstrap(StreamWriter writer, HackVmLineTranslator translator, bool includeSysInitCall)
    {
        writer.WriteLine("// bootstrap");
        writer.WriteLine("@256");
        writer.WriteLine("D=A");
        writer.WriteLine("@SP");
        writer.WriteLine("M=D");

        if (!includeSysInitCall)
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

        string? line;
        var lineNumber = 1;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            var sanitizedLine = SanitizeLine(line);
            if (sanitizedLine is null)
            {
                continue;
            }

            var translatedLines = translator.Translate(sanitizedLine, lineNumber, fileName);
            foreach (var translatedLine in translatedLines)
            {
                writer.WriteLine(translatedLine);
            }
        }
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
}

using System.Diagnostics;

namespace VMTranslator.Tests;

public class VmTranslatorProgramIntegrationTests : IDisposable
{
    private readonly List<string> temporaryDirectories = [];

    [Fact]
    public async Task Run_WithHelpCommand_PrintsUsageAndSucceeds()
    {
        var result = await RunProgramAsync(workingDirectory: CreateTemporaryDirectory(), "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("vm-translator", result.StandardOutput);
        Assert.Contains("Usage:", result.StandardOutput);
        Assert.Contains("Translate one .vm file into a sibling .asm file.", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public async Task Run_WithRelativeVmPath_CreatesSiblingAsmFile()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var sourceFileName = "SimpleAdd.vm";
        var sourcePath = Path.Combine(workingDirectory, sourceFileName);
        var destinationPath = Path.Combine(workingDirectory, "SimpleAdd.asm");

        await File.WriteAllTextAsync(
            sourcePath,
            """
            // this line should be ignored
            push constant 7

              push constant 8   // trailing comment
            add
            """);

        var result = await RunProgramAsync(workingDirectory, sourceFileName);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(destinationPath));
        Assert.Contains($"Translating {sourceFileName}", result.StandardOutput);
        Assert.Contains("Translation completed:", result.StandardOutput);
        Assert.Contains(Path.GetFileName(destinationPath), result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);

        var actualLines = await File.ReadAllLinesAsync(destinationPath);
        Assert.Equal(
            new[]
            {
                "// push constant 7",
                "@7",
                "D=A",
                "@SP",
                "A=M",
                "M=D",
                "@SP",
                "M=M+1",
                "// push constant 8",
                "@8",
                "D=A",
                "@SP",
                "A=M",
                "M=D",
                "@SP",
                "M=M+1",
                "// add",
                "@SP",
                "AM=M-1",
                "D=M",
                "A=A-1",
                "M=M+D"
            },
            actualLines);
    }

    [Fact]
    public async Task Run_WithAbsoluteVmPath_WhenTranslationFails_DeletesDestinationFile()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var sourcePath = Path.Combine(workingDirectory, "Invalid.vm");
        var destinationPath = Path.Combine(workingDirectory, "Invalid.asm");

        await File.WriteAllTextAsync(
            sourcePath,
            """
            push constant 1
            pop constant 0
            """);

        await File.WriteAllTextAsync(destinationPath, "stale output");

        var result = await RunProgramAsync(workingDirectory, sourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(destinationPath));
        Assert.Contains("Translating Invalid.vm", result.StandardOutput);
        Assert.Contains("Translation failed:", result.StandardError);
        Assert.Contains("pop constant 0", result.StandardError);
    }

    [Fact]
    public async Task Run_WithThirtyLineVmProgram_TranslatesTheWholeFile()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var sourceFileName = "ThirtyLines.vm";
        var sourcePath = Path.Combine(workingDirectory, sourceFileName);
        var destinationPath = Path.Combine(workingDirectory, "ThirtyLines.asm");

        var commands = new[]
        {
            "push constant 7",
            "push constant 8",
            "add",
            "push constant 10",
            "sub",
            "push constant 0",
            "eq",
            "push constant 3",
            "push constant 1",
            "gt",
            "push constant 2",
            "push constant 5",
            "lt",
            "push local 0",
            "pop argument 2",
            "push this 1",
            "pop that 3",
            "push temp 4",
            "pop temp 2",
            "push pointer 0",
            "pop pointer 1",
            "push static 6",
            "pop static 7",
            "push constant 11",
            "neg",
            "push constant 12",
            "not",
            "push constant 6",
            "push constant 9",
            "and"
        };

        await File.WriteAllTextAsync(sourcePath, string.Join(Environment.NewLine, commands));

        var result = await RunProgramAsync(workingDirectory, sourceFileName);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(destinationPath));
        Assert.Contains($"Translating {sourceFileName}", result.StandardOutput);
        Assert.Contains("Translation completed:", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);

        var actualLines = await File.ReadAllLinesAsync(destinationPath);
        var expectedLines = BuildExpectedProgram(commands);

        Assert.Equal(expectedLines, actualLines);
    }

    public void Dispose()
    {
        foreach (var directory in temporaryDirectories)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private async Task<ProcessResult> RunProgramAsync(string workingDirectory, params string[] arguments)
    {
        var projectPath = GetProjectPath();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            NormalizeNewLines(await standardOutputTask),
            NormalizeNewLines(await standardErrorTask));
    }

    private string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vmtranslator-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        temporaryDirectories.Add(path);
        return path;
    }

    private static string GetProjectPath()
    {
        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../VMTranslator/VMTranslator.csproj"));
    }

    private static string NormalizeNewLines(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private static string[] BuildExpectedProgram(string[] commands)
    {
        var lines = new List<string>();

        for (var i = 0; i < commands.Length; i++)
        {
            lines.AddRange(ExpectedTranslation(commands[i], i + 1));
        }

        return lines.ToArray();
    }

    private static string[] ExpectedTranslation(string command, int lineNumber)
    {
        var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens[0] switch
        {
            "push" => ExpectedPush(tokens[1], int.Parse(tokens[2]), command),
            "pop" => ExpectedPop(tokens[1], int.Parse(tokens[2]), command),
            "add" => Binary(command, "+"),
            "sub" => Binary(command, "-"),
            "or" => Binary(command, "|"),
            "and" => Binary(command, "&"),
            "neg" => Unary(command, "M=-M"),
            "not" => Unary(command, "M=!M"),
            "eq" => Compare(command, "JEQ", lineNumber),
            "gt" => Compare(command, "JGT", lineNumber),
            "lt" => Compare(command, "JLT", lineNumber),
            _ => throw new InvalidOperationException($"Unsupported test command: {command}")
        };
    }

    private static string[] ExpectedPush(string segment, int index, string command)
    {
        return segment switch
        {
            "constant" =>
            [
                $"// {command}",
                $"@{index}",
                "D=A",
                .. PushFromD()
            ],
            "local" => PushFromSegment(command, "@LCL", index),
            "argument" => PushFromSegment(command, "@ARG", index),
            "this" => PushFromSegment(command, "@THIS", index),
            "that" => PushFromSegment(command, "@THAT", index),
            "temp" =>
            [
                $"// {command}",
                $"@{index}",
                "D=A",
                "@5",
                "A=D+A",
                "D=M",
                .. PushFromD()
            ],
            "pointer" =>
            [
                $"// {command}",
                index == 0 ? "@THIS" : "@THAT",
                "D=M",
                .. PushFromD()
            ],
            "static" =>
            [
                $"// {command}",
                $"@FileName.{index}",
                "D=M",
                .. PushFromD()
            ],
            _ => throw new InvalidOperationException($"Unsupported push segment in test: {segment}")
        };
    }

    private static string[] ExpectedPop(string segment, int index, string command)
    {
        return segment switch
        {
            "local" => PopToSegment(command, "@LCL", index),
            "argument" => PopToSegment(command, "@ARG", index),
            "this" => PopToSegment(command, "@THIS", index),
            "that" => PopToSegment(command, "@THAT", index),
            "temp" =>
            [
                $"// {command}",
                $"@{index}",
                "D=A",
                "@R5",
                "D=D+A",
                "@R13",
                "M=D",
                "@SP",
                "AM=M-1",
                "D=M",
                "@R13",
                "A=M",
                "M=D"
            ],
            "pointer" =>
            [
                $"// {command}",
                "@SP",
                "AM=M-1",
                "D=M",
                index == 0 ? "@THIS" : "@THAT",
                "M=D"
            ],
            "static" =>
            [
                $"// {command}",
                "@SP",
                "AM=M-1",
                "D=M",
                $"@FileName.{index}",
                "M=D"
            ],
            _ => throw new InvalidOperationException($"Unsupported pop segment in test: {segment}")
        };
    }

    private static string[] PushFromSegment(string command, string segmentSymbol, int index)
    {
        return
        [
            $"// {command}",
            $"@{index}",
            "D=A",
            segmentSymbol,
            "A=D+M",
            "D=M",
            .. PushFromD()
        ];
    }

    private static string[] PopToSegment(string command, string segmentSymbol, int index)
    {
        return
        [
            $"// {command}",
            $"@{index}",
            "D=A",
            segmentSymbol,
            "D=D+M",
            "@R13",
            "M=D",
            "@SP",
            "AM=M-1",
            "D=M",
            "@R13",
            "A=M",
            "M=D"
        ];
    }

    private static string[] Binary(string command, string op)
    {
        return
        [
            $"// {command}",
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            $"M=M{op}D"
        ];
    }

    private static string[] Unary(string command, string instruction)
    {
        return
        [
            $"// {command}",
            "@SP",
            "A=M-1",
            instruction
        ];
    }

    private static string[] Compare(string command, string jumpInstruction, int lineNumber)
    {
        return
        [
            $"// {command}",
            "@SP",
            "AM=M-1",
            "D=M",
            "A=A-1",
            "D=M-D",
            "M=-1",
            $"@END_{lineNumber}",
            $"D;{jumpInstruction}",
            "@SP",
            "A=M-1",
            "M=0",
            $"(END_{lineNumber})"
        ];
    }

    private static string[] PushFromD()
    {
        return
        [
            "@SP",
            "A=M",
            "M=D",
            "@SP",
            "M=M+1"
        ];
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

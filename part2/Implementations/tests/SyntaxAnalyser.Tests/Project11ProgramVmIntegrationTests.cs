using System.Diagnostics;

namespace SyntaxAnalyser.Tests;

public class Project11ProgramVmIntegrationTests : IDisposable
{
    private readonly List<string> temporaryDirectories = [];

    [Theory]
    [InlineData("Average", "Main")]
    [InlineData("ComplexArrays", "Main")]
    [InlineData("ConvertToBin", "Main")]
    [InlineData("Seven", "Main")]
    public async Task Run_WithSingleFileProjectDirectory_GeneratesMatchingVm(
        string projectName,
        string baseName)
    {
        var fixtureDirectory = Path.Combine(Project11TestSupport.Project11FixtureRoot, projectName);
        var workingDirectory = CreateTemporaryDirectory();

        CopyJackFiles(fixtureDirectory, workingDirectory);

        var result = await RunProgramAsync(workingDirectory, workingDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Analysis completed:", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);

        var actualVmPath = Path.Combine(workingDirectory, $"{baseName}.vm");
        var expectedVmPath = Path.Combine(fixtureDirectory, $"{baseName}.vm");

        Assert.True(File.Exists(actualVmPath), $"Missing generated file: {actualVmPath}");
        Project11TestSupport.AssertVmEquivalent(
            await File.ReadAllTextAsync(expectedVmPath),
            await File.ReadAllTextAsync(actualVmPath));
    }

    [Theory]
    [InlineData("Square", new[] { "Main", "Square", "SquareGame" })]
    [InlineData("Pong", new[] { "Ball", "Bat", "Main", "PongGame" })]
    public async Task Run_WithMultiFileProjectDirectory_GeneratesMatchingVmForEveryJackFile(
        string projectName,
        string[] baseNames)
    {
        var fixtureDirectory = Path.Combine(Project11TestSupport.Project11FixtureRoot, projectName);
        var workingDirectory = CreateTemporaryDirectory();

        CopyJackFiles(fixtureDirectory, workingDirectory);

        var result = await RunProgramAsync(workingDirectory, workingDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Analysis completed:", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);

        foreach (var baseName in baseNames)
        {
            var actualVmPath = Path.Combine(workingDirectory, $"{baseName}.vm");
            var expectedVmPath = Path.Combine(fixtureDirectory, $"{baseName}.vm");

            Assert.True(File.Exists(actualVmPath), $"Missing generated file: {actualVmPath}");
            Project11TestSupport.AssertVmEquivalent(
                await File.ReadAllTextAsync(expectedVmPath),
                await File.ReadAllTextAsync(actualVmPath));
        }
    }

    [Fact]
    public async Task Run_WithInvalidInputPath_ReturnsFailure_AndWritesNoVmFiles()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var missingPath = Path.Combine(workingDirectory, "MissingProject");

        var result = await RunProgramAsync(workingDirectory, missingPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Analysis failed:", result.StandardError);
        Assert.Empty(Directory.GetFiles(workingDirectory, "*.vm", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task Run_WhenCompilationFails_RemovesPartialVmOutputs()
    {
        var workingDirectory = CreateTemporaryDirectory();

        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, "A_First.jack"),
            """
            class A_First {
                function void ok() {
                    return;
                }
            }
            """);

        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, "B_Second.jack"),
            """
            class B_Second {
                function void broken( {
                    return;
                }
            }
            """);

        var result = await RunProgramAsync(workingDirectory, workingDirectory);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Analysis failed:", result.StandardError);
        Assert.Empty(Directory.GetFiles(workingDirectory, "*.vm", SearchOption.TopDirectoryOnly));
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

    private static void CopyJackFiles(string sourceDirectory, string destinationDirectory)
    {
        foreach (var sourceFile in Directory.GetFiles(sourceDirectory, "*.jack", SearchOption.TopDirectoryOnly))
        {
            File.Copy(sourceFile, Path.Combine(destinationDirectory, Path.GetFileName(sourceFile)));
        }
    }

    private async Task<ProcessResult> RunProgramAsync(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Project11TestSupport.SyntaxAnalyserProjectPath);
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
        var path = Path.Combine(Path.GetTempPath(), $"project11-program-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        temporaryDirectories.Add(path);
        return path;
    }

    private static string NormalizeNewLines(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

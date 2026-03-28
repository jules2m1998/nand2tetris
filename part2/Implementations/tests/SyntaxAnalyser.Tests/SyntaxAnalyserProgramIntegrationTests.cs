using System.Diagnostics;
using System.Xml.Linq;

namespace SyntaxAnalyser.Tests;

public class SyntaxAnalyserProgramIntegrationTests : IDisposable
{
    private readonly List<string> temporaryDirectories = [];

    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../SyntaxAnalyser.Tests/Fixtures/Project10"));

    [Fact]
    public async Task Run_WithProject10Directory_GeneratesSiblingXmlFilesForEveryJackFile()
    {
        var fixtureDirectory = Path.Combine(FixtureRoot, "ExpressionLessSquare");
        var workingDirectory = CreateTemporaryDirectory();

        foreach (var sourceFile in Directory.GetFiles(fixtureDirectory, "*.jack"))
        {
            File.Copy(sourceFile, Path.Combine(workingDirectory, Path.GetFileName(sourceFile)));
        }

        var result = await RunProgramAsync(workingDirectory, workingDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Analysis completed:", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);

        foreach (var baseName in new[] { "Main", "Square", "SquareGame" })
        {
            var actualXmlPath = Path.Combine(workingDirectory, $"{baseName}.xml");
            var expectedXmlPath = Path.Combine(fixtureDirectory, $"{baseName}.xml");

            Assert.True(File.Exists(actualXmlPath), $"Missing generated file: {actualXmlPath}");
            AssertXmlEquivalent(
                await File.ReadAllTextAsync(expectedXmlPath),
                await File.ReadAllTextAsync(actualXmlPath));
        }
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
        var path = Path.Combine(Path.GetTempPath(), $"syntaxanalyser-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        temporaryDirectories.Add(path);
        return path;
    }

    private static string GetProjectPath()
    {
        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../SyntaxAnalyser/SyntaxAnalyser.csproj"));
    }

    private static string NormalizeNewLines(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private static void AssertXmlEquivalent(string expectedXml, string actualXml)
    {
        var expected = XElement.Parse(expectedXml);
        var actual = XElement.Parse(actualXml);

        AssertElementEquivalent(expected, actual);
    }

    private static void AssertElementEquivalent(XElement expected, XElement actual)
    {
        Assert.Equal(expected.Name.LocalName, actual.Name.LocalName);

        var expectedChildren = expected.Elements().ToArray();
        var actualChildren = actual.Elements().ToArray();

        Assert.Equal(expectedChildren.Length, actualChildren.Length);

        if (expectedChildren.Length == 0)
        {
            Assert.Equal(expected.Value.Trim(), actual.Value.Trim());
            return;
        }

        for (var i = 0; i < expectedChildren.Length; i++)
        {
            AssertElementEquivalent(expectedChildren[i], actualChildren[i]);
        }
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

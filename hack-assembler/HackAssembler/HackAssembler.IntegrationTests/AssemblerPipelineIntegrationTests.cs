using System.Diagnostics;
using FluentAssertions;

namespace HackAssembler.IntegrationTests;

public class AssemblerCliIntegrationTests
{
    [Theory]
    [InlineData("Mult")]
    [InlineData("Fill")]
    public async Task Executable_Should_Assemble_Real_Programs_To_Expected_Hack_Output(string programName)
    {
        using var tempDirectory = new TempDirectory();
        var inputPath = CopyProgramTo(tempDirectory.Path, programName);
        var expectedOutput = await File.ReadAllLinesAsync(GetExpectedOutputPath(programName));
        using var process = CreateAssemblerProcess(inputPath, tempDirectory.Path);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        process.Start();
        await process.WaitForExitAsync(cancellationSource.Token);

        var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationSource.Token);
        var standardError = await process.StandardError.ReadToEndAsync(cancellationSource.Token);

        process.ExitCode.Should().Be(
            0,
            $"stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{standardError}");

        var outputPath = Path.Combine(tempDirectory.Path, $"{programName}.hack");

        File.Exists(outputPath).Should().BeTrue();
        Path.GetFileName(outputPath).Should().Be($"{programName}.hack");

        var actualOutput = await File.ReadAllLinesAsync(outputPath);
        actualOutput.Should().Equal(expectedOutput);
    }

    private static string CopyProgramTo(string outputDirectory, string programName)
    {
        var sourcePath = GetProgramPath(programName);
        var destinationPath = Path.Combine(outputDirectory, $"{programName}.asm");
        File.Copy(sourcePath, destinationPath);
        return destinationPath;
    }

    private static string GetProgramPath(string programName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "Programs", $"{programName}.asm");
    }

    private static string GetExpectedOutputPath(string programName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "Expected", $"{programName}.hack");
    }

    private static Process CreateAssemblerProcess(string inputPath, string outputDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetAssemblerExecutablePath(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = outputDirectory
        };

        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add(outputDirectory);

        return new Process { StartInfo = startInfo };
    }

    private static string GetAssemblerExecutablePath()
    {
        var executableName = OperatingSystem.IsWindows() ? "HackAssembler.exe" : "HackAssembler";
        return Path.Combine(AppContext.BaseDirectory, executableName);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hack-assembler-{Guid.NewGuid():N}");

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

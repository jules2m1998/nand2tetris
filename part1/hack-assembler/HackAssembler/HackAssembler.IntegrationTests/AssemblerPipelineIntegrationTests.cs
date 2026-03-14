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
        var result = await RunAssemblerAsync(inputPath, tempDirectory.Path);

        result.ExitCode.Should().Be(0, result.FormatForAssertion());

        var outputPath = Path.Combine(tempDirectory.Path, $"{programName}.hack");

        File.Exists(outputPath).Should().BeTrue();
        Path.GetFileName(outputPath).Should().Be($"{programName}.hack");

        var actualOutput = await File.ReadAllLinesAsync(outputPath);
        actualOutput.Should().Equal(expectedOutput);
    }

    [Fact]
    public async Task Executable_Should_Assemble_All_Asm_Files_When_Input_Is_A_Folder()
    {
        using var tempDirectory = new TempDirectory();
        var inputDirectory = Path.Combine(tempDirectory.Path, "input");
        var outputDirectory = Path.Combine(tempDirectory.Path, "output");
        Directory.CreateDirectory(inputDirectory);

        CopyProgramTo(inputDirectory, "Mult");
        CopyProgramTo(inputDirectory, "Fill");

        var result = await RunAssemblerAsync(inputDirectory, outputDirectory);
        result.ExitCode.Should().Be(0, result.FormatForAssertion());

        var multOutput = await File.ReadAllLinesAsync(Path.Combine(outputDirectory, "Mult.hack"));
        var fillOutput = await File.ReadAllLinesAsync(Path.Combine(outputDirectory, "Fill.hack"));
        var expectedMultOutput = await File.ReadAllLinesAsync(GetExpectedOutputPath("Mult"));
        var expectedFillOutput = await File.ReadAllLinesAsync(GetExpectedOutputPath("Fill"));

        multOutput.Should().Equal(expectedMultOutput);
        fillOutput.Should().Equal(expectedFillOutput);
    }

    [Fact]
    public async Task Executable_Should_Stop_On_First_Error_By_Default_When_Input_Is_A_Folder()
    {
        using var tempDirectory = new TempDirectory();
        var inputDirectory = Path.Combine(tempDirectory.Path, "input");
        var outputDirectory = Path.Combine(tempDirectory.Path, "output");
        Directory.CreateDirectory(inputDirectory);

        await File.WriteAllLinesAsync(Path.Combine(inputDirectory, "A-Bad.asm"), ["D=Q"]);
        CopyProgramTo(inputDirectory, "Mult");

        var result = await RunAssemblerAsync(inputDirectory, outputDirectory);

        result.ExitCode.Should().Be(1, result.FormatForAssertion());
        File.Exists(Path.Combine(outputDirectory, "Mult.hack")).Should().BeFalse();
    }

    [Fact]
    public async Task Executable_Should_Continue_On_Error_When_Flag_Is_Set()
    {
        using var tempDirectory = new TempDirectory();
        var inputDirectory = Path.Combine(tempDirectory.Path, "input");
        var outputDirectory = Path.Combine(tempDirectory.Path, "output");
        Directory.CreateDirectory(inputDirectory);

        await File.WriteAllLinesAsync(Path.Combine(inputDirectory, "A-Bad.asm"), ["D=Q"]);
        CopyProgramTo(inputDirectory, "Mult");

        var result = await RunAssemblerAsync(inputDirectory, outputDirectory, "--continue-on-error");

        result.ExitCode.Should().Be(1, result.FormatForAssertion());

        var multOutput = await File.ReadAllLinesAsync(Path.Combine(outputDirectory, "Mult.hack"));
        var expectedMultOutput = await File.ReadAllLinesAsync(GetExpectedOutputPath("Mult"));
        multOutput.Should().Equal(expectedMultOutput);
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

    private static Process CreateAssemblerProcess(string inputPath, string outputDirectory, params string[] additionalArguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetAssemblerExecutablePath(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        };

        foreach (var argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add(outputDirectory);

        return new Process { StartInfo = startInfo };
    }

    private static async Task<ProcessResult> RunAssemblerAsync(string inputPath, string outputDirectory, params string[] additionalArguments)
    {
        using var process = CreateAssemblerProcess(inputPath, outputDirectory, additionalArguments);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        process.Start();
        await process.WaitForExitAsync(cancellationSource.Token);

        var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationSource.Token);
        var standardError = await process.StandardError.ReadToEndAsync(cancellationSource.Token);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static string GetAssemblerExecutablePath()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[] { "HackAssembler.exe", "hack-assembler.exe" }
            : new[] { "HackAssembler", "hack-assembler" };

        foreach (var candidate in candidates)
        {
            var path = Path.Combine(AppContext.BaseDirectory, candidate);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"Unable to find the assembler executable in {AppContext.BaseDirectory}.");
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

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public string FormatForAssertion()
        {
            return $"stdout:{Environment.NewLine}{StandardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{StandardError}";
        }
    }
}

using System.Xml.Linq;
using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Services;

namespace SyntaxAnalyser.Tests;

internal static class Project11TestSupport
{
    internal static string Project11FixtureRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../SyntaxAnalyser.Tests/Fixtures/Project11"));

    internal static string SyntaxAnalyserProjectPath => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../SyntaxAnalyser/SyntaxAnalyser.csproj"));

    internal static (ICodeGenerator Generator, ITokenizer Tokenizer) CreateCodeGenerator(string source)
    {
        ITokenizer tokenizer = new Tokenizer(source);
        var implementationTypes = typeof(ICodeGenerator).Assembly
            .GetTypes()
            .Where(type =>
                typeof(ICodeGenerator).IsAssignableFrom(type) &&
                !type.IsInterface &&
                !type.IsAbstract)
            .ToArray();

        Assert.True(
            implementationTypes.Length == 1,
            "Add one concrete CodeGenerator implementation that implements ICodeGenerator.");

        var constructor = implementationTypes[0].GetConstructor([typeof(ITokenizer)]);
        Assert.NotNull(constructor);

        var generator = constructor.Invoke([tokenizer]) as ICodeGenerator;

        Assert.NotNull(generator);
        return (generator, tokenizer);
    }

    internal static void AssertCurrentToken(ITokenizer tokenizer, Models.TokenType expectedType, string expectedValue)
    {
        Assert.True(tokenizer.HasMoreTokens);
        Assert.Equal(new Models.Token(expectedType, expectedValue), tokenizer.CurrentToken);
    }

    internal static void AssertVmEquivalent(string expected, string actual)
    {
        Assert.Equal(NormalizeVm(expected), NormalizeVm(actual));
    }

    internal static string[] NormalizeVm(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    internal static string ReadFixture(string project, string fileName)
    {
        return File.ReadAllText(Path.Combine(Project11FixtureRoot, project, fileName));
    }

    internal static string FixturePath(string project, string fileName)
    {
        return Path.Combine(Project11FixtureRoot, project, fileName);
    }
}

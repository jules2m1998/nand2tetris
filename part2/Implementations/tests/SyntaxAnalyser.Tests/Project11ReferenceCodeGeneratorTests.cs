using SyntaxAnalyser.Abstractions;

namespace SyntaxAnalyser.Tests;

public class Project11ReferenceCodeGeneratorTests
{
    [Theory]
    [MemberData(nameof(Project11Cases))]
    public void CompileClass_MatchesOfficialProject11Vm(
        string scenario,
        string jackPath,
        string vmPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        Assert.True(File.Exists(jackPath), $"Missing Jack fixture: {jackPath}");
        Assert.True(File.Exists(vmPath), $"Missing VM fixture: {vmPath}");

        var source = File.ReadAllText(jackPath);
        var (generator, tokenizer) = Project11TestSupport.CreateCodeGenerator(source);

        var actualVm = generator.CompileClass();
        var expectedVm = File.ReadAllText(vmPath);

        Project11TestSupport.AssertVmEquivalent(expectedVm, actualVm);
        Assert.False(tokenizer.HasMoreTokens);
    }

    public static TheoryData<string, string, string> Project11Cases => new()
    {
        { "Average/Main", Project11TestSupport.FixturePath("Average", "Main.jack"), Project11TestSupport.FixturePath("Average", "Main.vm") },
        { "ComplexArrays/Main", Project11TestSupport.FixturePath("ComplexArrays", "Main.jack"), Project11TestSupport.FixturePath("ComplexArrays", "Main.vm") },
        { "ConvertToBin/Main", Project11TestSupport.FixturePath("ConvertToBin", "Main.jack"), Project11TestSupport.FixturePath("ConvertToBin", "Main.vm") },
        { "Pong/Ball", Project11TestSupport.FixturePath("Pong", "Ball.jack"), Project11TestSupport.FixturePath("Pong", "Ball.vm") },
        { "Pong/Bat", Project11TestSupport.FixturePath("Pong", "Bat.jack"), Project11TestSupport.FixturePath("Pong", "Bat.vm") },
        { "Pong/Main", Project11TestSupport.FixturePath("Pong", "Main.jack"), Project11TestSupport.FixturePath("Pong", "Main.vm") },
        { "Pong/PongGame", Project11TestSupport.FixturePath("Pong", "PongGame.jack"), Project11TestSupport.FixturePath("Pong", "PongGame.vm") },
        { "Seven/Main", Project11TestSupport.FixturePath("Seven", "Main.jack"), Project11TestSupport.FixturePath("Seven", "Main.vm") },
        { "Square/Main", Project11TestSupport.FixturePath("Square", "Main.jack"), Project11TestSupport.FixturePath("Square", "Main.vm") },
        { "Square/Square", Project11TestSupport.FixturePath("Square", "Square.jack"), Project11TestSupport.FixturePath("Square", "Square.vm") },
        { "Square/SquareGame", Project11TestSupport.FixturePath("Square", "SquareGame.jack"), Project11TestSupport.FixturePath("Square", "SquareGame.vm") },
    };
}

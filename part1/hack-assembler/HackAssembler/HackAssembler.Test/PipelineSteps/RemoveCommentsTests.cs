using FluentAssertions;
using HackAssembler.Implementations.PipelineSteps;
namespace HackAssembler.Test.PipelineSteps;

public class RemoveCommentsTests
{
    
    public static IEnumerable<object[]> ProcessCases()
    {
        yield return
        [
            new[]
            {
                "   // bootstrap comment   ",
                "",
                "   @2   ",
                "D=A   // load constant",
                "   ",
                "@3// next value",
                "   M=D   ",
                "// trailing comment line"
            },
            new[] { "@2", "D=A", "@3", "M=D" }
        ];

        yield return
        [
            new[]
            {
                "@R0",
                "D=M",
                "// comment only",
                "@R1",
                "M=D // store value"
            },
            new[] { "@R0", "D=M", "@R1", "M=D" }
        ];

        yield return
        [
            new[]
            {
                "   ",
                "// full line comment",
                "   @SCREEN   // screen base",
                "D=A",
                "@i",
                "M=1"
            },
            new[] { "@SCREEN", "D=A", "@i", "M=1" }
        ];
    }
    
    [Theory]
    [MemberData(nameof(ProcessCases))]
    public void Process_Should_Remove_Empty_Lines_And_Full_Line_Comments_And_Inline_Comments(string[] inputs, string[]  expected)
    {
        // Arrange
        var sut = new RemoveComments();

        // Act
        var result = sut.Process(inputs);

        // Assert
        result.Should().Equal(expected);
    }
}

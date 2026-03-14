using FluentAssertions;
using HackAssembler.Implementations.PipelineSteps;
namespace HackAssembler.Test.PipelineSteps;

public class ProcessLabelsTests
{
    [Theory]
    [MemberData(nameof(ProcessCases))]
    public void Process_Should_Replace_Label_Usages_With_Their_Mapped_Address(string[] input, string[] expected)
    {
        // Arrange
        var sut = new ProcessLabels();

        // Act
        var result = sut.Process(input);

        // Assert
        result.Should().Equal(expected);
    }

    public static IEnumerable<object[]> ProcessCases()
    {
        yield return
        [
            new[]
            {
                "(LOOP)",
                "@LOOP",
                "0;JMP"
            },
            new[]
            {
                "@0",
                "0;JMP"
            }
        ];

        yield return
        [
            new[]
            {
                "@R0",
                "D=M",
                "(END)",
                "@END",
                "0;JMP"
            },
            new[]
            {
                "@R0",
                "D=M",
                "@2",
                "0;JMP"
            }
        ];

        yield return
        [
            new[]
            {
                "(START)",
                "@START",
                "D;JGT",
                "@START",
                "0;JMP"
            },
            new[]
            {
                "@0",
                "D;JGT",
                "@0",
                "0;JMP"
            }
        ];

        yield return
        [
            new[]
            {
                "@i",
                "M=1",
                "(LOOP)",
                "@i",
                "M=M+1",
                "@LOOP",
                "0;JMP"
            },
            new[]
            {
                "@i",
                "M=1",
                "@i",
                "M=M+1",
                "@2",
                "0;JMP"
            }
        ];
        
        yield return
        [
            new[]
            {
                "(START)",
                "@START",
                "0;JMP",
                "(MIDDLE)",
                "@MIDDLE",
                "D;JGT",
                "(END)",
                "@START",
                "@MIDDLE",
                "@END",
                "0;JMP"
            },
            new[]
            {
                "@0",
                "0;JMP",
                "@2",
                "D;JGT",
                "@0",
                "@2",
                "@4",
                "0;JMP"
            }
        ];
    }
}

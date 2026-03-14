using FluentAssertions;
using HackAssembler.Implementations.PipelineSteps;

namespace HackAssembler.Test.PipelineSteps;

public class ProcessVariablesTests
{
    [Theory]
    [MemberData(nameof(ProcessCases))]
    public void Process_Should_Replace_Variables_With_Assigned_Addresses(string[] input, string[] expected)
    {
        // Arrange
        var sut = new ProcessVariables();

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
                "@i",
                "M=1"
            },
            new[]
            {
                "@16",
                "M=1"
            }
        ];

        yield return
        [
            new[]
            {
                "@i",
                "M=1",
                "@i",
                "M=M+1"
            },
            new[]
            {
                "@16",
                "M=1",
                "@16",
                "M=M+1"
            }
        ];

        yield return
        [
            new[]
            {
                "@i",
                "M=1",
                "@sum",
                "M=0",
                "@i",
                "D=M",
                "@sum",
                "M=D+M"
            },
            new[]
            {
                "@16",
                "M=1",
                "@17",
                "M=0",
                "@16",
                "D=M",
                "@17",
                "M=D+M"
            }
        ];

        yield return
        [
            new[]
            {
                "@R0",
                "D=M",
                "@SCREEN",
                "M=D",
                "@counter",
                "M=1"
            },
            new[]
            {
                "@0",
                "D=M",
                "@16384",
                "M=D",
                "@16",
                "M=1"
            }
        ];

        yield return
        [
            new[]
            {
                "@R1",
                "D=M",
                "@R2",
                "M=D",
                "@R15",
                "D=M"
            },
            new[]
            {
                "@1",
                "D=M",
                "@2",
                "M=D",
                "@15",
                "D=M"
            }
        ];

        yield return
        [
            new[]
            {
                "@SP",
                "M=1",
                "@LCL",
                "D=M",
                "@ARG",
                "M=D"
            },
            new[]
            {
                "@0",
                "M=1",
                "@1",
                "D=M",
                "@2",
                "M=D"
            }
        ];

        yield return
        [
            new[]
            {
                "@THIS",
                "D=M",
                "@THAT",
                "M=D",
                "@KBD",
                "D=M"
            },
            new[]
            {
                "@3",
                "D=M",
                "@4",
                "M=D",
                "@24576",
                "D=M"
            }
        ];

        yield return
        [
            new[]
            {
                "@SCREEN",
                "D=A",
                "@KBD",
                "D=D+A",
                "@temp",
                "M=D",
                "@R7",
                "D=M"
            },
            new[]
            {
                "@16384",
                "D=A",
                "@24576",
                "D=D+A",
                "@16",
                "M=D",
                "@7",
                "D=M"
            }
        ];

        yield return
        [
            new[]
            {
                "@21",
                "D=A",
                "@value",
                "M=D"
            },
            new[]
            {
                "@21",
                "D=A",
                "@16",
                "M=D"
            }
        ];
    }
}
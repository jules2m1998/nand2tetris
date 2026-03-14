using FluentAssertions;
using HackAssembler.Implementations.PipelineSteps;
namespace HackAssembler.Test.PipelineSteps;

public class HackToAssemblyTests
{
    [Theory]
    [MemberData(nameof(ProcessCases))]
    public void Process_Should_Convert_Hack_Assembly_To_Binary(string[] input, string[] expected)
    {
        // Arrange
        var sut = new HackToAssembly();

        // Act
        var result = sut.Process(input);

        // Assert
        result.Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(FailingCases))]
    public void Process_Should_Throw_For_Invalid_Hack_Assembly(string[] input)
    {
        // Arrange
        var sut = new HackToAssembly();

        // Act
        Action act = () => sut.Process(input);

        // Assert
        act.Should().Throw<Exception>();
    }

    public static IEnumerable<object[]> ProcessCases()
    {
        // Tests a single A-instruction with value 0.
        yield return
        [
            new[]
            {
                "@0"
            },
            new[]
            {
                "0000000000000000"
            }
        ];

        // Tests multiple A-instructions with small and large numeric values.
        yield return
        [
            new[]
            {
                "@1",
                "@2",
                "@16",
                "@16384"
            },
            new[]
            {
                "0000000000000001",
                "0000000000000010",
                "0000000000010000",
                "0100000000000000"
            }
        ];

        // Tests basic C-instructions using dest=comp forms.
        yield return
        [
            new[]
            {
                "D=A",
                "D=M",
                "M=D",
                "MD=D+1"
            },
            new[]
            {
                "1110110000010000",
                "1111110000010000",
                "1110001100001000",
                "1110011111011000"
            }
        ];

        // Tests C-instructions with jump-only and mixed dest/comp combinations.
        yield return
        [
            new[]
            {
                "D;JGT",
                "0;JMP",
                "D=D-A",
                "AMD=D|A",
                "AD=M-1"
            },
            new[]
            {
                "1110001100000001",
                "1110101010000111",
                "1110010011010000",
                "1110010101111000",
                "1111110010110000"
            }
        ];

        // Tests full dest=comp;jump combinations with multiple dest bits and jump bits.
        yield return
        [
            new[]
            {
                "AMD=D+M;JNE",
                "AM=D-1;JLE",
                "AD=A-1;JEQ",
                "AMD=M+1;JMP"
            },
            new[]
            {
                "1111000010111101",
                "1110001110101110",
                "1110110010110010",
                "1111110111111111"
            }
        ];

        // Tests a small complete Hack program converted line by line.
        yield return
        [
            new[]
            {
                "@2",
                "D=A",
                "@3",
                "D=D+A",
                "@0",
                "M=D"
            },
            new[]
            {
                "0000000000000010",
                "1110110000010000",
                "0000000000000011",
                "1110000010010000",
                "0000000000000000",
                "1110001100001000"
            }
        ];
    }
    public static IEnumerable<object[]> FailingCases()
    {
        // Tests an invalid A-instruction with a value larger than the 15-bit Hack limit.
        yield return
        [
            new[]
            {
                "@32768"
            }
        ];

        // Tests an invalid C-instruction with an unknown computation mnemonic.
        yield return
        [
            new[]
            {
                "D=Q"
            }
        ];

        // Tests an invalid C-instruction with an unknown jump mnemonic.
        yield return
        [
            new[]
            {
                "D;ABC"
            }
        ];

        // Tests an invalid C-instruction with an unknown destination mnemonic.
        yield return
        [
            new[]
            {
                "X=D"
            }
        ];

        // Tests a malformed instruction mixing A-instruction and C-instruction syntax.
        yield return
        [
            new[]
            {
                "@2;JMP"
            }
        ];

        // Tests a malformed C-instruction with no computation part.
        yield return
        [
            new[]
            {
                "D="
            }
        ];
    }
}
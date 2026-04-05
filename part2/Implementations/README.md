# nand2tetris implementations

C# and Jack implementations for the second half of the nand2tetris course.

## Projects

- [SyntaxAnalyser](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/SyntaxAnalyser): Jack tokenizer, XML syntax analysis, and VM code generation for Projects 10 and 11
- [VMTranslator](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/VMTranslator): VM-to-Hack assembly translator for Project 8
- [OperatingSystem](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem): Jack OS class implementations for Project 12

## Solution

The workspace is organized through [Implementations.slnx](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/Implementations.slnx).

Build everything from this folder with:

```bash
dotnet build Implementations.slnx
```

## Testing

Project-specific tests live under [tests](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/tests):

- [tests/SyntaxAnalyser.Tests](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/tests/SyntaxAnalyser.Tests)
- [tests/VMTranslator.Tests](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/tests/VMTranslator.Tests)

Run them with:

```bash
dotnet test tests/SyntaxAnalyser.Tests/SyntaxAnalyser.Tests.csproj
dotnet test tests/VMTranslator.Tests/VMTranslator.Tests.csproj
```

## Certificate

The course certificate for nand2tetris Part II is stored at:

- [Certificates/nand2tetris-part-2-certificate.pdf](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/Certificates/nand2tetris-part-2-certificate.pdf)

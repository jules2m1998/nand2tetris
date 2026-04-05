# syntax-analyser

A C# Jack compiler workspace for nand2tetris Projects 10 and 11.

The CLI accepts either a single `.jack` file or a directory of top-level `.jack` files. It generates sibling `.vm` output for code generation and also attempts to emit sibling `.xml` parse-tree output for the syntax-analysis side.

## What It Does

- Accepts either a `.jack` file path or a directory containing `.jack` files
- Supports relative and absolute paths
- Tokenizes Jack source and compiles it to VM code
- Generates a sibling `.vm` file for every input `.jack` file
- Also generates a sibling `.xml` file when the syntax-analysis engine succeeds for that source
- Stops immediately on analysis or compilation errors
- Deletes files created during the current run if a failure happens partway through
- Provides a simple help command for the CLI

The project currently includes:

- Project 10 tokenizer tests against the official `T.xml` outputs
- Project 10 compiler-engine tests for grammar rules and official parsed XML outputs
- Project 11 code-generator tests for VM generation behavior
- program-level integration tests for Jack-to-VM output against the official Project 11 fixtures

## Running the Compiler

From the repository root:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- ./path/to/Main.jack
```

To compile all top-level `.jack` files in a directory:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- ./path/to/Square
```

You can also use an absolute path:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- /absolute/path/to/Main.jack
```

Generated outputs are created next to the source:

```text
Main.jack  ->  Main.vm (+ Main.xml when XML generation succeeds)
Square/    ->  Main.vm, Square.vm, SquareGame.vm
```

### Help

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- --help
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- -h
```

## Project Structure

```text
part2/Implementations/
├── Implementations.slnx
├── SyntaxAnalyser/
│   ├── Abstractions/
│   ├── Models/
│   ├── Services/
│   │   ├── CodeGenerator.cs
│   │   ├── CompilerEngine.cs
│   │   ├── SyntaxAnalyser.cs
│   │   ├── Tokenizer.cs
│   │   ├── VmWriter.cs
│   │   └── XmlWriter.cs
│   ├── Program.cs
│   ├── README.md
│   └── SyntaxAnalyser.csproj
└── tests/
    └── SyntaxAnalyser.Tests/
        ├── CodeGeneratorFoundationsTests.cs
        ├── CodeGeneratorImplementationTests.cs
        ├── CompilerEngineImplementationTests.cs
        ├── Project10PathCoverageTests.cs
        ├── Project10ReferenceIntegrationTests.cs
        ├── Project11ProgramVmIntegrationTests.cs
        ├── Project11ReferenceCodeGeneratorTests.cs
        ├── SyntaxAnalyserProgramIntegrationTests.cs
        └── TokenizerTests.cs
```

## Testing

Build the project:

```bash
dotnet build part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj
```

Run the full test suite:

```bash
dotnet test part2/Implementations/tests/SyntaxAnalyser.Tests/SyntaxAnalyser.Tests.csproj
```

Run only the Project 11 reference code-generation tests:

```bash
dotnet test part2/Implementations/tests/SyntaxAnalyser.Tests/SyntaxAnalyser.Tests.csproj --filter FullyQualifiedName~Project11ReferenceCodeGeneratorTests
```

The suite currently covers:

- tokenizer behavior, including comments, strings, ranges, and keyword boundaries
- XML compilation for classes, subroutines, statements, expressions, terms, and expression lists
- exact VM generation for Project 11 fixtures
- CLI behavior for Jack directory compilation and output cleanup

## Current Limitations

- Directory compilation only reads top-level `.jack` files; it does not recurse into nested folders
- `SyntaxAnalyser.cs` is still a thin shell service; the orchestration remains in `Program.cs`
- XML generation is best-effort and is not required for the VM compilation path

## Related Projects

- [VMTranslator](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/VMTranslator)
- [OperatingSystem](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem)

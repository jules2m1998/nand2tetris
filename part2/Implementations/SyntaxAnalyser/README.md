# syntax-analyser

A small C# implementation of the Nand2Tetris syntax analyser for project 10.

The project accepts either a single `.jack` file or a directory of `.jack` files and generates the XML parse tree output expected by the book's compare tools.

## What It Does

- Accepts either a `.jack` file path or a directory containing `.jack` files
- Supports relative and absolute paths
- Parses Jack source using the tokenizer and compilation engine from this project
- Generates a sibling `.xml` file for each input `.jack` source
- Stops immediately on analysis errors
- Deletes any output files created during the current run if analysis fails partway through
- Provides a simple help command for the CLI

The project also contains:

- tokenizer reference tests against the official `T.xml` outputs
- compiler-engine tests for individual grammar rules
- Project 10 integration tests against the official parsed `.xml` outputs
- a CLI integration test that runs `dotnet run` and compares generated files with the official fixtures

## Running the Syntax Analyser

From the repository root:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- ./path/to/Main.jack
```

You can also pass a directory to analyse all top-level `.jack` files in that directory:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- ./path/to/ExpressionLessSquare
```

You can use an absolute path for either form:

```bash
dotnet run --project part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj -- /absolute/path/to/Main.jack
```

The generated output is created next to the source:

```text
Main.jack  ->  Main.xml
Square/    ->  Main.xml, Square.xml, SquareGame.xml
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
│   │   ├── ICompilerEngine.cs
│   │   ├── ISyntaxAnalyser.cs
│   │   └── ITokenizer.cs
│   ├── Models/
│   │   ├── Token.cs
│   │   └── TokenType.cs
│   ├── Services/
│   │   ├── CompilerEngine.cs
│   │   ├── SyntaxAnalyser.cs
│   │   ├── Tokenizer.cs
│   │   └── XmlWriter.cs
│   ├── Program.cs
│   ├── README.md
│   └── SyntaxAnalyser.csproj
└── tests/
    └── SyntaxAnalyser.Tests/
        ├── CompilerEngineImplementationTests.cs
        ├── Project10PathCoverageTests.cs
        ├── Project10ReferenceIntegrationTests.cs
        ├── SyntaxAnalyserProgramIntegrationTests.cs
        ├── TokenizerTests.cs
        └── SyntaxAnalyser.Tests.csproj
```

## Testing

Build the analyser project:

```bash
dotnet build part2/Implementations/SyntaxAnalyser/SyntaxAnalyser.csproj
```

Run the CLI integration test only:

```bash
dotnet test part2/Implementations/tests/SyntaxAnalyser.Tests/SyntaxAnalyser.Tests.csproj --filter FullyQualifiedName~SyntaxAnalyserProgramIntegrationTests
```

Run the full test project:

```bash
dotnet test part2/Implementations/tests/SyntaxAnalyser.Tests/SyntaxAnalyser.Tests.csproj
```

The suite currently covers:

- tokenizer behavior, including comments, strings, ranges, and keyword boundaries
- compilation of classes, subroutines, statements, expressions, terms, and expression lists
- reference comparisons against the official Project 10 `T.xml` and parsed `.xml` outputs
- CLI behavior for analysing a Project 10 directory end to end

## Current Limitations

- The CLI analyses only top-level `.jack` files in a directory; it does not recurse into nested folders.
- `SyntaxAnalyser.cs` is still an empty shell service; the CLI currently wires `Tokenizer` and `CompilerEngine` directly.
- The XML writer is intentionally simple and focused on the project 10 output shape.

## Next Steps

- Move the orchestration logic from `Program.cs` into a real `SyntaxAnalyser` service
- Add CLI tests for help output and failure cleanup paths
- Separate parsing, validation, and XML emission more explicitly inside the compilation engine

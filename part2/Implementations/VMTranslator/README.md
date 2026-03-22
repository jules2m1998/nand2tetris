# vm-translator
![vm-translator logo](./logo.png)

A small C# implementation of the Nand2Tetris VM translator for part 2 of the course work.

The project translates either a single `.vm` file or a directory of `.vm` files into Hack assembly while keeping the I/O flow streamed and failure-safe.

## What It Does

- Accepts either a `.vm` file path or a directory containing `.vm` files
- Supports relative and absolute paths
- Reads each source file line by line
- Writes the translated Hack assembly line by line into a sibling `.asm` file
- Stops immediately on translation errors
- Deletes the destination file if translation fails partway through
- Emits bootstrap code for directory translation and calls `Sys.init`
- Emits readable assembly comments, including structured `begin` / `end` blocks for function and program-flow commands
- Provides a help command, colored console output, and a simple loader while translation is running

## Supported VM Commands

The current translator handles:

- Memory access: `push` and `pop`
- Segments: `constant`, `local`, `argument`, `this`, `that`, `temp`, `pointer`, `static`
- Arithmetic/logical commands: `add`, `sub`, `and`, `or`, `neg`, `not`
- Comparison commands: `eq`, `gt`, `lt`
- Program flow commands: `label`, `goto`, `if-goto`
- Function commands: `function`, `call`, `return`

Each translated VM instruction is currently emitted with a leading assembly comment such as:

```asm
// push constant 7
```

Structured commands keep their block comments in the generated assembly, for example:

```asm
// call Math.add 2
// begin [call Math.add 2]
    // push return addr
    @Math.add.ret.4
    D=A
    ...
// end [call Math.add 2]
```

## Running the Translator

From the repository root:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- ./path/to/Program.vm
```

You can also pass a directory to translate all top-level `.vm` files into one assembly output named after that directory:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- ./path/to/ProgramFolder
```

You can use an absolute path for either form:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- /absolute/path/to/Program.vm
```

The generated output will be created next to the source:

```text
Program.vm  ->  Program.asm
ProgramFolder/  ->  ProgramFolder/ProgramFolder.asm
```

### Help

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- --help
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- -h
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- help
```

## Installing As `vm-translator`

The installed command name is `vm-translator`, so after installation you can run:

```bash
vm-translator ./file.vm
vm-translator ./ProgramFolder
```

The install scripts publish the app into a user-local application folder and place a small launcher command in your local bin directory.
This requires the .NET runtime/SDK to be available on the target machine.
The project logo comes from `logo.png`, and the Windows executable icon is generated from it via `logo.ico`.

## Recent Changes

- Added program-flow translation for `label`, `goto`, and `if-goto`
- Added function-level translation for `function`, `call`, and `return`
- Added directory translation with bootstrap initialization and automatic `Sys.init` call
- Fixed static symbol generation to use the actual VM source file name
- Refactored the C# translator to make the translation paths easier to read and extend
- Updated the CLI help text and runtime behavior documentation to match the current command surface

### macOS and Linux

The repository includes a Unix install script that:

- publishes the app into `~/.local/share/vm-translator`
- creates `~/.local/bin/vm-translator`

```bash
./part2/Implementations/scripts/install-vm-translator.sh
```

If `~/.local/bin` is not already on your `PATH`, add it in your shell profile:

```bash
export PATH="$HOME/.local/bin:$PATH"
```

You can override the install directory or application directory:

```bash
INSTALL_DIR="$HOME/.local/bin" APP_DIR="$HOME/.local/share/vm-translator" ./part2/Implementations/scripts/install-vm-translator.sh
```

### Windows

The PowerShell install script:

- publishes the app into `%USERPROFILE%\.local\share\vm-translator`
- creates `%USERPROFILE%\.local\bin\vm-translator.cmd`

```powershell
.\part2\Implementations\scripts\install-vm-translator.ps1
```

If needed, add that folder to your `PATH`:

```powershell
$env:PATH = "$HOME\.local\bin;$env:PATH"
```

You can override the install directory or application directory:

```powershell
.\part2\Implementations\scripts\install-vm-translator.ps1 -InstallDir "$HOME\.local\bin" -AppDir "$HOME\.local\share\vm-translator"
```

## Project Structure

```text
part2/Implementations/
├── Implementations.slnx
├── VMTranslator/
│   ├── Abstractions/
│   │   └── IVMLineTranslator.cs
│   ├── Services/
│   │   └── HackVmLineTranslator.cs
│   ├── Program.cs
│   ├── README.md
│   └── VMTranslator.csproj
├── scripts/
│   ├── install-vm-translator.ps1
│   └── install-vm-translator.sh
└── tests/
    └── VMTranslator.Tests/
        ├── HackVmLineTranslatorTests.cs
        ├── VmTranslatorProgramIntegrationTests.cs
        └── VMTranslator.Tests.csproj
```

## Testing

Build the translator project:

```bash
dotnet build part2/Implementations/VMTranslator/VMTranslator.csproj
```

Run the integration tests only:

```bash
dotnet test part2/Implementations/tests/VMTranslator.Tests/VMTranslator.Tests.csproj --filter FullyQualifiedName~VmTranslatorProgramIntegrationTests
```

Run the full test project:

```bash
dotnet test part2/Implementations/tests/VMTranslator.Tests/VMTranslator.Tests.csproj
```

The test suite currently covers:

- Memory access, arithmetic, logical, and comparison translation
- Function, call, and return translation
- Program flow translation for `label`, `goto`, and `if-goto`
- Single-file and directory CLI behavior, bootstrap emission, output generation, and failure cleanup

Publish a release build manually for the current machine:

```bash
dotnet publish part2/Implementations/VMTranslator/VMTranslator.csproj -c Release
```

## Current Limitations

- Directory translation only reads top-level `.vm` files; it does not recurse into nested folders.
- Directory bootstrap currently assumes the program should call `Sys.init`.
- The translator logic is concentrated in `HackVmLineTranslator`.
  As the project grows, it may be worth separating parsing, validation, and code generation into smaller components.

## Next Steps

- Add support for scoped labels tied to the current function context
- Extend the CLI validation and error messages for malformed multi-file program setups
- Split the translator into parser + code-writer layers

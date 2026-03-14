# vm-translator
![vm-translator logo](./logo.png)

A small C# implementation of the Nand2Tetris VM translator for part 2 of the course work.

The project currently translates a single `.vm` file into a sibling `.asm` file and focuses on streaming I/O, command-line usage, and line-by-line translation.

## What It Does

- Accepts one `.vm` file path as input
- Supports relative and absolute paths
- Reads the source file line by line
- Writes the translated Hack assembly line by line into a file with the same name and an `.asm` extension
- Stops immediately on translation errors
- Deletes the destination file if translation fails partway through
- Provides a help command, colored console output, and a simple loader while translation is running

## Supported VM Commands

The current translator handles:

- Memory access: `push` and `pop`
- Segments: `constant`, `local`, `argument`, `this`, `that`, `temp`, `pointer`, `static`
- Arithmetic/logical commands: `add`, `sub`, `and`, `or`, `neg`, `not`
- Comparison commands: `eq`, `gt`, `lt`

Each translated VM instruction is currently emitted with a leading assembly comment such as:

```asm
// push constant 7
```

## Running the Translator

From the repository root:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- ./path/to/Program.vm
```

You can also use an absolute path:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- /absolute/path/to/Program.vm
```

The generated output will be created next to the source file:

```text
Program.vm  ->  Program.asm
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
```

The install scripts publish the app into a user-local application folder and place a small launcher command in your local bin directory.
This requires the .NET runtime/SDK to be available on the target machine.
The project logo comes from `logo.png`, and the Windows executable icon is generated from it via `logo.ico`.

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

Publish a release build manually for the current machine:

```bash
dotnet publish part2/Implementations/VMTranslator/VMTranslator.csproj -c Release
```

## Current Limitations

- The CLI currently translates a single `.vm` file, not a whole directory of VM files.
- Static symbols are emitted with a placeholder file prefix like `FileName.3`.
  This is enough for the current project shape, but a multi-file translator should derive the prefix from the actual source file name.
- The translator logic is concentrated in `HackVmLineTranslator`.
  As the project grows, it may be worth separating parsing, validation, and code generation into smaller components.

## Next Steps

- Support directory-level translation
- Use the source file name automatically for static symbols
- Add bootstrap code when translating multi-file VM programs
- Split the translator into parser + code-writer layers

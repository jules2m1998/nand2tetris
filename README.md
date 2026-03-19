# nand2tetris

Personal course workspace for **Nand2Tetris / The Elements of Computing Systems**.

The repository is split into two main areas:

- `part1/`: hardware projects, assembly programs, and the Hack assembler work
- `part2/`: higher-level implementations, currently centered on the VM translator

## Repository Layout

```text
.
├── part1/
│   ├── certif.pdf
│   ├── hack-assembler/
│   │   └── HackAssembler/
│   ├── hadrware/
│   │   ├── project1/
│   │   ├── project2/
│   │   ├── project3/
│   │   └── project5/
│   └── software/
│       ├── fill/
│       ├── mult/
│       ├── project6/
│       └── test_my_hack_assembler/
└── part2/
    └── Implementations/
        ├── Implementations.slnx
        ├── VMTranslator/
        ├── tests/
        ├── scripts/
        └── submission/
```

## Part 1

`part1/` contains the lower-level course work:

- `part1/hadrware/`: HDL solutions for the hardware chapters
- `part1/software/`: assembly programs, `.hack` outputs, and small assembler test programs
- `part1/hack-assembler/HackAssembler/`: a C# Hack assembler project with its own solution, tests, assets, and README

Notes:

- The folder name `hadrware` is kept as-is to match the current repository history.
- `part1/software/project6/` contains generated `.hack` outputs used while working on the assembler.

For assembler-specific usage, build, and test details, see:
[README.md](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part1/hack-assembler/HackAssembler/README.md)

## Part 2

`part2/Implementations/` contains the current implementation work for the second half of the course.

Main areas:

- `part2/Implementations/VMTranslator/`: the VM translator application
- `part2/Implementations/tests/VMTranslator.Tests/`: unit and integration tests for the translator
- `part2/Implementations/scripts/`: helper scripts for installation and packaging
- `part2/Implementations/submission/`: course submission artifacts and generated zip files

The VM translator currently focuses on:

- translating a single `.vm` file into a sibling `.asm` file
- streaming file input and output line by line
- memory access, arithmetic, logical, comparison, and function commands
- command-line usage and packaging for local installs / course submissions

For VM translator-specific details, see:
[README.md](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/VMTranslator/README.md)

## Common Commands

Build the Hack assembler:

```bash
dotnet build part1/hack-assembler/HackAssembler/HackAssembler.slnx
```

Test the Hack assembler:

```bash
dotnet test part1/hack-assembler/HackAssembler/HackAssembler.slnx
```

Build the VM translator:

```bash
dotnet build part2/Implementations/VMTranslator/VMTranslator.csproj
```

Run the VM translator tests:

```bash
dotnet test part2/Implementations/tests/VMTranslator.Tests/VMTranslator.Tests.csproj
```

Run the VM translator from source:

```bash
dotnet run --project part2/Implementations/VMTranslator/VMTranslator.csproj -- ./path/to/Program.vm
```

## Submission Material

The repo also keeps submission-oriented files under:

- `part2/Implementations/submission/`

This area is for flat packaging and generated archives used for course uploads, separate from the main implementation source tree.

## Scope

This repository is a working course repo, not a polished framework or reusable library.
It mixes source code, tests, course artifacts, generated outputs, and submission packaging in one place so the full learning workflow stays together.

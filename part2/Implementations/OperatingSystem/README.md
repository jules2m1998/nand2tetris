# operating-system

Jack OS implementation for nand2tetris Project 12.

## Included Classes

- [Array.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Array.jack)
- [Math.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Math.jack)
- [Memory.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Memory.jack)
- [Output.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Output.jack)
- [Screen.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Screen.jack)
- [String.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/String.jack)
- [Keyboard.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Keyboard.jack)
- [Sys.jack](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/Sys.jack)

## Validation

The current implementation was checked against the official Project 12 tooling:

- `ArrayTest` passes
- `MathTest` passes
- `MemoryTest` passes
- `KeyboardTest`, `OutputTest`, `ScreenTest`, `StringTest`, and `SysTest` compile cleanly with the official `JackCompiler.sh`

## Notes

- The project is included in [Implementations.slnx](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/Implementations.slnx) through [OperatingSystem.csproj](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/OperatingSystem.csproj) so the Jack files live alongside the rest of the course work.
- [OS.zip](/Users/julesjuniormevaa/Desktop/learning/nand2tetris/part2/Implementations/OperatingSystem/OS.zip) is kept in the folder as a bundled artifact, while the `.jack` files are the editable source.

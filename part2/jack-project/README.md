# Jack Project

A small Jack game prototype built for the Nand2Tetris Part II environment.

The program draws a player sprite and a moving cactus directly into Hack screen memory, runs a simple input loop, and ends by drawing a `Game Over` banner when a collision is detected.

## Overview

- `Main.jack` owns the game loop, keyboard input, collision check, and final game-over screen
- `Player.jack` owns the player sprite, movement, and jump state
- `Cactus.jack` owns the obstacle sprite and leftward scrolling behavior
- Generated `.vm` files can be rebuilt locally when needed and are intentionally not tracked in git

## Gameplay

- The player starts near the lower-left area of the screen
- The cactus starts on the right and moves left every frame
- When the cactus reaches the left edge, it wraps back to the right side
- A collision ends the loop when the cactus overlaps the player near ground level
- The end state draws a `Game Over` message directly with `Memory.poke`

## Controls

- `Left Arrow`: move left
- `Right Arrow`: move right
- `Up Arrow`: move up
- `Down Arrow`: move down
- `Space`: jump

Notes:

- Jumping is animated over multiple frames and continues until the jump state resets
- While idle, the player is pulled left by the current game loop logic
- `Main.jack` contains a key-code `140` branch intended for exit, but that value is overwritten later in the loop by collision recomputation, so it does not currently act as a reliable quit path

## File Layout

```text
part2/jack-project/
├── Main.jack
├── Player.jack
├── Cactus.jack
├── README.md
├── .gitignore
└── scripts/
    └── create-project9-submission.sh
```

## Build

From the `part2/jack-project` directory, compile the Jack sources with:

```bash
bash ~/Downloads/nand2tetris/tools/JackCompiler.sh .
```

Generated `.vm` files are not tracked in git, so run the compiler locally after editing the `.jack` sources.

There is also a local VS Code task in `.vscode/tasks.json` named `compile jack` that runs the same compiler script.

## Run

1. Open the Nand2Tetris `VMEmulator`.
2. Load the `part2/jack-project` folder.
3. Run the program so the emulator starts from `Main.main`.

## Implementation Notes

- Rendering is done manually with `Memory.poke` rather than through higher-level drawing helpers
- Sprite placement is based on direct screen-word arithmetic
- Collision detection is intentionally simple and checks horizontal overlap plus a low-height player position
- The project is a self-contained Jack program, separate from the C# VM translator in `part2/Implementations`

## Current Limitations

- Only one obstacle type exists
- There is no score, restart flow, or menu
- Collision logic is coarse and does not use sprite-shape masking
- The quit key path is present in code but not effective in the current loop order

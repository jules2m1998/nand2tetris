// Draws a rectangular frame on the Hack screen.
//
// Screen facts:
// - SCREEN base address = 16384
// - Each row = 32 words
// - Each word = 16 horizontal pixels
// - Total screen = 256 rows
//
// This program draws:
// - top horizontal line
// - bottom horizontal line
// - left vertical line
// - right vertical line
//
// Then loops forever.

    // -------------------------
    // TOP HORIZONTAL LINE
    // Row 20, columns 4..27
    // -------------------------
    @16384
    D=A
    @640
    D=D+A          // 20 * 32 = 640
    @addr
    M=D

    @24
    D=A
    @count
    M=D

(TOP_LINE_LOOP)
    @addr
    A=M
    M=-1           // fill entire 16-pixel word

    @addr
    M=M+1

    @count
    M=M-1
    D=M
    @TOP_LINE_LOOP
    D;JGT

    // -------------------------
    // BOTTOM HORIZONTAL LINE
    // Row 120, columns 4..27
    // -------------------------
    @16384
    D=A
    @3840
    D=D+A          // 120 * 32 = 3840
    @addr
    M=D

    @24
    D=A
    @count
    M=D

(BOTTOM_LINE_LOOP)
    @addr
    A=M
    M=-1

    @addr
    M=M+1

    @count
    M=M-1
    D=M
    @BOTTOM_LINE_LOOP
    D;JGT

    // -------------------------
    // LEFT VERTICAL LINE
    // Column word index = 4
    // Rows 20..120
    // -------------------------
    @16384
    D=A
    @640
    D=D+A
    @4
    D=D+A
    @addr
    M=D

    @101
    D=A
    @count
    M=D

(LEFT_LINE_LOOP)
    @addr
    A=M
    M=-1

    @32
    D=A
    @addr
    M=D+M          // next row, same column

    @count
    M=M-1
    D=M
    @LEFT_LINE_LOOP
    D;JGT

    // -------------------------
    // RIGHT VERTICAL LINE
    // Column word index = 27
    // Rows 20..120
    // -------------------------
    @16384
    D=A
    @640
    D=D+A
    @27
    D=D+A
    @addr
    M=D

    @101
    D=A
    @count
    M=D

(RIGHT_LINE_LOOP)
    @addr
    A=M
    M=-1

    @32
    D=A
    @addr
    M=D+M

    @count
    M=M-1
    D=M
    @RIGHT_LINE_LOOP
    D;JGT

(END)
    @END
    0;JMP
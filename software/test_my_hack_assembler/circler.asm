// Draw an approximate circle on the Hack screen.
// The circle is built from horizontal segments of varying widths.

    // Row 40, width 4 words, start col 14
    @17678
    D=A
    @addr
    M=D
    @4
    D=A
    @count
    M=D
    @DRAWROW4
    0;JMP

(AFTERROW4A)

    // Row 41, width 8 words, start col 12
    @17708
    D=A
    @addr
    M=D
    @8
    D=A
    @count
    M=D
    @DRAWROW8
    0;JMP

(AFTERROW8A)

    // Row 42, width 10 words, start col 11
    @17739
    D=A
    @addr
    M=D
    @10
    D=A
    @count
    M=D
    @DRAWROW10
    0;JMP

(AFTERROW10A)

    // Row 43, width 12 words, start col 10
    @17770
    D=A
    @addr
    M=D
    @12
    D=A
    @count
    M=D
    @DRAWROW12
    0;JMP

(AFTERROW12A)

    // Row 44, width 12 words, start col 10
    @17802
    D=A
    @addr
    M=D
    @12
    D=A
    @count
    M=D
    @DRAWROW12
    0;JMP

(AFTERROW12B)

    // Row 45, width 10 words, start col 11
    @17835
    D=A
    @addr
    M=D
    @10
    D=A
    @count
    M=D
    @DRAWROW10
    0;JMP

(AFTERROW10B)

    // Row 46, width 8 words, start col 12
    @17868
    D=A
    @addr
    M=D
    @8
    D=A
    @count
    M=D
    @DRAWROW8
    0;JMP

(AFTERROW8B)

    // Row 47, width 4 words, start col 14
    @17902
    D=A
    @addr
    M=D
    @4
    D=A
    @count
    M=D
    @DRAWROW4
    0;JMP

(AFTERROW4B)

(END)
    @END
    0;JMP

// -------------------------
// Draw helpers
// -------------------------

(DRAWROW4)
    @count
    D=M
    @DRAWROW4_LOOP
    D;JGT
    @AFTERROW4A
    0;JMP

(DRAWROW4_LOOP)
    @addr
    A=M
    M=-1
    @addr
    M=M+1
    @count
    M=M-1
    D=M
    @DRAWROW4_LOOP
    D;JGT

    @AFTERROW4A
    0;JMP

(DRAWROW8)
    @count
    D=M
    @DRAWROW8_LOOP
    D;JGT
    @AFTERROW8A
    0;JMP

(DRAWROW8_LOOP)
    @addr
    A=M
    M=-1
    @addr
    M=M+1
    @count
    M=M-1
    D=M
    @DRAWROW8_LOOP
    D;JGT

    @AFTERROW8A
    0;JMP

(DRAWROW10)
    @count
    D=M
    @DRAWROW10_LOOP
    D;JGT
    @AFTERROW10A
    0;JMP

(DRAWROW10_LOOP)
    @addr
    A=M
    M=-1
    @addr
    M=M+1
    @count
    M=M-1
    D=M
    @DRAWROW10_LOOP
    D;JGT

    @AFTERROW10A
    0;JMP

(DRAWROW12)
    @count
    D=M
    @DRAWROW12_LOOP
    D;JGT
    @AFTERROW12A
    0;JMP

(DRAWROW12_LOOP)
    @addr
    A=M
    M=-1
    @addr
    M=M+1
    @count
    M=M-1
    D=M
    @DRAWROW12_LOOP
    D;JGT

    @AFTERROW12A
    0;JMP
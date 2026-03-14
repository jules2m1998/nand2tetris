// R2 = R0 * R1
// Y -> R2
@R2 // reset result to zero before excuting
M=0
@R1
D=M
@R3
M=D

(LOOP)
    // Decrement the multiplier and jump to the end if he's 0 or less
    @R3
    M=M-1
    D=M
    @ENDSECTION
    D;JLT


    @R0 // Loading from R0 and saving to D
    D=M
    @R2
    M=D+M // Adding it to R2
    @LOOP
    0;JEQ // Restart until R1 = 0 or less
(ENDSECTION)
    @0
    D=A
    @R3
    M=D
    @END
    0;JEQ

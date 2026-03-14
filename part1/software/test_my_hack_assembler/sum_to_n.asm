// Computes the sum 1 + 2 + ... + N
// N is stored in R0
// Result is stored in R1

    @10
    D=A
    @R0
    M=D        // R0 = 10

    @0
    D=A
    @R1
    M=D        // R1 = 0 (sum)

    @1
    D=A
    @i
    M=D        // i = 1

(LOOP)
    @i
    D=M
    @R0
    D=D-M
    @END
    D;JGT      // if i > N, jump to END

    @i
    D=M
    @R1
    M=D+M      // sum += i

    @i
    M=M+1      // i++

    @LOOP
    0;JMP

(END)
    @END
    0;JMP
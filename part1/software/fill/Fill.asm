(MAIN) // To check the current value in the keyboard addr
@KBD // Keyboard addr
D=M
@CLEAR_SCREEN
D;JEQ // If there is 0 in the keyboard addr goto clear the screen
@FILL_SCREEN // Else fill it
0;JMP


(CLEAR_SCREEN) // Save 0000000000000000 into the R0
D=0
@R0
M=D
@DRAW_R0
0;JMP


(FILL_SCREEN) // Save 1111111111111111 into the R0
D=-1
@R0
M=D
@DRAW_R0
0;JMP


(DRAW_R0) // Draw the content of R0 on each pixel of the screen
@8191 // The nomber of addr to fill with R0, nbr = @MAX_SCREEN - @SCREEN, where MAX_SCREEN = 24575, and 16384
      // so nbre = 24575 - 16384 = 8191
D=A
@R1
M=D // Saved nbre in R1

(DRAW_R0_LOOP) // Loop to fill each addr in [@SCREEN, @MAX_SCREEN] with R0
    @R1
    D=M
    @MAIN
    D;JLT // If R1 < 0 the drawing is over go back to main

    @SCREEN
    D=A
    @R1
    D=D+M
    @CURRENT_LOCATION
    M=D // Saved the current location to draw, current = screen + r1

    @R0
    D=M
    @CURRENT_LOCATION
    A=M
    M=D // Drawing the content of R0




    @R1 // Next addr to draw
    M=M-1
    @DRAW_R0_LOOP
    0;JMP // Loop back







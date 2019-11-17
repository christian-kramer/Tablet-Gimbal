/* 
 * File:   main.c
 * Author: Christian Kramer
 *
 * Created on November 16, 2019, 3:57 PM
 */

#define _XTAL_FREQ 8000000
#include <stdio.h>
#include <stdlib.h>
#include <xc.h>
#pragma config FOSC=INTRC_NOCLKOUT, WDTE=OFF, MCLRE=OFF, LVP=OFF    

/*
 * 
 */

char UART_Init(const long int baudrate) {
    unsigned int x;
    x = (_XTAL_FREQ - baudrate*64)/(baudrate*64);   //SPBRG for Low Baud Rate
    if (x > 255) {
        x = (_XTAL_FREQ - baudrate*16)/(baudrate*16);
        BRGH = 1;
    }
    
    if (x < 256) {
        SPBRG = x;
        SYNC = 0;
        SPEN = 1;
        CREN = 1;
        TXEN = 1;
        PIE1 |= 1ULL << 5;
        INTCON |= 1ULL << 7;
        INTCON |= 1ULL << 6;
        return 1;
    }
    return 0;
}

void UART_Write(char data)
{
  while(!TRMT);
  TXREG = data;
}
/*
char UART_Read()
{
    while(!RCIF);
    return RCREG;
}
*/
void UART_Write_Text(char *text)
{
  int i;
  for(i=0;text[i]!='\0';i++)
    UART_Write(text[i]);
}


__interrupt() void ISR(void) {
    PORTD = RCREG;
}

int main(int argc, char** argv) {
    /*
     * Setup Oscillator for 8MHz
     */
    OSCCON |= 1ULL << 6;
    OSCCON |= 1ULL << 5;
    OSCCON |= 1ULL << 4;
    
    UART_Init(9600);
    TRISD = 0x00;
    //INTCON |= 0b11000000;
    //PIE1 = 0b00100000;
    
    PORTD = 0x00;
    
    while (1) {
        UART_Write_Text("Why hello there\r\n");
        __delay_ms(1000);
        UART_Write_Text("Ayy Lmao\r\n");
        __delay_ms(1000);
    }
    return (EXIT_SUCCESS);
}


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
        CREN = 0;
        TXEN = 1;
        return 1;
    }
    return 0;
}

void UART_Write(char data)
{
  while(!TRMT);
  TXREG = data;
}

void UART_Write_Text(char *text)
{
  int i;
  for(i=0;text[i]!='\0';i++)
    UART_Write(text[i]);
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
    while (1) {
        PORTD = 0xff;
        UART_Write_Text("Why hello there\n");
        __delay_ms(1000);
        PORTD = 0x00;
        UART_Write_Text("Ayy Lmao\n");
        __delay_ms(1000);
    }
    return (EXIT_SUCCESS);
}


/* 
 * File:   main.c
 * Author: Christian Kramer
 *
 * Created on November 16, 2019, 2:20 PM
 */

#define _XTAL_FREQ 4000000

// CONFIG
#pragma config FOSC = INTOSCIO  // Oscillator Selection bits (INTOSC oscillator: I/O function on RA6/OSC2/CLKOUT pin, I/O function on RA7/OSC1/CLKIN)
#pragma config WDTE = OFF       // Watchdog Timer Enable bit (WDT disabled)
#pragma config PWRTE = OFF      // Power-up Timer Enable bit (PWRT disabled)
#pragma config MCLRE = ON       // RA5/MCLR/VPP Pin Function Select bit (RA5/MCLR/VPP pin function is MCLR)
#pragma config BOREN = OFF      // Brown-out Detect Enable bit (BOD disabled)
#pragma config LVP = OFF        // Low-Voltage Programming Enable bit (RB4/PGM pin has digital I/O function, HV on MCLR must be used for programming)
#pragma config CPD = OFF        // Data EE Memory Code Protection bit (Data memory code protection off)
#pragma config CP = OFF         // Flash Program Memory Code Protection bit (Code protection off)

#include <stdio.h>
#include <stdlib.h>
#include <xc.h>

/*
 * 
 */

char UART_Init(const long int baudrate) {
    unsigned int x;
    x = (_XTAL_FREQ - baudrate*64)/(baudrate*64);
    
    if (x > 255) {
        x = (_XTAL_FREQ - baudrate*16) / (baudrate * 16);
        //|= 1ULL << 1;
        //&= ~(1ULL << 6);
        TXSTA |= 1ULL << 2;
    }
    
    if (x < 256) {
        SPBRG = x;
        
        //0b00010000;
        TXSTA &= ~(1ULL << 4);  //  CLEAR SYNC
        
        RCSTA |= 1ULL << 7;     //  SET SPEN
        TRISB |= 1ULL << 2;     //  SET TRISB 2
        TRISB |= 1ULL << 1;     //  SET TRISB 1
        
        RCSTA |= 1ULL << 4;     //  SET CREN
        TXSTA |= 1ULL << 5;     //  SET TXEN
        
        return 1; //UART INIT SUCCESS
    }
    
    return 0;
}

void UART_Write(char data)
{
  while(!((TXSTA >> 1) & 1U));
  TXREG = data;
}

void UART_Write_Text(char *text)
{
  int i;
  for(i=0;text[i]!='\0';i++)
    UART_Write(text[i]);
}

int main(int argc, char** argv) {
    TRISB &= ~(1ULL << 4);
    UART_Init(9600);
    __delay_ms(100);
    
    while (1) {
        PORTB ^= 1UL << 4;
        UART_Write(5);
        __delay_ms(1000);
    }
    return (EXIT_SUCCESS);
}


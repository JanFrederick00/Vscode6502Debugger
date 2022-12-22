# wdcmon-debug README

This plugin allows you to debug code running on the [Western Design Center W65C02SXB single board computer.](https://wdc65xx.com/Single-Board-Computers/w65c02sxb/) in Visual Studio Code.

You do not need to use WDC's TIDE or compiler at all, as this plugin uses CA65/LD65 to compile the assembly source code.

Please note: this extension is still somewhat experimental. 

## Prerequesites

This plugin assumes that you have CC65 installed and accessible through your PATH variable. To make sure that it is correctly installed, run the command "CA65" from your command prompt. If windows tells you "'ca65' is not recognized as an internal or external command[...]", your PATH may be set incorrectly.

Connect the single-board computer to your PC via the USB port and make a note of the virtual COM-Port the device appears under.

## Example launch.json

```
{
    "version": "0.2.0",
    "configurations": [
        {
            "type": "wdcmon",
            "request": "launch",
            "name": "Launch Program",
            "program": "full/path/to/your/main/assembly/file.asm",
            "port": "your_sbc_port_eg_COM4",
            "startSymbol": "START"
        }
    ]
}
```

The startSymbol decides where the program starts executing when you click "Start Debugging" - usually that's your reset-vector.

## Features

With this plugin you can:

- Set breakpoints 
- Single-step your SBC by selecting "Step Into" or "Step Over"
- Pause the program at any time by pressing the "NMI"-Button on the device.
- See the 65C02's registers at each step
- Interactively modify the Value of the A,X,Y, and SP registers
- Interactively type code in the debug console to immediately execute it.

# How To...

### Supply a linker configuration for LD65

To use a custom linker configuration, specify the field "linkerConfig" in the launch.json. If no configuration is specified, -t none is used (LD65 default)

### Use the debug console in VS Code

When execution is halted you can use the debug console to run commands or read / write memory.

To run commands:

Simply type the assembly you want to run. This assembly is assembled and executed. When you click "continue", execution will continue regularly.
Most instructions are supported. Instructions that modify the program flow like JSR, JMP, RTS, RTI as well as the branch instructions are invalid.
The Debugger should reply with "OK.", and changes to the registers should be visible in the "Variables" window.

To read / write memory:

Monitor commands are marked by a '?' at the beginning of the line.

Numbers for the monitor can be specified in hexadecimal (by prefixing them by '$' or '0x') or decimal (default).

To read memory from a single location, simply type that location's address.
To read memory from a range of locations, type the first address, a dash ('-') and the second address (inclusive).
The monitor will print out the memory in that range.

To write memory, specify the starting address, a colon (':'), and the values to write to the memory region. You can specify multiple values which will be
written to consecutive locations in Memory.

Some examples:
```
Run an instruction:
    LDA #$00
        OK.
    TAX
        OK.
    
Read Memory:
    ? $0000 - 10
        $0000 $00 $01 $02 $03 $04 $05 $06 $07 $08 $09 $0A
    
    ? $0
        $00
        
Write Memory:

    ? $00: 1 2 0x03 4 $05 6 7 8 9 10
```

### Pause execution of the program:

To pause the program, you cannot use the "pause"-button in the IDE; The SBC is inable of listening to commands as it is executing your code. To break into the monitor, press the "NMI"-Button on the device. The debugger will notice that the program has haltet and the instruction pointer in the IDE will highlight the line the execution stopped on (the next instruction to be executed).

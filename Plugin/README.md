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

# DB25 Control Software (Arduino + Avalonia UI)

This repository contains the full DB25 control software used to interface with an Arduino-based DB25 I/O system. It includes both the Avalonia-based graphical interface and the Arduino firmware that the GUI communicates with. The intent of this project is to allow anyone, even with no prior experience, to recreate the setup, understand how it works, and modify it if needed.

The graphical interface is built using Avalonia.NET to ensure compatibility with both Linux and Windows. The `.axaml` file defines the visual layout of the interface, while the `.axaml.cs` file contains the backend logic responsible for serial communication, pin updates, gain control, and pulse control. These two files together form the complete GUI.

For users who simply want to run the software, a ready-to-launch build is included in the `DB25 Mk3 Linux.rar` file. Extracting this archive provides the compiled `.exe` along with all required runtime files. This executable contains only the GUI portion of the project. The Arduino firmware must be uploaded separately and can be found in the `sketch_mar11a/sketch_mar11a.ino` directory.

If you intend to build on top of this software or compile the GUI yourself, the process is straightforward. Begin by installing the Avalonia UI .NET package and creating a new Avalonia project in Visual Studio. Once the project is created, replace the generated `.axaml` and `.axaml.cs` files with the versions from this repository titled `MainWindow.axaml` and `MainWindow.axaml.cs` by copying their contents directly into the corresponding files in your new project. After that, open a PowerShell terminal inside the project directory and compile the application using a publish command such as:

"dotnet publish -c Release -r win-x64 --self-contained true -o "$env:USERPROFILE\Downloads"" (Without end start and end quotations)


This will generate a standalone executable that can be run on Windows without requiring any additional installations.

The Arduino firmware used by the GUI is included in this repository and is located at `sketch_mar11a/sketch_mar11a.ino`. Upload this file to your Arduino using the Arduino IDE. Once the firmware is running, the GUI will automatically communicate with the board over serial, allowing you to read DB25 input pins, write output pins, adjust gain values, and control pulse output.

In addition to the software and firmware, this repository contains a folder titled **TEAM Files**, which includes the complete set of documents, calculations, and design artifacts produced during the capstone project. This folder is organized into several subdirectories. The `01-admin` folder contains all capstone-related documentation, including the final report and every presentation delivered to the sponsor, academic advisor, and other stakeholders. This is the best resource for understanding the full development process, design decisions, and project evolution.

The `02-RF amplifier` folder contains handwritten calculations, verification work, and a Python script used to compute the required input signal for the RF system. This is especially useful if the coil parameters change, as the script can be used to quickly recompute the necessary input waveform. The `03-TR switch` folder contains CAD files related to the transmit/receive switching hardware. The `05-Coil` folder includes coil parameters and VNA screenshots used during tuning and validation. Finally, the `Capstone fair` folder contains the poster and block diagram used during the public capstone showcase.

Some redundant files exist in the repository due to earlier development stages, but all essential components are present and functional. The goal is to provide a complete and transparent reference for anyone who wants to understand, use, or extend what was accomplished by the Capstone team of 2026 for the ULF RF System. 

# Macross Software Command-Line

Macross.CommandLine is a simple and light-weight .NET Standard 2.0+ library for parsing the `string[]` command-line arguments passed into a process into a command with optional parameters, options, and switches.

## Format

The parser supports the general format used by the `dotnet` command-line interface:

`C:\application.exe command parameter1 parameter2 -option1 option1value -option2=option2value --option3 option3value -switch1`
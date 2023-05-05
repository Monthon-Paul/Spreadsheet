# Spreadsheet
Authors: Monthon Paul

Current Version: 1.2

Last Updated: 5/4/2023

# Purpose: 

This program is a simplified spreadsheet program. Its
purpose is to track words, numbers, and formulas. When
specified with an "=" in front, this will attempt to calculate
as a formula. All sheets made in this program are
saved with a .sprd in JSON format, for example "Sheet.sprd".

For more details, please look in the "About"/"How to Use" in the help tab.

# Requirement:
You would need this package for the spreadsheet functions to work: 
 
 * [ToolKit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/)
 
# How the spreadsheet works:
To add/change the contents of a cell, first, click on "Enter Content" text box and enter what you would like to add.  Then, you would need to press "enter" or 
click on the "Enter Content" button to add to the cell.  You should be able to add any number or text, but formulas should start with a "=" and have some limitations.  Formulas should contain at least one of the following: Non-negative numbers, Variables, Parentheses(To group), and the four standard operators.  
For more details on the limits, take a look in the help tab.

In the file tab, there is "New", "Open", "Save", and "Save As".  New creates a new spreadsheet, Open opens a existing spreadsheet file, Save saves the the file,
and Save As lets you save to a specific location.

In View: Screenshot and Show.  Screenshot saves a snap of the spreadsheet as a .PNG file. It will save in any User Documents folder. Show shows/hides elements in the GUI.

For more detailed explanations, take a look into the help tab in the spreadsheet.

# Extra Features
* Save As/Save: Seperate both functionality, Lets you pick a specific location to save the spreadsheet or quickly save Spreadsheet.  (Look in the help tab for more details)
* Screenshot: Captures and saves what the spreadsheet looks like.
* Show: Allows you to show/hide elements in the GUI

# How to Setup:

### [Download](https://drive.google.com/file/d/12Ku1jdMAA_dfihm_ukO74xB-jYiatIgD/view?usp=share_link) the application (MacOS only)

The Project was implemented in the .NET 7.0 Framwork & uses .NET MAUI for GUI, then require a compatible .NET SDK
This Program can be run in the Visual Studio IDE, or can be build/run by the Command line

#### Build 

First install .NET MAUI workload with the dotnet CLI 

```
dotnet workload install maui
```
Verify and install missing components with maui-check command line utility.
```
dotnet tool install -g redth.net.MAUI.check
maui-check
```

#### Run
Run the MAUI app either on Windows or MacOS (but first locate your directory for SpreadsheetGUI)

For MacOS
```
cd SpreadsheetGUI
dotnet build -t:Run -f net7.0-maccatalyst
```

For Windows
```
cd SpreadsheetGUI
dotnet build -t:Run -f net7.0-windows
```

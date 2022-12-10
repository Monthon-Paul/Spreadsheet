using System.Xml.Linq;
using SpreadsheetUtilities;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using SS;

namespace SpreadsheetGUI;

/// <summary>
/// SpreadsheetGUI for Spreadsheet
///
/// This program is a simplified spreadsheet program. Its
/// purpose is to track words, numbers, and formulas.When
/// specified with an "=" in front, this will attempt to calculate
/// as a formula. All sheets made in this program are
/// saved with a.sprd, for example "NewSheet.sprd".
/// Features include creating new, Save As/Saving,
/// and opening a Spreadsheet file. With Extra Features
///
/// Author: Monthon Paul
/// Version: November 5, 2022
/// </summary>
public partial class MainPage : ContentPage {

	// Initialize outside Variables
	private AbstractSpreadsheet sheet;
	private string fullpath;

	/// <summary>
	/// Constructor for MainPage
	/// </summary>
	public MainPage() {
		InitializeComponent();
		// Running the application creates a Spreadsheet
		this.sheet = new Spreadsheet(Valid, UpperCase, "sprd");
		// First cell to star tthe Program should be A1, therefore (0,0)
		spreadsheetGrid.SetSelection(0, 0);
		entryCell.Text = "A1";
		spreadsheetGrid.SelectionChanged += displaySelection;
	}

	/// <summary>
	/// Display the Cell name, Value and Entry Content of the GUI
	///		Cell Name:		a non-editable text box showing the cell name of
	///						the selected cell.
	///		Value:			a non-editable text box showing the cell
	///						value of the selected cell.
	///		Enter Contents:	an editable text box showing the contents of the selected cell.
	///						The User can type and edit information about the cell.
	///
	/// </summary>
	/// <param name="grid"> GUI grid of Spreadsheet</param>
	private void displaySelection(SpreadsheetGrid grid) {
		// Grab the selected Spreadsheet grid with its value
		spreadsheetGrid.GetSelection(out int col, out int row);
		spreadsheetGrid.GetValue(col, row, out string value);

		// Calculate selected cell section to a Cell name
		string cellname = NumtoAlpha(col) + (row + 1);

		// On Mouse click, the entry Text box should change due to the sheet
		entryCell.Text = cellname;
		entryValue.Text = value;
		entryContent.Focus();

		// if the cell is a Formula, add the "=" to the front, Otherwise display Value in String
		if (sheet.GetCellContents(cellname) is Formula) {
			entryContent.Text = "=" + sheet.GetCellContents(cellname).ToString();
		} else {
			entryContent.Text = sheet.GetCellContents(cellname).ToString();
		}
	}

	/// <summary>
	/// Button click:	When entering it content, Run the application in displaying onto the
	///					Spreadheet Grid, the Grid should contains it Value with each specifc Textbox
	///					displaying it correct entry.
	/// </summary>
	/// <param name="sender">Pointer to the Button</param>
	/// <param name="e">triggle an event</param>
	private void OnClicked(Object sender, EventArgs e) {
		try {
			// If clicking on the button if the Enter Content text box is empty, tell the User to enter something first
			if (entryContent.Text is null) {
				DisplayAlert("Enter Content", "Please enter a Content in the text box", "OK");
				return;
			}

			//Grabs the selected cell
			spreadsheetGrid.GetSelection(out int col, out int row);
			sheet.SetContentsOfCell(entryCell.Text, entryContent.Text);

			// Check if it's a number or string, then set it Value for Calculating, update GUI display
			if (sheet.GetCellValue(entryCell.Text) is double || sheet.GetCellValue(entryCell.Text) is string) {
				spreadsheetGrid.SetValue(col, row, sheet.GetCellValue(entryCell.Text).ToString());
				spreadsheetGrid.GetValue(col, row, out string value);
				entryValue.Text = value;

				//Update previous Cell Values for recalculation
				foreach (string cellname in sheet.GetNamesOfAllNonemptyCells()) {
					//ascii calculation for cellname to rows & col
					int AlphatoNum = cellname[0] - 65;
					string numString = cellname.Substring(1, cellname.Length - 1);
					int.TryParse(numString, out int num);

					//Plot the value to its cellname in the spreadsheet
					spreadsheetGrid.SetValue(AlphatoNum, num - 1, sheet.GetCellValue(cellname).ToString());
					if (sheet.GetCellValue(cellname) is SpreadsheetUtilities.FormulaError) {
						spreadsheetGrid.SetValue(AlphatoNum, num - 1, "FormulaError");
					}
				}
			} else {
				// If it's a FormulaError, set it to be a Formula Error, then display an alert
				spreadsheetGrid.SetValue(col, row, "FormulaError");
				spreadsheetGrid.GetValue(col, row, out string value);
				entryValue.Text = value;
			}
		}
		//If there was any problems, Reverse its Content
		catch (Exception ex) when (ex is FormulaFormatException || ex is CircularException) {
			if (sheet.GetCellContents(entryCell.Text) is Formula) {
				entryContent.Text = "=" + sheet.GetCellContents(entryCell.Text).ToString();
			} else {
				entryContent.Text = sheet.GetCellContents(entryCell.Text).ToString();
			}
			// Specify what type of problem to display to the User
			switch (ex) {
				case FormulaFormatException:
					DisplayAlert("InvalidFormulaFormat", "Please enter a valid formula into the Spreadsheet.", "OK");
					break;
				case CircularException:
					DisplayAlert("CircularException", "Please enter a formula that doesn't go full circle.", "OK");
					break;
			}
		}
	}
	/// <summary>
	/// On Complete for the entry text box.
	/// i.e press "Enter" key (Windows) or "Return" key (MacOS) on Complete
	/// </summary>
	/// <param name="sender">Pointer to the entry</param>
	/// <param name="e"> trigger an event</param>
	private void EnterKey(Object sender, EventArgs e) {
		// same function as the button click
		OnClicked(sender, e);
	}

	/// <summary>
	/// Creates a new Spreadsheet. Details that if there are any Unsaved Changes,
	/// ask the User if wanting to save is an option. If not, generate a new Spreadsheet.
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e"> trigger an event</param>
	private async void NewClicked(Object sender, EventArgs e) {
		// Check that if there are any changes in the Spreadsheet.
		if (sheet.Changed) {
			//Asks the user if they want to save or not, cancel feature so that it doesn't clear
			string confirm = await DisplayActionSheet("Unsaved changes, Would you like to save changes?", "Cancel", null, "Yes", "No");
			// User select Cancel does nothing
			if (confirm is "Cancel") {
				return;
				// User wants to save Spreadsheet
			} else if (confirm is "Yes") {
				// checks if there is already a saved path, to just quickly save
				if (File.Exists(fullpath)) {
					SaveClicked(sender, e);
					goto Clear;
				}
				// Ask the User for a Full file path, if they pick "cancel", just return back with unsave changes
				var PathFile = await DisplayPromptAsync("Path Location", "Enter a a file Path for where to save",
					accept: "OK", cancel: "Cancel", placeholder: "Enter Path");
				// To make sure that there is a entry for the name of the fullpath
				// If Cancel for any reason, do nothing
				if (PathFile is null) {
					return;
				}

				// Ask the User to name the file to save, if they pick "cancel", just return back with unsave changes
				var save = await DisplayPromptAsync("Save File", "Enter a File name for the Spreadhseet",
					accept: "save", cancel: "cancel", placeholder: "Enter Spreadsheet Name");
				// To make sure that there is a entry for the name of the file
				// If Cancel for any reason, do nothing
				if (save is null) {
					return;
				}
				// Try to save, if the Path is not valid, display a Alert to the User
				string filename = save + ".sprd";
				fullpath = Path.Combine(PathFile, filename);
				try {
					sheet.Save(fullpath);
				} catch (Exception) {
					await DisplayAlert("Fail to Save", "Please enter a valid path to save your file, nothing is going to change.", "OK");
					return;
				}
			}
		}
		//Clears and creates a new spreadsheet
		Clear:
		entryValue.Text = "";
		entryContent.Text = "";
		spreadsheetGrid.Clear();
		sheet = new Spreadsheet(Valid, UpperCase, "sprd");
	}

	/// <summary>
	/// Saves the spreadsheet,
	/// If the spreadsheet already has a path,
	/// quickly save the contents &update old file.
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private void SaveClicked(Object sender, EventArgs e) {
		//Takes the already save path and update the spreadsheet
		if (sheet.Changed) {
			if (!File.Exists(fullpath)) {
				SaveAsClicked(sender, e);
				return;
			}
			sheet.Save(fullpath);
		}
	}

	/// <summary>
	/// Let the User save the spreadsheet at a specific location.
	/// The spreadsheet will ask you for a path & name and it'll create a new file.
	/// However, if there is already a file, it'll update the old file.
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private async void SaveAsClicked(Object sender, EventArgs e) {
		// if Changes are ture start to save, Otherwise do nothing
		if (sheet.Changed) {
			SaveAgain:
			// Ask the User for file path, if they pick "cancel", just return back with unsave changes
			var PathFile = await DisplayPromptAsync("Path Location", "Enter a file Path for where to save",
				accept: "OK", cancel: "cancel", placeholder: "Enter Path");
			// To make sure that there is a entry for the name of the fullpath
			// If Cancel for any reason, do nothing
			if (PathFile is null) {
				return;
			}

			// Ask the User to name the file, if they pick "cancel", just return back with unsave changes
			var save = await DisplayPromptAsync("Save File", "Enter a File name for the Spreadhseet",
				accept: "save", cancel: "cancel", placeholder: "Enter Spreadsheet Name");
			// To make sure that there is a entry for the name of the file
			// If Cancel for any reason, do nothing
			if (save is null) {
				return;
			}
			// Try to save, if the Path is not valid, display a Alert to the User
			string filename = save + ".sprd";
			fullpath = Path.Combine(PathFile, filename);
			try {
				sheet.Save(fullpath);
			} catch (Exception) {
				// Ask the User if the wanting to Save again because of in valid path
				if (await DisplayAlert("Fail to Save", "Please enter a valid path to save your file, " +
					"Do you want to save again?", "Yes", "Cancel")) {
					goto SaveAgain;
				}
			}
		}
	}

	/// <summary>
	/// Opens any file as text and prints its contents.
	/// Note the use of async and await, concepts we will learn more about
	/// later this semester.
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private async void OpenClicked(Object sender, EventArgs e) {
		//Checks if the spreadsheet has been changed
		if (sheet.Changed) {
			SaveAgain:
			//Asks the user if they want to save
			if (await DisplayAlert("Unsaved Changes", "Would you like to save changes?", "Yes", "No")) {
				// checks if there is already a saved path, to just quickly save
				if (File.Exists(fullpath)) {
					SaveClicked(sender, e);
					goto OpenFile;
				}

				// Ask the User for file path, if they pick "cancel", just return back with unsave changes
				var PathFile = await DisplayPromptAsync("Path Location", "Enter a a file Path for where to save",
					accept: "OK", cancel: "cancel", placeholder: "Enter Path");
				// To make sure that there is a entry for the name of the fullpath
				// If Cancel for any reason, go to clear then open
				if (PathFile is null) {
					goto OpenFile;
				}
				//Ask the User to name the file, if they pick "cancel", just return back with unsave changes
				var save = await DisplayPromptAsync("Saving file", "Enter file name:",
					accept: "save", cancel: "cancel", placeholder: "Enter Spreadsheet Name");
				// To make sure that there is a entry for the name of the file
				// If Cancel for any reason, go to clear then open
				if (save is null) {
					goto OpenFile;
				}
				// Try to save, if the Path is not valid, display a Alert to the User
				string filename = save + ".sprd";
				fullpath = Path.Combine(PathFile, filename);
				try {
					sheet.Save(fullpath);
				} catch (Exception) {
					// Ask the User if the wanting to Save again because of in valid path
					if (await DisplayAlert("Fail to Save", "Please enter a valid path to save your file, " +
						"Do you want to save again?", "Yes", "Cancel")) {
						goto SaveAgain;
					}
				}
			}
		}
		//Opens the file
		OpenFile:
		try {
			//Grabs the name of the file
			FileResult fileResult = await FilePicker.Default.PickAsync();
			//Makes sure that there is a name for the file or that the file open ends with ".sprd"
			// Otherwise re-display previous sheet
			if (fileResult != null) {
				if (!fileResult.FileName.EndsWith("sprd")) {
					throw new Exception();
				}
				Console.WriteLine("Successfully chose file: " + fileResult.FileName);
				fullpath = fileResult.FullPath;
				string fileContents = File.ReadAllText(fileResult.FullPath);
				sheet = new Spreadsheet(fileResult.FullPath, Valid, UpperCase, "sprd");
				// When creating new sheet, clear the Spreadsheet first
				spreadsheetGrid.Clear();
			} else {
				//If the User dosen't select any file
				Console.WriteLine("No file selected.");
			}
		}
		//If there anything that went wrong with opening/plotting the file, revert back the sheet.
		catch (Exception ex) {
			Console.WriteLine("Error opening file:");
			await DisplayAlert("Error Opening File", "Please open a file ending in \".sprd\"", "OK");
			Console.WriteLine(ex);
		}

		//Grabs the value and name of the cell and plots them into the spreadsheet
		foreach (string cellname in sheet.GetNamesOfAllNonemptyCells()) {
			//Changes the letters to number & grabing the number for Cell
			//ascii calculation for cellname to rows & col
			int AlphatoNum = cellname[0] - 65;
			string numString = cellname.Substring(1, cellname.Length - 1);
			int.TryParse(numString, out int num);
			//Plot the value to its cellname in the spreadsheet
			spreadsheetGrid.SetValue(AlphatoNum, num - 1, sheet.GetCellValue(cellname).ToString());
		}
		//Keep track with the current cell selected
		displaySelection(new());
	}

	/// <summary>
	/// For this program, it allow the User to take a screenshot
	///	of the Spreadsheet, it will then save to any computer that 
	/// has a Document folder, therefore allowing for Crossplatform for the User.
	/// </summary>
	/// <param name="sender">pointer to MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private async void Screencapture(Object sender, EventArgs e) {
		// take a screenshot
		var screenshot = await Spreadsheet.CaptureAsync();
		var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		// Turn into a MemoryStream
		using MemoryStream memory = new();
		await screenshot.CopyToAsync(memory);

		// Ask the User to name the screenshot
		var screen = await DisplayPromptAsync("Screenshot Name", "Enter a name for Screenshot of Spreadhseet",
					accept: "Ok", cancel: "Cancel", placeholder: "Enter Screenshot Name");
		// If "Cance", do nothing
		if (screen is null) {
			return;
		}
		// Save it into a path and write the .PNG file
		var fullpath = Path.Combine(path, screen + ".png");

		File.WriteAllBytes(fullpath, memory.ToArray());
	}

	/// <summary>
	/// Allow the User to hide contents of Enter Cell, name and entry box
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private void ShowCell(Object sender, EventArgs e) {
		// it's visible first, therefore hide it, vise versa
		if (LabelCell.IsVisible && entryCell.IsVisible) {
			LabelCell.IsVisible = false;
			entryCell.IsVisible = false;
		} else {
			LabelCell.IsVisible = true;
			entryCell.IsVisible = true;
		}

	}

	/// <summary>
	/// Allow the User to hide contents of Enter Cell, name and entry box
	/// </summary>
	/// <param name="sender">pointer to MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private void ShowValue(Object sender, EventArgs e) {
		// it's visible first, therefore hide it, vise versa
		if (LabelValue.IsVisible && entryValue.IsVisible) {
			LabelValue.IsVisible = false;
			entryValue.IsVisible = false;
		} else {
			LabelValue.IsVisible = true;
			entryValue.IsVisible = true;
		}
	}

	/// <summary>
	/// Display a Popup info about the Spreadsheet.
	/// Details explain the Purpose, Author, Version, & last updated Date of the Program
	/// </summary>
	/// <param name="sender">Pointer to the MenuFlyoutItem</param>
	/// <param name="e">trigger an event</param>
	private void AboutClicked(Object sender, EventArgs e) {
		// Load HTML format for information
		string HTML = LoadHtml("About.html");
		// Add a button functions, i.e close popup
		Button button = new Button {
			Text = "Close",
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			WidthRequest = 500,
			HeightRequest = 50,
			BackgroundColor = new Color(245, 245, 245)
		};

		// Display a Popup displaying "about" the Program
		Popup about = new Popup() {
			CanBeDismissedByTappingOutsideOfPopup = false,
			Size = new Size(500, 500),
			Content = new StackLayout {
				BackgroundColor = new Color(255, 255, 255),
				Children = {
					//Using  HTML format for information
					new WebView {
						HeightRequest= 450,
						Source = new HtmlWebViewSource {
							Html = HTML
						}
					},
					button, // show button
				}
			}
		};
		// Event to trigger the close of the popup
		button.Clicked += (sender, args) => about.Close();
		this.ShowPopup(about);
	}

	/// <summary>
	/// Display a Popup info How to Use the Spreadsheet.
	/// Details explain Each section in the Menu Flyout.
	/// File explain of how to use New Save, Open, Save As.
	/// Help gives details on About & How to use.
	/// Entry Properties on Cell name, Value, Content, Spreadsheet/grid.
	/// Explantion on Functionality of the Program Formula.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void HTUClicked(Object sender, EventArgs e) {
		// Load HTML format for information
		string HTML = LoadHtml("HowToUse.html");
		// Add a button functions, i.e close popup
		Button button = new Button {
			Text = "Close",
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			WidthRequest = 520,
			HeightRequest = 50,
			BackgroundColor = new Color(245, 245, 245) // Whitesmoke
		};

		// Display a Popup displaying "How to Use" the Program
		Popup HTU = new Popup() {
			CanBeDismissedByTappingOutsideOfPopup = false,
			Size = new Size(520, 700),
			Content = new StackLayout {
				BackgroundColor = new Color(255, 255, 255), // White
				Children = {
					//Using  HTML format for information
					new WebView {
						HeightRequest= 650,
						Source = new HtmlWebViewSource {
							Html = HTML
						}
					},
					button, // show button
				}
			}
		};
		button.Clicked += (sender, args) => HTU.Close();
		this.ShowPopup(HTU);
	}

	/// <summary>
	/// Load the HTML file located in "Resources/Raw/**"
	/// Using StreamReader, locate the html file in system then read the file
	/// to save onto a string.
	/// </summary>
	/// <param name="html">Specifc file for html opening</param>
	private string LoadHtml(string html) {
		using var stream = FileSystem.OpenAppPackageFileAsync(html);
		using var reader = new StreamReader(stream.Result);

		string HTML = reader.ReadToEnd();
		return HTML;
	}

	/// <summary>
	/// Takes the inputted string and makes them upper case
	/// </summary>
	/// <param name="s">The string to change to upper case</param>
	/// <returns>The string that has been changed to upper case</returns>
	private string UpperCase(string s) {
		return s.ToUpper();
	}

	/// <summary>
	/// Changes the inputted integer and changes it to a letter
	/// </summary>
	/// <param name="i">The int to change</param>
	/// <returns>The letter that is associated to the int</returns>
	private string NumtoAlpha(int i) {
		char letter = (char) (i + 65);
		return Char.ToString(letter);
	}

	/// <summary>
	/// Checks if the string is valid
	/// </summary>
	/// <param name="s">The string to make sure that is valid</param>
	/// <returns>True if it's valid, Otherwise False </returns>
	private bool Valid(string s) {
		return Char.IsUpper(s, 0);
	}
}

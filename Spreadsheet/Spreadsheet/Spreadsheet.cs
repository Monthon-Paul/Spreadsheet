using SpreadsheetUtilities;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SS;
/// <summary>
/// Spreadsheet class represents a mapping of variable names to cells,
/// where its content is store in the cell class.
/// 
/// A spreadsheet contains a cell corresponding to every possible cell name.  (This
/// means that a spreadsheet contains an infinite number of cells.)  In addition to 
/// a name, each cell has a contents and a value.  The distinction is important.
/// 
/// The contents of a cell can be a string, double, or Formula. If the
/// contents is an empty string, we say that the cell is empty string. With
/// Spreadsheet, it can get the names of the cell name, it's content of a specific cell name,
/// And set the cell name to a specific content it holds. Just like either Google Spreadsheet
/// or Microsoft Excel.
///
/// Author: Monthon Paul
/// Version: September 17 2023
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class Spreadsheet : AbstractSpreadsheet {

	// Initialize variables
	[JsonProperty(PropertyName = "cells")]
	private readonly Dictionary<string, Cell> spreadsheet;
	private readonly DependencyGraph graph;
	private bool change;

	//  4 args Contructor for Spreadsheet
	public Spreadsheet(string pathToFile, Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version) {
		// Initialize graph & spreadsheet
		spreadsheet = new();
		graph = new();
		// Convert a file to a Spreadsheet i.e load a file to Spreadsheet
		try {
			//Read the file that makes a json to an acutal spreadsheet
			string json = File.ReadAllText(pathToFile);
			Spreadsheet? sheet = JsonConvert.DeserializeObject<Spreadsheet>(json) ?? throw new SpreadsheetReadWriteException("Trouble opening, reading, & writing file");
			// Check if the version match
			if (!sheet.Version.Equals(version)) {
				throw new SpreadsheetReadWriteException("Wrong version, this version does not match the fileName version.");
			}
			// If any errors along the way to set graph & value, throw exception
			try {
				foreach (string cell in sheet.GetNamesOfAllNonemptyCells()) {
					SetContentsOfCell(cell, (string) sheet.GetCellContents(cell));
				}
			} catch (Exception) {
				throw new SpreadsheetReadWriteException("Spreadsheet can't load & write. Either wrong name or Dependency error");
			}
		} catch (Exception) {
			throw new SpreadsheetReadWriteException("Trouble opening, reading, & writing file");
		}
		change = false;
	}
	// Regular constructor
	public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version) {
		// Initialize graph & spreadsheet
		spreadsheet = new();
		graph = new();
		change = false;
	}
	// Default Constructor
	public Spreadsheet() :
		this(x => true, x => x, "default") {
	}

	/// <summary>
	/// Writes the contents of this spreadsheet to the named file using a JSON format.
	/// The JSON object should have the following fields:
	/// "Version" - the version of the spreadsheet software (a string)
	/// "cells" - an object containing 0 or more cell objects
	///           Each cell object has a field named after the cell itself 
	///           The value of that field is another object representing the cell's contents
	///               The contents object has a single field called "stringForm",
	///               representing the string form of the cell's contents
	///               - If the contents is a string, the value of stringForm is that string
	///               - If the contents is a double d, the value of stringForm is d.ToString()
	///               - If the contents is a Formula f, the value of stringForm is "=" + f.ToString()
	/// 
	/// If there are any problems opening, writing, or closing the file, the method should throw a
	/// SpreadsheetReadWriteException with an explanatory message.
	/// </summary>
	/// <param name="filename">file name being save as</param>
	/// <exception cref="SpreadsheetReadWriteException">Throws an exception if the Spreadsheet fail to save</exception>
	public override void Save(string filename) {
		// try to save the spreadsheet as a file
		try {
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);

			File.WriteAllText(filename, json);
		} catch (Exception) {
			throw new SpreadsheetReadWriteException("Problems on opening, writing, or closing the file");
		}
		Changed = false;
	}

	public override bool Changed { get => change; protected set => change = value; }

	/// <summary>
	/// Enumerates the names of all the non-empty cells in the spreadsheet.
	/// </summary>
	/// <returns>an Eneramable of names of cells names that are non-empty</returns>
	public override IEnumerable<string> GetNamesOfAllNonemptyCells() {
		return spreadsheet.Keys;
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
	/// value should be either a string, a double, or a Formula.
	/// </summary>
	/// <param name="name"> the Cell name</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns> the content of the cell, either be string, double, or Formula</returns>
	public override object GetCellContents(string name) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }

		string normalize = Normalize(name);
		// return the contents of the cell
		// Check if the Cell contains contents, if not return empty string
		if (!spreadsheet.ContainsKey(normalize)) {
			return "";
		}
		return spreadsheet[normalize].Content;
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
	/// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
	/// </summary>
	/// <param name="name"> the Cell name</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns> the value of the cell, either be string, double, or SpreadsheetUtilities.FormulaError</returns>
	public override object GetCellValue(string name) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }

		string normalize = Normalize(name);
		// return the value of the cell
		// Check if the Cell contains value, if not return empty string
		if (!spreadsheet.ContainsKey(normalize)) {
			return "";
		}

		return spreadsheet[normalize].Value;
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, if content parses as a double, the contents of the named
	/// cell becomes that double.
	/// 
	/// Otherwise, if content begins with the character '=', an attempt is made
	/// to parse the remainder of content into a Formula f using the Formula
	/// constructor.  There are then three possibilities:
	/// 
	///   (1) If the remainder of content cannot be parsed into a Formula, a 
	///       SpreadsheetUtilities.FormulaFormatException is thrown.
	///       
	///   (2) Otherwise, if changing the contents of the named cell to be f
	///       would cause a circular dependency, a CircularException is thrown,
	///       and no change is made to the spreadsheet.
	///       
	///   (3) Otherwise, the contents of the named cell becomes f.
	/// 
	/// Otherwise, the contents of the named cell becomes content.
	/// 
	/// If an exception is not thrown, the method returns a list consisting of
	/// name plus the names of all other cells whose value depends, directly
	/// or indirectly, on the named cell. The order of the list should be any
	/// order such that if cells are re-evaluated in that order, their dependencies 
	/// are satisfied by the time they are evaluated.
	/// </summary>
	/// <param name="name"> The Cell name</param>
	/// <param name="content"> String content to set in the Cell</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns>a list of of cell name & all other cells whose value depends</returns>
	public override IList<string> SetContentsOfCell(string name, string content) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }

		// get the return list maybe?
		IList<string> recalc = new List<string>();
		// Normalize the name (i.e change the Cell name)
		string normalize = Normalize(name);

		// Check either a number, Formula, lastly a String
		// Do correct algorithm
		if (double.TryParse(content, out double d)) {
			recalc = SetCellContents(normalize, d);
		} else if (content.StartsWith("=")) {
			Formula f = new(content[1..], Normalize, IsValid);
			recalc = SetCellContents(normalize, f);
		} else {
			recalc = SetCellContents(normalize, content);
		}

		Changed = true; // made changes

		// check if it's empty being set, therefore a clear option
		if (content is "") {
			spreadsheet.Remove(normalize);
			recalc.Remove(normalize);
		}
		// go through dependents list to evaluate to get the value
		foreach (string cellname in recalc) {
			if (spreadsheet[cellname].Content is string || spreadsheet[cellname].Content is double) {
				spreadsheet[cellname].Value = GetCellContents(normalize);
				// Evaulate to get a double or FormulaError for Formula
			} else if (spreadsheet[cellname].Content is Formula f) {
				spreadsheet[cellname].Value = f.Evaluate(Lookup);
			}
		}
		return recalc;
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, the contents of the named cell becomes number.  The method returns a
	/// list consisting of name plus the names of all other cells whose value depends, 
	/// directly or indirectly, on the named cell.
	/// </summary>
	/// <param name="name">Cell name</param>
	/// <param name="number">Content is a number</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns>a list of of cell name & all other cells whose value depends</returns>
	protected override IList<string> SetCellContents(string name, double number) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }

		// Check if the cell have any dependees,
		// if so replace it with an empty set
		// (to clear it dependents & dependees)
		if (graph.HasDependees(name)) {
			graph.ReplaceDependees(name, new HashSet<string>());
		}

		// Set the name of Cell content to be a double number
		// If it doesn't exist add a new Cell with content
		if (!spreadsheet.ContainsKey(name)) {
			spreadsheet.Add(name, new Cell(number));
		} else {
			spreadsheet[name].Content = number;
		}

		// return a list of dependent cells
		return GetCellsToRecalculate(name).ToList();
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, the contents of the named cell becomes text.  The method returns a
	/// list consisting of name plus the names of all other cells whose value depends, 
	/// directly or indirectly, on the named cell.
	/// </summary>
	/// <param name="name">Cell name</param>
	/// <param name="text">Content is a string</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns>a list of of cell name & all other cells whose value depends</returns>
	protected override IList<string> SetCellContents(string name, string text) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }

		// Check that setting an empty string, doesn't set it.
		if (text is "") {
			return GetCellsToRecalculate(name).ToList();
		}

		// Check if the cell have any dependees,
		// if so replace it with an empty set
		// (to clear it dependents & dependees)
		if (graph.HasDependees(name)) {
			graph.ReplaceDependees(name, new HashSet<string>());
		}

		// Set the name of Cell content to be a string text
		// If it doesn't exist add a new Cell with content
		if (!spreadsheet.ContainsKey(name)) {
			spreadsheet.Add(name, new Cell(text));
		} else {
			spreadsheet[name].Content = text;
		}

		// return a list of dependent cells
		return GetCellsToRecalculate(name).ToList();
	}

	/// <summary>
	/// If name is invalid, throws an InvalidNameException.
	/// 
	/// Otherwise, if changing the contents of the named cell to be the formula would cause a 
	/// circular dependency, throws a CircularException, and no change is made to the spreadsheet.
	/// 
	/// Otherwise, the contents of the named cell becomes formula.  The method returns a
	/// list consisting of name plus the names of all other cells whose value depends,
	/// directly or indirectly, on the named cell.
	/// </summary>
	/// <param name="name">Cell name</param>
	/// <param name="formula">Content is a Formula</param>
	/// <exception cref="InvalidNameException">Throws an invalid name as it is not the correct format</exception>
	/// <returns>a list of of cell name & all other cells whose value depends</returns>
	protected override IList<string> SetCellContents(string name, Formula formula) {
		// if name is invalid then throw exception
		if (!isCellname(name)) { throw new InvalidNameException(); }
		// Keep track previous Dependees
		IEnumerable<string> prevDependees = graph.GetDependees(name);
		object prevContent = ""; // Represent Empty Cell

		// Setting the cell name of content, it needs to update the graph.
		// Replace the Cell with new formula Variables.
		// (since replacing would update the DG for dependents & dependees)
		graph.ReplaceDependees(name, formula.GetVariables());

		// Set the name of Cell content to be a Formula value
		// If it doesn't exist add a new Cell with content
		if (!spreadsheet.ContainsKey(name)) {
			spreadsheet.Add(name, new Cell(formula));

		} else {
			prevContent = spreadsheet[name].Content;
			spreadsheet[name].Content = formula;
		}

		// First try to return a list of dependent cells
		// Special Case: Otherwise a circular dependency
		try {
			return GetCellsToRecalculate(name).ToList();
		} catch (CircularException e) {
			//if circular exist, reverse it back or delete the contents
			spreadsheet[name].Content = prevContent;
			if (prevContent is "") {
				spreadsheet.Remove(name);
			}
			graph.ReplaceDependees(name, prevDependees);
			throw e;
		}
	}

	/// <summary>
	/// Returns an enumeration, without duplicates, of the names of all cells whose
	/// values depend directly on the value of the named cell.  In other words, returns
	/// an enumeration, without duplicates, of the names of all cells that contain
	/// formulas containing name.
	/// </summary>
	/// <param name="name">Cell name</param>
	/// <returns>return a Enumerable of Dependents of cell name</returns>
	protected override IEnumerable<string> GetDirectDependents(string name) {
		return graph.GetDependents(name);
	}

	/// <summary>
	/// Make sure Cell name is valid, according to AbstractSpreadsheet Rules
	/// </summary>
	/// <param name="name"> the name of the cell that is checking to make sure is valid </param>
	/// <returns>True if it's the correct format, otherwise false</returns>
	private bool isCellname(string name) {
		string normalize = Normalize(name);
		return MyRegex().IsMatch(normalize) && IsValid(normalize);
	}

	/// <summary>
	/// This method is to passed into the Formula class,
	/// the formula can lookup the value of the 
	/// cell at the time of evalutaion. 
	/// </summary>
	/// <param name="name"> Cell name</param>
	/// <exception cref="ArgumentException"> either it is empty or not a number</exception>
	/// <returns>a number for Value</returns>
	private double Lookup(string name) {
		// Needs to be a Cell name and must be in the Dictionary.
		if (!isCellname(name) || !spreadsheet.ContainsKey(name)) {
			throw new ArgumentException("Either empty or not a Cell name");
		}
		// Check if it's a number, get the value from Cell name
		if (spreadsheet[name].Value is double) {
			return (double) spreadsheet[name].Value;
		}
		// Lastly it's content is not a number just throw Exception
		throw new ArgumentException("it's not a number");
	}

	/// <summary>
	/// Cell class for maping the names of variable to a single piece of content
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	private class Cell {
		//Initialize contents of the cell
		private object content;
		private object value;

		/// <summary>
		/// Constructor of the cell.
		/// </summary>
		/// <param name="content"> the content must be either be a string, double, or Formula </param>
		public Cell(object content) {
			this.content = content;
			value = content;
		}

		/// <summary>
		/// Getters and Setters for the content
		/// </summary>
		public object Content {
			get { return content; }
			set { content = value; }
		}

		/// <summary>
		/// Getters and Setters for the Value
		/// </summary>
		public object Value {
			get { return value; }
			set { this.value = value; }
		}

		/// <summary>
		/// Json stringForm content
		/// </summary>
		[JsonProperty(PropertyName = "stringForm")]
		private string StringForm {
			get {
				if (content is double d) {
					return d.ToString();
				} else if (content is Formula f) {
					return "=" + f.ToString();
				}
				return (string) content;
			}
			set {
				content = value;
				this.value = value;
			}
		}
	}

	[GeneratedRegex("^[a-zA-Z]+\\d+$")]
	private static partial Regex MyRegex();
}

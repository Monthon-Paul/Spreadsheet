using System.Text.RegularExpressions;

namespace SpreadsheetUtilities;
/// <summary>
/// Represents formulas written in standard infix notation using standard precedence
/// rules.  The allowed symbols are non-negative numbers written using double-precision 
/// floating-point syntax (without unary preceeding '-' or '+'); 
/// variables that consist of a letter or underscore followed by 
/// zero or more letters, underscores, or digits; parentheses; and the four operator 
/// symbols +, -, *, and /.  
/// 
/// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
/// a single variable, "x y" consists of two variables "x" and "y"; "x23" is a single variable; 
/// and "x 23" consists of a variable "x" and a number "23".
/// 
/// Associated with every formula are two delegates:  a normalizer and a validator.  The
/// normalizer is used to convert variables into a canonical form, and the validator is used
/// to add extra restrictions on the validity of a variable (beyond the standard requirement 
/// that it consist of a letter or underscore followed by zero or more letters, underscores,
/// or digits.)  Their use is described in detail in the constructor and method comments.
/// </summary>
/// 
/// Author: Monthon Paul
/// Version: September 17 2023
public partial class Formula {

	// Initialize Outside Variables
	private string formulaExp;
	private string[] tokens;
	private HashSet<string> variables;

	/// <summary>
	/// Creates a Formula from a string that consists of an infix expression written as
	/// described in the class comment.  If the expression is syntactically invalid,
	/// throws a FormulaFormatException with an explanatory Message.
	/// 
	/// The associated normalizer is the identity function, and the associated validator
	/// maps every string to true.  
	/// </summary>
	public Formula(string formula) :
		this(formula, s => s, s => true) {
	}

	/// <summary>
	/// Creates a Formula from a string that consists of an infix expression written as
	/// described in the class comment.  If the expression is syntactically incorrect,
	/// throws a FormulaFormatException with an explanatory Message.
	/// 
	/// The associated normalizer and validator are the second and third parameters,
	/// respectively.  
	/// 
	/// If the formula contains a variable v such that normalize(v) is not a legal variable, 
	/// throws a FormulaFormatException with an explanatory message. 
	/// 
	/// If the formula contains a variable v such that isValid(normalize(v)) is false,
	/// throws a FormulaFormatException with an explanatory message.
	/// 
	/// Suppose that N is a method that converts all the letters in a string to upper case, and
	/// that V is a method that returns true only if a string consists of one letter followed
	/// by one digit.  Then:
	/// 
	/// new Formula("x2+y3", N, V) should succeed
	/// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
	/// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
	/// </summary>
	/// <param name="normalize">Function that takes a given string to return a string</param>
	/// <param name="isValid">Function that check is given string is true or false</param>
	public Formula(string formula, Func<string, string> normalize, Func<string, bool> isValid) {
		// Initialize Variables first.
		formulaExp = "";
		variables = new();

		// Get Each token from the string formula. From Operators, Values, & Variables
		tokens = GetTokens(formula).ToArray();

		// Check if the String is empty
		if (tokens.Length is 0) {
			throw new FormulaFormatException("Enter in a non-empty formula.");
		}
		// Is it following proper formatting rules? Go through each statement.
		// If it doesn't follow the rules, throw an FormulFormatException
		if (!VerifyingTokens(normalize, isValid)) {
			throw new FormulaFormatException("Not a formula. Enter in a proper formula.");
		}
		if (!FormulaValid()) {
			throw new FormulaFormatException("Invalid Formula format. Enter in a valid formula format.");
		}
	}

	/// <summary>
	/// Go through each string token of the formula list
	/// into verifying if it's an Operator, Number Value, or Variable.
	/// If token is doesn't follow one of the rules, return false.
	/// Otherwise return true.
	/// </summary>
	/// <param name="normalize">Function that takes a given string to return a string</param>
	/// <param name="isValid">Function that check is given string is true or false</param>
	/// <returns></returns>
	private bool VerifyingTokens(Func<string, string> normalize, Func<string, bool> isValid) {
		// Generate temporary token list
		List<string> tempTokens = new();
		// Go through the Original Formula of tokens to check
		// if it's an Operator, Number Value, or Variable
		foreach (string ele in tokens) {
			//Check if it's a Operator
			if (OpRegex().IsMatch(ele)) {
				tempTokens.Add(ele);
				// Check if it's a number
			} else if (double.TryParse(ele, out double d)) {
				tempTokens.Add(d.ToString());
			} else if (IsVariable(ele)) {
				// If it's a variable, pass in Normalize
				// Then check if it's valid to add to the temp.
				string n = normalize(ele);
				if (isValid(n)) {
					tempTokens.Add(n);
					variables.Add(n);
				}
			} else {
				// if it matches none.
				return false;
			}
		}
		// Copy it to reassigned the list of tokens
		tokens = tempTokens.ToArray();
		return true;
	}

	/// <summary>
	/// Check if given string is a Variable.
	/// </summary>
	/// <param name="var"> Variable string</param>
	/// <returns>True if it's a Variable, Otherwise false</returns>
	private static bool IsVariable(string var) {
		return VarRegex1().IsMatch(var);
	}

	/// <summary>
	/// 1. One Token Rule: There must be at least one token. 
	/// 
	/// 2. Right Parentheses Rule: at no point should the number of closing
	/// parentheses seen so far be greater than the number of opening parentheses seen so far.
	/// 
	/// 3. Balanced Parentheses Rule: The total number of opening parentheses must equal the total number of closing parentheses.
	/// 
	/// 4. Starting Token Rule: The first token of an expression must be a number, a variable, or an opening parenthesis.
	/// 
	/// 5. Ending Token Rule: The last token of an expression must be a number, a variable, or a closing parenthesis.
	/// 
	/// 6. Parenthesis/Operator Following Rule: Any token that immediately follows an opening parenthesis or an operator
	/// must be either a number, a variable, or an opening parenthesis.
	/// 
	/// 7. Extra Following Rule: Any token that immediately follows a number, a variable, or a closing parenthesis
	/// must be either an operator or a closing parenthesis.
	/// </summary>
	/// <returns>Return true if the Formula Format correctly, Otherwise False</returns>
	private bool FormulaValid() {
		// Initialize variables
		int rightPar = 0;
		int leftPar = 0;
		double d;

		// One Token Rule
		if (tokens.Length is 1) {
			// Must be either a number or Variable
			if (double.TryParse(tokens[0], out d)) {
				formulaExp = d.ToString();
				return true;
			} else if (IsVariable(tokens[0])) {
				formulaExp = tokens[0];
				return true;
				// It can't be an Operator
			} else if (OpRegex().IsMatch(tokens[0])) {
				return false;
			}
		}
		//Starting Token Rule
		if (!(IsVariable(tokens[0]) || double.TryParse(tokens[0], out _) || tokens[0] is "(")) {
			return false;
		}
		// Ending Token Rule
		if (!(IsVariable(tokens[^1]) || double.TryParse(tokens[^1], out _) || tokens[^1] is ")")) {
			return false;
		}
		// Go through to check each tokens from Formula to see if it matches the ruleset
		for (int i = 0; i < tokens.Length; i++) {
			string nextele;
			// Keep track of the the ele before index
			// if it gets till the end (with nothing before index)
			// Break the loop.
			if (i != tokens.Length - 1) {
				nextele = tokens[i + 1];
				// Check taht before is a parenthese to add.
			} else if (tokens[i] is ")") {
				rightPar++;
				break;
			} else {
				break;
			}
			// Switch Cases for Parenthesis
			switch (tokens[i]) {
				case "(":
					//Left parenthesis next element should only be either a number, a variable, or an opening parenthesis.
					if (!(IsVariable(nextele) || double.TryParse(nextele, out _) || nextele is "(")) {
						return false;
					}
					// add on
					leftPar++;
					break;
				case ")":
					// Right parenthesis next element should only be either an Operator or an closing parenthesis.
					if (!MyRegex1().IsMatch(nextele)) {
						return false;
					}
					// add on
					rightPar++;
					break;
				default:
					break;
			}
			// Operator Following Rule: must be either a number, a variable, or an opening parenthesis.
			if (MyRegex2().IsMatch(tokens[i])) {
				if (!(IsVariable(nextele) || double.TryParse(nextele, out _) || nextele is "(")) {
					return false;
				}
			}
			// Any token that immediately follows a number, a variable
			// must be either an operator or a closing parenthesis.
			if (double.TryParse(tokens[i], out _) || IsVariable(tokens[i])) {
				if (!MyRegex1().IsMatch(nextele)) {
					return false;
				}
			}
		}
		// Balanced Parenthesis Rule / Right Parentheses Rule: if there are not equal than it's not balance
		if (leftPar != rightPar) {
			return false;
		}

		// Assign Formula format to a String.
		formulaExp = string.Concat(tokens);
		return true;
	}

	/// <summary>
	/// Evaluates this Formula, using the lookup delegate to determine the values of
	/// variables.  When a variable symbol v needs to be determined, it should be looked up
	/// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
	/// the constructor.)
	/// 
	/// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
	/// in a string to upper case:
	/// 
	/// new Formula("x+7", N, s => true).Evaluate(L) is 11
	/// new Formula("x+7").Evaluate(L) is 9
	/// 
	/// Given a variable symbol as its parameter, lookup returns the variable's value 
	/// (if it has one) or throws an ArgumentException (otherwise).
	/// 
	/// If no undefined variables or divisions by zero are encountered when evaluating 
	/// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
	/// The Reason property of the FormulaError should have a meaningful explanation.
	///
	/// This method should never throw an exception.
	/// </summary>
	/// <returns>Return a double number if Evaluated, Otherwise Formula Error for either divide by 0 or undefined variables</returns>
	public object Evaluate(Func<string, double> lookup) {
		// Initialize variables
		double number;
		Stack<double> value = new();
		Stack<string> operators = new();
		// An Algorithm to loop for each String to add on the stacks to perform Calculations
		foreach (string s in tokens) {
			// Case 1: Check given token matches one or more letters followed by one or more digits
			if (IsVariable(s)) {
				// Try to lookup the formula. if the formula doesn't exist
				// throw an exeception, but catch it to return FormulaError
				try {
					number = lookup(s);
					if (!IntegerAlgo(operators, value, number)) {
						return new FormulaError("Divide by Zero");
					}
				} catch (Exception) {
					return new FormulaError("Variable has no value");
				}
				continue;
			}

			// Case 2: Check if the token is a number
			if (double.TryParse(s.Trim(), out number)) {
				// Check if it can calculate the operation
				// In case of Divide by Zero
				if (!IntegerAlgo(operators, value, number)) {
					return new FormulaError("Divide by Zero");
				}
				continue;
			}
			// Case 3: Check if the token is an operator
			else if (OpRegex().IsMatch(s)) {
				// Go to a specific case in order to perform calculation
				switch (s) {
					case "(":
					case "*":
					case "/":
						operators.Push(s);
						break;
					case "+":
					case "-":
						if (operators.Count == 0) {
							operators.Push(s);
							continue;
						}
						// Perform add/subtract calculation
						OperatorAlgo(operators, value, "+", "-");
						operators.Push(s);
						break;
					case ")":
						// Perform add/subtract calculation
						OperatorAlgo(operators, value, "+", "-");

						// check for a complete parentheses
						if (operators.Peek() == "(" && s == ")") {
							operators.Pop();
						}
						// Check that if the next operator for Calculation
						// Check if it can calculate the operation
						// In case of Divide by Zero
						if (!OperatorAlgo(operators, value, "*", "/")) {
							return new FormulaError("Divide by Zero");
						}
						break;
				}
			}
		}
		// if there is no operator, finalize in returning single solution
		if (operators.Count is 0) {
			return value.Pop();
		}
		// return a finilize solution when 2 values and 1 operator
		// Perform  a calculation to return a solution.
		// In case one of the Valuse will never be 0
		double firstnum = value.Pop();
		Calculation(firstnum, value.Pop(), operators.Pop(), out double calc);
		return calc;
	}

	/// <summary>
	/// Calculation on given operators of addition, subtraction, multiplicaiton, & Division
	/// </summary>
	/// <param name="number">One of the numbers</param>
	/// <param name="secnum">Second of the numbers</param>
	/// <param name="op">operators for given calculation</param>
	/// <param name="result">Return by Refference the calculation number</param>
	/// <returns>Return ture if calculated, Otherwise False</returns>
	private bool Calculation(double number, double secnum, string op, out double result) {
		result = 0;
		switch (op) {
			case "/":
				// Special case into Dividing by 0
				if (number is 0) {
					return false;
				}
				result = secnum / number;
				break;
			case "*":
				result = secnum * number;
				break;
			case "+":
				result = secnum + number;
				break;
			case "-":
				result = secnum - number;
				break;
		}
		return true;
	}

	/// <summary>
	/// An Algorithm to perform the necessary calculation
	/// when the case is an floating point number
	/// </summary>
	/// <param name="operators">Operator stack </param>
	/// <param name="value"> floating point number stack</param>
	/// <param name="number">specific token which is an double value</param>
	/// <returns>return true if gone through the Algo, Otherwise False</returns>
	private bool IntegerAlgo(Stack<string> operators, Stack<double> value, double number) {
		if (operators.Count != 0) {
			// Perform neccessary floating point number Calculation
			// Special Case for if Divid by Zero.
			if (operators.Peek() == "/" || operators.Peek() == "*") {
				// Check if operator has any number
				if (Calculation(number, value.Pop(), operators.Pop(), out double calc)) {
					value.Push(calc);
					return true;
				}
				return false;
			}
		}
		value.Push(number);
		return true;
	}

	/// <summary>
	/// An Algorithm to perform the necessary Operator calculations
	/// </summary>
	/// <param name="operators"> Stack of operators</param>
	/// <param name="value"> Stack of int values</param>
	/// <param name="ope"> first Operator</param>
	/// <param name="secop"> Second Operator</param>
	/// <returns>return true if gone through the Algo, Otherwise False</returns>
	private bool OperatorAlgo(Stack<string> operators, Stack<double> value, string ope, string secop) {
		double firstnum;
		if (operators.Count != 0) {
			// compare the top of Operators stack with specific opperators.
			// as ope and secop can be :+-/*
			if (operators.Peek() == ope || operators.Peek() == secop) {
				firstnum = value.Pop();
				// Check if it can calculate the operation
				// Special Case for if Divid by Zero.
				if (Calculation(firstnum, value.Pop(), operators.Pop(), out double calc)) {
					value.Push(calc);
					return true;
				}
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Enumerates the normalized versions of all of the variables that occur in this 
	/// formula.  No normalization may appear more than once in the enumeration, even 
	/// if it appears more than once in this Formula.
	/// 
	/// For example, if N is a method that converts all the letters in a string to upper case:
	/// 
	/// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
	/// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
	/// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
	/// </summary>
	/// <returns>An Enumerable of given Variables</returns>
	public IEnumerable<string> GetVariables() {
		return variables;
	}

	/// <summary>
	/// Returns a string containing no spaces which, if passed to the Formula
	/// constructor, will produce a Formula f such that this.Equals(f).  All of the
	/// variables in the string should be normalized.
	/// 
	/// For example, if N is a method that converts all the letters in a string to upper case:
	/// 
	/// new Formula("x + y", N, s => true).ToString() should return "X+Y"
	/// new Formula("x + Y").ToString() should return "x+Y"
	/// </summary>
	/// <returns>the String of Formula</returns>
	public override string ToString() {
		return formulaExp;
	}

	/// <summary>
	/// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
	/// whether or not this Formula and obj are equal.
	/// 
	/// Two Formulae are considered equal if they consist of the same tokens in the
	/// same order.  To determine token equality, all tokens are compared as strings 
	/// except for numeric tokens and variable tokens.
	/// Numeric tokens are considered equal if they are equal after being "normalized" 
	/// by C#'s standard conversion from string to double, then back to string. This 
	/// eliminates any inconsistencies due to limited floating point precision.
	/// Variable tokens are considered equal if their normalized forms are equal, as 
	/// defined by the provided normalizer.
	/// 
	/// For example, if N is a method that converts all the letters in a string to upper case:
	///  
	/// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
	/// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
	/// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
	/// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
	/// </summary>
	/// <returns>return true if Equals, otherwise False</returns>
	public override bool Equals(object? obj) {
		// Check if the object is a Formula and not a null value
		if (obj is not Formula || obj is null) {
			return false;
		}
		// Cast the obj, Then check if it has the right HashCode number & right ToString
		Formula f = (Formula) obj;
		if (this.GetHashCode != f.GetHashCode && ToString() != f.ToString()) {
			return false;
		}
		return true;
	}

	/// <summary>
	/// Reports whether f1 == f2, using the notion of equality from the Equals method.
	/// Note that f1 and f2 cannot be null, because their types are non-nullable
	/// </summary>
	/// <returns>True if Equals, Otherwise False</returns>
	public static bool operator ==(Formula f1, Formula f2) {
		return f1.Equals(f2);
	}

	/// <summary>
	/// Reports whether f1 != f2, using the notion of equality from the Equals method.
	/// Note that f1 and f2 cannot be null, because their types are non-nullable
	/// </summary>
	/// <returns>True if not Equals, Otherwise False</returns>
	public static bool operator !=(Formula f1, Formula f2) {
		// Do the opposite or Equals
		return !f1.Equals(f2);
	}

	/// <summary>
	/// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
	/// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
	/// randomly-generated unequal Formulae have the same hash code should be extremely small.
	/// </summary>
	/// <returns>a number representation of Formula</returns>
	public override int GetHashCode() {
		return formulaExp.GetHashCode();
	}

	/// <summary>
	/// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
	/// right paren; one of the four operator symbols; a string consisting of a letter or underscore
	/// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
	/// match one of those patterns.  There are no empty tokens, and no token contains white space.
	/// </summary>
	/// <returns> an Enumerable of Tokens</returns>
	private static IEnumerable<string> GetTokens(string formula) {
		// Patterns for individual tokens
		string lpPattern = @"\(";
		string rpPattern = @"\)";
		string opPattern = @"[\+\-*/]";
		string varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
		string doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
		string spacePattern = @"\s+";

		// Overall pattern
		string pattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
										lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

		// Enumerate matching tokens that don't consist solely of white space.
		foreach (string s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace)) {
			if (!MyRegex().IsMatch(s)) {
				yield return s;
			}
		}

	}

	[GeneratedRegex("^[()/*+-]$")]
	private static partial Regex OpRegex();

	[GeneratedRegex("^[a-zA-Z_]+[a-zA-Z_\\d]*$")]
	private static partial Regex VarRegex1();

	[GeneratedRegex("^\\s*$", RegexOptions.Singleline)]
	private static partial Regex MyRegex();

	[GeneratedRegex("^[)/*+-]$")]
	private static partial Regex MyRegex1();

	[GeneratedRegex("^[/*+-]$")]
	private static partial Regex MyRegex2();
}

/// <summary>
/// Used to report syntactic errors in the argument to the Formula constructor.
/// </summary>
public class FormulaFormatException : Exception {
	/// <summary>
	/// Constructs a FormulaFormatException containing the explanatory message.
	/// </summary>
	public FormulaFormatException(string message)
		: base(message) {
	}
}

/// <summary>
/// Used as a possible return value of the Formula.Evaluate method.
/// </summary>
public struct FormulaError {
	/// <summary>
	/// Constructs a FormulaError containing the explanatory reason.
	/// </summary>
	/// <param name="reason"></param>
	public FormulaError(string reason)
		: this() {
		Reason = reason;
	}

	/// <summary>
	///  The reason why this FormulaError was created.
	/// </summary>
	public string Reason { get; private set; }
}

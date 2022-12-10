using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq.Expressions;

namespace FormulaEvaluator;
/// <summary>
/// This class represents evaluating an Evaluator for a String expression
/// Resulting in returning a value solution.
/// Works with variables too also perform Calculations.
///
/// Author: Monthon Paul
/// Version: Semptember 5, 2022
/// </summary>
public class Evaluator {

	// Creating the delegate
	public delegate int Lookup(String v);

	/// <summary>
	/// <c>Evaluate</c> A method that evaluates arithmetic expressions using standard infix notation.
	/// It should respect the usual precedence rules.
	/// The evaluator will support expressions with variables whose values are looked up via a delegate.
	/// </summary>
	/// <param name="exp">String of arithmetic expressions</param>
	/// <returns>Calculation of the arithmetic expressions</returns>
	/// <exception cref="ArgumentException"></exception>
	public static int Evaluate(String? exp, Lookup variableEvaluator) {
		// First check for null exeception
		if (exp == null || exp == "") {
			throw new ArgumentException("Expression is null");
		}
		int number, firstnum = 0;
		// initiate the stacks to construct
		Stack<int> value = new Stack<int>();
		Stack<String> operators = new Stack<string>();

		// Seperate and trim any unnesscary leading and trailing whitespace
		String[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");


		// An Algorithm to loop for each String to add on the stacks to perform Calculations
		foreach (String s in substrings) {
			// Ignore the extra String & Whitespace
			if (s == "" || s == " ") {
				continue;
			}

			// Case 1: Check given token matches one or more letters followed by one or more digits
			if (Regex.IsMatch(s.Trim(), @"[A-Za-z]+[0-9]+")) {
				number = variableEvaluator(s.Trim());
				IntegerAlgo(operators, value, number);
				continue;
			}

			// Case 2: Check if the token is a number
			if (int.TryParse(s.Trim(), out number)) {
				IntegerAlgo(operators, value, number);
				continue;
			}
			// Case 3: Check if the token is an operator
			else if (Regex.IsMatch(s, @"[()/*+-]")) {
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
						// Throw an error where it's not a complete parentheses
						try {
							if (operators.Peek() == "(" && s == ")") {
								operators.Pop();
							}
						} catch (Exception e) {
							e = new ArgumentException("Incomplete parentheses structure");
							throw e;
						}

						// Check that if the next operator for Calculation
						OperatorAlgo(operators, value, "*", "/");
						break;
				}
			}
			// Case 4: any specific token that doesn't match
			else {
				throw new ArgumentException("Having a character that is not valid for Expression");
			}
		}
		// if there is no operator, finalize in returning single solution
		// else if there is an operator, check that value stack has 2 values
		// Otherwise, throw an error
		if (operators.Count == 0) {
			return value.Pop();
		} else if (value.Count < 2 || value.Count > 2) {
			throw new ArgumentException("either too many operators");
		}
		// return a finilize solution when 2 values and 1 operator
		// Perform  a calculation to return a solution.
		firstnum = value.Pop();
		return Calculation(firstnum, value.Pop(), operators.Pop());
	}

	/// <summary>
	/// Calculation on given operators of addition, subtraction, multiplicaiton, & Division
	/// </summary>
	/// <param name="number">One of the numbers</param>
	/// <param name="secnum">Second of the numbers</param>
	/// <param name="op">operators for given calculation</param>
	/// <returns>Calculate integer value</returns>
	/// <exception cref="ArgumentException">
	/// For special case of parentheses being a operator or divide by zero
	/// </exception>
	private static int Calculation(int number, int secnum, string op) {
		int result = 0;
		switch (op) {
			case "/":
				if (number == 0) {
					throw new ArgumentException("divide by 0 = Error");
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
			case "(":
			case ")":
				throw new ArgumentException("Can't use parentheses to do operations");
		}
		return result;
	}

	/// <summary>
	/// An Algorithm to perform the necessary calculation
	/// when the case is and integer
	/// </summary>
	/// <param name="operators">Operator stack </param>
	/// <param name="value"> integer stack</param>
	/// <param name="number">specif token which is an int value</param>
	/// <exception cref="ArgumentException"> throw an error when getting a specific value for stacks</exception>
	private static void IntegerAlgo(Stack<String> operators, Stack<int> value, int number) {
		// Check if operator has any number
		int calc = 0;
		if (operators.Count != 0) {
			// In a special case for Negative numbers, it will be in a stack
			// but check if there is a int value in the value stack 
			if (Regex.IsMatch(operators.Peek(), @"[/*+-]") && value.Count == 0) {
				throw new ArgumentException("Unary Negative or exta operator with no value");
			} else if (operators.Peek() == "/" || operators.Peek() == "*") {
				calc = Calculation(number, value.Pop(), operators.Pop());
				value.Push(calc);
				return;
			}
		}
		value.Push(number);
	}

	/// <summary>
	/// An Algorithm to perform the necessary Operator calculations
	/// </summary>
	/// <param name="operators"> Stack of operators</param>
	/// <param name="value"> Stack of int values</param>
	/// <param name="ope"> first Operator</param>
	/// <param name="secop"> Second Operator</param>
	/// <exception cref="ArgumentException">
	/// Throw an error where operators doesn't have a peek or no 2 values from stack
	/// </exception>
	private static void OperatorAlgo(Stack<String> operators, Stack<int> value, String ope, String secop) {
		int firstnum, calc = 0;
		try {
			if (operators.Count != 0) {
				// compare the top of Operators stack with specific opperators.
				if (operators.Peek() == ope || operators.Peek() == secop) {
					firstnum = value.Pop();
					calc = Calculation(firstnum, value.Pop(), operators.Pop());
					value.Push(calc);
				}
			}
		} catch (Exception e) {
			e = new ArgumentException("either too many operators or no value for the expression");
			throw e;
		}
	}
}
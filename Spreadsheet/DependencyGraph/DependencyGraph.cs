using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadsheetUtilities;
/// <summary>
/// (s1,t1) is an ordered pair of strings
/// t1 depends on s1; s1 must be evaluated before t1
/// 
/// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
/// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
/// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
/// set, and the element is already in the set, the set remains unchanged.
/// 
/// Given a DependencyGraph DG:
/// 
///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
///        (The set of things that depend on s)    
///        
///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
///        (The set of things that s depends on) 
//
// For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
//     dependents("a") = {"b", "c"}
//     dependents("b") = {"d"}
//     dependents("c") = {}
//     dependents("d") = {"d"}
//     dependees("a") = {}
//     dependees("b") = {"a"}
//     dependees("c") = {"a"}
//     dependees("d") = {"b", "d"}
/// </summary>
///
/// Author: Monthon Paul
/// Version: September 10, 2022
public class DependencyGraph {

	// Initialize private variables
	// Two Dictionary containing no duplicate values
	private Dictionary<string, HashSet<string>> dependents;
	private Dictionary<string, HashSet<string>> dependees;
	private int countPairs;

	/// <summary>
	/// Creates an empty DependencyGraph.
	/// </summary>
	public DependencyGraph() {
		dependents = new();
		dependees = new();
		countPairs = 0;
	}

	/// <summary>
	/// The number of ordered pairs in the DependencyGraph.
	/// </summary>
	/// <returns> number of pairs</returns>
	public int Size {
		get { return this.countPairs; }
	}

	/// <summary>
	/// The size of dependees(s).
	/// This property is an example of an indexer.  If dg is a DependencyGraph, you would
	/// invoke it like this:
	/// dg["a"]
	/// It should return the size of dependees("a")
	/// </summary>
	/// <param name="s"> s is key of dependees</param>
	/// <returns> length of dependees maping</returns>
	public int this[string s] {
		get {
			HashSet<string>? set = new();
			if (dependees.TryGetValue(s, out set)) {
				return set.Count;
			}
			return 0;
		}
	}

	/// <summary>
	/// Reports whether dependents(s) is non-empty.
	/// </summary>
	/// <param name="s"> s is key of dependent</param>
	/// <returns> Does Dependents have values or not</returns>
	public bool HasDependents(string s) {
		//Checks to see if it is empty,
		if (!dependents.ContainsKey(s)) {
			return false;
		}
		return dependents[s].Count != 0;
	}

	/// <summary>
	/// Reports whether dependees(s) is non-empty.
	/// </summary>
	/// <param name="s"> s is key of dependee</param>
	/// <returns> Does Dependees have values or not</returns>
	public bool HasDependees(string s) {
		//Checks to see if it is empty,
		if (!dependees.ContainsKey(s)) {
			return false;
		}
		return dependees[s].Count != 0;
	}

	/// <summary>
	/// Enumerates dependents(s).
	/// </summary>
	/// <param name="s"> s is key of dependent</param>
	/// <returns> an Enumerable of given dependents</returns>
	public IEnumerable<string> GetDependents(string s) {
		// try get the set of dependent key, otherwise return an empty set
		HashSet<string>? set = new();
		if (dependents.TryGetValue(s, out set)) {
			return set;
		}
		return new HashSet<string>();

	}

	/// <summary>
	/// Enumerates dependees(s).
	/// </summary>
	/// <param name="s"> </param>
	/// <returns> an Enumerable of given dependees</returns>
	public IEnumerable<string> GetDependees(string s) {
		// try get the set of dependees key, otherwise return an empty set
		HashSet<string>? set = new();
		if (dependees.TryGetValue(s, out set)) {
			return set;
		}
		return new HashSet<string>();
	}

	/// <summary>
	/// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
	/// 
	/// <para>This should be thought of as:</para>   
	/// 
	///   t depends on s
	///
	/// </summary>
	/// <param name="s"> s must be evaluated first. T depends on S</param>
	/// <param name="t"> t cannot be evaluated until s is</param>
	public void AddDependency(string s, string t) {
		// Check if either s or t  doesn't contains in Dictionaries
		// map both variables in specific Dictionary
		if (!dependents.ContainsKey(s)) {
			dependents.Add(s, new HashSet<string>());
			dependents[s].Add(t);
			countPairs++;
		}
		if (!dependees.ContainsKey(t)) {
			dependees.Add(t, new HashSet<string>());
			dependees[t].Add(s);
		}
		// If already contains s or t, map their specific
		// varible to Dictionary, ignoring dupplicates
		if (!dependents[s].Contains(t)) {
			dependents[s].Add(t);
			countPairs++;
		}
		if (!dependees[t].Contains(s)) {
			dependees[t].Add(s);
		}
	}

	/// <summary>
	/// Removes the ordered pair (s,t), if it exists
	/// </summary>
	/// <param name="s"> s must be evaluated first. T depends on S</param>
	/// <param name="t"> t cannot be evaluated until s is</param>
	public void RemoveDependency(string s, string t) {
		// Check if the Dictionaries contains s or t,
		// remove there specific value
		// speacial case: if the Dictionary does not contain the value
		// set a bool value.
		bool isremove = false;
		if (dependents.ContainsKey(s)) {
			isremove = dependents[s].Remove(t);
		}
		if (dependees.ContainsKey(t)) {
			isremove = dependees[t].Remove(s);
		}
		// If the s or t key have no mapping
		// remove the key.
		if (!HasDependents(s)) {
			dependents.Remove(s);
		}
		if (!HasDependees(t)) {
			dependees.Remove(t);
		}
		// update pairs if there is a removal
		if (isremove) {
			countPairs--;
		}
	}

	/// <summary>
	/// Removes all existing ordered pairs of the form (s,r).  Then, for each
	/// t in newDependents, adds the ordered pair (s,t).
	/// </summary>
	/// <param name="s"> s is the key that it's map need replace</param>
	/// <param name="newDependents"> an Enumeralbe new elements for replacement</param>
	public void ReplaceDependents(string s, IEnumerable<string> newDependents) {
		// Get the s key map to there set,
		// Go through unmapping the s key to dependees
		HashSet<string>? set = new();
		if (dependents.TryGetValue(s, out set)) {
			foreach (string val in set) {
				dependees[val].Remove(s);
				// Check that dependee key have any value to remove the key
				if (dependees[val].Count == 0) {
					dependees.Remove(val);
				}
				countPairs--;
			}
			// Clear the values map to key s
			dependents.Remove(s);
			dependents.Add(s, new HashSet<string>());
			foreach (string t in newDependents) {
				// map s to the each element of newDependents
				dependents[s].Add(t);
				// map to Dependees as well
				if (dependees.ContainsKey(t)) {
					dependees[t].Add(s);
				} else {
					dependees.Add(t, new HashSet<string>());
					dependees[t].Add(s);
				}
				countPairs++;
			}
		} else {
			// if s key doesn't exist, add on to Dependents
			// map the new key s to new Dependents elements
			dependents.Add(s, new HashSet<string>());
			foreach (string t in newDependents) {
				// map s to the each element of newDependents
				dependents[s].Add(t);
				//  Map to Dependees as well
				if (dependees.ContainsKey(t)) {
					dependees[t].Add(s);
				} else {
					dependees.Add(t, new HashSet<string>());
					dependees[t].Add(s);
				}
				countPairs++;
			}
		}
	}

	/// <summary>
	/// Removes all existing ordered pairs of the form (r,s).  Then, for each 
	/// t in newDependees, adds the ordered pair (t,s).
	/// </summary>
	/// <param name="s"> s is the key that it's map need replace</param>
	/// <param name="newDependees"> an Enumeralbe new elements for replacement</param>
	public void ReplaceDependees(string s, IEnumerable<string> newDependees) {
		// Get the s key map to there set,
		// Go through unmapping the s key to dependents
		HashSet<string>? set = new();
		if (dependees.TryGetValue(s, out set)) {
			foreach (string val in set) {
				dependents[val].Remove(s);
				// Check that dependents key have any value to remove the key
				if (dependents[val].Count == 0) {
					dependents.Remove(val);
				}
				countPairs--;
			}
			// Clear the values map to key s
			dependees.Remove(s);
			dependees.Add(s, new HashSet<string>());
			foreach (string t in newDependees) {
				// map s to the each element of newDependents
				dependees[s].Add(t);
				// map to Dependents as well
				if (dependents.ContainsKey(t)) {
					dependents[t].Add(s);
				} else {
					dependents.Add(t, new HashSet<string>());
					dependents[t].Add(s);
				}
				countPairs++;
			}
		} else {
			// if s key doesn't exist, add on to Dependees
			// map the new key s to new Dependees elements
			dependees.Add(s, new HashSet<string>());
			foreach (string t in newDependees) {
				// map s to the each element of newDependents
				dependees[s].Add(t);
				//  Map to Dependents as well
				if (dependents.ContainsKey(t)) {
					dependents[t].Add(s);
				} else {
					dependents.Add(t, new HashSet<string>());
					dependents[t].Add(s);
				}
				countPairs++;
			}
		}
	}
}


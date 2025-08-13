using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.IO
{
    /// <summary>
    /// Responsible for reading and parsing Linear Programming model input files
    /// This class converts text file format into LinearProgrammingModel objects
    /// 
    /// Expected file format:
    /// Line 1: [max|min] [+|-][coefficient] [+|-][coefficient] ... (objective function)
    /// Line 2-N: [+|-][coeff] [+|-][coeff] ... [<=|>=|=] [rhs] (constraints)
    /// Last line: [+|-|urs|int|bin] [+|-|urs|int|bin] ... (variable types)
    /// </summary>
    public class InputReader
    {
        private string _filePath;

        /// <summary>
        /// Constructor - initializes the InputReader with a file path
        /// </summary>
        /// <param name="filePath">Path to the input file containing the LP model</param>
        public InputReader(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        /// <summary>
        /// Main method to read and parse the entire LP model from file
        /// This orchestrates the parsing of objective function, constraints, and variable types
        /// </summary>
        /// <returns>Complete LinearProgrammingModel object ready for solving</returns>
        /// <exception cref="FileNotFoundException">When input file doesn't exist</exception>
        /// <exception cref="InvalidDataException">When file format is incorrect</exception>
        public LinearProgrammingModel ReadModel()
        {
            try
            {
                // Check if file exists before attempting to read
                if (!File.Exists(_filePath))
                {
                    throw new FileNotFoundException($"Input file not found: {_filePath}");
                }

                // Read all lines from the file
                string[] lines = File.ReadAllLines(_filePath);

                // Validate minimum file structure (objective + at least one constraint + variable types)
                if (lines.Length < 3)
                {
                    throw new InvalidDataException("File must contain at least 3 lines: objective function, one constraint, and variable types");
                }

                // Create new model instance
                var model = new LinearProgrammingModel();

                // Parse each section of the file
                ParseObjectiveFunction(lines[0], model);
                ParseConstraints(lines, model);
                ParseVariableTypes(lines[lines.Length - 1], model);

                // Validate the constructed model
                if (!model.IsValid())
                {
                    throw new InvalidDataException("Parsed model is invalid - check variable counts and constraint structure");
                }

                Console.WriteLine($"✓ Successfully loaded model with {model.VariableCount} variables and {model.ConstraintCount} constraints");
                return model;
            }
            catch (Exception ex)
            {
                // Re-throw with context about which file failed
                throw new Exception($"Failed to read model from {_filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses the first line containing the objective function
        /// Format: [max|min] [+|-][coefficient] [+|-][coefficient] ...
        /// Example: "max +2 +3 +3 +5 +2 +4"
        /// </summary>
        /// <param name="objectiveLine">First line of the input file</param>
        /// <param name="model">Model to populate with objective data</param>
        private void ParseObjectiveFunction(string objectiveLine, LinearProgrammingModel model)
        {
            if (string.IsNullOrWhiteSpace(objectiveLine))
            {
                throw new InvalidDataException("Objective function line cannot be empty");
            }

            // Split the line into tokens (words separated by spaces)
            string[] tokens = objectiveLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 2)
            {
                throw new InvalidDataException("Objective function must have at least optimization type and one coefficient");
            }

            // First token determines if we're maximizing or minimizing
            string objectiveType = tokens[0].ToLower();
            if (objectiveType == "max")
            {
                model.ObjectiveType = ObjectiveType.Maximize;
            }
            else if (objectiveType == "min")
            {
                model.ObjectiveType = ObjectiveType.Minimize;
            }
            else
            {
                throw new InvalidDataException($"Objective type must be 'max' or 'min', found: {tokens[0]}");
            }

            // Parse coefficients (remaining tokens after objective type)
            for (int i = 1; i < tokens.Length; i++)
            {
                try
                {
                    // Parse coefficient - handle signs explicitly
                    double coefficient = ParseCoefficient(tokens[i]);
                    model.ObjectiveCoefficients.Add(coefficient);

                    // Create corresponding variable with default name
                    string variableName = $"x{i}"; // x1, x2, x3, etc.
                    var variable = new Variable(variableName, VariableType.Continuous);
                    model.Variables.Add(variable);
                }
                catch (FormatException)
                {
                    throw new InvalidDataException($"Invalid coefficient format in objective function: {tokens[i]}");
                }
            }

            Console.WriteLine($"✓ Parsed objective: {model.ObjectiveType} with {model.ObjectiveCoefficients.Count} coefficients");
        }

        /// <summary>
        /// Parses all constraint lines (everything between objective and variable types)
        /// Format: [+|-][coeff] [+|-][coeff] ... [<=|>=|=] [rhs]
        /// Example: "+11 +8 +6 +14 +10 +10 <= 40"
        /// </summary>
        /// <param name="lines">All lines from the file</param>
        /// <param name="model">Model to add constraints to</param>
        private void ParseConstraints(string[] lines, LinearProgrammingModel model)
        {
            // Constraint lines are all lines except first (objective) and last (variable types)
            for (int lineIndex = 1; lineIndex < lines.Length - 1; lineIndex++)
            {
                string constraintLine = lines[lineIndex].Trim();

                if (string.IsNullOrWhiteSpace(constraintLine))
                {
                    continue; // Skip empty lines
                }

                try
                {
                    var constraint = ParseSingleConstraint(constraintLine, model.VariableCount);
                    constraint.Name = $"C{model.Constraints.Count + 1}"; // C1, C2, C3, etc.
                    model.Constraints.Add(constraint);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error parsing constraint on line {lineIndex + 1}: {ex.Message}");
                }
            }

            Console.WriteLine($"✓ Parsed {model.Constraints.Count} constraints");
        }

        /// <summary>
        /// Parses a single constraint line into a Constraint object
        /// Handles different constraint types (<=, >=, =) and validates coefficient count
        /// </summary>
        /// <param name="constraintLine">Single line containing constraint data</param>
        /// <param name="expectedVariableCount">Number of variables (for validation)</param>
        /// <returns>Parsed Constraint object</returns>
        private Constraint ParseSingleConstraint(string constraintLine, int expectedVariableCount)
        {
            string[] tokens = constraintLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < expectedVariableCount + 2) // coefficients + relation + RHS
            {
                throw new InvalidDataException($"Constraint must have {expectedVariableCount} coefficients, relation, and RHS value");
            }

            var coefficients = new List<double>();

            // Parse coefficients (first N tokens where N = number of variables)
            for (int i = 0; i < expectedVariableCount; i++)
            {
                try
                {
                    double coefficient = ParseCoefficient(tokens[i]);
                    coefficients.Add(coefficient);
                }
                catch (FormatException)
                {
                    throw new InvalidDataException($"Invalid coefficient format: {tokens[i]}");
                }
            }

            // Parse constraint type (<=, >=, =)
            string relationToken = tokens[expectedVariableCount];
            ConstraintType constraintType;

            switch (relationToken)
            {
                case "<=":
                    constraintType = ConstraintType.LessThanOrEqual;
                    break;
                case ">=":
                    constraintType = ConstraintType.GreaterThanOrEqual;
                    break;
                case "=":
                    constraintType = ConstraintType.Equal;
                    break;
                default:
                    throw new InvalidDataException($"Invalid constraint relation: {relationToken}. Must be <=, >=, or =");
            }

            // Parse right-hand side value
            if (!double.TryParse(tokens[expectedVariableCount + 1], out double rhs))
            {
                throw new InvalidDataException($"Invalid RHS value: {tokens[expectedVariableCount + 1]}");
            }

            return new Constraint(coefficients, constraintType, rhs);
        }

        /// <summary>
        /// Parses the last line containing variable type specifications
        /// Format: [+|-|urs|int|bin] [+|-|urs|int|bin] ...
        /// Example: "bin bin bin bin bin bin"
        /// Updates the variable types in the model
        /// </summary>
        /// <param name="variableTypeLine">Last line of the input file</param>
        /// <param name="model">Model to update variable types for</param>
        private void ParseVariableTypes(string variableTypeLine, LinearProgrammingModel model)
        {
            if (string.IsNullOrWhiteSpace(variableTypeLine))
            {
                throw new InvalidDataException("Variable types line cannot be empty");
            }

            string[] typeTokens = variableTypeLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Validate that we have type specification for each variable
            if (typeTokens.Length != model.VariableCount)
            {
                throw new InvalidDataException($"Number of variable types ({typeTokens.Length}) must match number of variables ({model.VariableCount})");
            }

            // Update each variable's type based on the specification
            for (int i = 0; i < typeTokens.Length; i++)
            {
                try
                {
                    VariableType variableType = ParseVariableType(typeTokens[i]);
                    model.Variables[i].Type = variableType;

                    // Update bounds based on type
                    UpdateVariableBounds(model.Variables[i]);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error parsing variable type for variable {i + 1}: {ex.Message}");
                }
            }

            Console.WriteLine($"✓ Applied variable types: {string.Join(", ", typeTokens)}");
        }

        /// <summary>
        /// Converts string token to VariableType enum
        /// Handles all supported variable type specifications
        /// </summary>
        /// <param name="typeToken">String representation of variable type</param>
        /// <returns>Corresponding VariableType enum value</returns>
        private VariableType ParseVariableType(string typeToken)
        {
            return typeToken.ToLower() switch
            {
                "+" => VariableType.Continuous,     // Non-negative continuous
                "-" => VariableType.Continuous,     // Non-positive continuous (unusual)
                "urs" => VariableType.Unrestricted, // Unrestricted (can be negative)
                "int" => VariableType.Integer,      // Integer variable
                "bin" => VariableType.Binary,       // Binary (0 or 1)
                _ => throw new InvalidDataException($"Unknown variable type: {typeToken}. Valid types: +, -, urs, int, bin")
            };
        }

        /// <summary>
        /// Updates variable bounds based on its type and sign restriction
        /// Called after variable type is determined to set appropriate bounds
        /// </summary>
        /// <param name="variable">Variable to update bounds for</param>
        private void UpdateVariableBounds(Variable variable)
        {
            switch (variable.Type)
            {
                case VariableType.Continuous:
                    variable.LowerBound = 0.0; // Non-negative by default
                    variable.UpperBound = double.PositiveInfinity;
                    break;

                case VariableType.Integer:
                    variable.LowerBound = 0.0; // Non-negative integers
                    variable.UpperBound = double.PositiveInfinity;
                    break;

                case VariableType.Binary:
                    variable.LowerBound = 0.0;
                    variable.UpperBound = 1.0;
                    break;

                case VariableType.Unrestricted:
                    variable.LowerBound = double.NegativeInfinity;
                    variable.UpperBound = double.PositiveInfinity;
                    break;
            }
        }

        /// <summary>
        /// Helper method to parse coefficient strings that may include explicit + or - signs
        /// Handles cases like "+2.5", "-3.7", "4.2"
        /// </summary>
        /// <param name="coefficientString">String representation of coefficient</param>
        /// <returns>Parsed double value</returns>
        private double ParseCoefficient(string coefficientString)
        {
            if (string.IsNullOrWhiteSpace(coefficientString))
            {
                throw new FormatException("Coefficient string cannot be empty");
            }

            // Handle explicit positive sign
            if (coefficientString.StartsWith("+"))
            {
                coefficientString = coefficientString.Substring(1);
            }

            // Parse the remaining string as a double
            if (double.TryParse(coefficientString, out double result))
            {
                return result;
            }
            else
            {
                throw new FormatException($"Cannot parse '{coefficientString}' as a number");
            }
        }

        /// <summary>
        /// Utility method to validate file format without fully parsing
        /// Useful for quick validation before processing
        /// </summary>
        /// <returns>True if file appears to have valid format</returns>
        public bool ValidateFileFormat()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return false;

                string[] lines = File.ReadAllLines(_filePath);

                // Basic structure check
                if (lines.Length < 3)
                    return false;

                // Check if first line starts with max or min
                string firstLine = lines[0].Trim().ToLower();
                if (!firstLine.StartsWith("max") && !firstLine.StartsWith("min"))
                    return false;

                // Check if last line contains variable type specifications
                string lastLine = lines[lines.Length - 1].Trim().ToLower();
                string[] typeTokens = lastLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (string token in typeTokens)
                {
                    if (token != "+" && token != "-" && token != "urs" && token != "int" && token != "bin")
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
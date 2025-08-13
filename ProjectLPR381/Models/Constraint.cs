using System;
using System.Collections.Generic;
using System.Linq;

namespace LinearProgrammingProject.Models
{
    /// <summary>
    /// Represents a constraint in a Linear Programming model
    /// </summary>
    public class Constraint
    {
        public List<double> Coefficients { get; set; }
        public ConstraintType Type { get; set; }
        public double RightHandSide { get; set; }
        public string Name { get; set; }

        public Constraint()
        {
            Coefficients = new List<double>();
            Type = ConstraintType.LessThanOrEqual;
            RightHandSide = 0.0;
            Name = string.Empty;
        }

        public Constraint(List<double> coefficients, ConstraintType type, double rhs, string name = "")
        {
            Coefficients = coefficients ?? throw new ArgumentNullException(nameof(coefficients));
            Type = type;
            RightHandSide = rhs;
            Name = string.IsNullOrEmpty(name) ? $"C{DateTime.Now.Ticks}" : name;
        }

        /// <summary>
        /// Creates a constraint from arrays
        /// </summary>
        public Constraint(double[] coefficients, ConstraintType type, double rhs, string name = "")
            : this(coefficients.ToList(), type, rhs, name)
        {
        }

        /// <summary>
        /// Validates if the constraint has the correct number of coefficients
        /// </summary>
        public bool IsValid(int expectedVariableCount)
        {
            return Coefficients != null && Coefficients.Count == expectedVariableCount;
        }

        /// <summary>
        /// Evaluates the constraint for given variable values
        /// </summary>
        public double EvaluateLeftSide(List<double> variableValues)
        {
            if (variableValues.Count != Coefficients.Count)
                throw new ArgumentException("Variable values count must match coefficient count");

            double result = 0.0;
            for (int i = 0; i < Coefficients.Count; i++)
            {
                result += Coefficients[i] * variableValues[i];
            }
            return result;
        }

        /// <summary>
        /// Checks if the constraint is satisfied by given variable values
        /// </summary>
        public bool IsSatisfied(List<double> variableValues, double tolerance = 1e-9)
        {
            double leftSide = EvaluateLeftSide(variableValues);

            return Type switch
            {
                ConstraintType.LessThanOrEqual => leftSide <= RightHandSide + tolerance,
                ConstraintType.GreaterThanOrEqual => leftSide >= RightHandSide - tolerance,
                ConstraintType.Equal => Math.Abs(leftSide - RightHandSide) <= tolerance,
                _ => throw new InvalidOperationException($"Unknown constraint type: {Type}")
            };
        }

        /// <summary>
        /// Gets the slack/surplus value for this constraint
        /// </summary>
        public double GetSlack(List<double> variableValues)
        {
            double leftSide = EvaluateLeftSide(variableValues);

            return Type switch
            {
                ConstraintType.LessThanOrEqual => RightHandSide - leftSide, // Slack
                ConstraintType.GreaterThanOrEqual => leftSide - RightHandSide, // Surplus
                ConstraintType.Equal => 0.0, // No slack for equality constraints
                _ => throw new InvalidOperationException($"Unknown constraint type: {Type}")
            };
        }

        /// <summary>
        /// Converts constraint to standard form (≤) by multiplying by -1 if needed
        /// </summary>
        public Constraint ToStandardForm()
        {
            if (Type == ConstraintType.GreaterThanOrEqual)
            {
                // Convert a ≥ b to -a ≤ -b
                var newCoefficients = Coefficients.Select(c => -c).ToList();
                return new Constraint(newCoefficients, ConstraintType.LessThanOrEqual, -RightHandSide, Name + "_std");
            }

            return Clone(); // Already in standard form or is equality
        }

        /// <summary>
        /// Creates a string representation of the constraint
        /// </summary>
        public string ToString(List<Variable> variables)
        {
            if (variables == null || variables.Count != Coefficients.Count)
            {
                return ToString();
            }

            var terms = new List<string>();
            for (int i = 0; i < Coefficients.Count; i++)
            {
                var coeff = Coefficients[i];
                var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                terms.Add($"{sign}{coeff:F3}*{variables[i].Name}");
            }

            string typeSymbol = Type switch
            {
                ConstraintType.LessThanOrEqual => "<=",
                ConstraintType.GreaterThanOrEqual => ">=",
                ConstraintType.Equal => "=",
                _ => "?"
            };

            return $"{string.Join(" ", terms)} {typeSymbol} {RightHandSide:F3}";
        }

        /// <summary>
        /// String representation without variable names
        /// </summary>
        public override string ToString()
        {
            var terms = new List<string>();
            for (int i = 0; i < Coefficients.Count; i++)
            {
                var coeff = Coefficients[i];
                var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                terms.Add($"{sign}{coeff:F3}*x{i + 1}");
            }

            string typeSymbol = Type switch
            {
                ConstraintType.LessThanOrEqual => "<=",
                ConstraintType.GreaterThanOrEqual => ">=",
                ConstraintType.Equal => "=",
                _ => "?"
            };

            return $"{string.Join(" ", terms)} {typeSymbol} {RightHandSide:F3}";
        }

        /// <summary>
        /// Creates a deep copy of this constraint
        /// </summary>
        public Constraint Clone()
        {
            return new Constraint(new List<double>(Coefficients), Type, RightHandSide, Name);
        }

        /// <summary>
        /// Checks if this constraint is equivalent to another
        /// </summary>
        public bool IsEquivalent(Constraint other, double tolerance = 1e-9)
        {
            if (other == null || Type != other.Type || Coefficients.Count != other.Coefficients.Count)
                return false;

            if (Math.Abs(RightHandSide - other.RightHandSide) > tolerance)
                return false;

            for (int i = 0; i < Coefficients.Count; i++)
            {
                if (Math.Abs(Coefficients[i] - other.Coefficients[i]) > tolerance)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Types of constraints
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// Less than or equal to (≤)
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Greater than or equal to (≥)
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Equal to (=)
        /// </summary>
        Equal
    }
}
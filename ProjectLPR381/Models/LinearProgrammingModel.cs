using System;
using System.Collections.Generic;
using System.Linq;

namespace LinearProgrammingProject.Models
{
    /// <summary>
    /// Represents a Linear Programming model with objective function, constraints, and variables
    /// </summary>
    public class LinearProgrammingModel
    {
        public ObjectiveType ObjectiveType { get; set; }
        public List<Variable> Variables { get; set; }
        public List<Constraint> Constraints { get; set; }
        public List<double> ObjectiveCoefficients { get; set; }

        // For storing solution results
        public double OptimalValue { get; set; }
        public Dictionary<string, double> OptimalSolution { get; set; }
        public bool IsSolved { get; set; }
        public SolutionStatus Status { get; set; }

        public LinearProgrammingModel()
        {
            Variables = new List<Variable>();
            Constraints = new List<Constraint>();
            ObjectiveCoefficients = new List<double>();
            OptimalSolution = new Dictionary<string, double>();
            IsSolved = false;
            Status = SolutionStatus.Unknown;
        }

        /// <summary>
        /// Gets the number of decision variables in the model
        /// </summary>
        public int VariableCount => Variables.Count;

        /// <summary>
        /// Gets the number of constraints in the model
        /// </summary>
        public int ConstraintCount => Constraints.Count;

        /// <summary>
        /// Validates if the model is properly constructed
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Check if we have variables
                if (Variables == null || Variables.Count == 0)
                    return false;

                // Check if objective coefficients match variable count
                if (ObjectiveCoefficients == null || ObjectiveCoefficients.Count != Variables.Count)
                    return false;

                // Check if all constraints are valid
                if (Constraints != null)
                {
                    foreach (var constraint in Constraints)
                    {
                        if (!constraint.IsValid(Variables.Count))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a string representation of the objective function
        /// </summary>
        public string GetObjectiveFunctionString()
        {
            if (!IsValid()) return "Invalid model";

            var objType = ObjectiveType == ObjectiveType.Maximize ? "Maximize" : "Minimize";
            var terms = new List<string>();

            for (int i = 0; i < Variables.Count; i++)
            {
                var coeff = ObjectiveCoefficients[i];
                var sign = coeff >= 0 ? "+" : "";
                terms.Add($"{sign}{coeff:F3}*{Variables[i].Name}");
            }

            return $"{objType}: {string.Join(" ", terms)}";
        }

        /// <summary>
        /// Gets string representations of all constraints
        /// </summary>
        public List<string> GetConstraintStrings()
        {
            var constraintStrings = new List<string>();

            for (int i = 0; i < Constraints.Count; i++)
            {
                constraintStrings.Add($"C{i + 1}: {Constraints[i].ToString(Variables)}");
            }

            return constraintStrings;
        }

        /// <summary>
        /// Creates a deep copy of the model
        /// </summary>
        public LinearProgrammingModel Clone()
        {
            var clone = new LinearProgrammingModel
            {
                ObjectiveType = this.ObjectiveType,
                OptimalValue = this.OptimalValue,
                IsSolved = this.IsSolved,
                Status = this.Status
            };

            // Clone variables
            foreach (var variable in Variables)
            {
                clone.Variables.Add(variable.Clone());
            }

            // Clone constraints
            foreach (var constraint in Constraints)
            {
                clone.Constraints.Add(constraint.Clone());
            }

            // Clone objective coefficients
            clone.ObjectiveCoefficients.AddRange(ObjectiveCoefficients);

            // Clone optimal solution
            foreach (var kvp in OptimalSolution)
            {
                clone.OptimalSolution[kvp.Key] = kvp.Value;
            }

            return clone;
        }

        /// <summary>
        /// Checks if this is an Integer Programming model
        /// </summary>
        public bool IsIntegerProgram()
        {
            return Variables.Any(v => v.Type == VariableType.Integer || v.Type == VariableType.Binary);
        }

        /// <summary>
        /// Checks if this is a Binary Integer Programming model
        /// </summary>
        public bool IsBinaryProgram()
        {
            return Variables.Any(v => v.Type == VariableType.Binary);
        }

        /// <summary>
        /// Gets model statistics for display
        /// </summary>
        public string GetModelSummary()
        {
            return $"Model Summary:\n" +
                   $"  Variables: {VariableCount}\n" +
                   $"  Constraints: {ConstraintCount}\n" +
                   $"  Objective: {ObjectiveType}\n" +
                   $"  Type: {(IsIntegerProgram() ? (IsBinaryProgram() ? "Binary IP" : "Integer IP") : "Linear Program")}\n" +
                   $"  Status: {Status}";
        }
    }

    public enum ObjectiveType
    {
        Maximize,
        Minimize
    }

    public enum SolutionStatus
    {
        Unknown,
        Optimal,
        Infeasible,
        Unbounded,
        Error
    }
}
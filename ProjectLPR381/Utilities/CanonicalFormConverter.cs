using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Utilities
{
    /// <summary>
    /// Converts Linear Programming models to their canonical (standard) form
    /// 
    /// Canonical form requirements:
    /// 1. All constraints must be of the form: a₁x₁ + a₂x₂ + ... + aₙxₙ ≤ bᵢ
    /// 2. All variables must be non-negative: xⱼ ≥ 0
    /// 3. Objective function should be maximization (minimization converted by multiplying by -1)
    /// 4. All RHS values should be non-negative
    /// 
    /// This class handles:
    /// - Converting ≥ constraints to ≤ by multiplying by -1
    /// - Converting = constraints to two ≤ constraints (≤ and ≥)
    /// - Adding slack variables for ≤ constraints
    /// - Adding surplus variables for ≥ constraints
    /// - Converting unrestricted variables to difference of two non-negative variables
    /// - Converting minimization to maximization
    /// </summary>
    public class CanonicalFormConverter
    {
        private LinearProgrammingModel _originalModel;
        private LinearProgrammingModel _canonicalModel;

        // Track the transformations for later reference
        private Dictionary<string, string> _variableMapping;
        private List<string> _slackVariables;
        private List<string> _surplusVariables;
        private List<string> _artificialVariables;

        /// <summary>
        /// Constructor - initializes the converter with the original model
        /// </summary>
        /// <param name="originalModel">The LP model to convert to canonical form</param>
        public CanonicalFormConverter(LinearProgrammingModel originalModel)
        {
            _originalModel = originalModel ?? throw new ArgumentNullException(nameof(originalModel));
            _variableMapping = new Dictionary<string, string>();
            _slackVariables = new List<string>();
            _surplusVariables = new List<string>();
            _artificialVariables = new List<string>();
        }

        /// <summary>
        /// Main method to convert the LP model to canonical form
        /// This orchestrates all the transformation steps needed
        /// </summary>
        /// <returns>A new LinearProgrammingModel in canonical form</returns>
        public LinearProgrammingModel ConvertToCanonicalForm()
        {
            try
            {
                Console.WriteLine("🔄 Converting model to canonical form...");

                // Step 1: Create a copy of the original model to work with
                _canonicalModel = _originalModel.Clone();

                // Step 2: Convert minimization to maximization if needed
                ConvertObjectiveToMaximization();

                // Step 3: Handle unrestricted variables (convert to difference of two non-negative variables)
                HandleUnrestrictedVariables();

                // Step 4: Ensure all RHS values are non-negative
                EnsureNonNegativeRHS();

                // Step 5: Convert all constraints to ≤ form and add slack/surplus variables
                ConvertConstraintsToStandardForm();

                // Step 6: Validate the canonical form
                ValidateCanonicalForm();

                Console.WriteLine($"✅ Canonical form created successfully!");
                Console.WriteLine($"   Original variables: {_originalModel.VariableCount}");
                Console.WriteLine($"   Canonical variables: {_canonicalModel.VariableCount}");
                Console.WriteLine($"   Slack variables added: {_slackVariables.Count}");
                Console.WriteLine($"   Surplus variables added: {_surplusVariables.Count}");

                return _canonicalModel;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert model to canonical form: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts minimization problems to maximization by multiplying objective coefficients by -1
        /// This is necessary because the simplex algorithm typically works with maximization problems
        /// </summary>
        private void ConvertObjectiveToMaximization()
        {
            if (_canonicalModel.ObjectiveType == ObjectiveType.Minimize)
            {
                Console.WriteLine("   Converting minimization to maximization...");

                // Multiply all objective coefficients by -1
                for (int i = 0; i < _canonicalModel.ObjectiveCoefficients.Count; i++)
                {
                    _canonicalModel.ObjectiveCoefficients[i] = -_canonicalModel.ObjectiveCoefficients[i];
                }

                // Change the objective type to maximization
                _canonicalModel.ObjectiveType = ObjectiveType.Maximize;

                // Note: When we get the final solution, we'll need to multiply the optimal value by -1
                // to get the original minimization objective value
            }
        }

        /// <summary>
        /// Handles unrestricted variables by splitting them into two non-negative variables
        /// If xᵢ is unrestricted, replace it with xᵢ⁺ - xᵢ⁻ where both xᵢ⁺, xᵢ⁻ ≥ 0
        /// </summary>
        private void HandleUnrestrictedVariables()
        {
            var unrestrictedVariables = _canonicalModel.Variables
                .Where(v => v.Type == VariableType.Unrestricted)
                .ToList();

            if (unrestrictedVariables.Count == 0)
            {
                return; // No unrestricted variables to handle
            }

            Console.WriteLine($"   Handling {unrestrictedVariables.Count} unrestricted variables...");

            // We need to rebuild the model with split variables
            var newVariables = new List<Variable>();
            var newObjectiveCoefficients = new List<double>();
            var newConstraints = new List<Constraint>();

            // Process each original variable
            for (int i = 0; i < _canonicalModel.Variables.Count; i++)
            {
                var variable = _canonicalModel.Variables[i];

                if (variable.Type == VariableType.Unrestricted)
                {
                    // Split unrestricted variable into positive and negative parts
                    string positiveName = $"{variable.Name}+";
                    string negativeName = $"{variable.Name}-";

                    // Create two non-negative variables
                    var positiveVar = new Variable(positiveName, VariableType.Continuous, 0, double.PositiveInfinity);
                    var negativeVar = new Variable(negativeName, VariableType.Continuous, 0, double.PositiveInfinity);

                    newVariables.Add(positiveVar);
                    newVariables.Add(negativeVar);

                    // The coefficient for xᵢ becomes coefficient for xᵢ⁺ and -coefficient for xᵢ⁻
                    double originalCoeff = _canonicalModel.ObjectiveCoefficients[i];
                    newObjectiveCoefficients.Add(originalCoeff);   // For xᵢ⁺
                    newObjectiveCoefficients.Add(-originalCoeff);  // For xᵢ⁻

                    // Record the mapping for later reference
                    _variableMapping[variable.Name] = $"{positiveName} - {negativeName}";
                }
                else
                {
                    // Keep the variable as is (already non-negative or binary/integer)
                    newVariables.Add(variable.Clone());
                    newObjectiveCoefficients.Add(_canonicalModel.ObjectiveCoefficients[i]);
                    _variableMapping[variable.Name] = variable.Name;
                }
            }

            // Update constraints to handle split variables
            foreach (var constraint in _canonicalModel.Constraints)
            {
                var newCoefficients = new List<double>();
                int originalVarIndex = 0;

                for (int i = 0; i < _canonicalModel.Variables.Count; i++)
                {
                    var variable = _canonicalModel.Variables[i];
                    double originalCoeff = constraint.Coefficients[i];

                    if (variable.Type == VariableType.Unrestricted)
                    {
                        // Split the coefficient: coeff * xᵢ becomes coeff * xᵢ⁺ - coeff * xᵢ⁻
                        newCoefficients.Add(originalCoeff);   // Coefficient for xᵢ⁺
                        newCoefficients.Add(-originalCoeff);  // Coefficient for xᵢ⁻
                    }
                    else
                    {
                        // Keep the coefficient as is
                        newCoefficients.Add(originalCoeff);
                    }
                }

                // Create new constraint with updated coefficients
                var newConstraint = new Constraint(newCoefficients, constraint.Type, constraint.RightHandSide, constraint.Name);
                newConstraints.Add(newConstraint);
            }

            // Update the canonical model with new variables and constraints
            _canonicalModel.Variables = newVariables;
            _canonicalModel.ObjectiveCoefficients = newObjectiveCoefficients;
            _canonicalModel.Constraints = newConstraints;
        }

        /// <summary>
        /// Ensures all RHS values are non-negative by multiplying negative RHS constraints by -1
        /// If bᵢ < 0, multiply the entire constraint by -1 and flip the inequality sign
        /// </summary>
        private void EnsureNonNegativeRHS()
        {
            Console.WriteLine("   Ensuring all RHS values are non-negative...");

            for (int i = 0; i < _canonicalModel.Constraints.Count; i++)
            {
                var constraint = _canonicalModel.Constraints[i];

                if (constraint.RightHandSide < 0)
                {
                    Console.WriteLine($"     Converting negative RHS in constraint {constraint.Name}");

                    // Multiply all coefficients by -1
                    for (int j = 0; j < constraint.Coefficients.Count; j++)
                    {
                        constraint.Coefficients[j] = -constraint.Coefficients[j];
                    }

                    // Multiply RHS by -1
                    constraint.RightHandSide = -constraint.RightHandSide;

                    // Flip the inequality sign
                    constraint.Type = FlipConstraintType(constraint.Type);
                }
            }
        }

        /// <summary>
        /// Converts all constraints to standard ≤ form and adds slack/surplus variables as needed
        /// - For aᵢxᵢ ≤ bᵢ: add slack variable sᵢ ≥ 0, becomes aᵢxᵢ + sᵢ = bᵢ
        /// - For aᵢxᵢ ≥ bᵢ: subtract surplus variable, becomes aᵢxᵢ - sᵢ = bᵢ
        /// - For aᵢxᵢ = bᵢ: already in correct form, may need artificial variable for initial solution
        /// </summary>
        private void ConvertConstraintsToStandardForm()
        {
            Console.WriteLine("   Converting constraints to standard form and adding slack/surplus variables...");

            // We'll need to expand the number of variables to include slack/surplus variables
            var originalVariableCount = _canonicalModel.Variables.Count;
            var slackSurplusCount = 0;

            // First pass: count how many slack/surplus variables we need
            foreach (var constraint in _canonicalModel.Constraints)
            {
                if (constraint.Type != ConstraintType.Equal)
                {
                    slackSurplusCount++;
                }
            }

            // Add slack/surplus variables to the model
            for (int i = 0; i < slackSurplusCount; i++)
            {
                string varName = $"s{i + 1}";
                var slackVar = new Variable(varName, VariableType.Continuous, 0, double.PositiveInfinity);
                _canonicalModel.Variables.Add(slackVar);

                // Slack/surplus variables have 0 coefficient in objective function
                _canonicalModel.ObjectiveCoefficients.Add(0.0);
            }

            // Second pass: update constraint coefficients to include slack/surplus variables
            int slackIndex = 0;
            for (int constraintIndex = 0; constraintIndex < _canonicalModel.Constraints.Count; constraintIndex++)
            {
                var constraint = _canonicalModel.Constraints[constraintIndex];

                // Extend coefficient array to include slots for all slack/surplus variables
                while (constraint.Coefficients.Count < _canonicalModel.Variables.Count)
                {
                    constraint.Coefficients.Add(0.0);
                }

                switch (constraint.Type)
                {
                    case ConstraintType.LessThanOrEqual:
                        // Add slack variable: aᵢxᵢ ≤ bᵢ becomes aᵢxᵢ + sᵢ = bᵢ
                        int slackPosition = originalVariableCount + slackIndex;
                        constraint.Coefficients[slackPosition] = 1.0;
                        constraint.Type = ConstraintType.Equal;

                        string slackVarName = $"s{slackIndex + 1}";
                        _slackVariables.Add(slackVarName);
                        Console.WriteLine($"     Added slack variable {slackVarName} to constraint {constraint.Name}");

                        slackIndex++;
                        break;

                    case ConstraintType.GreaterThanOrEqual:
                        // Subtract surplus variable: aᵢxᵢ ≥ bᵢ becomes aᵢxᵢ - sᵢ = bᵢ
                        int surplusPosition = originalVariableCount + slackIndex;
                        constraint.Coefficients[surplusPosition] = -1.0;
                        constraint.Type = ConstraintType.Equal;

                        string surplusVarName = $"s{slackIndex + 1}";
                        _surplusVariables.Add(surplusVarName);
                        Console.WriteLine($"     Added surplus variable {surplusVarName} to constraint {constraint.Name}");

                        slackIndex++;
                        break;

                    case ConstraintType.Equal:
                        // Equality constraints are already in the correct form
                        // Note: In practice, we might need artificial variables for these in Phase I of simplex
                        Console.WriteLine($"     Constraint {constraint.Name} is already an equality");
                        break;
                }
            }
        }

        /// <summary>
        /// Validates that the canonical form is correct
        /// Checks that all constraints are equalities, all variables are non-negative, etc.
        /// </summary>
        private void ValidateCanonicalForm()
        {
            Console.WriteLine("   Validating canonical form...");

            // Check 1: All constraints should be equalities
            foreach (var constraint in _canonicalModel.Constraints)
            {
                if (constraint.Type != ConstraintType.Equal)
                {
                    throw new InvalidOperationException($"Constraint {constraint.Name} is not an equality in canonical form");
                }
            }

            // Check 2: All variables should be non-negative (except unrestricted, which should be handled)
            foreach (var variable in _canonicalModel.Variables)
            {
                if (variable.Type == VariableType.Unrestricted)
                {
                    throw new InvalidOperationException($"Variable {variable.Name} is still unrestricted in canonical form");
                }
            }

            // Check 3: All RHS values should be non-negative
            foreach (var constraint in _canonicalModel.Constraints)
            {
                if (constraint.RightHandSide < 0)
                {
                    throw new InvalidOperationException($"Constraint {constraint.Name} has negative RHS in canonical form");
                }
            }

            // Check 4: Objective should be maximization
            if (_canonicalModel.ObjectiveType != ObjectiveType.Maximize)
            {
                throw new InvalidOperationException("Objective should be maximization in canonical form");
            }

            Console.WriteLine("   ✅ Canonical form validation passed");
        }

        /// <summary>
        /// Helper method to flip constraint inequality signs
        /// Used when multiplying constraints by -1 to make RHS non-negative
        /// </summary>
        /// <param name="type">Original constraint type</param>
        /// <returns>Flipped constraint type</returns>
        private ConstraintType FlipConstraintType(ConstraintType type)
        {
            return type switch
            {
                ConstraintType.LessThanOrEqual => ConstraintType.GreaterThanOrEqual,
                ConstraintType.GreaterThanOrEqual => ConstraintType.LessThanOrEqual,
                ConstraintType.Equal => ConstraintType.Equal, // Equality doesn't change
                _ => throw new ArgumentException($"Unknown constraint type: {type}")
            };
        }

        /// <summary>
        /// Creates the initial tableau for the simplex algorithm from the canonical form
        /// This tableau will be used by the simplex algorithms to find the optimal solution
        /// </summary>
        /// <returns>2D array representing the initial simplex tableau</returns>
        public double[,] CreateInitialTableau()
        {
            if (_canonicalModel == null)
            {
                throw new InvalidOperationException("Must convert to canonical form before creating tableau");
            }

            int numConstraints = _canonicalModel.Constraints.Count;
            int numVariables = _canonicalModel.Variables.Count;

            // Tableau dimensions: (constraints + 1) × (variables + 1)
            // +1 for the objective function row and RHS column
            double[,] tableau = new double[numConstraints + 1, numVariables + 1];

            Console.WriteLine($"   Creating initial tableau: {numConstraints + 1} × {numVariables + 1}");

            // Fill constraint rows
            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = _canonicalModel.Constraints[i];

                // Copy constraint coefficients
                for (int j = 0; j < numVariables; j++)
                {
                    tableau[i, j] = constraint.Coefficients[j];
                }

                // Set RHS value
                tableau[i, numVariables] = constraint.RightHandSide;
            }

            // Fill objective function row (last row)
            // For maximization, we put negative coefficients in the tableau
            for (int j = 0; j < numVariables; j++)
            {
                tableau[numConstraints, j] = -_canonicalModel.ObjectiveCoefficients[j];
            }

            // Objective row RHS starts at 0
            tableau[numConstraints, numVariables] = 0.0;

            return tableau;
        }

        /// <summary>
        /// Gets the list of basic variables for the initial basic feasible solution
        /// Initially, slack variables are basic (they form the identity matrix columns)
        /// </summary>
        /// <returns>List of basic variable names</returns>
        public List<string> GetInitialBasicVariables()
        {
            var basicVariables = new List<string>();

            // Initially, slack and surplus variables are basic
            basicVariables.AddRange(_slackVariables);
            basicVariables.AddRange(_surplusVariables);

            // If we have more constraints than slack/surplus variables,
            // we might need artificial variables (for Phase I of simplex)
            int remainingConstraints = _canonicalModel.Constraints.Count - basicVariables.Count;
            for (int i = 0; i < remainingConstraints; i++)
            {
                string artificialVarName = $"a{i + 1}";
                _artificialVariables.Add(artificialVarName);
                basicVariables.Add(artificialVarName);
            }

            return basicVariables;
        }

        /// <summary>
        /// Gets the list of non-basic variables for the initial solution
        /// Initially, original decision variables are non-basic (set to 0)
        /// </summary>
        /// <returns>List of non-basic variable names</returns>
        public List<string> GetInitialNonBasicVariables()
        {
            var nonBasicVariables = new List<string>();

            // Original decision variables start as non-basic
            int originalVarCount = _canonicalModel.Variables.Count - _slackVariables.Count - _surplusVariables.Count;
            for (int i = 0; i < originalVarCount; i++)
            {
                nonBasicVariables.Add(_canonicalModel.Variables[i].Name);
            }

            return nonBasicVariables;
        }

        /// <summary>
        /// Gets information about the transformations applied during canonical form conversion
        /// Useful for interpreting the final solution in terms of original variables
        /// </summary>
        /// <returns>Dictionary containing transformation information</returns>
        public Dictionary<string, object> GetTransformationInfo()
        {
            return new Dictionary<string, object>
            {
                ["VariableMapping"] = _variableMapping,
                ["SlackVariables"] = _slackVariables,
                ["SurplusVariables"] = _surplusVariables,
                ["ArtificialVariables"] = _artificialVariables,
                ["WasMinimization"] = _originalModel.ObjectiveType == ObjectiveType.Minimize,
                ["OriginalVariableCount"] = _originalModel.Variables.Count,
                ["CanonicalVariableCount"] = _canonicalModel?.Variables.Count ?? 0
            };
        }

        /// <summary>
        /// Converts a solution from canonical form back to the original problem format
        /// This is essential for presenting results to the user in terms of their original variables
        /// </summary>
        /// <param name="canonicalSolution">Solution in terms of canonical form variables</param>
        /// <param name="canonicalObjectiveValue">Objective value from canonical form solution</param>
        /// <returns>Solution in terms of original problem variables</returns>
        public Dictionary<string, double> ConvertSolutionToOriginal(Dictionary<string, double> canonicalSolution, out double originalObjectiveValue)
        {
            var originalSolution = new Dictionary<string, double>();

            // Convert objective value back (multiply by -1 if original was minimization)
            originalObjectiveValue = _originalModel.ObjectiveType == ObjectiveType.Minimize ?
                                   -canonicalSolution.GetValueOrDefault("ObjectiveValue", 0) :
                                   canonicalSolution.GetValueOrDefault("ObjectiveValue", 0);

            // Convert variable values back to original variables
            foreach (var originalVar in _originalModel.Variables)
            {
                string originalName = originalVar.Name;

                if (_variableMapping.ContainsKey(originalName))
                {
                    string mapping = _variableMapping[originalName];

                    if (mapping.Contains(" - ")) // Unrestricted variable that was split
                    {
                        // Extract positive and negative part names
                        string[] parts = mapping.Split(" - ");
                        string positivePart = parts[0];
                        string negativePart = parts[1];

                        double positiveValue = canonicalSolution.GetValueOrDefault(positivePart, 0);
                        double negativeValue = canonicalSolution.GetValueOrDefault(negativePart, 0);

                        // Original value = positive part - negative part
                        originalSolution[originalName] = positiveValue - negativeValue;
                    }
                    else
                    {
                        // Variable wasn't transformed, just copy the value
                        originalSolution[originalName] = canonicalSolution.GetValueOrDefault(mapping, 0);
                    }
                }
                else
                {
                    // Fallback: try to find the variable value directly
                    originalSolution[originalName] = canonicalSolution.GetValueOrDefault(originalName, 0);
                }
            }

            return originalSolution;
        }

        /// <summary>
        /// Gets a string representation of the canonical form for display/debugging
        /// </summary>
        /// <returns>Human-readable string describing the canonical form</returns>
        public string GetCanonicalFormString()
        {
            if (_canonicalModel == null)
                return "Canonical form not yet created";

            var result = new System.Text.StringBuilder();

            result.AppendLine("CANONICAL FORM:");
            result.AppendLine("==============");
            result.AppendLine(_canonicalModel.GetObjectiveFunctionString());
            result.AppendLine();
            result.AppendLine("Subject to:");

            foreach (var constraint in _canonicalModel.Constraints)
            {
                result.AppendLine($"  {constraint}");
            }

            result.AppendLine();
            result.AppendLine("Variable bounds:");
            foreach (var variable in _canonicalModel.Variables)
            {
                result.AppendLine($"  {variable.GetBoundsString()}");
            }

            return result.ToString();
        }
    }
}
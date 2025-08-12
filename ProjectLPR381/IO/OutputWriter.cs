using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.IO
{
    /// <summary>
    /// Responsible for writing Linear Programming results to output files
    /// This class formats and exports solution data, tableau iterations, and analysis results
    /// 
    /// Output includes:
    /// - Original problem formulation
    /// - Canonical form representation  
    /// - Algorithm iterations and tableau steps
    /// - Optimal solution (if found)
    /// - Sensitivity analysis results
    /// </summary>
    public class OutputWriter
    {
        private string _outputFilePath;
        private StringBuilder _outputContent;

        /// <summary>
        /// Constructor - initializes the OutputWriter with specified file path
        /// Creates a StringBuilder to accumulate output content before writing
        /// </summary>
        /// <param name="outputFilePath">Path where output file will be written</param>
        public OutputWriter(string outputFilePath)
        {
            _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
            _outputContent = new StringBuilder();

            // Ensure the output directory exists
            string directory = Path.GetDirectoryName(_outputFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Writes the original problem formulation to output
        /// This shows the problem as it was read from the input file
        /// </summary>
        /// <param name="model">The original Linear Programming model</param>
        public void WriteOriginalProblem(LinearProgrammingModel model)
        {
            AddSection("ORIGINAL PROBLEM FORMULATION");
            AddLine("=" + new string('=', 50));

            // Write objective function
            AddLine($"Objective: {model.GetObjectiveFunctionString()}");
            AddLine();

            // Write constraints
            AddLine("Subject to:");
            var constraintStrings = model.GetConstraintStrings();
            foreach (string constraint in constraintStrings)
            {
                AddLine($"  {constraint}");
            }
            AddLine();

            // Write variable types and bounds
            AddLine("Variable Types:");
            foreach (var variable in model.Variables)
            {
                AddLine($"  {variable.GetBoundsString()}");
            }

            AddLine();
            AddLine($"Problem Type: {(model.IsIntegerProgram() ? (model.IsBinaryProgram() ? "Binary Integer Program" : "Integer Program") : "Linear Program")}");
            AddSectionSeparator();
        }

        /// <summary>
        /// Writes the canonical form of the LP problem
        /// Shows how the problem is transformed for algorithmic solution
        /// </summary>
        /// <param name="canonicalTableau">2D array representing the canonical tableau</param>
        /// <param name="basicVariables">List of basic variable names</param>
        /// <param name="nonBasicVariables">List of non-basic variable names</param>
        public void WriteCanonicalForm(double[,] canonicalTableau, List<string> basicVariables, List<string> nonBasicVariables)
        {
            AddSection("CANONICAL FORM");
            AddLine("=" + new string('=', 50));

            if (canonicalTableau == null)
            {
                AddLine("Canonical form not available.");
                AddSectionSeparator();
                return;
            }

            int rows = canonicalTableau.GetLength(0);
            int cols = canonicalTableau.GetLength(1);

            // Write tableau header with variable names
            var headerBuilder = new StringBuilder();
            headerBuilder.Append("Basic Var".PadRight(12));

            // Add non-basic variables to header
            foreach (string var in nonBasicVariables)
            {
                headerBuilder.Append(var.PadLeft(10));
            }
            headerBuilder.Append("RHS".PadLeft(12));

            AddLine(headerBuilder.ToString());
            AddLine(new string('-', headerBuilder.Length));

            // Write tableau rows
            for (int i = 0; i < rows - 1; i++) // Exclude objective row for now
            {
                var rowBuilder = new StringBuilder();

                // Basic variable name
                string basicVar = i < basicVariables.Count ? basicVariables[i] : $"s{i + 1}";
                rowBuilder.Append(basicVar.PadRight(12));

                // Coefficient values (rounded to 3 decimal places as required)
                for (int j = 0; j < cols - 1; j++)
                {
                    rowBuilder.Append(Math.Round(canonicalTableau[i, j], 3).ToString("F3").PadLeft(10));
                }

                // RHS value
                rowBuilder.Append(Math.Round(canonicalTableau[i, cols - 1], 3).ToString("F3").PadLeft(12));

                AddLine(rowBuilder.ToString());
            }

            // Write objective function row (typically the last row)
            if (rows > 0)
            {
                AddLine(new string('-', headerBuilder.Length));
                var objRowBuilder = new StringBuilder();
                objRowBuilder.Append("Z".PadRight(12));

                for (int j = 0; j < cols - 1; j++)
                {
                    objRowBuilder.Append(Math.Round(canonicalTableau[rows - 1, j], 3).ToString("F3").PadLeft(10));
                }
                objRowBuilder.Append(Math.Round(canonicalTableau[rows - 1, cols - 1], 3).ToString("F3").PadLeft(12));

                AddLine(objRowBuilder.ToString());
            }

            AddSectionSeparator();
        }

        /// <summary>
        /// Writes a single iteration of the simplex tableau
        /// Used to show step-by-step progress of the simplex algorithm
        /// </summary>
        /// <param name="iterationNumber">Current iteration number</param>
        /// <param name="tableau">Current tableau state</param>
        /// <param name="basicVariables">Current basic variables</param>
        /// <param name="pivotRow">Row index of pivot element (-1 if no pivot)</param>
        /// <param name="pivotCol">Column index of pivot element (-1 if no pivot)</param>
        /// <param name="isOptimal">Whether this iteration represents the optimal solution</param>
        public void WriteIteration(int iterationNumber, double[,] tableau, List<string> basicVariables,
                                 int pivotRow = -1, int pivotCol = -1, bool isOptimal = false)
        {
            AddLine($"ITERATION {iterationNumber}");
            AddLine(new string('-', 30));

            if (tableau == null)
            {
                AddLine("Tableau not available for this iteration.");
                AddLine();
                return;
            }

            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            // Show pivot information if available
            if (pivotRow >= 0 && pivotCol >= 0 && !isOptimal)
            {
                AddLine($"Pivot Element: Row {pivotRow}, Column {pivotCol} = {Math.Round(tableau[pivotRow, pivotCol], 3):F3}");
                AddLine();
            }

            // Write the tableau in a formatted way
            WriteTableauFormatted(tableau, basicVariables);

            if (isOptimal)
            {
                AddLine("*** OPTIMAL SOLUTION REACHED ***");
            }

            AddLine();
        }

        /// <summary>
        /// Writes the final optimal solution results
        /// Shows optimal variable values, objective function value, and solution status
        /// </summary>
        /// <param name="model">Solved Linear Programming model</param>
        /// <param name="algorithmUsed">Name of the algorithm used to solve</param>
        /// <param name="iterationCount">Total number of iterations required</param>
        public void WriteSolution(LinearProgrammingModel model, string algorithmUsed, int iterationCount)
        {
            AddSection("SOLUTION RESULTS");
            AddLine("=" + new string('=', 50));

            AddLine($"Algorithm Used: {algorithmUsed}");
            AddLine($"Total Iterations: {iterationCount}");
            AddLine($"Solution Status: {model.Status}");
            AddLine();

            switch (model.Status)
            {
                case SolutionStatus.Optimal:
                    WriteOptimalSolution(model);
                    break;

                case SolutionStatus.Infeasible:
                    AddLine("The problem is INFEASIBLE.");
                    AddLine("No feasible solution exists that satisfies all constraints.");
                    break;

                case SolutionStatus.Unbounded:
                    AddLine("The problem is UNBOUNDED.");
                    AddLine("The objective function can be improved indefinitely.");
                    break;

                default:
                    AddLine("Solution status is unknown or an error occurred during solving.");
                    break;
            }

            AddSectionSeparator();
        }

        /// <summary>
        /// Writes detailed optimal solution information
        /// Shows variable values, objective value, and constraint status
        /// </summary>
        /// <param name="model">Model with optimal solution</param>
        private void WriteOptimalSolution(LinearProgrammingModel model)
        {
            AddLine("OPTIMAL SOLUTION FOUND");
            AddLine();

            // Write optimal objective value
            AddLine($"Optimal Objective Value (Z): {Math.Round(model.OptimalValue, 3):F3}");
            AddLine();

            // Write optimal variable values
            AddLine("Optimal Variable Values:");
            if (model.OptimalSolution != null && model.OptimalSolution.Count > 0)
            {
                foreach (var kvp in model.OptimalSolution)
                {
                    AddLine($"  {kvp.Key} = {Math.Round(kvp.Value, 3):F3}");
                }
            }
            else
            {
                // Fallback to variable CurrentValue if OptimalSolution dictionary is empty
                for (int i = 0; i < model.Variables.Count; i++)
                {
                    AddLine($"  {model.Variables[i].Name} = {Math.Round(model.Variables[i].CurrentValue, 3):F3}");
                }
            }

            AddLine();

            // Check constraint satisfaction
            AddLine("Constraint Status:");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];
                var variableValues = new List<double>();

                // Get current variable values
                foreach (var variable in model.Variables)
                {
                    variableValues.Add(variable.CurrentValue);
                }

                double leftSide = constraint.EvaluateLeftSide(variableValues);
                double slack = constraint.GetSlack(variableValues);
                bool satisfied = constraint.IsSatisfied(variableValues);

                string status = satisfied ? "✓" : "✗";
                AddLine($"  {constraint.Name}: {Math.Round(leftSide, 3):F3} {GetConstraintSymbol(constraint.Type)} {constraint.RightHandSide:F3} {status} (Slack: {Math.Round(slack, 3):F3})");
            }
        }

        /// <summary>
        /// Writes sensitivity analysis results to the output
        /// Shows ranges for variables and RHS values, shadow prices, etc.
        /// </summary>
        /// <param name="analysisResults">Dictionary containing sensitivity analysis data</param>
        public void WriteSensitivityAnalysis(Dictionary<string, object> analysisResults)
        {
            AddSection("SENSITIVITY ANALYSIS");
            AddLine("=" + new string('=', 50));

            if (analysisResults == null || analysisResults.Count == 0)
            {
                AddLine("No sensitivity analysis results available.");
                AddSectionSeparator();
                return;
            }

            // Write shadow prices if available
            if (analysisResults.ContainsKey("ShadowPrices"))
            {
                AddLine("SHADOW PRICES:");
                var shadowPrices = analysisResults["ShadowPrices"] as Dictionary<string, double>;
                if (shadowPrices != null)
                {
                    foreach (var kvp in shadowPrices)
                    {
                        AddLine($"  {kvp.Key}: {Math.Round(kvp.Value, 3):F3}");
                    }
                }
                AddLine();
            }

            // Write variable ranges if available
            if (analysisResults.ContainsKey("VariableRanges"))
            {
                AddLine("VARIABLE COEFFICIENT RANGES:");
                var ranges = analysisResults["VariableRanges"] as Dictionary<string, (double min, double max)>;
                if (ranges != null)
                {
                    foreach (var kvp in ranges)
                    {
                        AddLine($"  {kvp.Key}: [{Math.Round(kvp.Value.min, 3):F3}, {Math.Round(kvp.Value.max, 3):F3}]");
                    }
                }
                AddLine();
            }

            // Write RHS ranges if available
            if (analysisResults.ContainsKey("RHSRanges"))
            {
                AddLine("RIGHT-HAND-SIDE RANGES:");
                var rhsRanges = analysisResults["RHSRanges"] as Dictionary<string, (double min, double max)>;
                if (rhsRanges != null)
                {
                    foreach (var kvp in rhsRanges)
                    {
                        AddLine($"  {kvp.Key}: [{Math.Round(kvp.Value.min, 3):F3}, {Math.Round(kvp.Value.max, 3):F3}]");
                    }
                }
                AddLine();
            }

            AddSectionSeparator();
        }

        /// <summary>
        /// Writes Branch and Bound tree information
        /// Shows the branching process for integer programming problems
        /// </summary>
        /// <param name="nodeInfo">Information about current B&B node</param>
        /// <param name="level">Depth level in the B&B tree</param>
        /// <param name="bounds">Current lower and upper bounds</param>
        public void WriteBranchAndBoundNode(string nodeInfo, int level, double lowerBound = 0, double upperBound = double.PositiveInfinity)
        {
            string indent = new string('  ', level); // Indent based on tree level
            AddLine($"{indent}NODE: {nodeInfo}");

            if (upperBound != double.PositiveInfinity)
            {
                AddLine($"{indent}Bounds: LB = {Math.Round(lowerBound, 3):F3}, UB = {Math.Round(upperBound, 3):F3}");
            }
            else
            {
                AddLine($"{indent}Bounds: LB = {Math.Round(lowerBound, 3):F3}, UB = ∞");
            }
            AddLine();
        }

        /// <summary>
        /// Writes duality analysis results
        /// Shows primal vs dual problem comparison and strong/weak duality verification
        /// </summary>
        /// <param name="primalValue">Optimal value of primal problem</param>
        /// <param name="dualValue">Optimal value of dual problem</param>
        /// <param name="dualityGap">Gap between primal and dual (should be 0 for strong duality)</param>
        /// <param name="dualSolution">Optimal solution of dual problem</param>
        public void WriteDualityAnalysis(double primalValue, double dualValue, double dualityGap, Dictionary<string, double> dualSolution)
        {
            AddSection("DUALITY ANALYSIS");
            AddLine("=" + new string('=', 50));

            AddLine($"Primal Optimal Value: {Math.Round(primalValue, 3):F3}");
            AddLine($"Dual Optimal Value: {Math.Round(dualValue, 3):F3}");
            AddLine($"Duality Gap: {Math.Round(dualityGap, 6):F6}");
            AddLine();

            // Determine duality type
            if (Math.Abs(dualityGap) < 1e-6)
            {
                AddLine("STRONG DUALITY holds - Primal and dual optimal values are equal.");
            }
            else
            {
                AddLine("WEAK DUALITY detected - There is a gap between primal and dual values.");
            }
            AddLine();

            // Write dual solution
            if (dualSolution != null && dualSolution.Count > 0)
            {
                AddLine("Dual Variable Values:");
                foreach (var kvp in dualSolution)
                {
                    AddLine($"  {kvp.Key} = {Math.Round(kvp.Value, 3):F3}");
                }
            }

            AddSectionSeparator();
        }

        /// <summary>
        /// Helper method to write a formatted tableau
        /// Creates a clean, aligned display of the simplex tableau
        /// </summary>
        /// <param name="tableau">2D array representing the tableau</param>
        /// <param name="basicVariables">List of basic variable names</param>
        private void WriteTableauFormatted(double[,] tableau, List<string> basicVariables)
        {
            if (tableau == null) return;

            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            // Create column headers
            var headerBuilder = new StringBuilder();
            headerBuilder.Append("Basic".PadRight(8));

            for (int j = 0; j < cols - 1; j++)
            {
                headerBuilder.Append($"x{j + 1}".PadLeft(10));
            }
            headerBuilder.Append("RHS".PadLeft(10));

            AddLine(headerBuilder.ToString());
            AddLine(new string('-', headerBuilder.Length));

            // Write data rows
            for (int i = 0; i < rows; i++)
            {
                var rowBuilder = new StringBuilder();

                // Basic variable or objective row indicator
                if (i < rows - 1)
                {
                    string basicVar = i < basicVariables.Count ? basicVariables[i] : $"s{i + 1}";
                    rowBuilder.Append(basicVar.PadRight(8));
                }
                else
                {
                    rowBuilder.Append("Z".PadRight(8));
                }

                // Tableau values
                for (int j = 0; j < cols; j++)
                {
                    rowBuilder.Append(Math.Round(tableau[i, j], 3).ToString("F3").PadLeft(10));
                }

                AddLine(rowBuilder.ToString());
            }

            AddLine();
        }

        /// <summary>
        /// Adds a major section header to the output
        /// </summary>
        /// <param name="sectionTitle">Title of the section</param>
        private void AddSection(string sectionTitle)
        {
            AddLine();
            AddLine($"### {sectionTitle} ###");
        }

        /// <summary>
        /// Adds a section separator line
        /// </summary>
        private void AddSectionSeparator()
        {
            AddLine();
            AddLine(new string('=', 80));
            AddLine();
        }

        /// <summary>
        /// Adds a single line to the output buffer
        /// </summary>
        /// <param name="line">Line to add (empty string for blank line)</param>
        private void AddLine(string line = "")
        {
            _outputContent.AppendLine(line);
        }

        /// <summary>
        /// Gets the constraint symbol for display purposes
        /// </summary>
        /// <param name="type">Constraint type enum</param>
        /// <returns>String representation of constraint symbol</returns>
        private string GetConstraintSymbol(ConstraintType type)
        {
            return type switch
            {
                ConstraintType.LessThanOrEqual => "≤",
                ConstraintType.GreaterThanOrEqual => "≥",
                ConstraintType.Equal => "=",
                _ => "?"
            };
        }

        /// <summary>
        /// Writes all accumulated content to the output file
        /// This should be called at the end to actually create the file
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                // Write header with timestamp
                var finalContent = new StringBuilder();
                finalContent.AppendLine("LINEAR PROGRAMMING SOLUTION OUTPUT");
                finalContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalContent.AppendLine(new string('=', 80));
                finalContent.AppendLine();

                // Add all accumulated content
                finalContent.Append(_outputContent.ToString());

                // Write footer
                finalContent.AppendLine();
                finalContent.AppendLine(new string('=', 80));
                finalContent.AppendLine("End of Output");

                // Write to file
                File.WriteAllText(_outputFilePath, finalContent.ToString());

                Console.WriteLine($"✓ Output successfully written to: {_outputFilePath}");
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to write output file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears all accumulated content
        /// Useful for starting a new output session
        /// </summary>
        public void ClearContent()
        {
            _outputContent.Clear();
        }

        /// <summary>
        /// Gets the current accumulated content as a string
        /// Useful for preview or debugging purposes
        /// </summary>
        /// <returns>Current output content</returns>
        public string GetCurrentContent()
        {
            return _outputContent.ToString();
        }

        /// <summary>
        /// Writes error information to the output
        /// Used when algorithms encounter errors or special cases
        /// </summary>
        /// <param name="errorType">Type of error encountered</param>
        /// <param name="errorMessage">Detailed error message</param>
        /// <param name="algorithmUsed">Algorithm that encountered the error</param>
        public void WriteError(string errorType, string errorMessage, string algorithmUsed)
        {
            AddSection($"ERROR ENCOUNTERED - {errorType}");
            AddLine("=" + new string('=', 50));

            AddLine($"Algorithm: {algorithmUsed}");
            AddLine($"Error Type: {errorType}");
            AddLine($"Details: {errorMessage}");
            AddLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            AddSectionSeparator();
        }

        /// <summary>
        /// Writes algorithm-specific information
        /// Used for documenting which algorithm was selected and why
        /// </summary>
        /// <param name="algorithmName">Name of the selected algorithm</param>
        /// <param name="reason">Reason for selecting this algorithm</param>
        /// <param name="parameters">Algorithm-specific parameters used</param>
        public void WriteAlgorithmInfo(string algorithmName, string reason, Dictionary<string, object> parameters = null)
        {
            AddSection($"ALGORITHM: {algorithmName}");
            AddLine("=" + new string('=', 50));

            AddLine($"Selected Algorithm: {algorithmName}");
            AddLine($"Selection Reason: {reason}");

            if (parameters != null && parameters.Count > 0)
            {
                AddLine();
                AddLine("Algorithm Parameters:");
                foreach (var kvp in parameters)
                {
                    AddLine($"  {kvp.Key}: {kvp.Value}");
                }
            }

            AddSectionSeparator();
        }
    }
}
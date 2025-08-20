using System;
using System.Collections.Generic;
using System.IO;
using LinearProgrammingProject.Models;
using LinearProgrammingProject.IO;
using LinearProgrammingProject.Utilities;

namespace LinearProgrammingProject.Utilities
{
    /// <summary>
    /// Main menu system that orchestrates the entire Linear Programming application
    /// This class serves as the primary user interface and coordinates between:
    /// - File input/output operations
    /// - Model parsing and validation
    /// - Algorithm selection and execution
    /// - Results display and analysis
    /// - Error handling and recovery
    /// 
    /// The menu system implements a hierarchical structure:
    /// 1. Main Menu - Primary application flow control
    /// 2. Algorithm Selection Menu - Choose solution method
    /// 3. Sensitivity Analysis Menu - Post-solution analysis options
    /// 4. Results Display Menu - Output formatting and export options
    /// </summary>
    public class MenuSystem
    {
        // Core application components
        private LinearProgrammingModel _currentModel;      // Currently loaded LP model
        private InputReader _inputReader;                  // Handles file reading and parsing
        private OutputWriter _outputWriter;                // Manages output formatting and file writing
        private CanonicalFormConverter _canonicalConverter; // Converts models to canonical form

        // Application state tracking
        private bool _modelLoaded = false;                 // Whether a model has been successfully loaded
        private bool _modelSolved = false;                 // Whether current model has been solved
        private string _lastUsedAlgorithm = "";           // Track which algorithm was last used
        private Dictionary<string, object> _solutionData; // Store solution results and metadata

        // Configuration settings
        private bool _showDetailedOutput = true;          // Whether to show detailed algorithm steps
        private bool _autoSaveResults = true;             // Whether to automatically save results to file

        /// <summary>
        /// Constructor initializes the menu system and sets up application state
        /// </summary>
        public MenuSystem()
        {
            _solutionData = new Dictionary<string, object>();
            Console.WriteLine("Linear Programming Solver - Version 1.0");
            Console.WriteLine("Developed for LPR381 Project");
            Console.WriteLine(new string('=', 60));
        }

        /// <summary>
        /// Main entry point for the application
        /// This method controls the primary application flow and handles top-level error recovery
        /// </summary>
        public void Run()
        {
            try
            {
                // Display welcome message and initial setup
                DisplayWelcomeMessage();

                // Main application loop - continues until user exits
                bool continueRunning = true;
                while (continueRunning)
                {
                    try
                    {
                        // Display main menu and get user selection
                        DisplayMainMenu();
                        int choice = GetUserChoice(1, 7); // Main menu has options 1-7

                        // Process user selection
                        continueRunning = ProcessMainMenuChoice(choice);
                    }
                    catch (Exception ex)
                    {
                        // Handle any unexpected errors in main loop
                        var errorInfo = ErrorHandler.HandleUserInputError(
                            ex.Message, "Valid menu selection", "main menu loop"
                        );

                        Console.WriteLine("An unexpected error occurred. Please try again.");
                        WaitForKeyPress();
                    }
                }

                // Application shutdown procedures
                DisplayExitMessage();
            }
            catch (Exception ex)
            {
                // Handle catastrophic application errors
                Console.WriteLine($"Critical application error: {ex.Message}");
                Console.WriteLine("Please restart the application.");
                ErrorHandler.HandleAlgorithmError(ex, "Application", "Critical startup/shutdown");
            }
        }

        /// <summary>
        /// Displays the main application menu with current status information
        /// Shows different options based on whether a model is loaded and solved
        /// </summary>
        private void DisplayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("LINEAR PROGRAMMING SOLVER - MAIN MENU");
            Console.WriteLine(new string('=', 60));

            // Display current application status
            Console.WriteLine($"Status: Model Loaded: {(_modelLoaded ? "YES" : "NO")} | " +
                            $"Model Solved: {(_modelSolved ? "YES" : "NO")}");

            if (_modelLoaded && _currentModel != null)
            {
                Console.WriteLine($"Current Model: {_currentModel.VariableCount} variables, " +
                                $"{_currentModel.ConstraintCount} constraints");
                Console.WriteLine($"Model Type: {(_currentModel.IsIntegerProgram() ?
                    (_currentModel.IsBinaryProgram() ? "Binary IP" : "Integer IP") : "Linear Program")}");
            }

            if (_modelSolved && !string.IsNullOrEmpty(_lastUsedAlgorithm))
            {
                Console.WriteLine($"Last Algorithm Used: {_lastUsedAlgorithm}");
                Console.WriteLine($"Solution Status: {_currentModel?.Status}");
            }

            Console.WriteLine();

            // Display menu options
            Console.WriteLine("MENU OPTIONS:");
            Console.WriteLine("1. Load Linear Programming Model from File");
            Console.WriteLine("2. Display Current Model Information");

            // Algorithm options - only show if model is loaded
            if (_modelLoaded)
            {
                Console.WriteLine("3. Solve Model using Selected Algorithm");
                Console.WriteLine("4. View Solution Results");

                // Sensitivity analysis - only show if model is solved
                if (_modelSolved && _currentModel?.Status == SolutionStatus.Optimal)
                {
                    Console.WriteLine("5. Perform Sensitivity Analysis");
                }
                else
                {
                    Console.WriteLine("5. Perform Sensitivity Analysis (Requires Optimal Solution)");
                }
            }
            else
            {
                Console.WriteLine("3. Solve Model using Selected Algorithm (Load Model First)");
                Console.WriteLine("4. View Solution Results (Load Model First)");
                Console.WriteLine("5. Perform Sensitivity Analysis (Load Model First)");
            }

            Console.WriteLine("6. Application Settings and Configuration");
            Console.WriteLine("7. Exit Application");
            Console.WriteLine();
            Console.WriteLine(new string('-', 60));
        }

        /// <summary>
        /// Processes the user's main menu selection and routes to appropriate functionality
        /// </summary>
        /// <param name="choice">User's menu selection (1-7)</param>
        /// <returns>True to continue running, false to exit application</returns>
        private bool ProcessMainMenuChoice(int choice)
        {
            switch (choice)
            {
                case 1:
                    LoadModelFromFile();
                    break;

                case 2:
                    DisplayModelInformation();
                    break;

                case 3:
                    if (_modelLoaded)
                    {
                        SolveModelMenu();
                    }
                    else
                    {
                        Console.WriteLine("Please load a model first (Option 1).");
                        WaitForKeyPress();
                    }
                    break;

                case 4:
                    if (_modelLoaded)
                    {
                        DisplaySolutionResults();
                    }
                    else
                    {
                        Console.WriteLine("Please load a model first (Option 1).");
                        WaitForKeyPress();
                    }
                    break;

                case 5:
                    if (_modelLoaded && _modelSolved && _currentModel?.Status == SolutionStatus.Optimal)
                    {
                        SensitivityAnalysisMenu();
                    }
                    else if (_modelLoaded && !_modelSolved)
                    {
                        Console.WriteLine("Please solve the model first (Option 3).");
                        WaitForKeyPress();
                    }
                    else if (_modelLoaded && _currentModel?.Status != SolutionStatus.Optimal)
                    {
                        Console.WriteLine("Sensitivity analysis requires an optimal solution.");
                        Console.WriteLine($"Current status: {_currentModel?.Status}");
                        WaitForKeyPress();
                    }
                    else
                    {
                        Console.WriteLine("Please load and solve a model first (Options 1 and 3).");
                        WaitForKeyPress();
                    }
                    break;

                case 6:
                    ApplicationSettingsMenu();
                    break;

                case 7:
                    return false; // Exit application

                default:
                    Console.WriteLine("Invalid selection. Please choose 1-7.");
                    WaitForKeyPress();
                    break;
            }

            return true; // Continue running
        }

        /// <summary>
        /// Handles loading a Linear Programming model from an input file
        /// Manages file selection, parsing, validation, and error recovery
        /// </summary>
        private void LoadModelFromFile()
        {
            Console.Clear();
            Console.WriteLine("LOAD LINEAR PROGRAMMING MODEL");
            Console.WriteLine(new string('=', 40));

            try
            {
                // Get input file path from user
                Console.WriteLine("Enter the path to your input file:");
                Console.WriteLine("(Example: C:\\models\\problem1.txt or /home/user/models/problem1.txt)");
                Console.Write("File Path: ");

                string filePath = Console.ReadLine()?.Trim();

                // Validate file path input
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    ErrorHandler.HandleUserInputError("", "Non-empty file path", "file path input");
                    Console.WriteLine("File path cannot be empty.");
                    WaitForKeyPress();
                    return;
                }

                // Check if file exists before attempting to read
                if (!File.Exists(filePath))
                {
                    ErrorHandler.HandleFileError(
                        new FileNotFoundException($"File not found: {filePath}"),
                        filePath,
                        "model loading"
                    );
                    WaitForKeyPress();
                    return;
                }

                Console.WriteLine($"Loading model from: {filePath}");
                Console.WriteLine("Please wait...");

                // Create input reader and attempt to load model
                _inputReader = new InputReader(filePath);

                // Validate file format first (quick check)
                if (!_inputReader.ValidateFileFormat())
                {
                    Console.WriteLine("Warning: File format may be invalid.");
                    Console.Write("Continue anyway? (y/N): ");
                    string response = Console.ReadLine()?.ToLower();

                    if (response != "y" && response != "yes")
                    {
                        Console.WriteLine("Model loading cancelled.");
                        WaitForKeyPress();
                        return;
                    }
                }

                // Load and parse the model
                _currentModel = _inputReader.ReadModel();

                // Validate the loaded model
                if (!_currentModel.IsValid())
                {
                    ErrorHandler.HandleValidationError(
                        new InvalidOperationException("Model validation failed"),
                        "complete model"
                    );
                    WaitForKeyPress();
                    return;
                }

                // Successfully loaded - update application state
                _modelLoaded = true;
                _modelSolved = false; // Reset solution state
                _lastUsedAlgorithm = "";
                _solutionData.Clear();

                // Display success information
                Console.WriteLine();
                Console.WriteLine("Model loaded successfully!");
                Console.WriteLine($"Variables: {_currentModel.VariableCount}");
                Console.WriteLine($"Constraints: {_currentModel.ConstraintCount}");
                Console.WriteLine($"Objective: {_currentModel.ObjectiveType}");
                Console.WriteLine($"Type: {(_currentModel.IsIntegerProgram() ?
                    (_currentModel.IsBinaryProgram() ? "Binary Integer Program" : "Integer Program") :
                    "Linear Program")}");

                // Ask if user wants to see model details
                Console.WriteLine();
                Console.Write("Display model details? (y/N): ");
                string showDetails = Console.ReadLine()?.ToLower();

                if (showDetails == "y" || showDetails == "yes")
                {
                    DisplayModelInformation();
                }
                else
                {
                    WaitForKeyPress();
                }
            }
            catch (Exception ex)
            {
                // Handle any errors during model loading
                ErrorHandler.HandleFileError(ex, filePath ?? "unknown", "model loading");
                _modelLoaded = false;
                _currentModel = null;
                WaitForKeyPress();
            }
        }

        /// <summary>
        /// Displays detailed information about the currently loaded model
        /// Shows objective function, constraints, variable types, and model statistics
        /// </summary>
        private void DisplayModelInformation()
        {
            Console.Clear();

            if (!_modelLoaded || _currentModel == null)
            {
                Console.WriteLine("No model currently loaded.");
                Console.WriteLine("Please load a model first (Main Menu Option 1).");
                WaitForKeyPress();
                return;
            }

            Console.WriteLine("CURRENT MODEL INFORMATION");
            Console.WriteLine(new string('=', 50));

            // Display model summary
            Console.WriteLine(_currentModel.GetModelSummary());
            Console.WriteLine();

            // Display objective function
            Console.WriteLine("OBJECTIVE FUNCTION:");
            Console.WriteLine(_currentModel.GetObjectiveFunctionString());
            Console.WriteLine();

            // Display constraints
            Console.WriteLine("CONSTRAINTS:");
            var constraintStrings = _currentModel.GetConstraintStrings();
            foreach (string constraint in constraintStrings)
            {
                Console.WriteLine($"  {constraint}");
            }
            Console.WriteLine();

            // Display variable information
            Console.WriteLine("VARIABLES:");
            for (int i = 0; i < _currentModel.Variables.Count; i++)
            {
                var variable = _currentModel.Variables[i];
                Console.WriteLine($"  {variable.Name}: {variable.Type} - {variable.GetBoundsString()}");
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 50));

            // If model has been solved, show solution status
            if (_modelSolved)
            {
                Console.WriteLine($"SOLUTION STATUS: {_currentModel.Status}");
                if (_currentModel.Status == SolutionStatus.Optimal)
                {
                    Console.WriteLine($"Optimal Value: {_currentModel.OptimalValue:F3}");
                }
                Console.WriteLine($"Algorithm Used: {_lastUsedAlgorithm}");
            }
            else
            {
                Console.WriteLine("MODEL STATUS: Loaded but not yet solved");
            }

            WaitForKeyPress();
        }

        /// <summary>
        /// Displays algorithm selection menu and handles algorithm execution
        /// Different algorithms are available based on problem type (LP vs IP)
        /// </summary>
        private void SolveModelMenu()
        {
            Console.Clear();
            Console.WriteLine("ALGORITHM SELECTION");
            Console.WriteLine(new string('=', 40));

            if (_currentModel == null)
            {
                Console.WriteLine("No model loaded.");
                WaitForKeyPress();
                return;
            }

            // Display problem type information
            Console.WriteLine($"Problem Type: {(_currentModel.IsIntegerProgram() ?
                (_currentModel.IsBinaryProgram() ? "Binary Integer Program" : "Integer Program") :
                "Linear Program")}");
            Console.WriteLine($"Variables: {_currentModel.VariableCount}, Constraints: {_currentModel.ConstraintCount}");
            Console.WriteLine();

            // Display available algorithms
            Console.WriteLine("AVAILABLE ALGORITHMS:");
            Console.WriteLine("1. Primal Simplex Algorithm");
            Console.WriteLine("2. Revised Primal Simplex Algorithm");

            // Integer programming algorithms (only show for integer problems)
            if (_currentModel.IsIntegerProgram())
            {
                Console.WriteLine("3. Branch and Bound Simplex Algorithm");
                Console.WriteLine("4. Cutting Plane Algorithm");

                // Knapsack-specific algorithm (only for binary problems)
                if (_currentModel.IsBinaryProgram())
                {
                    Console.WriteLine("5. Branch and Bound Knapsack Algorithm");
                }
            }
            else
            {
                Console.WriteLine("3. Branch and Bound Simplex (Integer Problems Only)");
                Console.WriteLine("4. Cutting Plane Algorithm (Integer Problems Only)");
                Console.WriteLine("5. Branch and Bound Knapsack (Binary Problems Only)");
            }

            Console.WriteLine("6. Return to Main Menu");
            Console.WriteLine();

            // Get algorithm selection
            int maxChoice = _currentModel.IsIntegerProgram() ?
                (_currentModel.IsBinaryProgram() ? 6 : 5) : 6;
            int algorithmChoice = GetUserChoice(1, maxChoice);

            if (algorithmChoice == maxChoice) // Return to main menu
            {
                return;
            }

            // Validate algorithm selection for problem type
            if (!_currentModel.IsIntegerProgram() && (algorithmChoice >= 3 && algorithmChoice <= 5))
            {
                Console.WriteLine("Selected algorithm is only available for Integer Programming problems.");
                Console.WriteLine("Your problem is a Linear Program. Please select algorithm 1 or 2.");
                WaitForKeyPress();
                return;
            }

            if (!_currentModel.IsBinaryProgram() && algorithmChoice == 5)
            {
                Console.WriteLine("Branch and Bound Knapsack algorithm is only for Binary Integer Programs.");
                Console.WriteLine("Your problem is not binary. Please select a different algorithm.");
                WaitForKeyPress();
                return;
            }

            // Execute selected algorithm
            ExecuteAlgorithm(algorithmChoice);
        }

        /// <summary>
        /// Executes the selected algorithm and handles the solution process
        /// This method coordinates between algorithm execution, output generation, and error handling
        /// </summary>
        /// <param name="algorithmChoice">Selected algorithm number (1-5)</param>
        private void ExecuteAlgorithm(int algorithmChoice)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("SOLVING LINEAR PROGRAMMING MODEL");
                Console.WriteLine(new string('=', 50));

                // Map algorithm choice to algorithm name
                string algorithmName = GetAlgorithmName(algorithmChoice);
                Console.WriteLine($"Selected Algorithm: {algorithmName}");
                Console.WriteLine("Processing...");
                Console.WriteLine();

                // Set up output writer for results
                string outputPath = GenerateOutputFilePath(algorithmName);
                _outputWriter = new OutputWriter(outputPath);

                // Write original problem to output
                _outputWriter.WriteOriginalProblem(_currentModel);

                // Convert to canonical form (required for all algorithms)
                Console.WriteLine("Converting to canonical form...");
                _canonicalConverter = new CanonicalFormConverter(_currentModel);
                var canonicalModel = _canonicalConverter.ConvertToCanonicalForm();

                // Create initial tableau
                var initialTableau = _canonicalConverter.CreateInitialTableau();
                var basicVariables = _canonicalConverter.GetInitialBasicVariables();
                var nonBasicVariables = _canonicalConverter.GetInitialNonBasicVariables();

                // Write canonical form to output
                _outputWriter.WriteCanonicalForm(initialTableau, basicVariables, nonBasicVariables);
                _outputWriter.WriteAlgorithmInfo(algorithmName, GetAlgorithmSelectionReason(algorithmChoice));

                // Execute the specific algorithm
                bool solutionFound = ExecuteSpecificAlgorithm(algorithmChoice, canonicalModel, initialTableau, basicVariables);

                if (solutionFound)
                {
                    // Process and display solution
                    ProcessSolutionResults(algorithmName);

                    // Update application state
                    _modelSolved = true;
                    _lastUsedAlgorithm = algorithmName;

                    Console.WriteLine($"Solution completed successfully!");
                    Console.WriteLine($"Results saved to: {outputPath}");
                }
                else
                {
                    Console.WriteLine("Algorithm completed but no optimal solution found.");
                    Console.WriteLine("Check output file for details about the solution status.");
                }

                // Save output file
                _outputWriter.SaveToFile();

            }
            catch (Exception ex)
            {
                // Handle algorithm execution errors
                var errorInfo = ErrorHandler.HandleAlgorithmError(ex, GetAlgorithmName(algorithmChoice), "execution");

                // Write error information to output if writer is available
                if (_outputWriter != null)
                {
                    _outputWriter.WriteError("Algorithm Execution Error", ex.Message, GetAlgorithmName(algorithmChoice));
                    try
                    {
                        _outputWriter.SaveToFile();
                    }
                    catch { } // Don't let output writing errors mask the original error
                }
            }
            finally
            {
                WaitForKeyPress();
            }
        }

        /// <summary>
        /// Placeholder for executing specific algorithm implementations
        /// This method would coordinate with the actual algorithm classes once they're implemented
        /// </summary>
        /// <param name="algorithmChoice">Selected algorithm number</param>
        /// <param name="canonicalModel">Model in canonical form</param>
        /// <param name="initialTableau">Initial simplex tableau</param>
        /// <param name="basicVariables">Initial basic variables</param>
        /// <returns>True if solution was found successfully</returns>
        private bool ExecuteSpecificAlgorithm(int algorithmChoice, LinearProgrammingModel canonicalModel,
                                            double[,] initialTableau, List<string> basicVariables)
        {
            string algorithmName = GetAlgorithmName(algorithmChoice);
            Console.WriteLine($"Executing {algorithmName}...");

            // NOTE: This is where you would integrate with the actual algorithm implementations
            // For now, this is a placeholder that demonstrates the integration structure

            try
            {
                switch (algorithmChoice)
                {
                    case 1: // Primal Simplex Algorithm
                        Console.WriteLine("Initializing Primal Simplex Algorithm...");
                        // TODO: Integrate with Algorithms/PrimalSimplex.cs
                        // var primalSimplex = new PrimalSimplex(canonicalModel);
                        // var result = primalSimplex.Solve();
                        return SimulatePrimalSimplex(canonicalModel, initialTableau, basicVariables);

                    case 2: // Revised Primal Simplex Algorithm  
                        Console.WriteLine("Initializing Revised Primal Simplex Algorithm...");
                        // TODO: Integrate with Algorithms/RevisedPrimalSimplex.cs
                        return SimulateRevisedPrimalSimplex(canonicalModel, initialTableau, basicVariables);

                    case 3: // Branch and Bound Simplex
                        Console.WriteLine("Initializing Branch and Bound Simplex Algorithm...");
                        // TODO: Integrate with Algorithms/BranchAndBoundSimplex.cs
                        return SimulateBranchAndBound(canonicalModel, initialTableau, basicVariables);

                    case 4: // Cutting Plane Algorithm
                        Console.WriteLine("Initializing Cutting Plane Algorithm...");
                        // TODO: Integrate with Algorithms/CuttingPlane.cs
                        return SimulateCuttingPlane(canonicalModel, initialTableau, basicVariables);

                    case 5: // Branch and Bound Knapsack
                        Console.WriteLine("Initializing Branch and Bound Knapsack Algorithm...");
                        // TODO: Integrate with Algorithms/KnapsackBranchAndBound.cs
                        return SimulateKnapsackBranchAndBound(canonicalModel);

                    default:
                        throw new ArgumentException($"Unknown algorithm choice: {algorithmChoice}");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleAlgorithmError(ex, algorithmName, "algorithm execution");
                return false;
            }
        }

        // PLACEHOLDER METHODS FOR ALGORITHM SIMULATION
        // These methods simulate algorithm execution for demonstration purposes
        // Replace these with actual algorithm class integrations

        private bool SimulatePrimalSimplex(LinearProgrammingModel model, double[,] tableau, List<string> basicVars)
        {
            Console.WriteLine("Simulating Primal Simplex iterations...");

            // Simulate multiple iterations
            for (int iteration = 1; iteration <= 3; iteration++)
            {
                Console.WriteLine($"  Iteration {iteration}...");
                _outputWriter.WriteIteration(iteration, tableau, basicVars,
                    iteration < 3 ? 0 : -1, iteration < 3 ? 1 : -1, iteration == 3);
                System.Threading.Thread.Sleep(500); // Simulate processing time
            }

            // Set mock optimal solution
            model.Status = SolutionStatus.Optimal;
            model.OptimalValue = 150.0;
            model.OptimalSolution["x1"] = 25.0;
            model.OptimalSolution["x2"] = 0.0;

            return true;
        }

        private bool SimulateRevisedPrimalSimplex(LinearProgrammingModel model, double[,] tableau, List<string> basicVars)
        {
            Console.WriteLine("Simulating Revised Primal Simplex with product form updates...");

            for (int iteration = 1; iteration <= 4; iteration++)
            {
                Console.WriteLine($"  Product Form Update {iteration}...");
                _outputWriter.WriteIteration(iteration, tableau, basicVars, -1, -1, iteration == 4);
                System.Threading.Thread.Sleep(300);
            }

            model.Status = SolutionStatus.Optimal;
            model.OptimalValue = 150.0;
            return true;
        }

        private bool SimulateBranchAndBound(LinearProgrammingModel model, double[,] tableau, List<string> basicVars)
        {
            Console.WriteLine("Simulating Branch and Bound tree...");

            _outputWriter.WriteBranchAndBoundNode("Root Node - LP Relaxation", 0, 0, 200);
            _outputWriter.WriteBranchAndBoundNode("Branch x1 <= 10", 1, 0, 180);
            _outputWriter.WriteBranchAndBoundNode("Branch x1 >= 11", 1, 0, 175);

            model.Status = SolutionStatus.Optimal;
            model.OptimalValue = 175.0;
            return true;
        }

        private bool SimulateCuttingPlane(LinearProgrammingModel model, double[,] tableau, List<string> basicVars)
        {
            Console.WriteLine("Simulating Cutting Plane iterations...");

            for (int cut = 1; cut <= 2; cut++)
            {
                Console.WriteLine($"  Adding cutting plane {cut}...");
                System.Threading.Thread.Sleep(400);
            }

            model.Status = SolutionStatus.Optimal;
            model.OptimalValue = 160.0;
            return true;
        }

        private bool SimulateKnapsackBranchAndBound(LinearProgrammingModel model)
        {
            Console.WriteLine("Simulating Knapsack Branch and Bound...");
            Console.WriteLine("  Calculating upper bounds using LP relaxation...");
            Console.WriteLine("  Branching on binary variables...");

            model.Status = SolutionStatus.Optimal;
            model.OptimalValue = 23.0;
            return true;
        }

        /// <summary>
        /// Processes solution results and updates the model with final values
        /// This method handles converting canonical form solutions back to original variables
        /// </summary>
        /// <param name="algorithmName">Name of algorithm used for solving</param>
        private void ProcessSolutionResults(string algorithmName)
        {
            try
            {
                // Convert solution from canonical form back to original problem
                if (_canonicalConverter != null && _currentModel.OptimalSolution.Count > 0)
                {
                    var originalSolution = _canonicalConverter.ConvertSolutionToOriginal(
                        _currentModel.OptimalSolution, out double originalObjectiveValue);

                    // Update model with converted solution
                    _currentModel.OptimalValue = originalObjectiveValue;
                    _currentModel.OptimalSolution = originalSolution;

                    // Update variable values
                    foreach (var kvp in originalSolution)
                    {
                        var variable = _currentModel.Variables.Find(v => v.Name == kvp.Key);
                        if (variable != null)
                        {
                            variable.CurrentValue = kvp.Value;
                        }
                    }
                }

                // Write solution to output
                int iterationCount = _solutionData.ContainsKey("IterationCount") ?
                                   (int)_solutionData["IterationCount"] : 0;

                _outputWriter.WriteSolution(_currentModel, algorithmName, iterationCount);

                // Store solution data for later analysis
                _solutionData["Algorithm"] = algorithmName;
                _solutionData["OptimalValue"] = _currentModel.OptimalValue;
                _solutionData["SolutionStatus"] = _currentModel.Status;
                _solutionData["SolutionTime"] = DateTime.Now;

            }
            catch (Exception ex)
            {
                ErrorHandler.HandleAlgorithmError(ex, algorithmName, "solution processing");
            }
        }

        /// <summary>
        /// Displays the solution results in a user-friendly format
        /// Shows optimal values, variable assignments, and constraint satisfaction
        /// </summary>
        private void DisplaySolutionResults()
        {
            Console.Clear();

            if (!_modelSolved || _currentModel == null)
            {
                Console.WriteLine("No solution available.");
                Console.WriteLine("Please solve a model first (Main Menu Option 3).");
                WaitForKeyPress();
                return;
            }

            Console.WriteLine("SOLUTION RESULTS");
            Console.WriteLine(new string('=', 40));

            Console.WriteLine($"Algorithm Used: {_lastUsedAlgorithm}");
            Console.WriteLine($"Solution Status: {_currentModel.Status}");
            Console.WriteLine();

            switch (_currentModel.Status)
            {
                case SolutionStatus.Optimal:
                    DisplayOptimalSolution();
                    break;

                case SolutionStatus.Infeasible:
                    Console.WriteLine("The problem is INFEASIBLE.");
                    Console.WriteLine("No feasible solution exists that satisfies all constraints.");
                    Console.WriteLine("Consider:");
                    Console.WriteLine("- Relaxing some constraints");
                    Console.WriteLine("- Checking for contradictory constraints");
                    Console.WriteLine("- Verifying constraint formulation");
                    break;

                case SolutionStatus.Unbounded:
                    Console.WriteLine("The problem is UNBOUNDED.");
                    Console.WriteLine("The objective function can be improved indefinitely.");
                    Console.WriteLine("Consider:");
                    Console.WriteLine("- Adding missing constraints");
                    Console.WriteLine("- Setting realistic upper bounds");
                    Console.WriteLine("- Checking constraint formulation");
                    break;

                default:
                    Console.WriteLine("Solution status is unknown or an error occurred.");
                    break;
            }

            WaitForKeyPress();
        }

        /// <summary>
        /// Displays detailed information about the optimal solution
        /// Shows variable values, objective value, and constraint analysis
        /// </summary>
        private void DisplayOptimalSolution()
        {
            Console.WriteLine("OPTIMAL SOLUTION FOUND!");
            Console.WriteLine();

            Console.WriteLine($"Optimal Objective Value: {_currentModel.OptimalValue:F3}");
            Console.WriteLine();

            Console.WriteLine("Optimal Variable Values:");
            foreach (var variable in _currentModel.Variables)
            {
                Console.WriteLine($"  {variable.Name} = {variable.CurrentValue:F3}");
            }
            Console.WriteLine();

            // Check constraint satisfaction
            Console.WriteLine("Constraint Analysis:");
            for (int i = 0; i < _currentModel.Constraints.Count; i++)
            {
                var constraint = _currentModel.Constraints[i];
                var variableValues = new List<double>();

                foreach (var variable in _currentModel.Variables)
                {
                    variableValues.Add(variable.CurrentValue);
                }

                double leftSide = constraint.EvaluateLeftSide(variableValues);
                double slack = constraint.GetSlack(variableValues);
                bool satisfied = constraint.IsSatisfied(variableValues);

                string status = satisfied ? "✓ Satisfied" : "✗ Violated";
                string constraintSymbol = GetConstraintSymbol(constraint.Type);

                Console.WriteLine($"  {constraint.Name}: {leftSide:F3} {constraintSymbol} {constraint.RightHandSide:F3} - {status}");
                Console.WriteLine($"    Slack/Surplus: {slack:F3}");
            }
        }

        /// <summary>
        /// Displays sensitivity analysis menu and handles analysis operations
        /// Only available for optimally solved linear programs
        /// </summary>
        private void SensitivityAnalysisMenu()
        {
            Console.Clear();
            Console.WriteLine("SENSITIVITY ANALYSIS");
            Console.WriteLine(new string('=', 40));

            if (_currentModel?.Status != SolutionStatus.Optimal)
            {
                Console.WriteLine("Sensitivity analysis requires an optimal solution.");
                WaitForKeyPress();
                return;
            }

            Console.WriteLine("SENSITIVITY ANALYSIS OPTIONS:");
            Console.WriteLine("1. Variable Coefficient Ranges");
            Console.WriteLine("2. Right-Hand-Side Ranges");
            Console.WriteLine("3. Shadow Prices");
            Console.WriteLine("4. Add New Activity");
            Console.WriteLine("5. Add New Constraint");
            Console.WriteLine("6. Duality Analysis");
            Console.WriteLine("7. Return to Main Menu");
            Console.WriteLine();

            int choice = GetUserChoice(1, 7);

            if (choice == 7) return;

            ExecuteSensitivityAnalysis(choice);
        }

        /// <summary>
        /// Executes the selected sensitivity analysis operation
        /// This is a placeholder for integrating with sensitivity analysis algorithms
        /// </summary>
        /// <param name="analysisChoice">Selected analysis type (1-6)</param>
        private void ExecuteSensitivityAnalysis(int analysisChoice)
        {
            Console.Clear();
            Console.WriteLine("SENSITIVITY ANALYSIS RESULTS");
            Console.WriteLine(new string('=', 50));

            // Placeholder implementation - integrate with actual sensitivity analysis classes
            switch (analysisChoice)
            {
                case 1:
                    Console.WriteLine("VARIABLE COEFFICIENT RANGES:");
                    Console.WriteLine("(This would show ranges for objective coefficients)");
                    // TODO: Integrate with SensitivityAnalysis/SensitivityEngine.cs
                    break;

                case 2:
                    Console.WriteLine("RIGHT-HAND-SIDE RANGES:");
                    Console.WriteLine("(This would show RHS value ranges)");
                    break;

                case 3:
                    Console.WriteLine("SHADOW PRICES:");
                    Console.WriteLine("(This would show dual variable values)");
                    break;

                case 4:
                    Console.WriteLine("ADD NEW ACTIVITY:");
                    Console.WriteLine("(This would analyze adding a new variable)");
                    break;

                case 5:
                    Console.WriteLine("ADD NEW CONSTRAINT:");
                    Console.WriteLine("(This would analyze adding a new constraint)");
                    break;

                case 6:
                    Console.WriteLine("DUALITY ANALYSIS:");
                    Console.WriteLine("(This would show dual problem analysis)");
                    break;
            }

            Console.WriteLine("Feature implementation pending - integrate with algorithm classes.");
            WaitForKeyPress();
        }

        /// <summary>
        /// Displays application settings and configuration options
        /// Allows users to customize behavior and preferences
        /// </summary>
        private void ApplicationSettingsMenu()
        {
            Console.Clear();
            Console.WriteLine("APPLICATION SETTINGS");
            Console.WriteLine(new string('=', 40));

            Console.WriteLine("CURRENT SETTINGS:");
            Console.WriteLine($"Show Detailed Output: {(_showDetailedOutput ? "Yes" : "No")}");
            Console.WriteLine($"Auto-Save Results: {(_autoSaveResults ? "Yes" : "No")}");
            Console.WriteLine();

            Console.WriteLine("SETTING OPTIONS:");
            Console.WriteLine("1. Toggle Detailed Output");
            Console.WriteLine("2. Toggle Auto-Save Results");
            Console.WriteLine("3. View Error History");
            Console.WriteLine("4. Clear Error History");
            Console.WriteLine("5. Return to Main Menu");
            Console.WriteLine();

            int choice = GetUserChoice(1, 5);

            switch (choice)
            {
                case 1:
                    _showDetailedOutput = !_showDetailedOutput;
                    Console.WriteLine($"Detailed output: {(_showDetailedOutput ? "Enabled" : "Disabled")}");
                    break;

                case 2:
                    _autoSaveResults = !_autoSaveResults;
                    Console.WriteLine($"Auto-save results: {(_autoSaveResults ? "Enabled" : "Disabled")}");
                    break;

                case 3:
                    DisplayErrorHistory();
                    break;

                case 4:
                    ErrorHandler.ClearErrorHistory();
                    break;

                case 5:
                    return;
            }

            if (choice != 3) // Error history has its own wait
            {
                WaitForKeyPress();
            }
        }

        /// <summary>
        /// Displays the error history for the current session
        /// Shows all errors encountered and their details
        /// </summary>
        //private void DisplayErrorHistory()
        //{
          //  Console.Clear();
           // Console.WriteLine("ERROR HISTORY");
            //Console.WriteLine(new string('=', 40));

           // var errorHistory = ErrorHandler.GetErrorHistory();
            //var errorStats = ErrorHandler.GetError

                ////////////////////////////////////////////////////////
            /// <summary>
            /// Displays the error history for the current session
            /// Shows all errors encountered and their details
            /// </summary>
        private void DisplayErrorHistory()
        {
            Console.Clear();
            Console.WriteLine("ERROR HISTORY");
            Console.WriteLine(new string('=', 40));

            var errorHistory = ErrorHandler.GetErrorHistory();
            var errorStats = ErrorHandler.GetErrorStatistics();

            Console.WriteLine($"Total Errors: {errorStats.TotalErrors}");
            Console.WriteLine($"File Errors: {errorStats.FileErrors}");
            Console.WriteLine($"Algorithm Errors: {errorStats.AlgorithmErrors}");
            Console.WriteLine($"Validation Errors: {errorStats.ValidationErrors}");
            Console.WriteLine($"User Input Errors: {errorStats.UserInputErrors}");
            Console.WriteLine();

            if (errorHistory.Count == 0)
            {
                Console.WriteLine("No errors recorded in this session.");
            }
            else
            {
                Console.WriteLine("DETAILED ERROR LOG:");
                Console.WriteLine(new string('-', 40));

                for (int i = 0; i < errorHistory.Count; i++)
                {
                    var error = errorHistory[i];
                    Console.WriteLine($"{i + 1}. [{error.Timestamp:yyyy-MM-dd HH:mm:ss}] {error.Type}");
                    Console.WriteLine($"   Context: {error.Context}");
                    Console.WriteLine($"   Message: {error.Message}");

                    if (!string.IsNullOrEmpty(error.AdditionalInfo))
                    {
                        Console.WriteLine($"   Details: {error.AdditionalInfo}");
                    }

                    Console.WriteLine();
                }
            }

            WaitForKeyPress();
        }

        // UTILITY METHODS

        /// <summary>
        /// Gets user choice from console input with validation
        /// Ensures input is within specified range
        /// </summary>
        /// <param name="minChoice">Minimum valid choice</param>
        /// <param name="maxChoice">Maximum valid choice</param>
        /// <returns>Valid user choice</returns>
        private int GetUserChoice(int minChoice, int maxChoice)
        {
            while (true)
            {
                Console.Write($"Enter your choice ({minChoice}-{maxChoice}): ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out int choice))
                {
                    if (choice >= minChoice && choice <= maxChoice)
                    {
                        return choice;
                    }
                    else
                    {
                        Console.WriteLine($"Please enter a number between {minChoice} and {maxChoice}.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid number.");
                }
            }
        }

        /// <summary>
        /// Maps algorithm choice number to algorithm name
        /// </summary>
        /// <param name="choice">Algorithm choice number</param>
        /// <returns>Algorithm name string</returns>
        private string GetAlgorithmName(int choice)
        {
            return choice switch
            {
                1 => "Primal Simplex Algorithm",
                2 => "Revised Primal Simplex Algorithm",
                3 => "Branch and Bound Simplex Algorithm",
                4 => "Cutting Plane Algorithm",
                5 => "Branch and Bound Knapsack Algorithm",
                _ => "Unknown Algorithm"
            };
        }

        /// <summary>
        /// Provides reasoning for why a specific algorithm was selected
        /// Used for documentation and output generation
        /// </summary>
        /// <param name="choice">Algorithm choice number</param>
        /// <returns>Selection reasoning string</returns>
        private string GetAlgorithmSelectionReason(int choice)
        {
            return choice switch
            {
                1 => "Standard linear programming problem - Primal Simplex provides efficient solution",
                2 => "Large-scale problem or numerical stability concerns - Revised Simplex reduces computational overhead",
                3 => "Integer programming problem - Branch and Bound explores integer solutions systematically",
                4 => "Integer programming problem - Cutting Plane adds constraints to eliminate fractional solutions",
                5 => "Binary knapsack problem - Specialized Branch and Bound for 0-1 variables",
                _ => "Algorithm selection reason not specified"
            };
        }

        /// <summary>
        /// Generates appropriate output file path based on algorithm and timestamp
        /// </summary>
        /// <param name="algorithmName">Name of algorithm being used</param>
        /// <returns>Complete file path for output</returns>
        private string GenerateOutputFilePath(string algorithmName)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string sanitizedAlgorithmName = algorithmName.Replace(" ", "_").Replace("&", "and");
            string filename = $"LP_Solution_{sanitizedAlgorithmName}_{timestamp}.txt";

            // Create output directory if it doesn't exist
            string outputDir = "Output";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return Path.Combine(outputDir, filename);
        }

        /// <summary>
        /// Gets constraint symbol for display purposes
        /// </summary>
        /// <param name="constraintType">Type of constraint</param>
        /// <returns>Symbol string (<=, >=, =)</returns>
        private string GetConstraintSymbol(ConstraintType constraintType)
        {
            return constraintType switch
            {
                ConstraintType.LessThanOrEqual => "<=",
                ConstraintType.GreaterThanOrEqual => ">=",
                ConstraintType.Equal => "=",
                _ => "?"
            };
        }

        /// <summary>
        /// Waits for user to press a key before continuing
        /// Provides consistent user interaction pattern
        /// </summary>
        private void WaitForKeyPress()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Displays welcome message and application information
        /// </summary>
        private void DisplayWelcomeMessage()
        {
            Console.Clear();
            Console.WriteLine("Welcome to the Linear Programming Solver!");
            Console.WriteLine();
            Console.WriteLine("This application supports:");
            Console.WriteLine("• Linear Programming (LP) problems");
            Console.WriteLine("• Integer Programming (IP) problems");
            Console.WriteLine("• Binary Integer Programming problems");
            Console.WriteLine("• Multiple solution algorithms");
            Console.WriteLine("• Sensitivity analysis");
            Console.WriteLine("• Comprehensive output reporting");
            Console.WriteLine();
            Console.WriteLine("To get started, load a model from a file (Option 1).");
            Console.WriteLine();
            WaitForKeyPress();
        }

        /// <summary>
        /// Displays exit message and cleanup information
        /// </summary>
        private void DisplayExitMessage()
        {
            Console.Clear();
            Console.WriteLine("Thank you for using the Linear Programming Solver!");
            Console.WriteLine();

            if (_modelSolved && _autoSaveResults)
            {
                Console.WriteLine("Your solution results have been automatically saved.");
            }

            var errorStats = ErrorHandler.GetErrorStatistics();
            if (errorStats.TotalErrors > 0)
            {
                Console.WriteLine($"Session completed with {errorStats.TotalErrors} error(s) logged.");
                Console.WriteLine("Check error history for details if needed.");
            }
            else
            {
                Console.WriteLine("Session completed successfully with no errors.");
            }

            Console.WriteLine();
            Console.WriteLine("Application developed for LPR381 Project");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
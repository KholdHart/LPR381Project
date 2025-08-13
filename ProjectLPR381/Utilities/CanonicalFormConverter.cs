using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Utilities
{
    /// <summary>
    /// Centralized error handling and logging system for the Linear Programming application
    /// 
    /// This class provides:
    /// - Consistent error handling across all components
    /// - Error logging to files and console
    /// - Special case detection (infeasible, unbounded solutions)
    /// - User-friendly error messages
    /// - Debug information for developers
    /// - Error recovery suggestions
    /// 
    /// The project requirements specify that the program should identify and resolve
    /// infeasible or unbounded programming models, so this class plays a crucial role
    /// in detecting and reporting these special cases.
    /// </summary>
    public class ErrorHandler
    {
        private static ErrorHandler _instance;
        private static readonly object _lock = new object();

        private string _logFilePath;
        private List<ErrorEntry> _errorHistory;
        private bool _isLoggingEnabled;

        /// <summary>
        /// Singleton pattern - ensures only one ErrorHandler instance exists
        /// This allows consistent error handling across the entire application
        /// </summary>
        public static ErrorHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ErrorHandler();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern
        /// Initializes logging and error tracking
        /// </summary>
        private ErrorHandler()
        {
            _errorHistory = new List<ErrorEntry>();
            _isLoggingEnabled = true;

            // Set default log file path
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            _logFilePath = Path.Combine(logDirectory, $"LP_ErrorLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        }

        /// <summary>
        /// Handles general exceptions that occur during algorithm execution
        /// Provides user-friendly messages while logging technical details
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="context">Description of what was happening when the error occurred</param>
        /// <param name="algorithmName">Name of the algorithm that encountered the error</param>
        /// <returns>ErrorResult containing user message and suggested actions</returns>
        public ErrorResult HandleException(Exception exception, string context, string algorithmName = "Unknown")
        {
            var errorEntry = new ErrorEntry
            {
                Timestamp = DateTime.Now,
                ErrorType = ErrorType.Exception,
                Exception = exception,
                Context = context,
                AlgorithmName = algorithmName,
                Message = exception.Message
            };

            // Log the error
            LogError(errorEntry);

            // Determine user-friendly response based on exception type
            var errorResult = CreateErrorResult(errorEntry);

            // Display to console with appropriate formatting
            DisplayError(errorResult);

            return errorResult;
        }

        /// <summary>
        /// Specifically handles infeasible Linear Programming problems
        /// An LP is infeasible if no solution exists that satisfies all constraints
        /// 
        /// Common causes:
        /// - Contradictory constraints (e.g., x ≤ 5 and x ≥ 10)
        /// - Over-constrained problems
        /// - Incorrect constraint formulation
        /// </summary>
        /// <param name="model">The infeasible LP model</param>
        /// <param name="algorithmName">Algorithm that detected infeasibility</param>
        /// <param name="detectionDetails">Technical details about how infeasibility was detected</param>
        /// <returns>ErrorResult with suggestions for resolving infeasibility</returns>
        public ErrorResult HandleInfeasibleSolution(LinearProgrammingModel model, string algorithmName, string detectionDetails = "")
        {
            var errorEntry = new ErrorEntry
            {
                Timestamp = DateTime.Now,
                ErrorType = ErrorType.Infeasible,
                Context = $"Model with {model.VariableCount} variables and {model.ConstraintCount} constraints",
                AlgorithmName = algorithmName,
                Message = "The Linear Programming problem is INFEASIBLE - no solution exists that satisfies all constraints",
                TechnicalDetails = detectionDetails
            };

            LogError(errorEntry);

            // Analyze the model to provide specific suggestions
            var suggestions = AnalyzeInfeasibleModel(model);

            var errorResult = new ErrorResult
            {
                IsSuccess = false,
                ErrorType = ErrorType.Infeasible,
                UserMessage = "❌ INFEASIBLE PROBLEM DETECTED\n\n" +
                             "No feasible solution exists that satisfies all constraints simultaneously.\n" +
                             "This means the constraints are contradictory or over-restrictive.",
                TechnicalMessage = $"Algorithm: {algorithmName}\nDetails: {detectionDetails}",
                Suggestions = suggestions,
                CanRetry = false // Infeasible problems can't be retried with same constraints
            };

            // Update model status
            model.Status = SolutionStatus.Infeasible;

            DisplayError(errorResult);
            return errorResult;
        }

        /// <summary>
        /// Handles unbounded Linear Programming problems
        /// An LP is unbounded if the objective function can be improved indefinitely
        /// 
        /// This typically occurs when:
        /// - Missing upper bound constraints
        /// - Incorrect constraint directions
        /// - Infeasible dual problem
        /// </summary>
        /// <param name="model">The unbounded LP model</param>
        /// <param name="algorithmName">Algorithm that detected unboundedness</param>
        /// <param name="direction">Direction of unboundedness (
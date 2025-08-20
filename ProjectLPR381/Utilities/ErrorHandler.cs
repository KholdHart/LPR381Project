using System;
using System.Collections.Generic;
using System.IO;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Utilities
{
    /// <summary>
    /// Centralized error handling system for the Linear Programming application
    /// This class manages all types of errors that can occur during:
    /// - File input/output operations
    /// - Model validation and parsing
    /// - Algorithm execution and mathematical computations
    /// - User interface interactions
    /// 
    /// The error handler provides:
    /// - Consistent error logging and reporting
    /// - User-friendly error messages
    /// - Recovery suggestions for common problems
    /// - Error categorization for different handling strategies
    /// </summary>
    public static class ErrorHandler
    {
        // Static list to maintain error history during application session
        private static List<ErrorInfo> _errorHistory = new List<ErrorInfo>();

        // Flag to determine if errors should be logged to console in addition to storage
        private static bool _enableConsoleLogging = true;

        // Path for error log file (if file logging is enabled)
        private static string _errorLogPath = "error_log.txt";

        /// <summary>
        /// Handles file-related errors that occur during input/output operations
        /// These are typically recoverable errors where user can provide different file path
        /// </summary>
        /// <param name="exception">The file-related exception that occurred</param>
        /// <param name="filePath">Path to the file that caused the error</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <returns>ErrorInfo object containing details and suggested recovery actions</returns>
        public static ErrorInfo HandleFileError(Exception exception, string filePath, string operation)
        {
            // Create comprehensive error information object
            var errorInfo = new ErrorInfo
            {
                ErrorType = ErrorType.FileError,
                ErrorMessage = exception.Message,
                ErrorDetails = $"Operation: {operation}, File: {filePath}",
                Timestamp = DateTime.Now,
                IsRecoverable = true // File errors are typically recoverable
            };

            // Provide specific recovery suggestions based on exception type
            if (exception is FileNotFoundException)
            {
                errorInfo.RecoverySuggestion = $"File '{filePath}' was not found. Please:\n" +
                                             "1. Check if the file path is correct\n" +
                                             "2. Ensure the file exists in the specified location\n" +
                                             "3. Verify you have read permissions for the file";
            }
            else if (exception is UnauthorizedAccessException)
            {
                errorInfo.RecoverySuggestion = $"Access denied to file '{filePath}'. Please:\n" +
                                             "1. Check if you have read/write permissions\n" +
                                             "2. Ensure the file is not open in another program\n" +
                                             "3. Run the application as administrator if necessary";
            }
            else if (exception is DirectoryNotFoundException)
            {
                errorInfo.RecoverySuggestion = $"Directory not found for '{filePath}'. Please:\n" +
                                             "1. Check if the directory path exists\n" +
                                             "2. Create the directory if it doesn't exist\n" +
                                             "3. Verify the full path is correct";
            }
            else
            {
                errorInfo.RecoverySuggestion = $"File operation failed. Please:\n" +
                                             "1. Check file permissions and availability\n" +
                                             "2. Ensure sufficient disk space\n" +
                                             "3. Try with a different file location";
            }

            // Log and return the error information
            LogError(errorInfo);
            return errorInfo;
        }

        /// <summary>
        /// Handles errors that occur during model validation and parsing
        /// These errors indicate problems with the input file format or mathematical model structure
        /// </summary>
        /// <param name="exception">The validation exception that occurred</param>
        /// <param name="modelContext">Context about which part of the model caused the error</param>
        /// <returns>ErrorInfo object with validation-specific recovery suggestions</returns>
        public static ErrorInfo HandleValidationError(Exception exception, string modelContext)
        {
            var errorInfo = new ErrorInfo
            {
                ErrorType = ErrorType.ValidationError,
                ErrorMessage = exception.Message,
                ErrorDetails = $"Model Context: {modelContext}",
                Timestamp = DateTime.Now,
                IsRecoverable = true // Validation errors can be fixed by correcting input
            };

            // Provide context-specific recovery suggestions
            if (modelContext.Contains("objective"))
            {
                errorInfo.RecoverySuggestion = "Objective function error. Please check:\n" +
                                             "1. First line starts with 'max' or 'min'\n" +
                                             "2. Coefficients are properly formatted (+2, -3, etc.)\n" +
                                             "3. Number of coefficients matches number of variables\n" +
                                             "4. All coefficients are valid numbers";
            }
            else if (modelContext.Contains("constraint"))
            {
                errorInfo.RecoverySuggestion = "Constraint error. Please check:\n" +
                                             "1. Each constraint line has correct format\n" +
                                             "2. Number of coefficients matches variables\n" +
                                             "3. Relation symbol is <=, >=, or =\n" +
                                             "4. Right-hand-side is a valid number";
            }
            else if (modelContext.Contains("variable"))
            {
                errorInfo.RecoverySuggestion = "Variable definition error. Please check:\n" +
                                             "1. Last line contains variable types\n" +
                                             "2. Types are: +, -, urs, int, or bin\n" +
                                             "3. Number of types matches number of variables\n" +
                                             "4. All type specifications are valid";
            }
            else
            {
                errorInfo.RecoverySuggestion = "Model validation failed. Please:\n" +
                                             "1. Check input file format against requirements\n" +
                                             "2. Ensure all lines are properly formatted\n" +
                                             "3. Verify variable and constraint counts match\n" +
                                             "4. Check for missing or extra values";
            }

            LogError(errorInfo);
            return errorInfo;
        }

        /// <summary>
        /// Handles mathematical and algorithmic errors that occur during problem solving
        /// These errors indicate issues with the mathematical solution process
        /// </summary>
        /// <param name="exception">The algorithm exception that occurred</param>
        /// <param name="algorithmName">Name of the algorithm that encountered the error</param>
        /// <param name="iterationInfo">Information about current iteration or step</param>
        /// <returns>ErrorInfo object with algorithm-specific error details</returns>
        public static ErrorInfo HandleAlgorithmError(Exception exception, string algorithmName, string iterationInfo = "")
        {
            var errorInfo = new ErrorInfo
            {
                ErrorType = ErrorType.AlgorithmError,
                ErrorMessage = exception.Message,
                ErrorDetails = $"Algorithm: {algorithmName}, Iteration Info: {iterationInfo}",
                Timestamp = DateTime.Now,
                IsRecoverable = false // Algorithm errors are typically not user-recoverable
            };

            // Provide algorithm-specific guidance
            if (algorithmName.Contains("Simplex"))
            {
                errorInfo.RecoverySuggestion = "Simplex algorithm error. This may indicate:\n" +
                                             "1. The problem is infeasible (no solution exists)\n" +
                                             "2. The problem is unbounded\n" +
                                             "3. Numerical instability in calculations\n" +
                                             "4. Try a different algorithm or check model formulation";
            }
            else if (algorithmName.Contains("Branch") && algorithmName.Contains("Bound"))
            {
                errorInfo.RecoverySuggestion = "Branch and Bound algorithm error. Consider:\n" +
                                             "1. The integer program may be too large\n" +
                                             "2. LP relaxation may be infeasible\n" +
                                             "3. Try adjusting branching strategies\n" +
                                             "4. Check if continuous relaxation solves correctly";
            }
            else if (algorithmName.Contains("Cutting"))
            {
                errorInfo.RecoverySuggestion = "Cutting Plane algorithm error. Check:\n" +
                                             "1. Maximum iterations may have been exceeded\n" +
                                             "2. Cuts may not be improving the solution\n" +
                                             "3. Try Branch and Bound instead\n" +
                                             "4. Verify the integer constraints are necessary";
            }
            else
            {
                errorInfo.RecoverySuggestion = "Algorithm execution failed. Try:\n" +
                                             "1. Using a different solution algorithm\n" +
                                             "2. Checking model formulation for errors\n" +
                                             "3. Simplifying the problem if possible\n" +
                                             "4. Contacting support with error details";
            }

            LogError(errorInfo);
            return errorInfo;
        }

        /// <summary>
        /// Handles user interface and input errors
        /// These occur when user provides invalid menu choices or input values
        /// </summary>
        /// <param name="userInput">The invalid input provided by user</param>
        /// <param name="expectedInput">Description of what input was expected</param>
        /// <param name="context">Context where the invalid input occurred</param>
        /// <returns>ErrorInfo object with user input guidance</returns>
        public static ErrorInfo HandleUserInputError(string userInput, string expectedInput, string context)
        {
            var errorInfo = new ErrorInfo
            {
                ErrorType = ErrorType.UserInputError,
                ErrorMessage = $"Invalid input received: '{userInput}'",
                ErrorDetails = $"Expected: {expectedInput}, Context: {context}",
                Timestamp = DateTime.Now,
                IsRecoverable = true // User can always provide correct input
            };

            // Provide context-specific input guidance
            if (context.Contains("menu"))
            {
                errorInfo.RecoverySuggestion = $"Invalid menu selection. Please:\n" +
                                             "1. Enter a number from the available options\n" +
                                             "2. Expected: {expectedInput}\n" +
                                             "3. Press Enter after typing your choice\n" +
                                             "4. Use only the numbers shown in the menu";
            }
            else if (context.Contains("file"))
            {
                errorInfo.RecoverySuggestion = "Invalid file path. Please:\n" +
                                             "1. Provide a valid file path\n" +
                                             "2. Use forward slashes (/) or double backslashes (\\\\)\n" +
                                             "3. Ensure the file has .txt extension\n" +
                                             "4. Check that the file exists";
            }
            else if (context.Contains("algorithm"))
            {
                errorInfo.RecoverySuggestion = "Invalid algorithm selection. Please:\n" +
                                             "1. Choose from the available algorithms\n" +
                                             "2. Enter the exact number shown\n" +
                                             "3. Ensure the algorithm supports your problem type\n" +
                                             "4. Refer to the menu options";
            }
            else
            {
                errorInfo.RecoverySuggestion = $"Invalid input format. Please:\n" +
                                             "1. Expected format: {expectedInput}\n" +
                                             "2. Check for typos in your input\n" +
                                             "3. Follow the exact format specified\n" +
                                             "4. Try again with correct format";
            }

            LogError(errorInfo);
            return errorInfo;
        }

        /// <summary>
        /// Handles special mathematical cases that occur in linear programming
        /// These are not necessarily errors but special conditions that need user attention
        /// </summary>
        /// <param name="caseType">Type of special case encountered</param>
        /// <param name="details">Additional details about the special case</param>
        /// <param name="algorithmUsed">Algorithm that detected the special case</param>
        /// <returns>ErrorInfo object describing the special case</returns>
        public static ErrorInfo HandleSpecialCase(SpecialCaseType caseType, string details, string algorithmUsed)
        {
            var errorInfo = new ErrorInfo
            {
                ErrorType = ErrorType.SpecialCase,
                ErrorMessage = $"Special case detected: {caseType}",
                ErrorDetails = $"Algorithm: {algorithmUsed}, Details: {details}",
                Timestamp = DateTime.Now,
                IsRecoverable = false // Special cases are mathematical properties, not errors to fix
            };

            // Provide explanations and guidance for each special case
            switch (caseType)
            {
                case SpecialCaseType.Infeasible:
                    errorInfo.RecoverySuggestion = "The problem is INFEASIBLE:\n" +
                                                 "1. No solution exists that satisfies all constraints\n" +
                                                 "2. Constraints may be contradictory\n" +
                                                 "3. Check constraint formulation\n" +
                                                 "4. Consider relaxing some constraints";
                    break;

                case SpecialCaseType.Unbounded:
                    errorInfo.RecoverySuggestion = "The problem is UNBOUNDED:\n" +
                                                 "1. Objective function can be improved infinitely\n" +
                                                 "2. Missing constraints may be needed\n" +
                                                 "3. Check if constraints properly limit variables\n" +
                                                 "4. Add realistic upper bounds to variables";
                    break;

                case SpecialCaseType.Degenerate:
                    errorInfo.RecoverySuggestion = "DEGENERACY detected:\n" +
                                                 "1. Multiple optimal solutions may exist\n" +
                                                 "2. Basic variable has value zero\n" +
                                                 "3. Algorithm may cycle\n" +
                                                 "4. Solution is still valid if found";
                    break;

                case SpecialCaseType.AlternateOptimal:
                    errorInfo.RecoverySuggestion = "ALTERNATE OPTIMAL solutions exist:\n" +
                                                 "1. Multiple solutions give same optimal value\n" +
                                                 "2. Zero reduced cost in optimal tableau\n" +
                                                 "3. Any convex combination is also optimal\n" +
                                                 "4. Choose solution based on practical considerations";
                    break;

                default:
                    errorInfo.RecoverySuggestion = "Special mathematical case encountered:\n" +
                                                 "1. This is not an error but a mathematical property\n" +
                                                 "2. Check algorithm output for details\n" +
                                                 "3. Consult linear programming theory\n" +
                                                 "4. Results may still be mathematically valid";
                    break;
            }

            LogError(errorInfo);
            return errorInfo;
        }

        /// <summary>
        /// Internal method to log error information to console and/or file
        /// Maintains error history and provides consistent formatting
        /// </summary>
        /// <param name="errorInfo">Error information to log</param>
        private static void LogError(ErrorInfo errorInfo)
        {
            // Add to error history for session tracking
            _errorHistory.Add(errorInfo);

            // Log to console if enabled (useful for debugging and immediate feedback)
            if (_enableConsoleLogging)
            {
                Console.WriteLine($"\n❌ ERROR ENCOUNTERED:");
                Console.WriteLine($"   Type: {errorInfo.ErrorType}");
                Console.WriteLine($"   Message: {errorInfo.ErrorMessage}");
                Console.WriteLine($"   Time: {errorInfo.Timestamp:HH:mm:ss}");

                if (!string.IsNullOrEmpty(errorInfo.ErrorDetails))
                {
                    Console.WriteLine($"   Details: {errorInfo.ErrorDetails}");
                }

                Console.WriteLine($"   Recoverable: {(errorInfo.IsRecoverable ? "Yes" : "No")}");

                if (!string.IsNullOrEmpty(errorInfo.RecoverySuggestion))
                {
                    Console.WriteLine($"\n💡 RECOVERY SUGGESTIONS:");
                    Console.WriteLine($"   {errorInfo.RecoverySuggestion}");
                }
                Console.WriteLine(); // Extra line for readability
            }

            // Optionally log to file for persistent error tracking
            try
            {
                LogErrorToFile(errorInfo);
            }
            catch
            {
                // Don't let logging errors crash the application
                // Silent failure is acceptable for error logging
            }
        }

        /// <summary>
        /// Logs error information to a persistent file for later analysis
        /// Useful for debugging and tracking error patterns
        /// </summary>
        /// <param name="errorInfo">Error information to write to file</param>
        private static void LogErrorToFile(ErrorInfo errorInfo)
        {
            string logEntry = $"[{errorInfo.Timestamp:yyyy-MM-dd HH:mm:ss}] " +
                            $"{errorInfo.ErrorType}: {errorInfo.ErrorMessage}\n" +
                            $"Details: {errorInfo.ErrorDetails}\n" +
                            $"Recoverable: {errorInfo.IsRecoverable}\n" +
                            $"Recovery: {errorInfo.RecoverySuggestion}\n" +
                            new string('-', 80) + "\n";

            // Append to log file (create if doesn't exist)
            File.AppendAllText(_errorLogPath, logEntry);
        }

        /// <summary>
        /// Gets the complete error history for the current session
        /// Useful for displaying error summary or debugging
        /// </summary>
        /// <returns>List of all ErrorInfo objects from current session</returns>
        public static List<ErrorInfo> GetErrorHistory()
        {
            return new List<ErrorInfo>(_errorHistory); // Return copy to prevent external modification
        }

        /// <summary>
        /// Gets count of errors by type for the current session
        /// Useful for error statistics and reporting
        /// </summary>
        /// <returns>Dictionary mapping error types to their occurrence count</returns>
        public static Dictionary<ErrorType, int> GetErrorStatistics()
        {
            var stats = new Dictionary<ErrorType, int>();

            // Initialize all error types with zero count
            foreach (ErrorType errorType in Enum.GetValues<ErrorType>())
            {
                stats[errorType] = 0;
            }

            // Count occurrences of each error type
            foreach (var error in _errorHistory)
            {
                stats[error.ErrorType]++;
            }

            return stats;
        }

        /// <summary>
        /// Clears the error history for the current session
        /// Useful when starting a new problem or resetting application state
        /// </summary>
        public static void ClearErrorHistory()
        {
            _errorHistory.Clear();
            Console.WriteLine("Error history cleared.");
        }

        /// <summary>
        /// Configures error handling behavior
        /// Allows customization of logging and error reporting
        /// </summary>
        /// <param name="enableConsoleLogging">Whether to display errors in console</param>
        /// <param name="errorLogPath">Path for persistent error log file</param>
        public static void Configure(bool enableConsoleLogging = true, string errorLogPath = "error_log.txt")
        {
            _enableConsoleLogging = enableConsoleLogging;
            _errorLogPath = errorLogPath ?? "error_log.txt";
        }

        /// <summary>
        /// Displays a summary of all errors encountered in the current session
        /// Useful for end-of-session reporting or debugging
        /// </summary>
        public static void DisplayErrorSummary()
        {
            var stats = GetErrorStatistics();
            int totalErrors = _errorHistory.Count;

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("ERROR SUMMARY FOR THIS SESSION");
            Console.WriteLine(new string('=', 50));

            if (totalErrors == 0)
            {
                Console.WriteLine("✅ No errors encountered this session!");
            }
            else
            {
                Console.WriteLine($"Total Errors: {totalErrors}");
                Console.WriteLine();

                foreach (var kvp in stats)
                {
                    if (kvp.Value > 0)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }

                Console.WriteLine("\nRecoverable Errors: " +
                                _errorHistory.Count(e => e.IsRecoverable));
                Console.WriteLine("Non-Recoverable Errors: " +
                                _errorHistory.Count(e => !e.IsRecoverable));
            }
            Console.WriteLine(new string('=', 50));
        }
    }

    /// <summary>
    /// Data structure to hold comprehensive information about an error
    /// This provides all necessary details for logging, reporting, and recovery guidance
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// Category of error that occurred - used for handling strategy
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// Primary error message - typically from exception or custom description
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Additional contextual information about the error
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// When the error occurred - useful for debugging and logging
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether this error can be recovered from by user action
        /// </summary>
        public bool IsRecoverable { get; set; }

        /// <summary>
        /// Specific suggestions for how user can resolve or work around the error
        /// </summary>
        public string RecoverySuggestion { get; set; }
    }

    /// <summary>
    /// Categories of errors that can occur in the linear programming application
    /// This helps determine appropriate handling strategy for each error type
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// File input/output operations failed
        /// </summary>
        FileError,

        /// <summary>
        /// Model validation or parsing failed
        /// </summary>
        ValidationError,

        /// <summary>
        /// Algorithm execution encountered an error
        /// </summary>
        AlgorithmError,

        /// <summary>
        /// User provided invalid input
        /// </summary>
        UserInputError,

        /// <summary>
        /// Special mathematical cases (not errors but require attention)
        /// </summary>
        SpecialCase,

        /// <summary>
        /// Unexpected or uncategorized errors
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Types of special mathematical cases that can occur in linear programming
    /// These are not errors but special conditions that affect solution interpretation
    /// </summary>
    public enum SpecialCaseType
    {
        /// <summary>
        /// No feasible solution exists
        /// </summary>
        Infeasible,

        /// <summary>
        /// Objective function can be improved infinitely
        /// </summary>
        Unbounded,

        /// <summary>
        /// Basic variable has value zero (potential cycling)
        /// </summary>
        Degenerate,

        /// <summary>
        /// Multiple optimal solutions exist
        /// </summary>
        AlternateOptimal,

        /// <summary>
        /// Other special mathematical conditions
        /// </summary>
        Other
    }
}
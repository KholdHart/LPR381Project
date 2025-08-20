using System;
using LinearProgrammingProject.Utilities;

namespace LinearProgrammingProject
{
    /// <summary>
    /// Main entry point for the Linear Programming Solver application
    /// This class initializes the application and starts the menu system
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point
        /// Sets up error handling and launches the main menu system
        /// </summary>
        /// <param name="args">Command line arguments (not used in this application)</param>
        static void Main(string[] args)
        {
            try
            {
                // Configure error handling system
                ErrorHandler.Configure(enableConsoleLogging: true, errorLogPath: "lp_solver_errors.txt");

                // Create and run the main menu system
                var menuSystem = new MenuSystem();
                menuSystem.Run();
            }
            catch (Exception ex)
            {
                // Handle any catastrophic startup errors
                Console.WriteLine("Critical application error during startup:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
using System;

class TestProgram
{
    static void Main()
    {
        // These should be detected as user-facing (high confidence)
        Console.WriteLine("Hello, World!");
        Console.WriteLine("Please enter your name:");
        throw new ArgumentException("Invalid input provided.");
        
        // These should be detected as internal (low confidence)
        var configKey = "database.connection.timeout";
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var logMessage = "DEBUG: Processing user request";
        
        // These are ambiguous (medium confidence)
        var userMessage = "Welcome to our application";
        var title = "Application Settings";
        var result = "Operation completed successfully";
    }
}

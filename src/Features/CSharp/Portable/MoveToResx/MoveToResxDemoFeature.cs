// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx.Demo;

/// <summary>
/// Demo class showcasing various string scenarios for MoveToResx analyzer testing.
/// This represents a realistic Roslyn IDE feature with mixed string types.
/// Based on real patterns found in Roslyn codebase.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = "DemoFeature"), Shared]
internal sealed class MoveToResxDemoFeature : CodeFixProvider
{
    // Easy Heuristic Cases - Obviously User-Facing
    
    private const string UserMessage = "The selected code cannot be refactored automatically."; // (user-facing)
    private const string ErrorTitle = "Refactoring Error"; // (user-facing)
    private const string WarningText = "This operation may affect code readability."; // (user-facing)
    private const string SuccessMessage = "Code has been successfully refactored!"; // (user-facing)
    private const string ConfirmationPrompt = "Are you sure you want to continue?"; // (user-facing)
    
    // Easy Heuristic Cases - Obviously Technical
    
    private const string ConfigurationKey = "ide.refactoring.enable_smart_rename"; // (non-user-facing)
    private const string LoggerCategory = "Microsoft.CodeAnalysis.CSharp.Refactoring"; // (non-user-facing)
    private const string FilePath = @"C:\Users\Dev\Source\MyProject\Analyzers\RefactoringAnalyzer.cs"; // (non-user-facing)
    private const string AssemblyName = "Microsoft.CodeAnalysis.CSharp.Features.dll"; // (non-user-facing)
    private const string NamespaceName = "Microsoft.CodeAnalysis.CSharp.MoveToResx"; // (non-user-facing)
    private const string SqlQuery = "SELECT * FROM CodeAnalysisResults WHERE Severity = 'Error'"; // (non-user-facing)
    private const string RegexPattern = @"^\w+\.\w+\.\w+$"; // (non-user-facing)
    private const string XmlElementName = "diagnostic"; // (non-user-facing)
    private const string JsonPropertyName = "isEnabled"; // (non-user-facing)
    private const string HttpEndpoint = "https://api.github.com/repos/dotnet/roslyn/issues"; // (non-user-facing)
    
    // Challenging Cases - Context Dependent
    
    private const string ValidationError = "Invalid input provided"; // (user-facing) - could be internal validation or user validation
    private const string ProcessingStatus = "Processing"; // (user-facing) - shown in UI status bar vs internal logging
    private const string DebugMessage = "Variable 'x' has been assigned value '42'"; // (non-user-facing) - debug output, not user message
    private const string TelemetryEvent = "RefactoringCompleted"; // (non-user-facing) - telemetry, but could be user-facing if shown in UI
    private const string CacheKey = "LastOpenedFile"; // (non-user-facing) - internal cache vs user-facing setting name
    
    // AI-Dependent Cases - Require Semantic Understanding
    
    private const string AmbiguousMessage1 = "Operation completed"; // (user-facing) - needs context: user notification vs log entry
    private const string AmbiguousMessage2 = "Ready"; // (user-facing) - needs context: UI state vs internal flag  
    private const string ContextualError = "Unable to process request"; // (user-facing) - needs context: user error vs internal error
    private const string StatusText = "Connected"; // (user-facing) - needs context: UI status vs debug info
    private const string GenericLabel = "Default"; // (user-facing) - needs context: UI label vs configuration value
    
    // Edge Cases - Tricky Scenarios
    
    private const string MixedPurpose = "Error"; // (user-facing) - used both as UI text and enum value
    private const string LocalizedKey = "SaveFileDialog_Title"; // (non-user-facing) - resource key that looks like user text
    private const string FormattedMessage = "Found {0} issues in {1} files"; // (user-facing) - format string for user messages
    private const string TechnicalFormat = "Assembly '{0}' loaded from '{1}'"; // (non-user-facing) - format string for logging
    private const string VersionString = "1.0.0.0"; // (non-user-facing) - version number
    private const string DisplayVersion = "Version 1.0"; // (user-facing) - user-friendly version display
    
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray<string>.Empty;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        
        try
        {
            // User-facing error message shown in VS
            throw new InvalidOperationException("The refactoring operation could not be completed. Please check your selection and try again."); // (user-facing)
        }
        catch (Exception ex)
        {
            // Internal logging - not user-facing
            LogError("RegisterCodeFixesAsync", ex.Message); // Method call with internal message
        }
        
        // Configuration and setup
        var options = await GetRefactoringOptionsAsync(document); // Method call
        if (!options.IsEnabled)
        {
            ShowUserMessage("This refactoring is currently disabled. You can enable it in Tools > Options."); // (user-facing)
            return;
        }
        
        // File operations
        var projectFile = Path.Combine(document.Project.FilePath ?? "", "project.json"); // (non-user-facing)
        if (!File.Exists(projectFile))
        {
            LogWarning($"Project file not found: {projectFile}"); // (non-user-facing) - internal logging
            DisplayWarning("Project configuration file is missing. Some features may not work correctly."); // (user-facing)
        }
        
        // Validation scenarios
        var userInput = GetUserInput();
        if (string.IsNullOrEmpty(userInput))
        {
            ShowValidationError("Please enter a valid name."); // (user-facing)
            RecordValidationFailure("empty_input"); // (non-user-facing)
        }
        
        // Database/Query operations  
        var connectionString = "Server=localhost;Database=RoslynAnalysis;Trusted_Connection=true;"; // (non-user-facing)
        var queryResults = await ExecuteQuery("SELECT COUNT(*) FROM Diagnostics WHERE IsResolved = 0"); // (non-user-facing)
        
        // Progress and status updates
        UpdateProgressBar("Analyzing code structure..."); // (user-facing)
        SetInternalStatus("analysis_in_progress"); // (non-user-facing)
        
        // Telemetry and analytics
        TrackEvent("refactoring_started"); // (non-user-facing)
        
        // Resource management
        var tempFile = Path.GetTempFileName(); // (non-user-facing)
        var userTempDir = GetUserTempDirectory(); // Could be user-facing if shown in UI
        
        // Completion scenarios
        NotifyUserOfCompletion("Refactoring completed successfully!"); // (user-facing)
        LogInfo("RefactoringEngine.RegisterCodeFixesAsync completed"); // (non-user-facing)
    }
    
    private async Task<RefactoringOptions> GetRefactoringOptionsAsync(Document document)
    {
        // Configuration keys
        var enabledKey = "refactoring.moveToResx.enabled"; // (non-user-facing)
        var maxFilesKey = "refactoring.moveToResx.maxFiles"; // (non-user-facing)
        
        // XML parsing for settings
        var settingsXml = "<settings><refactoring enabled='true' /></settings>"; // (non-user-facing)
        
        return new RefactoringOptions();
    }
    
    private void ShowUserMessage(string message)
    {
        // Simulated UI interaction
        Console.WriteLine("[USER MESSAGE] " + message);
    }
    
    private void DisplayWarning(string warning)
    {
        // User-facing warning display
        Console.WriteLine("[WARNING] " + warning);
    }
    
    private void ShowValidationError(string error)
    {
        // User input validation error
        Console.WriteLine("[VALIDATION ERROR] " + error);
    }
    
    private void UpdateProgressBar(string status)
    {
        // Progress shown to user
        Console.WriteLine("[PROGRESS] " + status);
    }
    
    private void NotifyUserOfCompletion(string message)
    {
        // Success notification to user
        Console.WriteLine("[SUCCESS] " + message);
    }
    
    private void LogError(string method, string message)
    {
        // Internal error logging
        System.Diagnostics.Debug.WriteLine("ERROR in " + method + ": " + message); // (non-user-facing)
    }
    
    private void LogWarning(string message)
    {
        // Internal warning logging  
        System.Diagnostics.Debug.WriteLine("WARNING: " + message); // (non-user-facing)
    }
    
    private void LogInfo(string message)
    {
        // Internal info logging
        System.Diagnostics.Debug.WriteLine("INFO: " + message); // (non-user-facing)
    }
    
    private void RecordValidationFailure(string reason)
    {
        // Internal tracking
        System.Diagnostics.Debug.WriteLine("Validation failed: " + reason); // (non-user-facing)
    }
    
    private void SetInternalStatus(string status)
    {
        // Internal state management
        System.Diagnostics.Debug.WriteLine("Status: " + status); // (non-user-facing)
    }
    
    private void TrackEvent(string eventName)
    {
        // Telemetry tracking
        System.Diagnostics.Debug.WriteLine("Event: " + eventName); // (non-user-facing)
    }
    
    private string GetUserInput()
    {
        // Simulated user input
        return "SampleInput";
    }
    
    private string GetUserTempDirectory()
    {
        // This could be shown to user in some contexts
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Temp"); // (non-user-facing)
    }
    
    private async Task<object> ExecuteQuery(string sql)
    {
        // Database query execution
        await Task.Delay(100);
        return new { Count = 42 };
    }
    
    // Nested class with more examples
    private class DiagnosticReporter
    {
        public void ReportIssue(string severity, string message)
        {
            if (severity == "error") // (non-user-facing) - string comparison with enum-like value
            {
                LogToFile("CRITICAL: " + message); // (non-user-facing)
                ShowUserError("An error occurred: " + message); // (user-facing)
            }
            else if (severity == "warning") // (non-user-facing)
            {
                LogToFile("WARNING: " + message); // (non-user-facing)
                ShowUserWarning("Warning: " + message); // (user-facing)
            }
        }
        
        private void LogToFile(string entry)
        {
            var logPath = @"C:\Logs\Roslyn\diagnostics.log"; // (non-user-facing)
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // (non-user-facing)
            File.AppendAllText(logPath, timestamp + " - " + entry + "\n"); // (non-user-facing)
        }
        
        private void ShowUserError(string message)
        {
            // User notification
            Console.WriteLine("[ERROR] " + message);
        }
        
        private void ShowUserWarning(string message)
        {
            // User notification
            Console.WriteLine("[WARNING] " + message);
        }
    }
    
    // Advanced edge cases
    private class AdvancedScenarios
    {
        public void DemonstrateEdgeCases()
        {
            // Context-sensitive strings
            ProcessCommand("Save"); // Could be user command or internal command
            ProcessCommand("INTERNAL_SAVE_STATE"); // Clearly internal
            
            // Validation with ambiguous intent
            ValidateInput("Input must not be empty"); // (user-facing) - shown to user
            ValidateInput("NULL_INPUT_DETECTED"); // (non-user-facing) - internal validation code
            
            // Mixed format strings
            string userFormat = "Progress: {0}% complete ({1} of {2} items)"; // (user-facing)
            string debugFormat = "PERF::{0}ms::{1}::{2}"; // (non-user-facing)
            
            // Resource-like strings that aren't resources
            var pseudoResource = "Button_Save_Text"; // (non-user-facing) - looks like resource key
            var actualText = "Save"; // (user-facing) - actual button text
            
            // Technical strings that could be user-facing in some contexts
            var status = "OK"; // Context-dependent: HTTP status vs UI message
            var result = "Success"; // Context-dependent: return value vs user feedback
            var state = "Ready"; // Context-dependent: internal state vs UI display
        }
        
        private void ProcessCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("Processing: " + command);
        }
        
        private void ValidateInput(string message)
        {
            System.Diagnostics.Debug.WriteLine("Validation: " + message);
        }
    }
    
    // ========================================
    // WHAT'S NEXT: Future Improvements
    // ========================================
    
    /// <summary>
    /// Future enhancement: Interpolated string support.
    /// These examples demonstrate scenarios that would benefit from MoveToResx 
    /// but require interpolated string parsing and format string generation.
    /// </summary>
    private class FutureInterpolatedStringSupport
    {
        public void DemonstrateInterpolatedStringChallenges()
        {
            var fileName = "MyClass.cs";
            var lineNumber = 42;
            var count = 5;
            
            // FUTURE: Simple interpolated strings → string.Format
            var userMessage = $"Error on line {lineNumber} in file {fileName}"; // (user-facing)
            // Could become: string.Format(Resources.ErrorOnLine, lineNumber, fileName)
            // Resources.ErrorOnLine = "Error on line {0} in file {1}"
            
            var progressMessage = $"Processing {count} files..."; // (user-facing)  
            // Could become: string.Format(Resources.ProcessingFiles, count)
            // Resources.ProcessingFiles = "Processing {0} files..."
            
            // FUTURE: Format specifiers and alignment
            var amount = 123.456m;
            var formattedPrice = $"Total: {amount:C2}"; // (user-facing)
            // Could become: string.Format(Resources.TotalPrice, amount) 
            // Resources.TotalPrice = "Total: {0:C2}"
            
            var alignedText = $"Name: {fileName,20}"; // (user-facing)
            // Could become: string.Format(Resources.NameField, fileName)
            // Resources.NameField = "Name: {0,20}"
            
            // FUTURE: Mixed user-facing and technical interpolated strings
            var logEntry = $"ParseError:L{lineNumber}:F{fileName}"; // (non-user-facing)
            var timestamp = DateTime.Now;
            var auditLog = $"{timestamp:yyyy-MM-dd HH:mm:ss} - User action completed"; // (non-user-facing)
            
            // FUTURE: Complex expressions in interpolations
            var complexMessage = $"Found {GetErrorCount()} errors in {GetFileList().Count} files"; // (user-facing)
            // Would need expression extraction and temporary variables
            
            // FUTURE: Verbatim interpolated strings
            var verbatimPath = $@"Configuration saved to: {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\MyApp\config.json"; // (user-facing)
            
            // FUTURE: Raw interpolated strings (C# 11+)  
            var rawInterpolated = $$"""
                Error Report:
                - Line: {{lineNumber}}
                - File: {{fileName}}
                - Status: Critical
                """; // (user-facing)
        }
        
        private int GetErrorCount() => 5;
        private List<string> GetFileList() => new() { "file1.cs", "file2.cs" };
    }
}

// Supporting types
internal class RefactoringOptions
{
    public bool IsEnabled { get; set; } = true;
    public int MaxFiles { get; set; } = 100;
}

/* 
 * SUMMARY OF STRING CATEGORIES IN THIS DEMO:
 * 
 * EASY HEURISTIC WINS (should be correctly identified):
 * - Obviously user-facing: Error messages, confirmations, success notifications
 * - Obviously technical: File paths, SQL queries, namespaces, config keys, regex patterns
 * 
 * CHALLENGING CASES (heuristic might struggle):
 * - Context-dependent validation messages
 * - Status strings used in multiple contexts  
 * - Debug vs user messages with similar content
 * 
 * AI-DEPENDENT CASES (require semantic understanding):
 * - Generic words like "Ready", "Default" that need usage context
 * - Messages that could be user-facing or internal based on where they appear
 * - Strings used in both technical and user-facing contexts
 * 
 * EDGE CASES (demonstrate analyzer limitations):
 * - Strings used for multiple purposes (UI text + enum values)
 * - Resource keys that look like user text
 * - Format strings with ambiguous intent
 * - Context-sensitive strings where usage determines user-facing nature
 * 
 * WHAT'S NEXT - FUTURE IMPROVEMENTS:
 * 1. Interpolated string support (see FutureInterpolatedStringSupport class)
 *    - Parse InterpolatedStringExpressionSyntax
 *    - Extract format patterns and expressions
 *    - Generate string.Format replacements
 *    - Handle format specifiers and alignment
 * 2. Cross-reference analysis for strings used in multiple contexts
 * 3. Semantic understanding of method intent (logging vs user notification)
 * 4. Detection of resource key patterns vs actual user text
 * 5. Analysis of string flow through method calls to determine final usage
 */

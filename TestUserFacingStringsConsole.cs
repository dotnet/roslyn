using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace UserFacingStringTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("AI-Powered User-Facing String Analysis");
            Console.WriteLine("======================================");
            
            // Create a workspace with MEF composition
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            using var workspace = new AdhocWorkspace(host);
            
            // Create a test project
            var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
                
            var project = workspace.AddProject(projectInfo);
            
            // Add our test file
            var testCode = @"
using System;

class TestProgram
{
    static void Main()
    {
        // These should be detected as user-facing (high confidence)
        Console.WriteLine(""Hello, World!"");
        Console.WriteLine(""Please enter your name:"");
        throw new ArgumentException(""Invalid input provided."");
        
        // These should be detected as internal (low confidence)
        var configKey = ""database.connection.timeout"";
        var sql = ""SELECT * FROM Users WHERE Id = @id"";
        var logMessage = ""DEBUG: Processing user request"";
        
        // These are ambiguous (medium confidence)
        var userMessage = ""Welcome to our application"";
        var title = ""Application Settings"";
        var result = ""Operation completed successfully"";
    }
}";
            
            var document = workspace.AddDocument(project.Id, "TestProgram.cs", SourceText.From(testCode));
            
            // Get the extractor service
            var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
            if (extractorService == null)
            {
                Console.WriteLine("Error: IUserFacingStringExtractorService not available");
                return;
            }
            
            Console.WriteLine("Analyzing strings with AI...");
            Console.WriteLine();
            
            try
            {
                // Perform the analysis
                var results = await extractorService.ExtractAndAnalyzeAsync(document, CancellationToken.None);
                
                // Display results
                foreach (var result in results.OrderByDescending(r => r.analysis.ConfidenceScore))
                {
                    Console.WriteLine($"String: \"{result.candidate.Value}\"");
                    Console.WriteLine($"Confidence: {result.analysis.ConfidenceScore:P1}");
                    Console.WriteLine($"Reasoning: {result.analysis.Reasoning}");
                    Console.WriteLine($"Suggested Key: {result.analysis.SuggestedResourceKey}");
                    Console.WriteLine($"Context: {result.candidate.Context}");
                    Console.WriteLine($"Classification: {(result.analysis.ConfidenceScore >= 0.7 ? "USER-FACING" : "INTERNAL")}");
                    Console.WriteLine();
                }
                
                Console.WriteLine($"Total strings analyzed: {results.Length}");
                Console.WriteLine($"High confidence user-facing: {results.Count(r => r.analysis.ConfidenceScore >= 0.7)}");
                Console.WriteLine($"Medium confidence: {results.Count(r => r.analysis.ConfidenceScore >= 0.4 && r.analysis.ConfidenceScore < 0.7)}");
                Console.WriteLine($"Low confidence (internal): {results.Count(r => r.analysis.ConfidenceScore < 0.4)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during analysis: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

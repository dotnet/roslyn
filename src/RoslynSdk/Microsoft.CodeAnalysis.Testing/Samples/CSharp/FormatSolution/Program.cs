using System;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

// This program will format all Visual Basic and C# source files for an entire solution.
static class Program
{
    static async Task Main(string[] args)
    {
        // Locate and register the default instance of MSBuild installed on this machine.
        MSBuildLocator.RegisterDefaults();

        // The test solution is copied to the output directory when you build this sample.
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();

        // Open the solution within the workspace.
        Solution originalSolution = await workspace.OpenSolutionAsync(args[0]);

        // Declare a variable to store the intermediate solution snapshot at each step.
        Solution newSolution = originalSolution;

        // Note how we can't simply iterate over originalSolution.Projects or project.Documents
        // because it will return objects from the unmodified originalSolution, not from the newSolution.
        // We need to use the ProjectIds and DocumentIds (that don't change) to look up the corresponding
        // snapshots in the newSolution.
        foreach (ProjectId projectId in originalSolution.ProjectIds)
        {
            // Look up the snapshot for the original project in the latest forked solution.
            Project project = newSolution.GetProject(projectId);

            foreach (DocumentId documentId in project.DocumentIds)
            {
                // Look up the snapshot for the original document in the latest forked solution.
                Document document = newSolution.GetDocument(documentId);

                // Get a transformed version of the document (a new solution snapshot is created
                // under the covers to contain it - none of the existing objects are modified).
                Document newDocument = await Formatter.FormatAsync(document);

                // Store the solution implicitly constructed in the previous step as the latest
                // one so we can continue building it up in the next iteration.
                newSolution = newDocument.Project.Solution;
            }
        }

        // Actually apply the accumulated changes and save them to disk. At this point
        // workspace.CurrentSolution is updated to point to the new solution.
        if (workspace.TryApplyChanges(newSolution))
        {
            Console.WriteLine("Solution updated.");
        }
        else
        {
            Console.WriteLine("Update failed!");
        }
    }
}

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.Build.Locator
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.MSBuild
Imports Microsoft.CodeAnalysis.Text

Module $safeprojectname$

    Sub Main(args As String())
        ' Attempt to set the version of MSBuild.
        Dim visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray()
        Dim instance = If(visualStudioInstances.Length = 1, visualStudioInstances(0), SelectVisualStudioInstance(visualStudioInstances))

        Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.")

        ' NOTE: Be sure to register an instance with the MSBuildLocator 
        '       before calling MSBuildWorkspace.Create()
        '       otherwise, MSBuildWorkspace won't MEF compose.
        MSBuildLocator.RegisterInstance(instance)

        Using workspace = MSBuildWorkspace.Create()
            ' Print message for WorkspaceFailed event to help diagnosing project load failures.
            AddHandler workspace.WorkspaceFailed, Sub(o, e)
                                                      Console.WriteLine(e.Diagnostic.Message)
                                                  End Sub

            Dim solutionPath = args(0)
            Console.WriteLine($"Loading solution '{solutionPath}'")

            ' Attach progress reporter so we print projects as they are loaded.
            Dim solution = workspace.OpenSolutionAsync(solutionPath, New ConsoleProgressReporter()).GetAwaiter.GetResult
            Console.WriteLine($"Finished loading solution '{solutionPath}'")

            ' TODO: Do analysis on the projects in the loaded solution
        End Using
    End Sub

    Function SelectVisualStudioInstance(visualStudioInstances As VisualStudioInstance()) As VisualStudioInstance
        Console.WriteLine("Multiple installs of MSBuild detected please select one:")
        For index = 0 To visualStudioInstances.Length
            Console.WriteLine($"Instance {index + 1}")
            Console.WriteLine($"    Name: {visualStudioInstances(index).Name}")
            Console.WriteLine($"    Version: {visualStudioInstances(index).Version}")
            Console.WriteLine($"    MSBuild Path: {visualStudioInstances(index).MSBuildPath}")
        Next

        While True
            Dim userResponse = Console.ReadLine()
            Dim instanceNumber As Integer = 0
            If Integer.TryParse(userResponse, instanceNumber) AndAlso instanceNumber > 0 AndAlso instanceNumber <= visualStudioInstances.Length Then
                Return visualStudioInstances(instanceNumber - 1)
            End If
            Console.WriteLine("Input not accepted, try again.")
        End While

        Return Nothing
    End Function

    Private Class ConsoleProgressReporter
        Implements IProgress(Of ProjectLoadProgress)

        Public Sub Report(loadProgress As ProjectLoadProgress) Implements IProgress(Of ProjectLoadProgress).Report
            Dim projectDisplay = Path.GetFileName(loadProgress.FilePath)
            If loadProgress.TargetFramework IsNot Nothing Then
                projectDisplay += $" ({loadProgress.TargetFramework})"
            End If

            Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\:ss\.fffffff} {projectDisplay}")
        End Sub
    End Class

End Module

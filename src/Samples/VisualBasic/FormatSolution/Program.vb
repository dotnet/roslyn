' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.MSBuild

' This program will format all Visual Basic and C# source files for an entire solution.
Module Program

    Sub Main(args As String())

        ' The test solution is copied to the output directory when you build this sample.
        Dim workspace As MSBuildWorkspace = MSBuildWorkspace.Create()

        ' Open the solution within the workspace.
        Dim originalSolution As Solution = workspace.OpenSolutionAsync("TestSolutionForVB\Test.sln").Result

        ' Declare a variable to store the intermediate solution snapshot at each step.
        Dim newSolution As Solution = originalSolution

        ' Note how we can't simply iterate over originalSolution.Projects or project.Documents
        ' because it will return objects from the unmodified originalSolution, Not from the newSolution.
        ' We need to use the ProjectIds And DocumentIds (that don't change) to look up the corresponding
        ' snapshots in the newSolution.
        For Each projectId As ProjectId In originalSolution.ProjectIds
            ' Look up the snapshot for the original project in the latest forked solution.
            Dim project As Project = newSolution.GetProject(projectId)

            For Each documentId As DocumentId In project.DocumentIds

                ' Look up the snapshot for the original document in the latest forked solution.
                Dim document As Document = newSolution.GetDocument(documentId)

                ' Get a transformed version of the document (a new solution snapshot is created
                ' under the covers to contain it - none of the existing objects are modified).
                Dim newDocument As Document = Formatter.FormatAsync(document).Result

                ' Store the solution implicitly constructed in the previous step as the latest
                ' one so we can continue building it up in the next iteration.
                newSolution = newDocument.Project.Solution
            Next
        Next

        ' Actually apply the accumulated changes And save them to disk. At this point
        ' workspace.CurrentSolution Is updated to point to the New solution.
        If workspace.TryApplyChanges(newSolution) Then
            Console.WriteLine("Solution updated.")
        Else
            Console.WriteLine("Update failed!")
        End If
    End Sub
End Module

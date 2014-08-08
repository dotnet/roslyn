' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

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
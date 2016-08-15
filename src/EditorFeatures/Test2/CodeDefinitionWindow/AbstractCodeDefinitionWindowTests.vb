' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    Public MustInherit Class AbstractCodeDefinitionWindowTests
        Protected MustOverride Function CreateWorkspaceAsync(code As String) As Task(Of TestWorkspace)

        Protected Async Function VerifyContextLocationInSameFile(code As String, displayName As String) As Task
            Using workspace = Await CreateWorkspaceAsync(code)
                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tree = Await document.GetSyntaxTreeAsync()

                Assert.Empty(tree.GetDiagnostics(CancellationToken.None))

                Dim definitionContextTracker As New DefinitionContextTracker(Nothing, Nothing)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    TaskScheduler.Current,
                    CancellationToken.None)

                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    displayName,
                    tree.GetLocation(hostDocument.SelectedSpans.Single()).GetLineSpan())

                Assert.Equal(expectedLocation, locations.Single())
            End Using
        End Function

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Friend Class GoToHelpers
    Friend Shared Async Function TestAsync(
                                          workspaceDefinition As XElement,
                                          testingMethod As Func(Of Document, Integer, SimpleFindUsagesContext, Task),
                                          Optional shouldSucceed As Boolean = True,
                                          Optional metadataDefinitions As String() = Nothing) As Task
        Using workspace = TestWorkspace.Create(workspaceDefinition)
            Dim documentWithCursor = workspace.DocumentWithCursor
            Dim position = documentWithCursor.CursorPosition.Value

            Dim document = workspace.CurrentSolution.GetDocument(documentWithCursor.Id)

            Dim context = New SimpleFindUsagesContext(CancellationToken.None)
            Await testingMethod(document, position, context)

            If Not shouldSucceed Then
                Assert.NotNull(context.Message)
            Else
                Dim actualDefinitions = context.GetDefinitions().
                                                SelectMany(Function(d) d.SourceSpans).
                                                Select(Function(ss) New FilePathAndSpan(ss.Document.FilePath, ss.SourceSpan)).
                                                ToList()
                actualDefinitions.Sort()

                Dim expectedDefinitions = workspace.Documents.SelectMany(
                    Function(d) d.SelectedSpans.Select(Function(ss) New FilePathAndSpan(d.FilePath, ss))).ToList()

                expectedDefinitions.Sort()

                Assert.Equal(expectedDefinitions.Count, actualDefinitions.Count)

                For i = 0 To actualDefinitions.Count - 1
                    Dim actual = actualDefinitions(i)
                    Dim expected = expectedDefinitions(i)

                    Assert.True(actual.CompareTo(expected) = 0,
                                $"Expected: ({expected}) but got: ({actual})")
                Next

                Dim actualDefintionsWithoutSpans = context.GetDefinitions().
                Where(Function(d) d.SourceSpans.IsDefaultOrEmpty).
                Select(Function(di)
                           Return String.Format("{0}:{1}",
                                                String.Join("", di.OriginationParts.Select(Function(t) t.Text)),
                                                String.Join("", di.NameDisplayParts.Select(Function(t) t.Text)))
                       End Function).ToList()

                actualDefintionsWithoutSpans.Sort()

                If metadataDefinitions Is Nothing Then
                    metadataDefinitions = {}
                End If

                Assert.Equal(actualDefintionsWithoutSpans.Count, metadataDefinitions.Count)

                For i = 0 To actualDefinitions.Count - 1
                    Dim actual = actualDefinitions(i)
                    Dim expected = expectedDefinitions(i)

                    Assert.True(actual.CompareTo(expected) = 0,
                                $"Expected: ({expected}) but got: ({actual})")
                Next
            End If
        End Using
    End Function
End Class

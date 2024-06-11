' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Remote.Testing

Friend Class GoToHelpers
    Friend Shared Async Function TestAsync(
            workspaceDefinition As XElement,
            testHost As TestHost,
            testingMethod As Func(Of Document, Integer, SimpleFindUsagesContext, Task),
            Optional shouldSucceed As Boolean = True,
            Optional metadataDefinitions As String() = Nothing) As Task

        Using workspace = TestWorkspace.Create(workspaceDefinition, composition:=EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost))
            Dim documentWithCursor = workspace.DocumentWithCursor
            Dim position = documentWithCursor.CursorPosition.Value

            Dim solution = workspace.CurrentSolution
            Dim document = Await solution.GetRequiredDocumentAsync(documentWithCursor.Id, includeSourceGenerated:=True)

            Dim context = New SimpleFindUsagesContext(workspace.GlobalOptions)
            Await testingMethod(document, position, context)

            If Not shouldSucceed Then
                Assert.NotNull(context.Message)
            Else
                Dim actualDefinitions = New List(Of FilePathAndSpan)

                For Each definition In context.GetDefinitions()
                    For Each sourceSpan In definition.SourceSpans
                        actualDefinitions.Add(New FilePathAndSpan(sourceSpan.Document.FilePath, sourceSpan.SourceSpan))
                    Next
                Next
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

                AssertEx.Equal(metadataDefinitions, actualDefintionsWithoutSpans)
            End If
        End Using
    End Function
End Class

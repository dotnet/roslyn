' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeLens
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeLens
    Public Class VisualBasicCodeLensTests
        Private _referenceService As CodeLensReferenceService

        Private Async Function SetupWorkspaceAsync(content As String) As Task(Of TestWorkspace)
            Dim workspace = Await TestWorkspace.CreateVisualBasicAsync(content)
            _referenceService = New CodeLensReferenceService()
            Return workspace
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
        Public Async Function TestCount() As Task
            Using workspace = Await SetupWorkspaceAsync("" + vbCrLf +
"Class A" + vbCrLf +
"    Sub B()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub C()" + vbCrLf +
"        D()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub D()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"End Class")
                Dim solution = workspace.CurrentSolution
                Dim documentId = workspace.Documents.First().Id
                Dim document = solution.GetDocument(documentId)
                Dim syntaxNode = Await document.GetSyntaxRootAsync()
                Dim iterator = syntaxNode.ChildNodes().First().ChildNodes().Skip(1).GetEnumerator()
                iterator.MoveNext()

                Dim result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.True(result.HasValue)
                Assert.Equal(0, result.Value.Count)
                Assert.False(result.Value.IsCapped)

                iterator.MoveNext()
                result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.True(result.HasValue)
                Assert.Equal(2, result.Value.Count)
                Assert.False(result.Value.IsCapped)

                iterator.MoveNext()
                result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.True(result.HasValue)
                Assert.Equal(1, result.Value.Count)
                Assert.False(result.Value.IsCapped)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
        Public Async Function TestCapping() As Task
            Using workspace = Await SetupWorkspaceAsync("" + vbCrLf +
"Class A" + vbCrLf +
"    Sub B()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub C()" + vbCrLf +
"        D()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub D()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"End Class")
                Dim solution = workspace.CurrentSolution
                Dim documentId = workspace.Documents.First().Id
                Dim document = solution.GetDocument(documentId)
                Dim syntaxNode = Await document.GetSyntaxRootAsync()
                Dim iterator = syntaxNode.ChildNodes().First().ChildNodes().Skip(1).GetEnumerator()
                iterator.MoveNext()

                Dim result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.True(result.HasValue)
                Assert.Equal(0, result.Value.Count)
                Assert.False(result.Value.IsCapped)

                iterator.MoveNext()
                result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None, 1)
                Assert.True(result.HasValue)
                Assert.Equal(1, result.Value.Count)
                Assert.True(result.Value.IsCapped)

                iterator.MoveNext()
                result = Await _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None, 1)
                Assert.True(result.HasValue)
                Assert.Equal(1, result.Value.Count)
                Assert.False(result.Value.IsCapped)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
        Public Async Function TestDisplay() As Task
            Using workspace = Await SetupWorkspaceAsync("" + vbCrLf +
"Class A" + vbCrLf +
"    Sub B()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub C()" + vbCrLf +
"        D()" + vbCrLf +
"    End Sub" + vbCrLf +
"" + vbCrLf +
"    Sub D()" + vbCrLf +
"        C()" + vbCrLf +
"    End Sub" + vbCrLf +
"End Class")
                Dim solution = workspace.CurrentSolution
                Dim documentId = workspace.Documents.First().Id
                Dim document = solution.GetDocument(documentId)
                Dim syntaxNode = Await document.GetSyntaxRootAsync()
                Dim iterator = syntaxNode.ChildNodes().First().ChildNodes().Skip(1).GetEnumerator()
                iterator.MoveNext()

                Dim result = Await _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.Equal(0, result.Count())

                iterator.MoveNext()
                result = Await _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.Equal(2, result.Count())

                iterator.MoveNext()
                result = Await _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current.ChildNodes().First(), CancellationToken.None)
                Assert.Equal(1, result.Count())
            End Using
        End Function
    End Class
End Namespace
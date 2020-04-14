' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests.Utilities
    Friend Class TestLsifOutput
        Private ReadOnly _testLsifJsonWriter As TestLsifJsonWriter
        Private ReadOnly _workspace As TestWorkspace

        Public Sub New(testLsifJsonWriter As TestLsifJsonWriter, workspace As TestWorkspace)
            _testLsifJsonWriter = testLsifJsonWriter
            _workspace = workspace
        End Sub

        Public Shared Async Function GenerateForWorkspaceAsync(workspace As TestWorkspace) As Task(Of TestLsifOutput)
            Dim testLsifJsonWriter = New TestLsifJsonWriter()
            Dim generator = New Generator(testLsifJsonWriter)

            For Each project In workspace.CurrentSolution.Projects
                Dim compilation = Await project.GetCompilationAsync()
                Await generator.GenerateForCompilation(compilation, project.FilePath, project.LanguageServices)
            Next

            Return New TestLsifOutput(testLsifJsonWriter, workspace)
        End Function

        Public Function GetElementById(Of T As Element)(id As Id(Of T)) As T
            Return _testLsifJsonWriter.GetElementById(id)
        End Function

        ''' <summary>
        ''' Returns all the vertices linked to the given vertex by the edge type.
        ''' </summary>
        Public Function GetLinkedVertices(Of T As Vertex)(vertex As Graph.Vertex, edgeLabel As String) As ImmutableArray(Of T)
            Return _testLsifJsonWriter.GetLinkedVertices(Of T)(vertex, edgeLabel)
        End Function

        Public ReadOnly Property Vertices As IEnumerable(Of Vertex)
            Get
                Return _testLsifJsonWriter.Vertices
            End Get
        End Property

        ''' <summary>
        ''' Returns the <see cref="Range" /> vertex in the output that corresponds to the selected range in the <see cref="TestWorkspace" />.
        ''' </summary>
        Public Async Function GetSelectedRangeAsync() As Task(Of Graph.Range)
            Dim selectedTestDocument = _workspace.Documents.Single(Function(d) d.SelectedSpans.Any())
            Dim selectedDocument = _workspace.CurrentSolution.GetDocument(selectedTestDocument.Id)
            Dim selectionTextSpan = selectedTestDocument.SelectedSpans.Single()
            Dim selectionRange = Range.FromTextSpan(selectionTextSpan, Await selectedDocument.GetTextAsync())

            Dim documentVertex = _testLsifJsonWriter.Vertices _
                                                    .OfType(Of Graph.Document) _
                                                    .Where(Function(d) d.Uri.LocalPath = selectedDocument.FilePath) _
                                                    .Single()

            Return _testLsifJsonWriter.GetLinkedVertices(Of Range)(documentVertex, "contains") _
                                      .Where(Function(r) r.Start = selectionRange.Start AndAlso
                                                         r.End = selectionRange.End) _
                                      .Single()
        End Function
    End Class
End Namespace

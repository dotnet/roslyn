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
                generator.GenerateForCompilation(compilation, project.FilePath, project.LanguageServices)
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
        ''' Returns the <see cref="Range" /> verticies in the output that corresponds to the selected range in the <see cref="TestWorkspace" />.
        ''' </summary>
        Public Async Function GetSelectedRangesAsync() As Task(Of IEnumerable(Of Graph.Range))
            Dim builder = ImmutableArray.CreateBuilder(Of Range)

            For Each testDocument In _workspace.Documents
                Dim documentVertex = _testLsifJsonWriter.Vertices _
                                                        .OfType(Of Graph.Document) _
                                                        .Where(Function(d) d.Uri.LocalPath = testDocument.FilePath) _
                                                        .Single()
                Dim rangeVertices = GetLinkedVertices(Of Range)(documentVertex, "contains")


                For Each selectedSpan In testDocument.SelectedSpans
                    Dim document = _workspace.CurrentSolution.GetDocument(testDocument.Id)
                    Dim selectionRange = Range.FromTextSpan(selectedSpan, Await document.GetTextAsync())

                    builder.Add(rangeVertices.Where(Function(r) r.Start = selectionRange.Start AndAlso
                                                                r.End = selectionRange.End) _
                                             .Single())
                Next
            Next

            Return builder.ToImmutable()
        End Function

        Public Async Function GetSelectedRangeAsync() As Task(Of Graph.Range)
            Return (Await GetSelectedRangesAsync()).Single()
        End Function
    End Class
End Namespace

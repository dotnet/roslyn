' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing

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

            Await GenerateForWorkspaceAsync(workspace, testLsifJsonWriter)

            Return New TestLsifOutput(testLsifJsonWriter, workspace)
        End Function

        Public Shared Async Function GenerateForWorkspaceAsync(workspace As TestWorkspace, jsonWriter As ILsifJsonWriter) As Task
            Dim generator = New Generator(jsonWriter)

            For Each project In workspace.CurrentSolution.Projects
                Dim compilation = Await project.GetCompilationAsync()

                ' Assert we don't have any errors to prevent any typos in the tests
                Assert.Empty(compilation.GetDiagnostics().Where(Function(d) d.Severity = DiagnosticSeverity.Error))

                generator.GenerateForCompilation(compilation, project.FilePath, project.LanguageServices)
            Next
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
                                                        .OfType(Of Graph.LsifDocument) _
                                                        .Where(Function(d) d.Uri.LocalPath = testDocument.FilePath) _
                                                        .Single()
                Dim rangeVertices = GetLinkedVertices(Of Range)(documentVertex, "contains")

                For Each selectedSpan In testDocument.SelectedSpans
                    Dim document = _workspace.CurrentSolution.GetDocument(testDocument.Id)
                    Dim text = Await document.GetTextAsync()
                    Dim linePositionSpan = text.Lines.GetLinePositionSpan(selectedSpan)
                    Dim positionStart = Range.ConvertLinePositionToPosition(linePositionSpan.Start)
                    Dim positionEnd = Range.ConvertLinePositionToPosition(linePositionSpan.End)

                    builder.Add(rangeVertices.Where(Function(r) r.Start = positionStart AndAlso
                                                                r.End = positionEnd) _
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

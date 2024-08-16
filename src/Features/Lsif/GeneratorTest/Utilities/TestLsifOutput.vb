' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServer
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.Extensions.Logging
Imports Roslyn.Utilities
Imports LSP = Roslyn.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests.Utilities
    Friend Class TestLsifOutput
        Private ReadOnly _testLsifJsonWriter As TestLsifJsonWriter
        Private ReadOnly _workspace As EditorTestWorkspace

        ''' <summary>
        ''' A MEF composition that matches the exact same MEF composition that will be used in the actual LSIF tool.
        ''' </summary>
        Public Shared ReadOnly TestComposition As TestComposition = TestComposition.Empty.AddAssemblies(Composition.MefCompositionAssemblies)

        Public Sub New(testLsifJsonWriter As TestLsifJsonWriter, workspace As EditorTestWorkspace)
            _testLsifJsonWriter = testLsifJsonWriter
            _workspace = workspace
        End Sub

        Public Shared Function GenerateForWorkspaceAsync(workspaceElement As XElement) As Task(Of TestLsifOutput)
            Dim workspace = EditorTestWorkspace.CreateWorkspace(workspaceElement, openDocuments:=False, composition:=TestComposition)
            Return GenerateForWorkspaceAsync(workspace)
        End Function

        Public Shared Async Function GenerateForWorkspaceAsync(workspace As EditorTestWorkspace) As Task(Of TestLsifOutput)
            Dim testLsifJsonWriter = New TestLsifJsonWriter()

            Await GenerateForWorkspaceAsync(workspace, testLsifJsonWriter)

            Return New TestLsifOutput(testLsifJsonWriter, workspace)
        End Function

        Public Shared Async Function GenerateForWorkspaceAsync(workspace As EditorTestWorkspace, jsonWriter As ILsifJsonWriter) As Task
            ' We always want to assert that we're running with the correct composition, or otherwise the test doesn't reflect the real
            ' world function of the indexer.
            Assert.Equal(workspace.Composition, TestComposition)

            Dim logger = New TestLogger()
            Dim lsifGenerator = Generator.CreateAndWriteCapabilitiesVertex(jsonWriter, logger)

            For Each project In workspace.CurrentSolution.Projects
                Dim compilation = Await project.GetCompilationAsync()

                ' Assert we don't have any errors to prevent any typos in the tests
                Assert.Empty(compilation.GetDiagnostics().Where(Function(d) d.Severity = DiagnosticSeverity.Error))

                Await lsifGenerator.GenerateForProjectAsync(project, GeneratorOptions.Default, CancellationToken.None)
            Next

            ' The only things would have logged were an error, so this should be empty
            Assert.Empty(logger.LoggedMessages)
        End Function

        Private Class TestLogger
            Implements ILogger

            Public ReadOnly LoggedMessages As New ConcurrentBag(Of String)

            Public Sub Log(Of TState)(logLevel As LogLevel, eventId As EventId, state As TState, exception As Exception, formatter As Func(Of TState, Exception, String)) Implements ILogger.Log
                Dim message = formatter(state, exception)
                LoggedMessages.Add(message)
            End Sub

            Public Function IsEnabled(logLevel As LogLevel) As Boolean Implements ILogger.IsEnabled
                Return True
            End Function

            Public Function BeginScope(Of TState)(state As TState) As IDisposable Implements ILogger.BeginScope
                Throw New NotImplementedException()
            End Function
        End Class

        Public Function GetElementById(Of T As Element)(id As Id(Of T)) As T
            Return _testLsifJsonWriter.GetElementById(id)
        End Function

        Public Function GetLinkedVertices(Of T As Vertex)(vertex As Graph.Vertex, predicate As Func(Of Edge, Boolean)) As ImmutableArray(Of T)
            Return _testLsifJsonWriter.GetLinkedVertices(Of T)(vertex, predicate)
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

        Private Async Function GetRangesAsync(selector As Func(Of TestHostDocument, IEnumerable(Of TextSpan))) As Task(Of IEnumerable(Of Graph.Range))
            Dim builder = ImmutableArray.CreateBuilder(Of Range)

            For Each testDocument In _workspace.Documents
                Dim documentVertex = _testLsifJsonWriter.Vertices _
                                                        .OfType(Of Graph.LsifDocument) _
                                                        .Where(Function(d) d.Uri.LocalPath = testDocument.FilePath) _
                                                        .Single()
                Dim rangeVertices = GetLinkedVertices(Of Range)(documentVertex, "contains")

                For Each selectedSpan In selector(testDocument)
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

        ''' <summary>
        ''' Returns the <see cref="Range" /> vertices in the output that corresponds to the selected range in the <see cref="TestWorkspace" />.
        ''' </summary>
        Public Function GetSelectedRangesAsync() As Task(Of IEnumerable(Of Graph.Range))
            Return GetRangesAsync(Function(testDocument) testDocument.SelectedSpans)
        End Function

        Public Async Function GetSelectedRangeAsync() As Task(Of Graph.Range)
            Return (Await GetSelectedRangesAsync()).Single()
        End Function

        Public Function GetAnnotatedRangesAsync(annotation As String) As Task(Of IEnumerable(Of Graph.Range))
            Return GetRangesAsync(Function(testDocument) testDocument.AnnotatedSpans.GetValueOrDefault(annotation))
        End Function

        Public Async Function GetAnnotatedRangeAsync(annotation As String) As Task(Of Graph.Range)
            Return (Await GetAnnotatedRangesAsync(annotation)).Single()
        End Function

        ''' <summary>
        ''' Returns an LSP Range type for the text span annotated with the given name. This is distinct from returning an LSIF Range vertex, which is what <see cref="GetAnnotatedRangeAsync(String)"/> does.
        ''' </summary>
        Public Async Function GetAnnotatedLspRangeAsync(annotation As String) As Task(Of LSP.Range)
            Dim annotatedDocument = _workspace.Documents.Single(Function(d) d.AnnotatedSpans.ContainsKey(annotation))
            Dim annotatedSpan = annotatedDocument.AnnotatedSpans(annotation).Single()

            Dim text = Await _workspace.CurrentSolution.GetRequiredDocument(annotatedDocument.Id).GetTextAsync()
            Dim linePositionSpan = text.Lines.GetLinePositionSpan(annotatedSpan)
            Return ProtocolConversions.LinePositionToRange(linePositionSpan)
        End Function

        Public Function GetFoldingRanges(document As Document) As LSP.FoldingRange()
            Dim documentVertex = _testLsifJsonWriter.Vertices.
                                                        OfType(Of LsifDocument).
                                                        Where(Function(d) d.Uri.LocalPath = document.FilePath).
                                                        Single()
            Dim foldingRangeVertex = GetLinkedVertices(Of FoldingRangeResult)(documentVertex, "textDocument/foldingRange").Single()
            Return foldingRangeVertex.Result
        End Function

        Public Function GetSemanticTokens(document As Document) As LSP.SemanticTokens
            Dim documentVertex = _testLsifJsonWriter.Vertices.
                OfType(Of LsifDocument).
                Where(Function(d) d.Uri.LocalPath = document.FilePath).
                Single()

            Dim semanticTokensVertex = GetLinkedVertices(Of SemanticTokensResult)(documentVertex, "textDocument/semanticTokens/full").Single()
            Return semanticTokensVertex.Result
        End Function
    End Class
End Namespace

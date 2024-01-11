' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Friend Class ProgressionTestState
        Implements IDisposable

        Public ReadOnly Workspace As EditorTestWorkspace

        Public Sub New(workspace As EditorTestWorkspace)
            Me.Workspace = workspace
        End Sub

        Public Shared Function Create(workspaceXml As XElement) As ProgressionTestState
            Dim workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)

            Return New ProgressionTestState(workspace)
        End Function

        Public Function GetGraphWithDocumentNode(filePath As String) As Graph
            Dim graphBuilder As New GraphBuilder(Workspace.CurrentSolution)
            Dim documentId = Workspace.Documents.Single(Function(d) d.FilePath = filePath).Id
            Assert.NotNull(graphBuilder.TryAddNodeForDocument(Workspace.CurrentSolution.GetDocument(documentId), CancellationToken.None))
            Return graphBuilder.Graph
        End Function

        Public Async Function GetGraphWithMarkedSymbolNodeAsync(Optional symbolTransform As Func(Of ISymbol, ISymbol) = Nothing) As Task(Of Graph)
            Dim hostDocument As TestHostDocument = Workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim document = Workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim symbol = Await GetMarkedSymbolAsync()

            If symbolTransform IsNot Nothing Then
                symbol = symbolTransform(symbol)
            End If

            Dim graphBuilder As New GraphBuilder(Workspace.CurrentSolution)
            Await graphBuilder.AddNodeAsync(symbol, document.Project, document, CancellationToken.None)
            Return graphBuilder.Graph
        End Function

        Public Async Function GetGraphContextAfterQuery(graph As Graph, graphQuery As IGraphQuery, direction As GraphContextDirection) As Task(Of IGraphContext)
            Dim graphContext As New MockGraphContext(direction, graph.Copy(), graph.Nodes)
            Dim graphBuilder = Await graphQuery.GetGraphAsync(Workspace.CurrentSolution, graphContext, CancellationToken.None)
            graphBuilder.ApplyToGraph(graphContext.Graph, CancellationToken.None)

            Return graphContext
        End Function

        Public Async Function GetGraphContextAfterQueryWithSolution(graph As Graph, solution As Solution, graphQuery As IGraphQuery, direction As GraphContextDirection) As Task(Of IGraphContext)
            Dim graphContext As New MockGraphContext(direction, graph.Copy(), graph.Nodes)
            Dim graphBuilder = Await graphQuery.GetGraphAsync(solution, graphContext, CancellationToken.None)
            graphBuilder.ApplyToGraph(graphContext.Graph, CancellationToken.None)

            Return graphContext
        End Function

        Private Sub Dispose() Implements IDisposable.Dispose
            Workspace.Dispose()
        End Sub

        Public Async Function AssertMarkedSymbolLabelIsAsync(graphCommandId As String, label As String, description As String) As Task
            Dim graphNode = (Await GetGraphWithMarkedSymbolNodeAsync()).Nodes.Single()
            Dim formattedLabelExtension As New GraphFormattedLabelExtension()

            Assert.Equal(label, formattedLabelExtension.Label(graphNode, graphCommandId))
            Assert.Equal(description, formattedLabelExtension.Description(graphNode, graphCommandId))
        End Function

        Public Function GetMarkedSymbolAsync() As Task(Of ISymbol)
            Dim hostDocument As TestHostDocument = Workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim document = Workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Return SymbolFinder.FindSymbolAtPositionAsync(document, hostDocument.CursorPosition.Value)
        End Function

        Public Function GetSolution() As Solution
            Return Workspace.CurrentSolution
        End Function
    End Class
End Namespace

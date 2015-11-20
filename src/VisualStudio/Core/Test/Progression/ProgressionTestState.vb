' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Friend Class ProgressionTestState
        Implements IDisposable

        Private ReadOnly _workspace As TestWorkspace

        Public Sub New(workspace As TestWorkspace)
            _workspace = workspace
        End Sub

        Public Shared Async Function CreateAsync(workspaceXml As XElement) As Task(Of ProgressionTestState)
            Dim workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml,
                                                              exportProvider:=MinimalTestExportProvider.CreateExportProvider(CompositionCatalog))

            Return New ProgressionTestState(workspace)
        End Function

        Public Function GetGraphWithDocumentNode(filePath As String) As Graph
            Dim graphBuilder As New GraphBuilder(_workspace.CurrentSolution, CancellationToken.None)
            Dim documentId = _workspace.Documents.Single(Function(d) d.FilePath = filePath).Id
            graphBuilder.AddNodeForDocument(_workspace.CurrentSolution.GetDocument(documentId))
            Return graphBuilder.Graph
        End Function

        Public Function GetGraphWithMarkedSymbolNode(Optional symbolTransform As Func(Of ISymbol, ISymbol) = Nothing) As Graph
            Dim hostDocument As TestHostDocument = _workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim document = _workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim symbol = GetMarkedSymbol()

            If symbolTransform IsNot Nothing Then
                symbol = symbolTransform(symbol)
            End If

            Dim graphBuilder As New GraphBuilder(_workspace.CurrentSolution, CancellationToken.None)
            graphBuilder.AddNodeForSymbolAsync(symbol, document.Project, document).Wait(CancellationToken.None)
            Return graphBuilder.Graph
        End Function

        Public Async Function GetGraphContextAfterQuery(graph As Graph, graphQuery As IGraphQuery, direction As GraphContextDirection) As Task(Of IGraphContext)
            Dim graphContext As New MockGraphContext(direction, graph.Copy(), graph.Nodes)
            Dim graphBuilder = Await graphQuery.GetGraphAsync(_workspace.CurrentSolution, graphContext, CancellationToken.None)
            graphBuilder.ApplyToGraph(graphContext.Graph)

            Return graphContext
        End Function

        Public Async Function GetGraphContextAfterQueryWithSolution(graph As Graph, solution As Solution, graphQuery As IGraphQuery, direction As GraphContextDirection) As Task(Of IGraphContext)
            Dim graphContext As New MockGraphContext(direction, graph.Copy(), graph.Nodes)
            Dim graphBuilder = Await graphQuery.GetGraphAsync(solution, graphContext, CancellationToken.None)
            graphBuilder.ApplyToGraph(graphContext.Graph)

            Return graphContext
        End Function

        Private Sub Dispose() Implements IDisposable.Dispose
            _workspace.Dispose()
        End Sub

        Public Sub AssertMarkedSymbolLabelIs(graphCommandId As String, label As String, description As String)
            Dim graphNode = GetGraphWithMarkedSymbolNode().Nodes.Single()
            Dim formattedLabelExtension As New GraphFormattedLabelExtension()

            Assert.Equal(label, formattedLabelExtension.Label(graphNode, graphCommandId))
            Assert.Equal(description, formattedLabelExtension.Description(graphNode, graphCommandId))
        End Sub

        Public Function GetMarkedSymbol() As ISymbol
            Dim hostDocument As TestHostDocument = _workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim document = _workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Return SymbolFinder.FindSymbolAtPositionAsync(document, hostDocument.CursorPosition.Value).Result
        End Function

        Public Function GetSolution() As Solution
            Return _workspace.CurrentSolution
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.VisualStudio.GraphModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Friend Class MockGraphContext
        Implements IGraphContext

        Private ReadOnly _direction As GraphContextDirection
        Private ReadOnly _graph As Graph
        Private ReadOnly _inputNodes As ISet(Of GraphNode)
        Private ReadOnly _outputNodes As New HashSet(Of GraphNode)

        Public Sub New(direction As GraphContextDirection, graph As Graph, inputNodes As IEnumerable(Of GraphNode))
            _direction = direction
            _graph = graph
            _inputNodes = New HashSet(Of GraphNode)(inputNodes)
        End Sub

        Public Event Canceled(sender As Object, e As EventArgs) Implements IGraphContext.Canceled

        Public ReadOnly Property CancelToken As CancellationToken Implements IGraphContext.CancelToken
            Get

            End Get
        End Property

        Public Event Completed(sender As Object, e As EventArgs) Implements IGraphContext.Completed

        Public ReadOnly Property Direction As GraphContextDirection Implements IGraphContext.Direction
            Get
                Return _direction
            End Get
        End Property

        Public ReadOnly Property Errors As IEnumerable(Of Exception) Implements IGraphContext.Errors
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Function GetValue(Of T)(name As String) As T Implements IGraphContext.GetValue
            Return Nothing
        End Function

        Public Property Graph As Graph Implements IGraphContext.Graph
            Get
                Return _graph
            End Get

            Set(value As Graph)
                Throw New NotImplementedException()
            End Set
        End Property

        Public Function HasValue(name As String) As Boolean Implements IGraphContext.HasValue
            Return False
        End Function

        Public ReadOnly Property InputNodes As ISet(Of GraphNode) Implements IGraphContext.InputNodes
            Get
                Return _inputNodes
            End Get
        End Property

        Public ReadOnly Property LinkCategories As IEnumerable(Of GraphCategory) Implements IGraphContext.LinkCategories
            Get
                Throw New NotImplementedException()

            End Get
        End Property

        Public ReadOnly Property LinkDepth As Integer Implements IGraphContext.LinkDepth
            Get
                Return 1
            End Get
        End Property

        Public ReadOnly Property NodeCategories As IEnumerable(Of GraphCategory) Implements IGraphContext.NodeCategories
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Sub OnCompleted() Implements IGraphContext.OnCompleted
        End Sub

        Public ReadOnly Property OutputNodes As ISet(Of GraphNode) Implements IGraphContext.OutputNodes
            Get
                Return _outputNodes
            End Get
        End Property

        Public Sub ReportError(exception As Exception) Implements IGraphContext.ReportError

        End Sub

        Public Sub ReportProgress(current As Integer, maximum As Integer, message As String) Implements IGraphContext.ReportProgress

        End Sub

        Public ReadOnly Property RequestedProperties As IEnumerable(Of GraphProperty) Implements IGraphContext.RequestedProperties
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Sub SetValue(Of T)(name As String, value As T) Implements IGraphContext.SetValue

        End Sub

        Public ReadOnly Property TrackChanges As Boolean Implements IGraphContext.TrackChanges
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

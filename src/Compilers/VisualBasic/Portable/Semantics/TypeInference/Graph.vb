' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class TypeArgumentInference

        Private Class GraphNode(Of TGraphNode As GraphNode(Of TGraphNode))

            Public ReadOnly Graph As Graph(Of TGraphNode)
            Public IsAddedToVertices As Boolean

            Public ReadOnly IncomingEdges As ArrayBuilder(Of TGraphNode)
            Public ReadOnly OutgoingEdges As ArrayBuilder(Of TGraphNode)

            Public AlgorithmData As GraphAlgorithmData(Of TGraphNode)

            Protected Sub New(graph As Graph(Of TGraphNode))
                Me.Graph = graph
                Me.IsAddedToVertices = False
                Me.IncomingEdges = New ArrayBuilder(Of TGraphNode)()
                Me.OutgoingEdges = New ArrayBuilder(Of TGraphNode)()
            End Sub

        End Class

        Private Enum DFSColor As Byte
            None
            Grey
            Black
        End Enum

        Private Structure GraphAlgorithmData(Of TGraphNode As GraphNode(Of TGraphNode))

            ' DFS specific fields
            Public Color As DFSColor

            ' Used for quick lookup of which strongly connected component this node is in.
            Public StronglyConnectedComponent As StronglyConnectedComponent(Of TGraphNode)
        End Structure

        Private Class StronglyConnectedComponent(Of TGraphNode As GraphNode(Of TGraphNode))
            Inherits GraphNode(Of StronglyConnectedComponent(Of TGraphNode))

            Public ReadOnly ChildNodes As ArrayBuilder(Of TGraphNode)

            Public Sub New(graph As Graph(Of StronglyConnectedComponent(Of TGraphNode)))
                MyBase.New(graph)
                ChildNodes = New ArrayBuilder(Of TGraphNode)()
            End Sub
        End Class

        Private Class Graph(Of TGraphNode As GraphNode(Of TGraphNode))

            Public ReadOnly Vertices As ArrayBuilder(Of TGraphNode)

            Public Sub New()
                Vertices = New ArrayBuilder(Of TGraphNode)()
            End Sub

            Public Sub AddEdge(
                source As TGraphNode,
                target As TGraphNode
            )
                AddNode(source)
                AddNode(target)

                source.OutgoingEdges.Add(target)
                target.IncomingEdges.Add(source)
            End Sub

            Public Sub AddNode(
                node As TGraphNode
            )
                Debug.Assert(node.Graph Is Me)

                If Not node.IsAddedToVertices Then
                    Vertices.Add(node)
                    node.IsAddedToVertices = True
                End If
            End Sub

            Public Sub RemoveEdge(
                source As TGraphNode,
                target As TGraphNode
            )
                Debug.Assert(Contains(source))
                Debug.Assert(Contains(target))

                Remove(source.OutgoingEdges, target)
                Remove(target.IncomingEdges, source)
            End Sub

            Private Shared Sub Remove(list As ArrayBuilder(Of TGraphNode), toRemove As TGraphNode)

                Dim lastIndex As Integer = list.Count - 1

                For i As Integer = 0 To lastIndex Step 1
                    If list(i) Is toRemove Then

                        If i < lastIndex Then
                            list(i) = list(lastIndex)
                        End If

                        list.Clip(lastIndex)
                        Return
                    End If
                Next

                Throw ExceptionUtilities.Unreachable
            End Sub


            Public Function BuildStronglyConnectedComponents() As Graph(Of StronglyConnectedComponent(Of TGraphNode))

                Dim sccGraph As New Graph(Of StronglyConnectedComponent(Of TGraphNode))()

                ' The first three steps are implementing Kosaraju's algorithm of finding 
                ' the strongly connected components of a directed graph.

                ' Step 1: Perform regular Dfs and build a list with the deepest node last.
                Dim orderedList = ArrayBuilder(Of TGraphNode).GetInstance()
                Dfs(orderedList)

                ' Step 2: Reset graph algorithm Data
                For Each current As TGraphNode In orderedList
                    current.AlgorithmData = New GraphAlgorithmData(Of TGraphNode)()
                Next

                ' Step 3: Walk the nodes and place each tree in the forest in a separate node.
                For Each current As TGraphNode In orderedList
                    If current.AlgorithmData.Color = DFSColor.None Then
                        Dim sccNode As New StronglyConnectedComponent(Of TGraphNode)(sccGraph)
                        CollectSccChildren(current, sccNode)
                        sccGraph.AddNode(sccNode)
                    End If
                Next

                orderedList.Free()
                orderedList = Nothing

                ' Step 4: Link incoming and outgoing edges.
                For Each sccNode As StronglyConnectedComponent(Of TGraphNode) In sccGraph.Vertices
                    For Each innerNodeIterCurrent As TGraphNode In sccNode.ChildNodes
                        For Each innerOutGoingInterCurrent As TGraphNode In innerNodeIterCurrent.OutgoingEdges

                            Dim target As StronglyConnectedComponent(Of TGraphNode) = innerOutGoingInterCurrent.AlgorithmData.StronglyConnectedComponent

                            ' Don't create self-edges.
                            If sccNode IsNot target Then
                                sccGraph.AddEdge(sccNode, target)
                            End If
                        Next
                    Next
                Next

                Return sccGraph
            End Function

            Private Sub CollectSccChildren(
                node As TGraphNode,
                sccNode As StronglyConnectedComponent(Of TGraphNode)
            )
                node.AlgorithmData.Color = DFSColor.Grey

                For Each current As TGraphNode In node.IncomingEdges
                    If current.AlgorithmData.Color = DFSColor.None Then
                        CollectSccChildren(current, sccNode)
                    End If
                Next

                node.AlgorithmData.Color = DFSColor.Black

                Debug.Assert(node.AlgorithmData.StronglyConnectedComponent Is Nothing)
                sccNode.ChildNodes.Add(node)
                node.AlgorithmData.StronglyConnectedComponent = sccNode
            End Sub

            Public Sub TopoSort(resultList As ArrayBuilder(Of TGraphNode))
                Dfs(resultList)
            End Sub

            Private Sub Dfs(resultList As ArrayBuilder(Of TGraphNode))
                For Each current In Vertices
                    current.AlgorithmData = New GraphAlgorithmData(Of TGraphNode)()
                Next

                Dim oldListSize = resultList.Count
                resultList.AddMany(Nothing, Vertices.Count)
                Dim newListSize = resultList.Count

                Dim insertAt As Integer = newListSize - 1

                For Each current As TGraphNode In Vertices
                    If current.AlgorithmData.Color = DFSColor.None Then
                        DfsVisit(current, resultList, insertAt)
                    End If
                Next

                Debug.Assert(insertAt = oldListSize - 1)
            End Sub

            Private Sub DfsVisit(
                node As TGraphNode,
                resultList As ArrayBuilder(Of TGraphNode),
                ByRef insertAt As Integer
            )
                node.AlgorithmData.Color = DFSColor.Grey

                For Each current As TGraphNode In node.OutgoingEdges
                    If current.AlgorithmData.Color = DFSColor.None Then
                        DfsVisit(current, resultList, insertAt)
                    End If
                Next

                node.AlgorithmData.Color = DFSColor.Black

                resultList(insertAt) = node
                insertAt -= 1
            End Sub

            Private Function Contains(node As TGraphNode) As Boolean
                Return node.Graph Is Me AndAlso node.IsAddedToVertices
            End Function

        End Class

    End Class

End Namespace


' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests.Utilities
    ''' <summary>
    ''' A implementation of <see cref="ILsifJsonWriter" /> for use in unit tests. It does additional validation of the
    ''' correctness of the output, And stores the entire output And offers useful helpers to inspect the result.
    ''' </summary>
    Friend Class TestLsifJsonWriter
        Implements ILsifJsonWriter

        Private ReadOnly _gate As Object = New Object()
        Private ReadOnly _elementsById As Dictionary(Of Id(Of Element), Element) = New Dictionary(Of Id(Of Element), Element)
        Private ReadOnly _edgesByOutVertex As Dictionary(Of Vertex, List(Of Edge)) = New Dictionary(Of Vertex, List(Of Edge))

        Private Sub ILsifJsonWriter_WriteAll(elements As List(Of Element)) Implements ILsifJsonWriter.WriteAll
            For Each element In elements
                ILsifJsonWriter_Write(element)
            Next
        End Sub

        Private Sub ILsifJsonWriter_Write(element As Element) Implements ILsifJsonWriter.Write
            SyncLock _gate
                ' We intentionally use Add so it'll throw if we have a duplicate ID.
                _elementsById.Add(element.Id, element)

                Dim edge = TryCast(element, Edge)

                If edge IsNot Nothing Then
                    ' Fetch all the out And in vertices, which validates they exist. This ensures we satisfy the rule
                    ' that an edge can only be written after all the vertices it writes to already exist.
                    Dim outVertex = GetElementById(edge.OutVertex)

                    For Each inVertexId In edge.GetInVerticies()
                        ' We are ignoring the return, but this call implicitly validates the element
                        ' exists and is of the correct type.
                        GetElementById(inVertexId)
                    Next

                    ' Record the edge in a map of edges exiting this outVertex. We could do a nested Dictionary
                    ' for this but that seems a bit expensive when many nodes may only have one item.
                    Dim edgesForOutVertex As List(Of Edge) = Nothing
                    If Not _edgesByOutVertex.TryGetValue(outVertex, edgesForOutVertex) Then
                        edgesForOutVertex = New List(Of Edge)(capacity:=1)
                        _edgesByOutVertex.Add(outVertex, edgesForOutVertex)
                    End If

                    ' It's possible to have more than one item edge, but for anything else we really only expect one.
                    If edge.Label <> "item" Then
                        If (edgesForOutVertex.Any(Function(e) e.Label = edge.Label)) Then
                            Throw New InvalidOperationException($"The outVertex {outVertex} already has an edge with label {edge.Label}.")
                        End If
                    End If

                    edgesForOutVertex.Add(edge)
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Returns all the vertices linked to the given vertex by the edge type.
        ''' </summary>
        Public Function GetLinkedVertices(Of T As Vertex)(vertex As Graph.Vertex, edgeLabel As String) As ImmutableArray(Of T)
            Return GetLinkedVertices(Of T)(vertex, Function(e) e.Label = edgeLabel)
        End Function

        ''' <summary>
        ''' Returns all the vertices linked to the given vertex by the edge predicate.
        ''' </summary>
        Public Function GetLinkedVertices(Of T As Vertex)(vertex As Graph.Vertex, predicate As Func(Of Edge, Boolean)) As ImmutableArray(Of T)
            SyncLock _gate
                Dim builder = ImmutableArray.CreateBuilder(Of T)

                Dim edges As List(Of Edge) = Nothing
                If _edgesByOutVertex.TryGetValue(vertex, edges) Then
                    Dim inVerticesId = edges.Where(predicate).SelectMany(Function(e) e.GetInVerticies())

                    For Each inVertexId In inVerticesId
                        ' This is an unsafe "cast" if you will converting the ID to the expected type;
                        ' GetElementById checks the real vertex type so thta will stay safe in the end.
                        builder.Add(GetElementById(Of T)(New Id(Of T)(inVertexId.NumericId)))
                    Next
                End If

                Return builder.ToImmutable()
            End SyncLock
        End Function

        Public ReadOnly Property Vertices As ImmutableArray(Of Vertex)
            Get
                SyncLock _gate
                    Return _elementsById.Values.OfType(Of Vertex).ToImmutableArray()
                End SyncLock
            End Get
        End Property

        Public Function GetElementById(Of T As Element)(id As Id(Of T)) As T
            SyncLock _gate
                Dim element As Element = Nothing

                ' TODO: why am I unable to use the extension method As here?
                If Not _elementsById.TryGetValue(New Id(Of Element)(id.NumericId), element) Then
                    Throw New Exception($"Element {id} could not be found.")
                End If

                Return Assert.IsAssignableFrom(Of T)(element)
            End SyncLock
        End Function
    End Class
End Namespace

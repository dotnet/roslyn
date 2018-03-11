' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Module SyntaxDifferences

    Public Function GetRebuiltNodes(oldTree As SyntaxTree, newTree As SyntaxTree) As ImmutableArray(Of SyntaxNodeOrToken)
        Dim hashSet = New HashSet(Of GreenNode)
        GatherNodes(oldTree.GetRoot(), hashSet)

        Dim nodes = ArrayBuilder(Of SyntaxNodeOrToken).GetInstance()
        GetRebuiltNodes(newTree.GetRoot(), hashSet, nodes)
        Return nodes.ToImmutableAndFree()
    End Function

    Private Sub GetRebuiltNodes(newNode As SyntaxNodeOrToken, hashSet As HashSet(Of GreenNode), nodes As ArrayBuilder(Of SyntaxNodeOrToken))
        If hashSet.Contains(newNode.UnderlyingNode) Then
            Return
        End If

        nodes.Add(newNode)

        For Each child In newNode.ChildNodesAndTokens()
            GetRebuiltNodes(child, hashSet, nodes)
        Next
    End Sub

    Private Sub GatherNodes(node As SyntaxNodeOrToken, hashSet As HashSet(Of GreenNode))
        hashSet.Add(node.UnderlyingNode)

        For Each child In node.ChildNodesAndTokens()
            GatherNodes(child, hashSet)
        Next
    End Sub

End Module

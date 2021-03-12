' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue

    Friend MustInherit Class AbstractSyntaxComparer

        Inherits Microsoft.CodeAnalysis.Differencing.AbstractSyntaxComparer

        Friend Sub New(oldRoot As SyntaxNode, newRoot As SyntaxNode, oldRootChildren As IEnumerable(Of SyntaxNode), newRootChildren As IEnumerable(Of SyntaxNode), compareStatementSyntax As Boolean)
            MyBase.New(oldRoot, newRoot, oldRootChildren, newRootChildren, compareStatementSyntax)
        End Sub

#Region "Comparison"

        Public NotOverridable Overrides Function GetDistance(oldNode As SyntaxNode, newNode As SyntaxNode) As Double
            Debug.Assert(GetLabel(oldNode) = GetLabel(newNode) AndAlso GetLabel(oldNode) <> IgnoredNode)

            If oldNode Is newNode Then
                Return ExactMatchDist
            End If

            Dim weightedDistance As Double
            If TryComputeWeightedDistance(oldNode, newNode, weightedDistance) Then
                If weightedDistance = ExactMatchDist AndAlso Not SyntaxFactory.AreEquivalent(oldNode, newNode) Then
                    weightedDistance = EpsilonDist
                End If

                Return weightedDistance
            End If

            Return ComputeValueDistance(oldNode, newNode)
        End Function

        Friend Shared Function ComputeValueDistance(leftNode As SyntaxNode, rightNode As SyntaxNode) As Double
            If SyntaxFactory.AreEquivalent(leftNode, rightNode) Then
                Return ExactMatchDist
            End If

            Dim distance As Double = ComputeDistance(leftNode, rightNode)
            Return If(distance = ExactMatchDist, EpsilonDist, distance)
        End Function

        Friend Overloads Shared Function ComputeDistance(oldNodeOrToken As SyntaxNodeOrToken, newNodeOrToken As SyntaxNodeOrToken) As Double
            Debug.Assert(newNodeOrToken.IsToken = oldNodeOrToken.IsToken)

            Dim distance As Double
            If oldNodeOrToken.IsToken Then
                Dim leftToken = oldNodeOrToken.AsToken()
                Dim rightToken = newNodeOrToken.AsToken()

                distance = ComputeDistance(leftToken, rightToken)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(leftToken, rightToken) OrElse distance = ExactMatchDist)
            Else
                Dim leftNode = oldNodeOrToken.AsNode()
                Dim rightNode = newNodeOrToken.AsNode()

                distance = ComputeDistance(leftNode, rightNode)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(leftNode, rightNode) OrElse distance = ExactMatchDist)
            End If

            Return distance
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SyntaxList(Of TSyntaxNode), newList As SyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SeparatedSyntaxList(Of TSyntaxNode), newList As SeparatedSyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        ''' <summary>
        ''' Enumerates tokens of all nodes in the list.
        ''' </summary>
        Friend Shared Iterator Function GetDescendantTokensIgnoringSeparators(Of TSyntaxNode As SyntaxNode)(list As SyntaxList(Of TSyntaxNode)) As IEnumerable(Of SyntaxToken)
            For Each node In list
                For Each token In node.DescendantTokens()
                    Yield token
                Next
            Next
        End Function

        ''' <summary>
        ''' Enumerates tokens of all nodes in the list. Doesn't include separators.
        ''' </summary>
        Private Shared Iterator Function GetDescendantTokensIgnoringSeparators(Of TSyntaxNode As SyntaxNode)(list As SeparatedSyntaxList(Of TSyntaxNode)) As IEnumerable(Of SyntaxToken)
            For Each node In list
                For Each token In node.DescendantTokens()
                    Yield token
                Next
            Next
        End Function

        ''' <summary>
        ''' Calculates the distance between two syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldNode As SyntaxNode, newNode As SyntaxNode) As Double
            If oldNode Is Nothing OrElse newNode Is Nothing Then
                Return If(oldNode Is newNode, 0.0, 1.0)
            End If

            Return ComputeDistance(oldNode.DescendantTokens(), newNode.DescendantTokens())
        End Function

        ''' <summary>
        ''' Calculates the distance between two syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the tokens are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldToken As SyntaxToken, newToken As SyntaxToken) As Double
            Return LongestCommonSubstring.ComputeDistance(oldToken.ValueText, newToken.ValueText)
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As Double
            Return ComputeDistance(oldTokens.AsImmutableOrNull(), newTokens.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As ImmutableArray(Of SyntaxToken), newTokens As ImmutableArray(Of SyntaxToken)) As Double
            Return LcsTokens.Instance.ComputeDistance(oldTokens.NullToEmpty(), newTokens.NullToEmpty())
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxNode), newTokens As IEnumerable(Of SyntaxNode)) As Double
            Return ComputeDistance(oldTokens.AsImmutableOrNull(), newTokens.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As ImmutableArray(Of SyntaxNode), newTokens As ImmutableArray(Of SyntaxNode)) As Double
            Return LcsNodes.Instance.ComputeDistance(oldTokens.NullToEmpty(), newTokens.NullToEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldNodes As IEnumerable(Of SyntaxNode), newNodes As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return LcsNodes.Instance.GetEdits(oldNodes.AsImmutableOrEmpty(), newNodes.AsImmutableOrEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldNodes As ImmutableArray(Of SyntaxNode), newNodes As ImmutableArray(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return LcsNodes.Instance.GetEdits(oldNodes.NullToEmpty(), newNodes.NullToEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return LcsTokens.Instance.GetEdits(oldTokens.AsImmutableOrEmpty(), newTokens.AsImmutableOrEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldTokens As ImmutableArray(Of SyntaxToken), newTokens As ImmutableArray(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return LcsTokens.Instance.GetEdits(oldTokens.NullToEmpty(), newTokens.NullToEmpty())
        End Function

        Private NotInheritable Class LcsTokens
            Inherits LongestCommonImmutableArraySubsequence(Of SyntaxToken)

            Friend Shared ReadOnly Instance As LcsTokens = New LcsTokens()

            Protected Overrides Function Equals(oldElement As SyntaxToken, newElement As SyntaxToken) As Boolean
                Return SyntaxFactory.AreEquivalent(oldElement, newElement)
            End Function
        End Class

        Private NotInheritable Class LcsNodes
            Inherits LongestCommonImmutableArraySubsequence(Of SyntaxNode)

            Friend Shared ReadOnly Instance As LcsNodes = New LcsNodes()

            Protected Overrides Function Equals(oldElement As SyntaxNode, newElement As SyntaxNode) As Boolean
                Return SyntaxFactory.AreEquivalent(oldElement, newElement)
            End Function
        End Class
#End Region
    End Class
End Namespace

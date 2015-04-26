' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing

Namespace Microsoft.CodeAnalysis.VisualBasic.Differencing

    Friend MustInherit Class VisualBasicSyntaxComparer
        Inherits SyntaxComparer

        Protected Sub New()
            MyBase.New(VisualBasicSyntaxEquivalent.Instance)
        End Sub

        Friend Overloads Shared Function ComputeValueDistance(leftNode As SyntaxNode, rightNode As SyntaxNode) As Double
            Return SyntaxComparer.ComputeValueDistance(leftNode, rightNode, VisualBasicSyntaxEquivalent.Instance)
        End Function

        Friend Overloads Shared Function ComputeDistance(oldNodeOrToken As SyntaxNodeOrToken, newNodeOrToken As SyntaxNodeOrToken) As Double
            Return SyntaxComparer.ComputeDistance(oldNodeOrToken, newNodeOrToken, VisualBasicSyntaxEquivalent.Instance)
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SyntaxList(Of TSyntaxNode), newList As SyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SeparatedSyntaxList(Of TSyntaxNode), newList As SeparatedSyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        ''' <summary>
        ''' Calculates the distance between two syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldNode As SyntaxNode, newNode As SyntaxNode) As Double
            Return SyntaxComparer.ComputeDistance(oldNode, newNode, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As Double
            Return SyntaxComparer.ComputeDistance(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As ImmutableArray(Of SyntaxToken), newTokens As ImmutableArray(Of SyntaxToken)) As Double
            Return SyntaxComparer.ComputeDistance(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxNode), newTokens As IEnumerable(Of SyntaxNode)) As Double
            Return SyntaxComparer.ComputeDistance(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As ImmutableArray(Of SyntaxNode), newTokens As ImmutableArray(Of SyntaxNode)) As Double
            Return SyntaxComparer.ComputeDistance(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Overloads Shared Function GetSequenceEdits(oldNodes As IEnumerable(Of SyntaxNode), newNodes As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldNodes, newNodes, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Overloads Shared Function GetSequenceEdits(oldNodes As ImmutableArray(Of SyntaxNode), newNodes As ImmutableArray(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldNodes, newNodes, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Overloads Shared Function GetSequenceEdits(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Overloads Shared Function GetSequenceEdits(oldTokens As ImmutableArray(Of SyntaxToken), newTokens As ImmutableArray(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldTokens, newTokens, VisualBasicSyntaxEquivalent.Instance)
        End Function

        Private Class VisualBasicSyntaxEquivalent
            Implements ISyntaxEquivalentChecker

            Public Shared ReadOnly Instance As VisualBasicSyntaxEquivalent = New VisualBasicSyntaxEquivalent()

            Private ReadOnly _lcsNodes As LcsNodes = New LcsNodes(Function(l, r) SyntaxFactory.AreEquivalent(l, r))
            Private ReadOnly _lcsTokens As LcsTokens = New LcsTokens(Function(l, r) SyntaxFactory.AreEquivalent(l, r))

            Public ReadOnly Property LcsNodes As LcsNodes Implements ISyntaxEquivalentChecker.LcsNodes
                Get
                    Return _lcsNodes
                End Get
            End Property

            Public ReadOnly Property LcsTokens As LcsTokens Implements ISyntaxEquivalentChecker.LcsTokens
                Get
                    Return _lcsTokens
                End Get
            End Property

            Public Function AreEquivalent(left As SyntaxToken, right As SyntaxToken) As Boolean Implements ISyntaxEquivalentChecker.AreEquivalent
                Return SyntaxFactory.AreEquivalent(left, right)
            End Function

            Public Function AreEquivalent(left As SyntaxNode, right As SyntaxNode) As Boolean Implements ISyntaxEquivalentChecker.AreEquivalent
                Return SyntaxFactory.AreEquivalent(left, right)
            End Function
        End Class
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Else" keyword for the statement context.
    ''' </summary>
    Friend Class ElseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken
            Dim parent = targetToken.GetAncestor(Of SingleLineIfStatementSyntax)()

            If parent IsNot Nothing AndAlso Not parent.Statements.IsEmpty() Then
                If context.IsFollowingCompleteStatement(Of SingleLineIfStatementSyntax)(Function(ifStatement) ifStatement.Statements.Last()) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Else", VBFeaturesResources.ElseKeywordToolTip))
                End If
            End If

            If context.IsSingleLineStatementContext AndAlso
               IsDirectlyInIfOrElseIf(context) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Else", VBFeaturesResources.ElseKeywordToolTip))
            End If

            ' Determine whether we can offer "Else" after "Case" in a Select block.
            If targetToken.Kind = SyntaxKind.CaseKeyword AndAlso targetToken.Parent.IsKind(SyntaxKind.CaseStatement) Then
                ' Next, grab the parenting "Select" block and ensure that it doesn't have any Case Else statements
                Dim selectBlock = targetToken.GetAncestor(Of SelectBlockSyntax)()
                If selectBlock IsNot Nothing AndAlso
                   Not selectBlock.CaseBlocks.Any(Function(cb) cb.CaseStatement.Kind = SyntaxKind.CaseElseStatement) Then

                    ' Finally, ensure this case statement is the last one in the parenting "Select" block.
                    If selectBlock.CaseBlocks.Last().CaseStatement Is targetToken.Parent Then
                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Else", VBFeaturesResources.CaseElseKeywordToolTip))
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function IsDirectlyInIfOrElseIf(context As VisualBasicSyntaxContext) As Boolean
            ' Maybe we're after the Then keyword
            If context.TargetToken.IsKind(SyntaxKind.ThenKeyword) AndAlso
                context.TargetToken.Parent?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) Then
                Return True
            End If

            Dim statement = context.TargetToken.Parent.GetAncestor(Of StatementSyntax)
            Return If(statement?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock), False)
        End Function
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Is" keyword at the beginning of any clause in a "Case" statement
    ''' </summary>
    Friend Class IsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            ' Determine whether we can offer "Is" at the beginning of a CaseClauseSyntax. Make sure
            ' that the token is not after a CaseElseStatement.
            Dim selectBlock = targetToken.GetAncestor(Of SelectBlockSyntax)()
            If selectBlock IsNot Nothing Then
                Dim caseElseBlock = selectBlock.CaseBlocks.FirstOrDefault(Function(caseBlock) caseBlock.CaseStatement.Kind = SyntaxKind.CaseElseStatement)
                If caseElseBlock Is Nothing OrElse targetToken.SpanStart < caseElseBlock.SpanStart Then
                    ' Handle cases where the token is at the beginning of the first clause
                    ' (following the Case keyword) and where the token is at the beginning of
                    ' subsequent clauses (following the list separator token).
                    If targetToken.IsChildToken(Of CaseStatementSyntax)(Function(caseStatement) caseStatement.CaseKeyword) OrElse
                       targetToken.IsChildSeparatorToken(Of CaseStatementSyntax, CaseClauseSyntax)(Function(caseStatement) caseStatement.Cases) Then
                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Is", VBFeaturesResources.CaseIsKeywordToolTip))
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

    End Class
End Namespace

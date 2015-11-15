' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "If" keyword for the statement context
    ''' </summary>
    Friend Class IfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("If", VBFeaturesResources.IfKeywordToolTip))
            End If

            ' We might be typing "Else If" as two keywords. At this point, the parser is parsing this statement as a
            ' ElseStatementSyntax.
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsChildToken(Of ElseStatementSyntax)(Function(ifStatement) ifStatement.ElseKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("If", VBFeaturesResources.ElseIfKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

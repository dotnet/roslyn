' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "To" keyword in array bounds.
    ''' </summary>
    Friend Class ToKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            Dim simpleArgument = targetToken.GetAncestor(Of SimpleArgumentSyntax)()
            If simpleArgument IsNot Nothing Then
                Dim modifiedIdentifier = targetToken.GetAncestor(Of ModifiedIdentifierSyntax)()
                If modifiedIdentifier IsNot Nothing Then
                    If modifiedIdentifier.ArrayBounds IsNot Nothing AndAlso
                       modifiedIdentifier.ArrayBounds.Arguments.Contains(simpleArgument) Then
                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("To", VBFeaturesResources.ToKeywordToolTip))
                    End If
                End If

                ' For ReDim, this will be a ReDim clause.
                Dim clause = targetToken.GetAncestor(Of RedimClauseSyntax)()
                If clause IsNot Nothing Then
                    Dim redimStatement = targetToken.GetAncestor(Of ReDimStatementSyntax)()
                    If redimStatement IsNot Nothing Then
                        If redimStatement.Clauses.Contains(clause) Then
                            Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("To", VBFeaturesResources.ToKeywordToolTip))
                        End If
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

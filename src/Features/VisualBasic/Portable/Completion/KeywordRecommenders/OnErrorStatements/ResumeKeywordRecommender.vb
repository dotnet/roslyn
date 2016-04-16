' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Resume Next" after "On Error", or "Resume" as a standalone statement
    ''' </summary>
    Friend Class ResumeKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' On Error statements are never valid in lambdas
            If context.IsInLambda Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.Kind = SyntaxKind.ErrorKeyword AndAlso IsOnErrorStatement(targetToken.Parent) Then
                Return SpecializedCollections.SingletonEnumerable(
                            New RecommendedKeyword("Resume Next", VBFeaturesResources.OnErrorResumeNextKeywordToolTip))
            End If

            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(
                            New RecommendedKeyword("Resume", VBFeaturesResources.OnErrorResumeNextKeywordToolTip))
                ' TODO: we are inconsistent here in Dev10. We offer "On Error Resume Next" even after typing just "On",
                ' yet curiously we don't show "Resume Next" as it's own statement. This might be something to fix if
                ' we determine we even care.
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Then" keyword in an If statement.
    ''' </summary>
    Friend Class ThenKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim isFollowingIfStatement = context.SyntaxTree.IsFollowingCompleteExpression(Of IfStatementSyntax)(
                context.Position,
                context.TargetToken,
                childGetter:=Function(ifStatement) ifStatement.Condition,
                cancellationToken:=cancellationToken,
                allowImplicitLineContinuation:=False)

            Dim isFollowingIfDirective = context.IsPreProcessorDirectiveContext AndAlso
                context.SyntaxTree.IsFollowingCompleteExpression(Of IfDirectiveTriviaSyntax)(
                    context.Position,
                    context.TargetToken,
                    childGetter:=Function(ifDirective) ifDirective.Condition,
                    cancellationToken:=cancellationToken,
                    allowImplicitLineContinuation:=False)

            If isFollowingIfStatement OrElse isFollowingIfDirective Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Then", VBFeaturesResources.ThenKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

    End Class
End Namespace

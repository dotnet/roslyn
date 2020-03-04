' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Then", VBFeaturesResources.Introduces_a_statement_block_to_be_compiled_or_executed_if_a_tested_condition_is_true))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

    End Class
End Namespace

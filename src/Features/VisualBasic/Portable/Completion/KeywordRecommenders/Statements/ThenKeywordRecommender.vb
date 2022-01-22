' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Then", VBFeaturesResources.Introduces_a_statement_block_to_be_compiled_or_executed_if_a_tested_condition_is_true))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
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
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

    End Class
End Namespace

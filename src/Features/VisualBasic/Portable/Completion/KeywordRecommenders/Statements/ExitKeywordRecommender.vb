' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Exit" keyword at the start of a statement
    ''' </summary>
    Friend Class ExitKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Exit", VBFeaturesResources.Exits_a_procedure_or_block_and_transfers_execution_immediately_to_the_statement_following_the_procedure_call_or_block_definition_Exit_Do_For_Function_Property_Select_Sub_Try_While))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            ' Make sure we're in an exitable block
            If Not context.IsInStatementBlockOfKind(
                SyntaxKind.SimpleDoLoopBlock,
                SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
                SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock,
                SyntaxKind.ForBlock, SyntaxKind.ForEachBlock,
                SyntaxKind.FunctionBlock,
                SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression,
                SyntaxKind.PropertyBlock,
                SyntaxKind.SelectBlock,
                SyntaxKind.SubBlock,
                SyntaxKind.TryBlock, SyntaxKind.CatchBlock,
                SyntaxKind.WhileBlock) Then

                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            If context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            ' We know that in any executable statement context, there always must be at least one thing we can exit: the
            ' function or sub itself (except for Finally blocks)
            Return If(context.IsStatementContext, s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function

    End Class
End Namespace

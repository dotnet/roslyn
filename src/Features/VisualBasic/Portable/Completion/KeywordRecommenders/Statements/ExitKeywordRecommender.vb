' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Exit" keyword at the start of a statement
    ''' </summary>
    Friend Class ExitKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
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

                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' We know that in any executable statement context, there always must be at least one thing we can exit: the
            ' function or sub itself (except for Finally blocks)
            If context.IsSingleLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Exit", VBFeaturesResources.Exits_a_procedure_or_block_and_transfers_execution_immediately_to_the_statement_following_the_procedure_call_or_block_definition_Exit_Do_For_Function_Property_Select_Sub_Try_While))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function

    End Class
End Namespace

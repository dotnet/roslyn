' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Sub" keyword in member declaration contexts
    ''' </summary>
    Friend Class SubKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsTypeMemberDeclarationKeywordContext OrElse context.IsInterfaceMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Method) AndAlso
                   modifiers.IteratorKeyword.Kind = SyntaxKind.None Then

                    Return ImmutableArray.Create(New RecommendedKeyword("Sub", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Sub_procedure_that_is_a_procedure_that_does_not_return_a_value_to_the_calling_code))
                End If
            End If

            ' Exit Sub
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.SubBlock, SyntaxKind.MultiLineSubLambdaExpression) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then
                Return ImmutableArray.Create(New RecommendedKeyword("Sub", VBFeaturesResources.Exits_a_Sub_procedure_and_transfers_execution_immediately_to_the_statement_following_the_call_to_the_Sub_procedure))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace

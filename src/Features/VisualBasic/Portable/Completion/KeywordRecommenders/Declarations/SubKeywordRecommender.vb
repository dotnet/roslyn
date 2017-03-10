' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsTypeMemberDeclarationKeywordContext OrElse context.IsInterfaceMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Method) AndAlso
                   modifiers.IteratorKeyword.Kind = SyntaxKind.None Then

                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Sub", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Sub_procedure_that_is_a_procedure_that_does_not_return_a_value_to_the_calling_code))
                End If
            End If

            ' Exit Sub
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.SubBlock, SyntaxKind.MultiLineSubLambdaExpression) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Sub", VBFeaturesResources.Exits_a_Sub_procedure_and_transfers_execution_immediately_to_the_statement_following_the_call_to_the_Sub_procedure))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

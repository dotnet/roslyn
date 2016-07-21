' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Dim" keyword in all appropriate contexts.
    ''' </summary>
    Friend Class DimKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' It can start a statement
            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(
                            New RecommendedKeyword("Dim", VBFeaturesResources.Declares_and_allocates_storage_space_for_one_or_more_variables_Dim_var_bracket_As_bracket_New_bracket_dataType_bracket_boundList_bracket_bracket_bracket_initializer_bracket_bracket_var2_bracket))
            End If

            If context.IsTypeMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts

                ' In Dev10, we don't show it after Const (but will after ReadOnly, even though the formatter removes it)
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Field) AndAlso
                   modifiers.MutabilityOrWithEventsKeyword.Kind <> SyntaxKind.ConstKeyword AndAlso
                   modifiers.DimKeyword.Kind = SyntaxKind.None Then
                    Return SpecializedCollections.SingletonEnumerable(
                                New RecommendedKeyword("Dim", VBFeaturesResources.Declares_and_allocates_storage_space_for_one_or_more_variables_Dim_var_bracket_As_bracket_New_bracket_dataType_bracket_boundList_bracket_bracket_bracket_initializer_bracket_bracket_var2_bracket))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

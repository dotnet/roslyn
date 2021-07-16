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
    ''' Recommends the "Dim" keyword in all appropriate contexts.
    ''' </summary>
    Friend Class DimKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Dim", VBFeaturesResources.Declares_and_allocates_storage_space_for_one_or_more_variables_Dim_var_bracket_As_bracket_New_bracket_dataType_bracket_boundList_bracket_bracket_bracket_initializer_bracket_bracket_var2_bracket))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            ' It can start a statement
            If context.IsMultiLineStatementContext Then
                Return s_keywords
            End If

            If context.IsTypeMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts

                ' In Dev10, we don't show it after Const (but will after ReadOnly, even though the formatter removes it)
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Field) AndAlso
                   modifiers.MutabilityOrWithEventsKeyword.Kind <> SyntaxKind.ConstKeyword AndAlso
                   modifiers.DimKeyword.Kind = SyntaxKind.None Then
                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace

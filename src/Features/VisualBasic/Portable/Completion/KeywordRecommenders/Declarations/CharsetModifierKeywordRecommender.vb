' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends one of the charset modifiers after a "Declare" keyword
    ''' </summary>
    Friend Class CharsetModifierKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("Ansi", VBFeaturesResources.Used_in_a_Declare_statement_The_Ansi_modifier_specifies_that_Visual_Basic_should_marshal_all_strings_to_ANSI_values_and_should_look_up_the_procedure_without_modifying_its_name_during_the_search_If_no_character_set_is_specified_ANSI_is_the_default),
            New RecommendedKeyword("Unicode", VBFeaturesResources.Used_in_a_Declare_statement_Specifies_that_Visual_Basic_should_marshal_all_strings_to_Unicode_values_in_a_call_into_an_external_procedure_and_should_look_up_the_procedure_without_modifying_its_name),
            New RecommendedKeyword("Auto", VBFeaturesResources.Used_in_a_Declare_statement_The_Auto_modifier_specifies_that_Visual_Basic_should_marshal_strings_according_to_NET_Framework_rules_and_should_determine_the_base_character_set_of_the_run_time_platform_and_possibly_modify_the_external_procedure_name_if_the_initial_search_fails))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            Return If(targetToken.IsChildToken(Of DeclareStatementSyntax)(Function(externalMethodDeclaration) externalMethodDeclaration.DeclareKeyword),
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace

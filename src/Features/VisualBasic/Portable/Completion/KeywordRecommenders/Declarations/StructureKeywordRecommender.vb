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
    ''' Recommends the "Structure" keyword in type declaration contexts
    ''' </summary>
    Friend Class StructureKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Structure", VBFeaturesResources.Declares_the_name_of_a_structure_and_introduces_the_definition_of_the_variables_properties_events_and_procedures_that_make_up_the_structure))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsTypeDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Structure) Then
                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace

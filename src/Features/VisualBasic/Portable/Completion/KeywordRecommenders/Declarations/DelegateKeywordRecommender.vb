' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Delegate" keyword in member declaration contexts
    ''' </summary>
    Friend Class DelegateKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsTypeDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Delegate) Then
                    Return SpecializedCollections.SingletonEnumerable(
                                New RecommendedKeyword("Delegate", VBFeaturesResources.Used_to_declare_a_delegate_A_delegate_is_a_reference_type_that_refers_to_a_shared_method_of_a_type_or_to_an_instance_method_of_an_object_Any_procedure_that_is_convertible_or_that_has_matching_parameter_types_and_return_type_may_be_used_to_create_an_instance_of_this_delegate_class))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

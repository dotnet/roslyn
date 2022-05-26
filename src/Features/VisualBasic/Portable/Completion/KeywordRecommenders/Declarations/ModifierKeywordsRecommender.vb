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
    ''' Recommends the "Property" keyword in member declaration contexts
    ''' </summary>
    Friend Class ModifierKeywordsRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If Not context.IsTypeMemberDeclarationKeywordContext AndAlso Not context.IsTypeDeclarationKeywordContext Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim modifierFacts = context.ModifierCollectionFacts
            Dim recommendations As New List(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            Dim innermostDeclaration = GetInnermostDeclarationContext(targetToken)
            Dim innermostDeclarationKind =
                If(innermostDeclaration IsNot Nothing AndAlso innermostDeclaration.Kind <> SyntaxKind.CompilationUnit,
                   innermostDeclaration.Kind,
                   SyntaxKind.NamespaceBlock)

            If modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.None AndAlso Not context.IsInterfaceMemberDeclarationKeywordContext Then
                If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                    recommendations.Add(New RecommendedKeyword("Public", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_have_no_access_restrictions))
                End If

                ' Only "Public" is legal for operators
                If modifierFacts.NarrowingOrWideningKeyword.Kind = SyntaxKind.None Then
                    If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                        recommendations.Add(New RecommendedKeyword("Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_the_assembly_that_contains_their_declaration))
                    End If

                    If modifierFacts.DefaultKeyword.Kind = SyntaxKind.None AndAlso innermostDeclarationKind <> SyntaxKind.NamespaceBlock Then
                        recommendations.Add(New RecommendedKeyword("Private", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_module_class_or_structure))
                    End If
                End If
            End If

            If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.ProtectedMember) Then
                If modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.None Then
                    recommendations.Add(New RecommendedKeyword("Protected", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_own_class_or_from_a_derived_class))
                    recommendations.Add(New RecommendedKeyword("Protected Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_members_of_a_class_are_accessible_from_anywhere_in_the_same_assembly_their_own_classes_and_derived_classes))
                ElseIf modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.ProtectedKeyword AndAlso Not modifierFacts.HasProtectedAndFriend Then
                    ' We could still have a "Friend" later
                    recommendations.Add(New RecommendedKeyword("Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_the_assembly_that_contains_their_declaration))
                ElseIf modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.FriendKeyword AndAlso Not modifierFacts.HasProtectedAndFriend Then
                    ' We could still have a "Protected" later
                    recommendations.Add(New RecommendedKeyword("Protected", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_own_class_or_from_a_derived_class))
                End If
            End If

            ' Show "Partial" at the module level. Recommending it before "Private"
            ' is fine, because we'll prettylist it.
            If innermostDeclarationKind = SyntaxKind.ClassBlock OrElse
               innermostDeclarationKind = SyntaxKind.ModuleBlock OrElse
               innermostDeclarationKind = SyntaxKind.StructureBlock OrElse
               innermostDeclarationKind = SyntaxKind.NamespaceBlock Then

                If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class) AndAlso
                    modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword AndAlso
                    modifierFacts.AsyncKeyword.Kind = SyntaxKind.None AndAlso
                    modifierFacts.IteratorKeyword.Kind = SyntaxKind.None Then

                    recommendations.Add(New RecommendedKeyword("Partial", VBFeaturesResources.Indicates_that_a_method_class_or_structure_declaration_is_a_partial_definition_of_the_method_class_or_structure))
                End If
            End If

            If modifierFacts.AsyncKeyword.Kind = SyntaxKind.None AndAlso
               modifierFacts.IteratorKeyword.Kind = SyntaxKind.None Then

                If modifierFacts.MutabilityOrWithEventsKeyword.Kind = SyntaxKind.None Then
                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field) Then
                        recommendations.Add(New RecommendedKeyword("Const", VBFeaturesResources.Declares_and_defines_one_or_more_constants))
                        recommendations.Add(New RecommendedKeyword("WithEvents", VBFeaturesResources.Specifies_that_one_or_more_declared_member_variables_refer_to_an_instance_of_a_class_that_can_raise_events))
                    End If

                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) OrElse modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field) Then
                        recommendations.Add(New RecommendedKeyword("ReadOnly", VBFeaturesResources.Specifies_that_a_variable_or_property_can_be_read_but_not_written_to))
                    End If

                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) Then
                        recommendations.Add(New RecommendedKeyword("WriteOnly", VBFeaturesResources.Specifies_that_a_property_can_be_written_to_but_not_read))
                    End If
                End If

                ' Some modifiers cannot appear at the module level
                If innermostDeclarationKind = SyntaxKind.ClassBlock OrElse
                   innermostDeclarationKind = SyntaxKind.InterfaceBlock OrElse
                   innermostDeclarationKind = SyntaxKind.StructureBlock OrElse
                   innermostDeclarationKind = SyntaxKind.NamespaceBlock Then

                    If modifierFacts.InheritenceKeyword.Kind = SyntaxKind.None AndAlso modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class) Then
                        recommendations.Add(New RecommendedKeyword("MustInherit", VBFeaturesResources.Specifies_that_a_class_can_be_used_only_as_a_base_class_and_that_you_cannot_create_an_object_directly_from_it))
                        recommendations.Add(New RecommendedKeyword("NotInheritable", VBFeaturesResources.Specifies_that_a_class_cannot_be_used_as_a_base_class))
                    End If

                    If modifierFacts.OverridableSharedOrPartialKeyword.Kind = SyntaxKind.None Then
                        If Not context.IsInterfaceMemberDeclarationKeywordContext AndAlso
                           modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.OverridesKeyword Then
                            recommendations.Add(New RecommendedKeyword("Shared", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_associated_with_all_instances_of_a_class_or_structure))
                        End If

                        If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.OverridableMethod) Then
                            recommendations.Add(New RecommendedKeyword("MustOverride", VBFeaturesResources.Specifies_that_a_property_or_procedure_is_not_implemented_in_the_class_and_must_be_overridden_in_a_derived_class_before_it_can_be_used))

                            If modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.ShadowsKeyword Then
                                recommendations.Add(New RecommendedKeyword("NotOverridable", VBFeaturesResources.Specifies_that_a_property_or_procedure_cannot_be_overridden_in_a_derived_class))
                            End If

                            If modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.OverridesKeyword Then
                                recommendations.Add(New RecommendedKeyword("Overridable", VBFeaturesResources.Specifies_that_a_property_or_procedure_can_be_overridden_by_an_identically_named_property_or_procedure_in_a_derived_class))
                            End If
                        End If
                    End If

                    If modifierFacts.OverridesOrShadowsKeyword.Kind = SyntaxKind.None Then
                        If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.OverridableKeyword AndAlso
                           modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Property) AndAlso
                           Not context.IsInterfaceMemberDeclarationKeywordContext AndAlso
                           modifierFacts.SharedKeyword.Kind = SyntaxKind.None AndAlso
                           modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                            recommendations.Add(New RecommendedKeyword("Overrides", VBFeaturesResources.Specifies_that_a_property_or_procedure_overrides_an_identically_named_property_or_procedure_inherited_from_a_base_class))
                        End If

                        If modifierFacts.OverloadsKeyword.Kind = SyntaxKind.None AndAlso
                           modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.NotOverridableKeyword AndAlso
                           modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Property Or PossibleDeclarationTypes.Operator) Then
                            recommendations.Add(New RecommendedKeyword("Shadows", VBFeaturesResources.Specifies_that_a_declared_programming_element_redeclares_and_hides_an_identically_named_element_in_a_base_class))
                        End If
                    End If

                    If modifierFacts.OverloadsKeyword.Kind = SyntaxKind.None AndAlso
                       modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.ShadowsKeyword AndAlso
                       modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property Or PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Operator) Then
                        recommendations.Add(New RecommendedKeyword("Overloads", VBFeaturesResources.Specifies_that_a_property_or_procedure_re_declares_one_or_more_existing_properties_or_procedures_with_the_same_name))
                    End If

                    If modifierFacts.DefaultKeyword.Kind = SyntaxKind.None AndAlso
                       modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) AndAlso
                       modifierFacts.AccessibilityKeyword.Kind <> SyntaxKind.PrivateKeyword Then
                        recommendations.Add(New RecommendedKeyword("Default", VBFeaturesResources.Identifies_a_property_as_the_default_property_of_its_class_structure_or_interface))
                    End If

                    If modifierFacts.NarrowingOrWideningKeyword.Kind = SyntaxKind.None AndAlso modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Operator) Then
                        recommendations.Add(New RecommendedKeyword("Narrowing", VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_might_not_be_able_to_hold_some_of_the_possible_values_of_the_original_class_or_structure))
                        recommendations.Add(New RecommendedKeyword("Widening", VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_can_hold_all_possible_values_of_the_original_class_or_structure))
                    End If
                End If
            End If

            Return recommendations.ToImmutableArray()
        End Function
    End Class
End Namespace

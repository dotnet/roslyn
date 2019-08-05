// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    // TODO (tomat): cleanup and localize

    internal static class RudeEditDiagnosticDescriptors
    {
        private static readonly ImmutableDictionary<RudeEditKind, DiagnosticDescriptor> s_descriptors = new List<KeyValuePair<RudeEditKind, DiagnosticDescriptor>>
        {
            { GetDescriptorPair(RudeEditKind.InsertAroundActiveStatement,               FeaturesResources.Adding_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeleteAroundActiveStatement,               FeaturesResources.Deleting_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeleteActiveStatement,                     FeaturesResources.An_active_statement_has_been_removed_from_its_original_method_You_must_revert_your_changes_to_continue_or_restart_the_debugging_session) },
            { GetDescriptorPair(RudeEditKind.UpdateAroundActiveStatement,               FeaturesResources.Updating_a_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdateExceptionHandlerOfActiveTry,         FeaturesResources.Modifying_a_catch_finally_handler_with_an_active_statement_in_the_try_block_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdateTryOrCatchWithActiveFinally,         FeaturesResources.Modifying_a_try_catch_finally_statement_when_the_finally_block_is_active_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdateCatchHandlerAroundActiveStatement,   FeaturesResources.Modifying_a_catch_handler_around_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.Update,                                    FeaturesResources.Updating_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ModifiersUpdate,                           FeaturesResources.Updating_the_modifiers_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.VarianceUpdate,                            FeaturesResources.Updating_the_variance_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.TypeUpdate,                                FeaturesResources.Updating_the_type_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InitializerUpdate,                         FeaturesResources.Updating_the_initializer_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.FixedSizeFieldUpdate,                      FeaturesResources.Updating_the_size_of_a_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.EnumUnderlyingTypeUpdate,                  FeaturesResources.Updating_the_underlying_type_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.BaseTypeOrInterfaceUpdate,                 FeaturesResources.Updating_the_base_class_and_or_base_interface_s_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.TypeKindUpdate,                            FeaturesResources.Updating_the_kind_of_a_type_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.AccessorKindUpdate,                        FeaturesResources.Updating_the_kind_of_an_property_event_accessor_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.MethodKindUpdate,                          FeaturesResources.Updating_the_kind_of_a_method_Sub_Function_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeclareAliasUpdate,                        FeaturesResources.Updating_the_alias_of_Declare_Statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeclareLibraryUpdate,                      FeaturesResources.Updating_the_library_name_of_Declare_Statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.FieldKindUpdate,                           FeaturesResources.Updating_a_field_to_an_event_or_vice_versa_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.Renamed,                                   FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.Insert,                                    FeaturesResources.Adding_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertVirtual,                             FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertOverridable,                         FeaturesResources.Adding_a_MustOverride_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertExtern,                              FeaturesResources.Adding_an_extern_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertDllImport,                           FeaturesResources.Adding_an_imported_method_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertOperator,                            FeaturesResources.Adding_a_user_defined_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertIntoStruct,                          FeaturesResources.Adding_0_into_a_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertIntoClassWithLayout,                 FeaturesResources.Adding_0_into_a_class_with_explicit_or_sequential_layout_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertGenericMethod,                       FeaturesResources.Adding_a_generic_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.Move,                                      FeaturesResources.Moving_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.Delete,                                    FeaturesResources.Deleting_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.MethodBodyAdd,                             FeaturesResources.Adding_a_method_body_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.MethodBodyDelete,                          FeaturesResources.Deleting_a_method_body_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.GenericMethodUpdate,                       FeaturesResources.Modifying_a_generic_method_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.GenericMethodTriviaUpdate,                 FeaturesResources.Modifying_whitespace_or_comments_in_a_generic_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.GenericTypeUpdate,                         FeaturesResources.Modifying_a_method_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.GenericTypeTriviaUpdate,                   FeaturesResources.Modifying_whitespace_or_comments_in_0_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.GenericTypeInitializerUpdate,              FeaturesResources.Modifying_the_initializer_of_0_in_a_generic_type_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.PartialTypeInitializerUpdate,              FeaturesResources.Modifying_the_initializer_of_0_in_a_partial_type_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas,  FeaturesResources.Adding_a_constructor_to_a_type_with_a_field_or_property_initializer_that_contains_an_anonymous_function_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.RenamingCapturedVariable,                  FeaturesResources.Renaming_a_captured_variable_from_0_to_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.StackAllocUpdate,                          FeaturesResources.Modifying_0_which_contains_the_stackalloc_operator_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ExperimentalFeaturesEnabled,               FeaturesResources.Modifying_source_with_experimental_language_features_enabled_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.AwaitStatementUpdate,                      FeaturesResources.Updating_a_complex_statement_containing_an_await_expression_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingConstructorVisibility,             FeaturesResources.Changing_visibility_of_a_constructor_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.CapturingVariable,                         FeaturesResources.Capturing_variable_0_that_hasn_t_been_captured_before_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.NotCapturingVariable,                      FeaturesResources.Ceasing_to_capture_variable_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeletingCapturedVariable,                  FeaturesResources.Deleting_captured_variable_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingCapturedVariableType,              FeaturesResources.Changing_the_type_of_a_captured_variable_0_previously_of_type_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingCapturedVariableScope,             FeaturesResources.Changing_the_declaration_scope_of_a_captured_variable_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingLambdaParameters,                  FeaturesResources.Changing_the_parameters_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingLambdaReturnType,                  FeaturesResources.Changing_the_return_type_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingQueryLambdaType,                   FeaturesResources.Changing_the_type_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.AccessingCapturedVariableInLambda,         FeaturesResources.Accessing_captured_variable_0_that_hasn_t_been_accessed_before_in_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.NotAccessingCapturedVariableInLambda,      FeaturesResources.Ceasing_to_access_captured_variable_0_in_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertLambdaWithMultiScopeCapture,         FeaturesResources.Adding_0_that_accesses_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.DeleteLambdaWithMultiScopeCapture,         FeaturesResources.Removing_0_that_accessed_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementUpdate,                     FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementLambdaRemoved,              FeaturesResources.Removing_0_that_contains_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            // TODO: change the error message to better explain what's going on
            { GetDescriptorPair(RudeEditKind.PartiallyExecutedActiveStatementUpdate,    FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.PartiallyExecutedActiveStatementDelete,    FeaturesResources.An_active_statement_has_been_removed_from_its_original_method_You_must_revert_your_changes_to_continue_or_restart_the_debugging_session) },
            { GetDescriptorPair(RudeEditKind.InsertFile,                                FeaturesResources.Adding_a_new_file_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, FeaturesResources.Updating_async_or_iterator_modifier_around_an_active_statement_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdatingStateMachineMethodMissingAttribute, FeaturesResources.Attribute_0_is_missing_Updating_an_async_method_or_an_iterator_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.SwitchBetweenLambdaAndLocalFunction, FeaturesResources.Switching_between_lambda_and_local_function_will_prevent_the_debug_session_from_continuing ) },
            { GetDescriptorPair(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, FeaturesResources.Adding_method_with_explicit_interface_specifier_will_prevernt_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertIntoInterface,                       FeaturesResources.Adding_0_into_an_interface_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod,    FeaturesResources.Adding_0_into_an_interface_method_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InternalError,                             FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_due_to_internal_error) },
            { GetDescriptorPair(RudeEditKind.SwitchExpressionUpdate,                    FeaturesResources.Modifying_0_which_contains_a_switch_expression_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingFromAsynchronousToSynchronous,     FeaturesResources.Changing_0_from_asynchronous_to_synchronous_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ChangingStateMachineShape,                 FeaturesResources.Changing_0_to_1_will_prevent_the_debug_session_from_continuing_because_it_changes_the_shape_of_the_state_machine) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_COMPLEX_QUERY_EXPRESSION,        FeaturesResources.Modifying_0_which_contains_an_Aggregate_Group_By_or_Join_query_clauses_will_prevent_the_debug_session_from_continuing) },

            // VB specific,
            { GetDescriptorPair(RudeEditKind.HandlesClauseUpdate,                       FeaturesResources.Updating_the_Handles_clause_of_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ImplementsClauseUpdate,                    FeaturesResources.Updating_the_Implements_clause_of_a_0_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.ConstraintKindUpdate,                      FeaturesResources.Changing_the_constraint_from_0_to_1_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.InsertHandlesClause,                       FeaturesResources.Adding_0_with_the_Handles_clause_will_prevent_the_debug_session_from_continuing) },
            { GetDescriptorPair(RudeEditKind.UpdateStaticLocal,                         FeaturesResources.Modifying_0_which_contains_a_static_variable_will_prevent_the_debug_session_from_continuing) },
        }.ToImmutableDictionary();

        private static KeyValuePair<RudeEditKind, DiagnosticDescriptor> GetDescriptorPair(RudeEditKind kind, string message)
        {
            return new KeyValuePair<RudeEditKind, DiagnosticDescriptor>(kind,
                                                                        new DiagnosticDescriptor(id: "ENC" + ((int)kind).ToString("0000"),
                                                                            title: message, // TODO: come up with real titles.
                                                                            messageFormat: message,
                                                                            category: DiagnosticCategory.EditAndContinue,
                                                                            defaultSeverity: DiagnosticSeverity.Error,
                                                                            isEnabledByDefault: true,
                                                                            customTags: DiagnosticCustomTags.EditAndContinue));
        }

        internal static ImmutableArray<DiagnosticDescriptor> AllDescriptors
        {
            get
            {
                return s_descriptors.Values.ToImmutableArray();
            }
        }

        internal static DiagnosticDescriptor GetDescriptor(RudeEditKind kind)
        {
            return s_descriptors[kind];
        }
    }
}

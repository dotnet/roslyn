// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class EditAndContinueDiagnosticDescriptors
{
    private const int GeneralDiagnosticBaseId = 1000;
    private const int ModuleDiagnosticBaseId = 2000;
    private static readonly int s_diagnosticBaseIndex;

    private static readonly LocalizableResourceString s_rudeEditLocString;
    private static readonly LocalizableResourceString s_encLocString;
    private static readonly LocalizableResourceString s_encDisallowedByProjectLocString;

    private static readonly ImmutableArray<DiagnosticDescriptor> s_descriptors;

    // descriptors for diagnostics reported by the debugger:
    private static Dictionary<ManagedHotReloadAvailabilityStatus, DiagnosticDescriptor> s_lazyModuleDiagnosticDescriptors;
    private static readonly object s_moduleDiagnosticDescriptorsGuard;

    static EditAndContinueDiagnosticDescriptors()
    {
        s_moduleDiagnosticDescriptorsGuard = new object();

        s_rudeEditLocString = new LocalizableResourceString(nameof(FeaturesResources.RudeEdit), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        s_encLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        s_encDisallowedByProjectLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinueDisallowedByProject), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

        void add(int index, int id, string resourceName, LocalizableResourceString title, DiagnosticSeverity severity)
        {
            if (index >= builder.Count)
            {
                builder.Count = index + 1;
            }

            builder[index] = new DiagnosticDescriptor(
                $"ENC{id:D4}",
                title,
                messageFormat: new LocalizableResourceString(resourceName, FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                DiagnosticCategory.EditAndContinue,
                severity,
                isEnabledByDefault: true,
                customTags: DiagnosticCustomTags.EditAndContinue);
        }

        void AddRudeEdit(RudeEditKind kind, string resourceName, DiagnosticSeverity severity = DiagnosticSeverity.Error)
            => add(GetDescriptorIndex(kind), (int)kind, resourceName, s_rudeEditLocString, severity);

        void AddGeneralDiagnostic(EditAndContinueErrorCode code, string resourceName, DiagnosticSeverity severity = DiagnosticSeverity.Error)
            => add(GetDescriptorIndex(code), GeneralDiagnosticBaseId + (int)code, resourceName, s_encLocString, severity);

        //
        // rude edits
        //

        AddRudeEdit(RudeEditKind.InsertAroundActiveStatement, nameof(FeaturesResources.Adding_0_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.DeleteAroundActiveStatement, nameof(FeaturesResources.Deleting_0_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.DeleteActiveStatement, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateAroundActiveStatement, nameof(FeaturesResources.Updating_a_0_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateExceptionHandlerOfActiveTry, nameof(FeaturesResources.Modifying_a_catch_finally_handler_with_an_active_statement_in_the_try_block_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateTryOrCatchWithActiveFinally, nameof(FeaturesResources.Modifying_a_try_catch_finally_statement_when_the_finally_block_is_active_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateCatchHandlerAroundActiveStatement, nameof(FeaturesResources.Modifying_a_catch_handler_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.Update, nameof(FeaturesResources.Updating_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ModifiersUpdate, nameof(FeaturesResources.Updating_the_modifiers_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.VarianceUpdate, nameof(FeaturesResources.Updating_the_variance_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.TypeUpdate, nameof(FeaturesResources.Updating_the_type_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InitializerUpdate, nameof(FeaturesResources.Updating_the_initializer_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.FixedSizeFieldUpdate, nameof(FeaturesResources.Updating_the_size_of_a_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.EnumUnderlyingTypeUpdate, nameof(FeaturesResources.Updating_the_underlying_type_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.BaseTypeOrInterfaceUpdate, nameof(FeaturesResources.Updating_the_base_class_and_or_base_interface_s_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.TypeKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_a_type_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.AccessorKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_a_property_event_accessor_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.DeclareAliasUpdate, nameof(FeaturesResources.Updating_the_alias_of_Declare_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.DeclareLibraryUpdate, nameof(FeaturesResources.Updating_the_library_name_of_Declare_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.FieldKindUpdate, nameof(FeaturesResources.Changing_a_field_to_an_event_or_vice_versa_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.Renamed, nameof(FeaturesResources.Renaming_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.Insert, nameof(FeaturesResources.Adding_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertVirtual, nameof(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertOverridable, nameof(FeaturesResources.Adding_a_MustOverride_0_or_overriding_an_inherited_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertExtern, nameof(FeaturesResources.Adding_an_extern_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertDllImport, nameof(FeaturesResources.Adding_an_imported_method_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertOperator, nameof(FeaturesResources.Adding_a_user_defined_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertOrMoveStructMember, nameof(FeaturesResources.Adding_or_moving_0_of_1_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertOrMoveTypeWithLayoutMember, nameof(FeaturesResources.Adding_or_moving_0_of_1_with_explicit_or_sequential_layout_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.Move, nameof(FeaturesResources.Moving_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.Delete, nameof(FeaturesResources.Deleting_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.GenericMethodUpdate, nameof(FeaturesResources.Modifying_a_generic_method_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.GenericTypeUpdate, nameof(FeaturesResources.Modifying_a_method_inside_the_context_of_a_generic_type_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, nameof(FeaturesResources.Adding_a_constructor_to_a_type_with_a_field_or_property_initializer_that_contains_an_anonymous_function_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.RenamingCapturedVariable, nameof(FeaturesResources.Renaming_a_captured_variable_from_0_to_1_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.StackAllocUpdate, nameof(FeaturesResources.Modifying_0_which_contains_the_stackalloc_operator_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ExperimentalFeaturesEnabled, nameof(FeaturesResources.Modifying_source_with_experimental_language_features_enabled_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.AwaitStatementUpdate, nameof(FeaturesResources.Updating_a_complex_statement_containing_an_await_expression_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingAccessibility, nameof(FeaturesResources.Changing_visibility_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingCapturedVariableType, nameof(FeaturesResources.Changing_the_type_of_a_captured_variable_0_previously_of_type_1_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingCapturedVariableScope, nameof(FeaturesResources.Changing_the_declaration_scope_of_a_captured_variable_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingLambdaParameters, nameof(FeaturesResources.Changing_the_parameters_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingLambdaReturnType, nameof(FeaturesResources.Changing_the_return_type_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingQueryLambdaType, nameof(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.ActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ActiveStatementLambdaRemoved, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementDelete, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertFile, nameof(FeaturesResources.Adding_a_new_file_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, nameof(FeaturesResources.Updating_async_or_iterator_modifier_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdatingStateMachineMethodMissingAttribute, nameof(FeaturesResources.Attribute_0_is_missing_Updating_an_async_method_or_an_iterator_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.SwitchBetweenLambdaAndLocalFunction, nameof(FeaturesResources.Switching_between_lambda_and_local_function_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, nameof(FeaturesResources.Adding_a_method_with_an_explicit_interface_specifier_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertIntoInterface, nameof(FeaturesResources.Adding_0_into_an_interface_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod, nameof(FeaturesResources.Adding_0_into_an_interface_method_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InternalError, nameof(FeaturesResources.Modifying_source_file_0_requires_restarting_the_application_due_to_internal_error_1));
        AddRudeEdit(RudeEditKind.ChangingFromAsynchronousToSynchronous, nameof(FeaturesResources.Changing_0_from_asynchronous_to_synchronous_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingStateMachineShape, nameof(FeaturesResources.Changing_0_to_1_requires_restarting_the_application_because_it_changes_the_shape_of_the_state_machine));
        AddRudeEdit(RudeEditKind.ComplexQueryExpression, nameof(FeaturesResources.Modifying_0_which_contains_an_Aggregate_Group_By_or_Join_query_clauses_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.MemberBodyInternalError, nameof(FeaturesResources.Modifying_body_of_0_requires_restarting_the_application_due_to_internal_error_1));
        AddRudeEdit(RudeEditKind.MemberBodyTooBig, nameof(FeaturesResources.Modifying_body_of_0_requires_restarting_the_application_because_the_body_has_too_many_statements));
        AddRudeEdit(RudeEditKind.SourceFileTooBig, nameof(FeaturesResources.Modifying_source_file_0_requires_restarting_the_application_because_the_file_is_too_big));
        AddRudeEdit(RudeEditKind.NotSupportedByRuntime, nameof(FeaturesResources.Applying_source_changes_while_the_application_is_running_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.MakeMethodAsyncNotSupportedByRuntime, nameof(FeaturesResources.Making_a_method_asynchronous_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.MakeMethodIteratorNotSupportedByRuntime, nameof(FeaturesResources.Making_a_method_an_iterator_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.InsertNotSupportedByRuntime, nameof(FeaturesResources.Adding_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingAttributesNotSupportedByRuntime, nameof(FeaturesResources.Updating_the_attributes_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, nameof(FeaturesResources.Updating_reloadable_type_marked_by_0_attribute_or_its_member_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.ChangingParameterTypes, nameof(FeaturesResources.Changing_parameter_types_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingTypeParameters, nameof(FeaturesResources.Changing_type_parameters_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingConstraints, nameof(FeaturesResources.Changing_constraints_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangeImplicitMainReturnType, nameof(FeaturesResources.An_update_that_causes_the_return_type_of_implicit_main_to_change_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.RenamingNotSupportedByRuntime, nameof(FeaturesResources.Renaming_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.ChangingNonCustomAttribute, nameof(FeaturesResources.Changing_pseudo_custom_attribute_0_of_1_requires_restarting_th_application));
        AddRudeEdit(RudeEditKind.ChangingNamespace, nameof(FeaturesResources.Changing_the_containing_namespace_of_0_from_1_to_2_requires_restarting_th_application));
        AddRudeEdit(RudeEditKind.ChangingSignatureNotSupportedByRuntime, nameof(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.DeleteNotSupportedByRuntime, nameof(FeaturesResources.Deleting_0_requires_restarting_the_application_because_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, nameof(FeaturesResources.Updating_async_or_iterator_requires_restarting_the_application_because_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.UpdatingGenericNotSupportedByRuntime, nameof(FeaturesResources.Updating_0_within_generic_type_or_method_requires_restarting_the_application_because_is_not_supported_by_the_runtime));
        AddRudeEdit(RudeEditKind.CapturingPrimaryConstructorParameter, nameof(FeaturesResources.Capturing_primary_constructor_parameter_0_that_hasn_t_been_captured_before_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.NotCapturingPrimaryConstructorParameter, nameof(FeaturesResources.Ceasing_to_capture_primary_constructor_parameter_0_of_1_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingAttribute, nameof(FeaturesResources.Changing_attribute_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ChangingNameOrSignatureOfActiveMember, nameof(FeaturesResources.Changing_name_or_signature_of_0_that_contains_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateMightNotHaveAnyEffect, nameof(FeaturesResources.Changing_0_might_not_have_any_effect_until_the_application_is_restarted), DiagnosticSeverity.Warning);
        AddRudeEdit(RudeEditKind.TypeUpdateAroundActiveStatement, nameof(FeaturesResources.Updating_a_0_around_an_active_statement_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertOrMoveComInterfaceMember, nameof(FeaturesResources.Adding_or_moving_0_of_a_COM_interface_requires_restarting_the_application));

        // VB specific
        AddRudeEdit(RudeEditKind.HandlesClauseUpdate, nameof(FeaturesResources.Updating_the_Handles_clause_of_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.ImplementsClauseUpdate, nameof(FeaturesResources.Updating_the_Implements_clause_of_a_0_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.InsertHandlesClause, nameof(FeaturesResources.Adding_0_with_the_Handles_clause_requires_restarting_the_application));
        AddRudeEdit(RudeEditKind.UpdateStaticLocal, nameof(FeaturesResources.Modifying_0_which_contains_a_static_variable_requires_restarting_the_application));

        //
        // other Roslyn reported diagnostics:
        //

        s_diagnosticBaseIndex = builder.Count;

        AddGeneralDiagnostic(EditAndContinueErrorCode.ErrorReadingFile, nameof(FeaturesResources.ErrorReadingFile));
        AddGeneralDiagnostic(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError, nameof(FeaturesResources.CannotApplyChangesUnexpectedError));
        AddGeneralDiagnostic(EditAndContinueErrorCode.ChangesDisallowedWhileStoppedAtException, nameof(FeaturesResources.ChangesDisallowedWhileStoppedAtException));
        AddGeneralDiagnostic(EditAndContinueErrorCode.DocumentIsOutOfSyncWithDebuggee, nameof(FeaturesResources.DocumentIsOutOfSyncWithDebuggee), DiagnosticSeverity.Warning);
        AddGeneralDiagnostic(EditAndContinueErrorCode.UnableToReadSourceFileOrPdb, nameof(FeaturesResources.UnableToReadSourceFileOrPdb), DiagnosticSeverity.Warning);
        AddGeneralDiagnostic(EditAndContinueErrorCode.AddingTypeRuntimeCapabilityRequired, nameof(FeaturesResources.ChangesRequiredSynthesizedType));

        s_descriptors = builder.ToImmutable();
    }

    internal static ImmutableArray<DiagnosticDescriptor> GetDescriptors()
        => s_descriptors.WhereAsArray(d => d != null);

    internal static DiagnosticDescriptor GetDescriptor(RudeEditKind kind)
        => s_descriptors[GetDescriptorIndex(kind)];

    internal static DiagnosticDescriptor GetDescriptor(EditAndContinueErrorCode errorCode)
        => s_descriptors[GetDescriptorIndex(errorCode)];

    internal static DiagnosticDescriptor GetModuleDiagnosticDescriptor(ManagedHotReloadAvailabilityStatus status)
    {
        lock (s_moduleDiagnosticDescriptorsGuard)
        {
            s_lazyModuleDiagnosticDescriptors ??= [];

            if (!s_lazyModuleDiagnosticDescriptors.TryGetValue(status, out var descriptor))
            {
                s_lazyModuleDiagnosticDescriptors.Add(status, descriptor = new DiagnosticDescriptor(
                    $"ENC{ModuleDiagnosticBaseId + (int)status:D4}",
                    s_encLocString,
                    s_encDisallowedByProjectLocString,
                    DiagnosticCategory.EditAndContinue,
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    customTags: DiagnosticCustomTags.EditAndContinue));
            }

            return descriptor;
        }
    }

    private static int GetDescriptorIndex(RudeEditKind kind)
        => (int)kind;

    private static int GetDescriptorIndex(EditAndContinueErrorCode errorCode)
        => s_diagnosticBaseIndex + (int)errorCode;
}

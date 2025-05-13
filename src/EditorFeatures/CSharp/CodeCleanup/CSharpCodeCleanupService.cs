// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup;

[ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCodeCleanupService(ICodeFixService codeFixService)
    : AbstractCodeCleanupService(codeFixService)
{
    /// <summary>
    /// Maps format document code cleanup options to DiagnosticId[]
    /// </summary>
    private static readonly ImmutableArray<DiagnosticSet> s_diagnosticSets =
        [
            new DiagnosticSet(FeaturesResources.Apply_using_directive_placement_preferences,
                IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_file_header_preferences,
                IDEDiagnosticIds.FileHeaderMismatch),
            new DiagnosticSet(AnalyzersResources.Add_this_or_Me_qualification,
                IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId,
                IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_language_framework_type_preferences,
                IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_parentheses_preferences,
                IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
                IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId),
            new DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                IDEDiagnosticIds.AddOrRemoveAccessibilityModifiersDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_coalesce_expression_preferences,
                IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_object_collection_initialization_preferences,
                IDEDiagnosticIds.UseCollectionInitializerDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_tuple_name_preferences,
                IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_namespace_matches_folder_preferences,
                IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_null_propagation_preferences,
                IDEDiagnosticIds.UseNullPropagationDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_object_initializer_preferences,
                IDEDiagnosticIds.UseObjectInitializerDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_auto_property_preferences,
                IDEDiagnosticIds.UseAutoPropertyDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_compound_assignment_preferences,
                IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId,
                IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_conditional_expression_preferences,
                // dotnet_style_prefer_conditional_expression_over_assignment
                IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                // dotnet_style_prefer_conditional_expression_over_return
                IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_inferred_anonymous_type_member_names_preferences,
                IDEDiagnosticIds.UseInferredMemberNameDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_null_checking_preferences,
                IDEDiagnosticIds.UseIsNullCheckDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_simplify_boolean_expression_preferences,
                IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_string_interpolation_preferences,
                IDEDiagnosticIds.SimplifyInterpolationId),
            new DiagnosticSet(CSharpFeaturesResources.Make_private_field_readonly_when_possible,
                IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId),
            new DiagnosticSet(FeaturesResources.Remove_unused_parameters,
                IDEDiagnosticIds.UnusedParameterDiagnosticId),
            new DiagnosticSet(FeaturesResources.Remove_unused_suppressions,
                IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_blank_line_preferences_experimental,
                IDEDiagnosticIds.MultipleBlankLinesDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_statement_after_block_preferences_experimental,
                IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_var_preferences,
                IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                IDEDiagnosticIds.UseExplicitTypeDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_expression_block_body_preferences,
                // csharp_style_expression_bodied_accessors
                IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                // csharp_style_expression_bodied_constructors
                IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                // csharp_style_expression_bodied_indexers
                IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                // csharp_style_expression_bodied_lambdas
                IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId,
                // csharp_style_expression_bodied_local_functions
                IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId,
                // csharp_style_expression_bodied_methods
                IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                // csharp_style_expression_bodied_operators
                IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                // csharp_style_expression_bodied_properties
                IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_pattern_matching_preferences,
                // csharp_style_pattern_matching_over_as_with_null_check
                IDEDiagnosticIds.InlineAsTypeCheckId,
                // csharp_style_pattern_matching_over_is_with_cast_check
                IDEDiagnosticIds.InlineIsTypeCheckId,
                // csharp_style_prefer_extended_property_pattern
                IDEDiagnosticIds.SimplifyPropertyPatternDiagnosticId,
                // csharp_style_prefer_not_pattern
                IDEDiagnosticIds.UseNotPatternDiagnosticId,
                // csharp_style_prefer_pattern_matching
                IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId,
                // csharp_style_prefer_switch_expression
                IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId,
                // csharp_style_prefer_null_check_over_type_check
                IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_conditional_delegate_call_preferences,
                IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_static_local_function_preferences,
                IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId),
            new DiagnosticSet(FeaturesResources.Sort_accessibility_modifiers,
                IDEDiagnosticIds.OrderModifiersDiagnosticId,
                "CS0267"),
            new DiagnosticSet(CSharpFeaturesResources.Apply_readonly_struct_preferences,
                IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Add_required_braces_for_single_line_control_statements,
                IDEDiagnosticIds.AddBracesDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_using_statement_preferences,
                IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_namespace_preferences,
                IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_method_group_conversion_preferences,
                IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_default_T_preferences,
                IDEDiagnosticIds.UseDefaultLiteralDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_deconstruct_preferences,
                // csharp_style_deconstructed_variable_declaration
                IDEDiagnosticIds.UseDeconstructionDiagnosticId,
                // csharp_style_prefer_tuple_swap
                IDEDiagnosticIds.UseTupleSwapDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_new_preferences,
                IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_inline_out_variable_preferences,
                IDEDiagnosticIds.InlineDeclarationDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_range_preferences,
                // csharp_style_prefer_index_operator
                IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                // csharp_style_prefer_range_operator
                IDEDiagnosticIds.UseRangeOperatorDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_local_over_anonymous_function_preferences,
                IDEDiagnosticIds.UseLocalFunctionDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_throw_expression_preferences,
                IDEDiagnosticIds.UseThrowExpressionDiagnosticId),
            new DiagnosticSet(FeaturesResources.Apply_unused_value_preferences,
                IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_blank_line_after_colon_in_constructor_initializer_preferences_experimental,
                IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_blank_lines_between_consecutive_braces_preferences_experimental,
                IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId),
            new DiagnosticSet(CSharpFeaturesResources.Apply_embedded_statements_on_same_line_preferences_experimental,
                IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId),
            new DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId),
            new DiagnosticSet(FeaturesResources.Remove_unused_variables,
                CSharpRemoveUnusedVariableCodeFixProvider.CS0168,
                CSharpRemoveUnusedVariableCodeFixProvider.CS0219),
            new DiagnosticSet(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive,
                IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
                IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId)
,
        ];

    protected override string OrganizeImportsDescription
        => CSharpFeaturesResources.Organize_Usings;

    protected override ImmutableArray<DiagnosticSet> GetDiagnosticSets()
        => s_diagnosticSets;
}

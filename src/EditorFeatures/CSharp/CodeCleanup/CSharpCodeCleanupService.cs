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

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    [ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class CSharpCodeCleanupService(ICodeFixService codeFixService, IDiagnosticAnalyzerService diagnosticAnalyzerService) : AbstractCodeCleanupService(codeFixService, diagnosticAnalyzerService)
    {
        /// <summary>
        /// Maps format document code cleanup options to DiagnosticId[]
        /// </summary>
        private static readonly ImmutableArray<DiagnosticSet> s_diagnosticSets =
            ImmutableArray.Create(
                // Organize usings
                //   dotnet_separate_import_directive_groups
                //   dotnet_sort_system_directives_first
                new DiagnosticSet(FeaturesResources.Apply_using_directive_placement_preferences,
                    IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId),
                //   file_header_template
                new DiagnosticSet(FeaturesResources.Apply_file_header_preferences,
                    IDEDiagnosticIds.FileHeaderMismatch),

                // this. preferences
                //   dotnet_style_qualification_for_event
                //   dotnet_style_qualification_for_field
                //   dotnet_style_qualification_for_method
                //   dotnet_style_qualification_for_property
                new DiagnosticSet(AnalyzersResources.Add_this_or_Me_qualification,
                    IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId,
                    IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId),

                // Language keywords vs BCL types preferences
                //   dotnet_style_predefined_type_for_locals_parameters_members
                //   dotnet_style_predefined_type_for_member_access
                new DiagnosticSet(FeaturesResources.Apply_language_framework_type_preferences,
                    IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId),

                // Parentheses preferences
                //   dotnet_style_parentheses_in_arithmetic_binary_operators
                //   dotnet_style_parentheses_in_other_binary_operators
                //   dotnet_style_parentheses_in_other_operators
                //   dotnet_style_parentheses_in_relational_binary_operators
                new DiagnosticSet(FeaturesResources.Apply_parentheses_preferences,
                    IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
                    IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId),

                // Modifier preferences
                //   dotnet_style_require_accessibility_modifiers
                new DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                    IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId),

                // Expression-level preferences
                //   dotnet_style_coalesce_expression
                new DiagnosticSet(FeaturesResources.Apply_coalesce_expression_preferences,
                    IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId),
                //   dotnet_style_collection_initializer
                new DiagnosticSet(FeaturesResources.Apply_object_collection_initialization_preferences,
                    IDEDiagnosticIds.UseCollectionInitializerDiagnosticId),
                //   dotnet_style_explicit_tuple_names
                new DiagnosticSet(FeaturesResources.Apply_tuple_name_preferences,
                    IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId),
                //   dotnet_style_namespace_match_folder
                new DiagnosticSet(FeaturesResources.Apply_namespace_matches_folder_preferences,
                    IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId),
                //   dotnet_style_null_propagation
                new DiagnosticSet(FeaturesResources.Apply_null_propagation_preferences,
                    IDEDiagnosticIds.UseNullPropagationDiagnosticId),
                //   dotnet_style_object_initializer
                new DiagnosticSet(FeaturesResources.Apply_object_initializer_preferences,
                    IDEDiagnosticIds.UseObjectInitializerDiagnosticId),
                //   dotnet_style_prefer_auto_properties
                new DiagnosticSet(FeaturesResources.Apply_auto_property_preferences,
                    IDEDiagnosticIds.UseAutoPropertyDiagnosticId),
                //   dotnet_style_prefer_compound_assignment
                new DiagnosticSet(FeaturesResources.Apply_compound_assignment_preferences,
                    IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId,
                    IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId),
                new DiagnosticSet(FeaturesResources.Apply_conditional_expression_preferences,
                    // dotnet_style_prefer_conditional_expression_over_assignment
                    IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                    // dotnet_style_prefer_conditional_expression_over_return
                    IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId),
                //   dotnet_style_prefer_inferred_anonymous_type_member_names
                //   dotnet_style_prefer_inferred_tuple_names
                new DiagnosticSet(FeaturesResources.Apply_inferred_anonymous_type_member_names_preferences,
                    IDEDiagnosticIds.UseInferredMemberNameDiagnosticId),
                //   dotnet_style_prefer_is_null_check_over_reference_equality_method
                new DiagnosticSet(FeaturesResources.Apply_null_checking_preferences,
                    IDEDiagnosticIds.UseIsNullCheckDiagnosticId),
                //   dotnet_style_prefer_simplified_boolean_expressions
                new DiagnosticSet(FeaturesResources.Apply_simplify_boolean_expression_preferences,
                    IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId),
                //   dotnet_style_prefer_simplified_interpolation
                new DiagnosticSet(FeaturesResources.Apply_string_interpolation_preferences,
                    IDEDiagnosticIds.SimplifyInterpolationId),

                // Field preferences
                //   dotnet_style_readonly_field
                new DiagnosticSet(CSharpFeaturesResources.Make_private_field_readonly_when_possible,
                    IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId),

                // Parameter preferences
                //   dotnet_code_quality_unused_parameters
                new DiagnosticSet(FeaturesResources.Remove_unused_parameters,
                    IDEDiagnosticIds.UnusedParameterDiagnosticId),

                // Suppression preferences
                //   dotnet_remove_unnecessary_suppression_exclusions
                new DiagnosticSet(FeaturesResources.Remove_unused_suppressions,
                    IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId),

                // New line preferences
                //   dotnet_style_allow_multiple_blank_lines_experimental
                new DiagnosticSet(FeaturesResources.Apply_blank_line_preferences_experimental,
                    IDEDiagnosticIds.MultipleBlankLinesDiagnosticId),
                //   dotnet_style_allow_statement_immediately_after_block_experimental
                new DiagnosticSet(FeaturesResources.Apply_statement_after_block_preferences_experimental,
                    IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId),

                // C# Coding Conventions

                // var preferences
                //   csharp_style_var_elsewhere
                //   csharp_style_var_for_built_in_types
                //   csharp_style_var_when_type_is_apparent
                new DiagnosticSet(CSharpFeaturesResources.Apply_var_preferences,
                    IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                    IDEDiagnosticIds.UseExplicitTypeDiagnosticId),

                // Expression-bodied members
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

                // Pattern matching preferences
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

                // Null-checking preferences
                //   csharp_style_conditional_delegate_call
                new DiagnosticSet(CSharpFeaturesResources.Apply_conditional_delegate_call_preferences,
                    IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId),

                // Modifier preferences
                //   csharp_prefer_static_local_function
                new DiagnosticSet(CSharpFeaturesResources.Apply_static_local_function_preferences,
                    IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId),
                //   csharp_preferred_modifier_order
                new DiagnosticSet(FeaturesResources.Sort_accessibility_modifiers,
                    IDEDiagnosticIds.OrderModifiersDiagnosticId,
                    "CS0267"),
                new DiagnosticSet(CSharpFeaturesResources.Apply_readonly_struct_preferences,
                    IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId),

                // Code-block preferences
                //   csharp_prefer_braces
                new DiagnosticSet(CSharpFeaturesResources.Add_required_braces_for_single_line_control_statements,
                    IDEDiagnosticIds.AddBracesDiagnosticId),

                //   csharp_prefer_simple_using_statement
                new DiagnosticSet(CSharpFeaturesResources.Apply_using_statement_preferences,
                    IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId),

                //   csharp_style_namespace_declarations
                new DiagnosticSet(CSharpFeaturesResources.Apply_namespace_preferences,
                    IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId),

                //   csharp_style_prefer_method_group_conversion
                new DiagnosticSet(CSharpFeaturesResources.Apply_method_group_conversion_preferences,
                    IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId),

                // Expression-level preferences
                //   csharp_prefer_simple_default_expression
                new DiagnosticSet(CSharpFeaturesResources.Apply_default_T_preferences,
                    IDEDiagnosticIds.UseDefaultLiteralDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_deconstruct_preferences,
                    // csharp_style_deconstructed_variable_declaration
                    IDEDiagnosticIds.UseDeconstructionDiagnosticId,
                    // csharp_style_prefer_tuple_swap
                    IDEDiagnosticIds.UseTupleSwapDiagnosticId),

                //   csharp_style_implicit_object_creation_when_type_is_apparent
                new DiagnosticSet(CSharpFeaturesResources.Apply_new_preferences,
                    IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId),

                //   csharp_style_inlined_variable_declaration
                new DiagnosticSet(CSharpFeaturesResources.Apply_inline_out_variable_preferences,
                    IDEDiagnosticIds.InlineDeclarationDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_range_preferences,
                    // csharp_style_prefer_index_operator
                    IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                    // csharp_style_prefer_range_operator
                    IDEDiagnosticIds.UseRangeOperatorDiagnosticId),

                //   csharp_style_prefer_local_over_anonymous_function
                new DiagnosticSet(CSharpFeaturesResources.Apply_local_over_anonymous_function_preferences,
                    IDEDiagnosticIds.UseLocalFunctionDiagnosticId),

                //   csharp_style_throw_expression
                new DiagnosticSet(CSharpFeaturesResources.Apply_throw_expression_preferences,
                    IDEDiagnosticIds.UseThrowExpressionDiagnosticId),

                //   csharp_style_unused_value_assignment_preference
                //   csharp_style_unused_value_expression_statement_preference
                new DiagnosticSet(FeaturesResources.Apply_unused_value_preferences,
                    IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                    IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId),

                // New line preferences
                //   csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental
                new DiagnosticSet(CSharpFeaturesResources.Apply_blank_line_after_colon_in_constructor_initializer_preferences_experimental,
                    IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId),
                //   csharp_style_allow_blank_lines_between_consecutive_braces_experimental
                new DiagnosticSet(CSharpFeaturesResources.Apply_blank_lines_between_consecutive_braces_preferences_experimental,
                    IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId),
                //   csharp_style_allow_embedded_statements_on_same_line_experimental
                new DiagnosticSet(CSharpFeaturesResources.Apply_embedded_statements_on_same_line_preferences_experimental,
                    IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId),

                // Simplification rules

                new DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                    IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId),

                new DiagnosticSet(FeaturesResources.Remove_unused_variables,
                    CSharpRemoveUnusedVariableCodeFixProvider.CS0168,
                    CSharpRemoveUnusedVariableCodeFixProvider.CS0219),

                new DiagnosticSet(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive,
                    IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
                    IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId)
                );

        protected override string OrganizeImportsDescription
            => CSharpFeaturesResources.Organize_Usings;

        protected override ImmutableArray<DiagnosticSet> GetDiagnosticSets()
            => s_diagnosticSets;
    }
}

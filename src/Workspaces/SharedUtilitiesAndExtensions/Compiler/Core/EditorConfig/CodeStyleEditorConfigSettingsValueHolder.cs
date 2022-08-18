// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal partial class EditorConfigSettingsValueHolder
    {
        private static readonly BidirectionalMap<string, AccessibilityModifiersRequired> AccessibilityModifiersRequiredMap =
            new(new[]
            {
                KeyValuePairUtil.Create("never", AccessibilityModifiersRequired.Never),
                KeyValuePairUtil.Create("always", AccessibilityModifiersRequired.Always),
                KeyValuePairUtil.Create("for_non_interface_members", AccessibilityModifiersRequired.ForNonInterfaceMembers),
                KeyValuePairUtil.Create("omit_if_default", AccessibilityModifiersRequired.OmitIfDefault),
            });

        private static readonly BidirectionalMap<string, ParenthesesPreference> ParenthesesPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always_for_clarity", ParenthesesPreference.AlwaysForClarity),
                KeyValuePairUtil.Create("never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary),
            });

        private static readonly BidirectionalMap<string, UnusedParametersPreference> UnusedParametersPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("non_public", UnusedParametersPreference.NonPublicMethods),
                KeyValuePairUtil.Create("all", UnusedParametersPreference.AllMethods),
            });

        // Qualify Options
        public static EditorConfigData<bool> QualifyFieldAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_field", CompilerExtensionsResources.Qualify_field_access_with_this_or_Me);
        public static EditorConfigData<bool> QualifyPropertyAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_property", CompilerExtensionsResources.Qualify_property_access_with_this_or_Me);
        public static EditorConfigData<bool> QualifyMethodAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_method", CompilerExtensionsResources.Qualify_method_access_with_this_or_Me);
        public static EditorConfigData<bool> QualifyEventAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_event", CompilerExtensionsResources.Qualify_event_access_with_this_or_Me);

        // Predefined Types Options
        public static EditorConfigData<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration = new BooleanEditorConfigData("dotnet_style_predefined_type_for_locals_parameters_members", CompilerExtensionsResources.For_locals_parameters_and_members);
        public static EditorConfigData<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = new BooleanEditorConfigData("dotnet_style_predefined_type_for_member_access", CompilerExtensionsResources.For_member_access_expressions);

        // Null checking Options
        public static EditorConfigData<bool> PreferCoalesceExpression = new BooleanEditorConfigData("dotnet_style_coalesce_expression", CompilerExtensionsResources.Prefer_coalesce_expression);
        public static EditorConfigData<bool> PreferNullPropagation = new BooleanEditorConfigData("dotnet_style_null_propagation", CompilerExtensionsResources.Prefer_null_propagation);
        public static EditorConfigData<bool> PreferIsNullCheckOverReferenceEqualityMethod = new BooleanEditorConfigData("dotnet_style_prefer_is_null_check_over_reference_equality_method", CompilerExtensionsResources.Prefer_is_null_for_reference_equality_checks);

        // Modifier Options
        public static EditorConfigData<AccessibilityModifiersRequired> RequireAccessibilityModifiers = new EnumEditorConfigData<AccessibilityModifiersRequired>("dotnet_style_require_accessibility_modifiers", CompilerExtensionsResources.Require_accessibility_modifiers, AccessibilityModifiersRequiredMap);
        public static EditorConfigData<bool> PreferReadonly = new BooleanEditorConfigData("dotnet_style_readonly_field", CompilerExtensionsResources.Prefer_readonly_fields);

        // Code Block Options
        public static EditorConfigData<bool> PreferAutoProperties = new BooleanEditorConfigData("dotnet_style_prefer_auto_properties", CompilerExtensionsResources.Analyzer_Prefer_auto_properties);

        // Expression Options
        public static EditorConfigData<bool> PreferObjectInitializer = new BooleanEditorConfigData("dotnet_style_object_initializer", CompilerExtensionsResources.Prefer_object_initializer);
        public static EditorConfigData<bool> PreferCollectionInitializer = new BooleanEditorConfigData("dotnet_style_collection_initializer", CompilerExtensionsResources.Prefer_collection_initializer);
        public static EditorConfigData<bool> PreferSimplifiedBooleanExpressions = new BooleanEditorConfigData("dotnet_style_prefer_simplified_boolean_expressions", CompilerExtensionsResources.Prefer_simplified_boolean_expressions);
        public static EditorConfigData<bool> PreferConditionalExpressionOverAssignment = new BooleanEditorConfigData("dotnet_style_prefer_conditional_expression_over_assignment", CompilerExtensionsResources.Prefer_conditional_expression_over_if_with_assignments);
        public static EditorConfigData<bool> PreferConditionalExpressionOverReturn = new BooleanEditorConfigData("dotnet_style_prefer_conditional_expression_over_return", CompilerExtensionsResources.Prefer_conditional_expression_over_if_with_returns);
        public static EditorConfigData<bool> PreferExplicitTupleNames = new BooleanEditorConfigData("dotnet_style_explicit_tuple_names", CompilerExtensionsResources.Prefer_explicit_tuple_name);
        public static EditorConfigData<bool> PreferInferredTupleNames = new BooleanEditorConfigData("dotnet_style_prefer_inferred_tuple_names", CompilerExtensionsResources.Prefer_inferred_tuple_names);
        public static EditorConfigData<bool> PreferInferredAnonymousTypeMemberNames = new BooleanEditorConfigData("dotnet_style_prefer_inferred_anonymous_type_member_names", CompilerExtensionsResources.Prefer_inferred_anonymous_type_member_names);
        public static EditorConfigData<bool> PreferCompoundAssignment = new BooleanEditorConfigData("dotnet_style_prefer_compound_assignment", CompilerExtensionsResources.Prefer_compound_assignments);
        public static EditorConfigData<bool> PreferSimplifiedInterpolation = new BooleanEditorConfigData("dotnet_style_prefer_simplified_interpolation", CompilerExtensionsResources.Prefer_simplified_interpolation);

        // Parentheses Options
        public static EditorConfigData<ParenthesesPreference> ArithmeticBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_arithmetic_binary_operators", CompilerExtensionsResources.In_arithmetic_binary_operators, ParenthesesPreferenceMap);
        public static EditorConfigData<ParenthesesPreference> OtherBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_other_binary_operators", CompilerExtensionsResources.In_other_binary_operators, ParenthesesPreferenceMap);
        public static EditorConfigData<ParenthesesPreference> RelationalBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_relational_binary_operators", CompilerExtensionsResources.In_relational_binary_operators, ParenthesesPreferenceMap);
        public static EditorConfigData<ParenthesesPreference> OtherParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_other_operators", CompilerExtensionsResources.In_other_operators, ParenthesesPreferenceMap);

        // Parameter Options
        public static EditorConfigData<UnusedParametersPreference> UnusedParameters = new EnumEditorConfigData<UnusedParametersPreference>("dotnet_code_quality_unused_parameters", CompilerExtensionsResources.Avoid_unused_parameters, UnusedParametersPreferenceMap);

        // Experimental Options
        public static EditorConfigData<bool> PreferNamespaceAndFolderMatchStructure = new BooleanEditorConfigData("dotnet_style_namespace_match_folder", CompilerExtensionsResources.Prefer_namespace_and_folder_match_structure);
        public static EditorConfigData<bool> AllowMultipleBlankLines = new BooleanEditorConfigData("dotnet_style_allow_multiple_blank_lines_experimental", CompilerExtensionsResources.Allow_multiple_blank_lines);
        public static EditorConfigData<bool> AllowStatementImmediatelyAfterBlock = new BooleanEditorConfigData("dotnet_style_allow_statement_immediately_after_block_experimental", CompilerExtensionsResources.Allow_statement_immediately_after_block);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal partial class EditorConfigSettingsData
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

        private static readonly BidirectionalMap<string, DayOfWeek> DayOfWeekMap =
            new(new[]
            {
                KeyValuePairUtil.Create("Monday", DayOfWeek.Monday),
                KeyValuePairUtil.Create("Tuesday", DayOfWeek.Tuesday),
                KeyValuePairUtil.Create("Wednesday", DayOfWeek.Wednesday),
                KeyValuePairUtil.Create("Thursday", DayOfWeek.Thursday),
                KeyValuePairUtil.Create("Friday", DayOfWeek.Friday),
                KeyValuePairUtil.Create("Saturday", DayOfWeek.Saturday),
                KeyValuePairUtil.Create("Sunday", DayOfWeek.Sunday),
            });

        private static readonly BidirectionalMap<string, ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSourcePreferencePreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always", ForEachExplicitCastInSourcePreference.Always),
                KeyValuePairUtil.Create("when_strongly_typed", ForEachExplicitCastInSourcePreference.WhenStronglyTyped),
            });

        private static readonly BidirectionalMap<string, UnusedValuePreference> UnusedExpressionAssignmentPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("discard_variable", UnusedValuePreference.DiscardVariable),
                KeyValuePairUtil.Create("unused_local_variable", UnusedValuePreference.UnusedLocalVariable),
            });

        #region Qualify Options
        public static EditorConfigData<bool> QualifyFieldAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_field",
                                                                                              CompilerExtensionsResources.Qualify_field_access_with_this_or_Me,
                                                                                              valuesDocumentation: EditorConfigSettingsValuesDocumentation.ThisOrMeDocumentation);

        public static EditorConfigData<bool> QualifyPropertyAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_property",
                                                                                                 CompilerExtensionsResources.Qualify_property_access_with_this_or_Me,
                                                                                                 valuesDocumentation: EditorConfigSettingsValuesDocumentation.ThisOrMeDocumentation);

        public static EditorConfigData<bool> QualifyMethodAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_method",
                                                                                               CompilerExtensionsResources.Qualify_method_access_with_this_or_Me,
                                                                                               valuesDocumentation: EditorConfigSettingsValuesDocumentation.ThisOrMeDocumentation);

        public static EditorConfigData<bool> QualifyEventAccess = new BooleanEditorConfigData("dotnet_style_qualification_for_event",
                                                                                              CompilerExtensionsResources.Qualify_event_access_with_this_or_Me,
                                                                                              valuesDocumentation: EditorConfigSettingsValuesDocumentation.ThisOrMeDocumentation);
        #endregion

        #region Predefined Types Options
        public static EditorConfigData<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration = new BooleanEditorConfigData("dotnet_style_predefined_type_for_locals_parameters_members",
                                                                                                                             CompilerExtensionsResources.For_locals_parameters_and_members,
                                                                                                                             valuesDocumentation: EditorConfigSettingsValuesDocumentation.PreferTypeDocumentation);

        public static EditorConfigData<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = new BooleanEditorConfigData("dotnet_style_predefined_type_for_member_access",
                                                                                                                              CompilerExtensionsResources.For_member_access_expressions,
                                                                                                                              valuesDocumentation: EditorConfigSettingsValuesDocumentation.PreferTypeDocumentation);
        #endregion

        #region Null Checking Options
        public static EditorConfigData<bool> PreferCoalesceExpression = new BooleanEditorConfigData("dotnet_style_coalesce_expression",
                                                                                                    CompilerExtensionsResources.Prefer_coalesce_expression,
                                                                                                    valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferNullPropagation = new BooleanEditorConfigData("dotnet_style_null_propagation",
                                                                                                 CompilerExtensionsResources.Prefer_null_propagation,
                                                                                                 valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferIsNullCheckOverReferenceEqualityMethod = new BooleanEditorConfigData("dotnet_style_prefer_is_null_check_over_reference_equality_method",
                                                                                                                        CompilerExtensionsResources.Prefer_is_null_for_reference_equality_checks,
                                                                                                                        valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);
        #endregion

        #region Modifier Options
        public static EditorConfigData<AccessibilityModifiersRequired> RequireAccessibilityModifiers = new EnumEditorConfigData<AccessibilityModifiersRequired>("dotnet_style_require_accessibility_modifiers",
                                                                                                                                                                CompilerExtensionsResources.Require_accessibility_modifiers,
                                                                                                                                                                AccessibilityModifiersRequiredMap,
                                                                                                                                                                EditorConfigSettingsValuesDocumentation.AccessibilityModifiersDocumentation);

        public static EditorConfigData<bool> PreferReadonly = new BooleanEditorConfigData("dotnet_style_readonly_field",
                                                                                          CompilerExtensionsResources.Prefer_readonly_fields,
                                                                                          valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);
        #endregion

        #region Code Block Options
        public static EditorConfigData<bool> PreferAutoProperties = new BooleanEditorConfigData("dotnet_style_prefer_auto_properties",
                                                                                                CompilerExtensionsResources.Analyzer_Prefer_auto_properties,
                                                                                                valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferSystemHashCode = new BooleanEditorConfigData("",
                                                                                                CompilerExtensionsResources.Prefer_System_HashCode_in_GetHashCode);
        #endregion

        #region Expression Options
        public static EditorConfigData<bool> PreferObjectInitializer = new BooleanEditorConfigData("dotnet_style_object_initializer",
                                                                                                   CompilerExtensionsResources.Prefer_object_initializer,
                                                                                                   valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferCollectionInitializer = new BooleanEditorConfigData("dotnet_style_collection_initializer",
                                                                                                       CompilerExtensionsResources.Prefer_collection_initializer,
                                                                                                       valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferSimplifiedBooleanExpressions = new BooleanEditorConfigData("dotnet_style_prefer_simplified_boolean_expressions",
                                                                                                              CompilerExtensionsResources.Prefer_simplified_boolean_expressions,
                                                                                                              valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferConditionalExpressionOverAssignment = new BooleanEditorConfigData("dotnet_style_prefer_conditional_expression_over_assignment",
                                                                                                                     CompilerExtensionsResources.Prefer_conditional_expression_over_if_with_assignments,
                                                                                                                     valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferConditionalExpressionOverReturn = new BooleanEditorConfigData("dotnet_style_prefer_conditional_expression_over_return",
                                                                                                                 CompilerExtensionsResources.Prefer_conditional_expression_over_if_with_returns,
                                                                                                                 valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferExplicitTupleNames = new BooleanEditorConfigData("dotnet_style_explicit_tuple_names",
                                                                                                    CompilerExtensionsResources.Prefer_explicit_tuple_name,
                                                                                                    valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferInferredTupleNames = new BooleanEditorConfigData("dotnet_style_prefer_inferred_tuple_names",
                                                                                                    CompilerExtensionsResources.Prefer_inferred_tuple_names,
                                                                                                    valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferInferredAnonymousTypeMemberNames = new BooleanEditorConfigData("dotnet_style_prefer_inferred_anonymous_type_member_names",
                                                                                                                  CompilerExtensionsResources.Prefer_inferred_anonymous_type_member_names,
                                                                                                                  valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferCompoundAssignment = new BooleanEditorConfigData("dotnet_style_prefer_compound_assignment",
                                                                                                    CompilerExtensionsResources.Prefer_compound_assignments,
                                                                                                    valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> PreferSimplifiedInterpolation = new BooleanEditorConfigData("dotnet_style_prefer_simplified_interpolation",
                                                                                                         CompilerExtensionsResources.Prefer_simplified_interpolation,
                                                                                                         valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);
        #endregion

        #region Parentheses Options
        public static EditorConfigData<ParenthesesPreference> ArithmeticBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_arithmetic_binary_operators",
                                                                                                                                            CompilerExtensionsResources.In_arithmetic_binary_operators,
                                                                                                                                            ParenthesesPreferenceMap,
                                                                                                                                            EditorConfigSettingsValuesDocumentation.ParenthesesPreferenceDocumentation);

        public static EditorConfigData<ParenthesesPreference> OtherBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_other_binary_operators",
                                                                                                                                       CompilerExtensionsResources.In_other_binary_operators,
                                                                                                                                       ParenthesesPreferenceMap,
                                                                                                                                       EditorConfigSettingsValuesDocumentation.ParenthesesPreferenceDocumentation);

        public static EditorConfigData<ParenthesesPreference> RelationalBinaryParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_relational_binary_operators",
                                                                                                                                            CompilerExtensionsResources.In_relational_binary_operators,
                                                                                                                                            ParenthesesPreferenceMap,
                                                                                                                                            EditorConfigSettingsValuesDocumentation.ParenthesesPreferenceDocumentation);

        public static EditorConfigData<ParenthesesPreference> OtherParentheses = new EnumEditorConfigData<ParenthesesPreference>("dotnet_style_parentheses_in_other_operators",
                                                                                                                                 CompilerExtensionsResources.In_other_operators,
                                                                                                                                 ParenthesesPreferenceMap,
                                                                                                                                 EditorConfigSettingsValuesDocumentation.ParenthesesPreferenceDocumentation);
        #endregion

        #region Paramenter Options
        public static EditorConfigData<UnusedParametersPreference> UnusedParameters = new EnumEditorConfigData<UnusedParametersPreference>("dotnet_code_quality_unused_parameters",
                                                                                                                                           CompilerExtensionsResources.Avoid_unused_parameters,
                                                                                                                                           UnusedParametersPreferenceMap,
                                                                                                                                           EditorConfigSettingsValuesDocumentation.UnusedParametersPreferenceDocumentation);
        #endregion

        #region Experimental Options
        public static EditorConfigData<bool> PreferNamespaceAndFolderMatchStructure = new BooleanEditorConfigData("dotnet_style_namespace_match_folder",
                                                                                                                  CompilerExtensionsResources.Prefer_namespace_and_folder_match_structure,
                                                                                                                  valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> AllowMultipleBlankLines = new BooleanEditorConfigData("dotnet_style_allow_multiple_blank_lines_experimental",
                                                                                                   CompilerExtensionsResources.Allow_multiple_blank_lines,
                                                                                                   valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<bool> AllowStatementImmediatelyAfterBlock = new BooleanEditorConfigData("dotnet_style_allow_statement_immediately_after_block_experimental",
                                                                                                               CompilerExtensionsResources.Allow_statement_immediately_after_block,
                                                                                                               valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);
        #endregion

        #region Visual Basic Options
        public static EditorConfigData<bool> PreferIsNotExpression = new BooleanEditorConfigData("visual_basic_style_prefer_isnot_expression",
                                                                                                 "");

        public static EditorConfigData<bool> PreferSimplifiedObjectCreation = new BooleanEditorConfigData("visual_basic_style_prefer_simplified_object_creation",
                                                                                                          "");

        public static EditorConfigData<string> VBPreferredModifierOrder = new StringEditorConfigData("visual_basic_preferred_modifier_order",
                                                                                                     "",
                                                                                                     "",
                                                                                                     "");

        public static EditorConfigData<UnusedValuePreference> VBUnusedValueExpressionStatement = new EnumEditorConfigData<UnusedValuePreference>("visual_basic_style_unused_value_expression_statement_preference",
                                                                                                                                                 CompilerExtensionsResources.In_other_operators,
                                                                                                                                                 UnusedExpressionAssignmentPreferenceMap);

        public static EditorConfigData<UnusedValuePreference> VBUnusedValueAssignment = new EnumEditorConfigData<UnusedValuePreference>("visual_basic_style_unused_value_assignment_preference",
                                                                                                                                        CompilerExtensionsResources.In_other_operators,
                                                                                                                                        UnusedExpressionAssignmentPreferenceMap);
        #endregion

        #region Other Options
        public static EditorConfigData<string> CSPreferredModifierOrder = new StringEditorConfigData("csharp_preferred_modifier_order",
                                                                                                     "",
                                                                                                     "",
                                                                                                     "");

        public static EditorConfigData<UnusedValuePreference> CSUnusedValueExpressionStatement = new EnumEditorConfigData<UnusedValuePreference>("csharp_style_unused_value_expression_statement_preference",
                                                                                                                                                 CompilerExtensionsResources.In_other_operators,
                                                                                                                                                 UnusedExpressionAssignmentPreferenceMap);

        public static EditorConfigData<UnusedValuePreference> CSUnusedValueAssignment = new EnumEditorConfigData<UnusedValuePreference>("csharp_style_unused_value_assignment_preference",
                                                                                                                                        CompilerExtensionsResources.In_other_operators,
                                                                                                                                        UnusedExpressionAssignmentPreferenceMap);

        public static EditorConfigData<string> FileHeaderTemplate = new StringEditorConfigData("file_header_template",
                                                                                               "",
                                                                                               "unset",
                                                                                               "");

        public static EditorConfigData<string> RemoveUnnecessarySuppressionExclusions = new StringEditorConfigData("dotnet_remove_unnecessary_suppression_exclusions",
                                                                                                                   "",
                                                                                                                   "none",
                                                                                                                   "");

        public static EditorConfigData<ForEachExplicitCastInSourcePreference> ForEachExplicitCastInSource = new EnumEditorConfigData<ForEachExplicitCastInSourcePreference>("dotnet_style_prefer_foreach_explicit_cast_in_source",
                                                                                                                                                                            "",
                                                                                                                                                                            ForEachExplicitCastInSourcePreferencePreferenceMap);

        public static EditorConfigData<bool> PlaceSystemNamespaceFirst = new BooleanEditorConfigData("dotnet_sort_system_directives_first",
                                                                                                     "");

        public static EditorConfigData<bool> SeparateImportDirectiveGroups = new BooleanEditorConfigData("dotnet_separate_import_directive_groups",
                                                                                                         "");
        #endregion

        #region Test
        public static EditorConfigData<bool> BoolCodeStyleTest = new BooleanEditorConfigData("BoolCodeStyleTest",
                                                                                             "TestDescription",
                                                                                             valuesDocumentation: EditorConfigSettingsValuesDocumentation.YesOrNoDocumentation);

        public static EditorConfigData<DayOfWeek> DayOfWeekCodeStyleTest = new EnumEditorConfigData<DayOfWeek>("DayOfWeekCodeStyleTest",
                                                                                                               "TestDescription",
                                                                                                               DayOfWeekMap);
        #endregion
    }
}

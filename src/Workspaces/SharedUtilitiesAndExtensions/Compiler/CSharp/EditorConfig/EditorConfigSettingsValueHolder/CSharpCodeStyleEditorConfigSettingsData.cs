// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditorConfigSettings

{
    internal partial class CSharpEditorConfigSettingsData
    {
        private static readonly BidirectionalMap<string, AddImportPlacement> AddImportPlacementMap =
            new(new[]
            {
                KeyValuePairUtil.Create("inside_namespace", AddImportPlacement.InsideNamespace),
                KeyValuePairUtil.Create("outside_namespace", AddImportPlacement.OutsideNamespace),
            });

        private static readonly BidirectionalMap<string, PreferBracesPreference> PreferBracesPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("false", PreferBracesPreference.None),
                KeyValuePairUtil.Create("when_multiline", PreferBracesPreference.WhenMultiline),
                KeyValuePairUtil.Create("true", PreferBracesPreference.Always),
            });

        private static readonly BidirectionalMap<string, NamespaceDeclarationPreference> NNamespaceDeclarationPreferencesMap =
            new(new[]
            {
                KeyValuePairUtil.Create("block_scoped", NamespaceDeclarationPreference.BlockScoped),
                KeyValuePairUtil.Create("file_scoped", NamespaceDeclarationPreference.FileScoped),
            });

        private static readonly BidirectionalMap<string, ExpressionBodyPreference> ExpressionBodyPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("false", ExpressionBodyPreference.Never),
                KeyValuePairUtil.Create("true", ExpressionBodyPreference.WhenPossible),
                KeyValuePairUtil.Create("when_on_single_line", ExpressionBodyPreference.WhenOnSingleLine),
            });

        private static readonly BidirectionalMap<string, UnusedValuePreference> UnusedValuePreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("discard_variable", UnusedValuePreference.DiscardVariable),
                KeyValuePairUtil.Create("unused_local_variable", UnusedValuePreference.UnusedLocalVariable),
            });

        #region Var Options
        public static EditorConfigData<bool> VarForBuiltInTypes = new BooleanEditorConfigData("csharp_style_var_for_built_in_types",
                                                                                              CSharpCompilerExtensionsResources.For_built_in_types);

        public static EditorConfigData<bool> VarWhenTypeIsApparent = new BooleanEditorConfigData("csharp_style_var_when_type_is_apparent",
                                                                                                 CSharpCompilerExtensionsResources.When_variable_type_is_apparent);

        public static EditorConfigData<bool> VarElsewhere = new BooleanEditorConfigData("csharp_style_var_elsewhere",
                                                                                        CSharpCompilerExtensionsResources.Elsewhere);
        #endregion

        #region Usings Options
        public static EditorConfigData<AddImportPlacement> PreferredUsingDirectivePlacement = new EnumEditorConfigData<AddImportPlacement>("csharp_using_directive_placement",
                                                                                                                                           CSharpCompilerExtensionsResources.Preferred_using_directive_placement,
                                                                                                                                           AddImportPlacementMap);
        #endregion

        #region Null Checking Options
        public static EditorConfigData<bool> PreferThrowExpression = new BooleanEditorConfigData("csharp_style_throw_expression",
                                                                                                 CSharpCompilerExtensionsResources.Prefer_throw_expression);

        public static EditorConfigData<bool> PreferConditionalDelegateCall = new BooleanEditorConfigData("csharp_style_conditional_delegate_call",
                                                                                                         CSharpCompilerExtensionsResources.Prefer_conditional_delegate_call);

        public static EditorConfigData<bool> PreferNullCheckOverTypeCheck = new BooleanEditorConfigData("csharp_style_prefer_null_check_over_type_check",
                                                                                                        CSharpCompilerExtensionsResources.Prefer_null_check_over_type_check);
        #endregion

        #region Modifier Options
        public static EditorConfigData<bool> PreferStaticLocalFunction = new BooleanEditorConfigData("csharp_prefer_static_local_function",
                                                                                                     CSharpCompilerExtensionsResources.Prefer_static_local_functions);
        #endregion

        #region Code Block Options
        public static EditorConfigData<bool> PreferSimpleUsingStatement = new BooleanEditorConfigData("csharp_prefer_simple_using_statement",
                                                                                                      CSharpCompilerExtensionsResources.Prefer_simple_using_statement);

        public static EditorConfigData<PreferBracesPreference> PreferBraces = new EnumEditorConfigData<PreferBracesPreference>("csharp_prefer_braces",
                                                                                                                               CSharpCompilerExtensionsResources.Prefer_braces,
                                                                                                                               PreferBracesPreferenceMap);

        public static EditorConfigData<NamespaceDeclarationPreference> NamespaceDeclarations = new EnumEditorConfigData<NamespaceDeclarationPreference>("csharp_style_namespace_declarations",
                                                                                                                                                        CSharpCompilerExtensionsResources.Namespace_declarations,
                                                                                                                                                        NNamespaceDeclarationPreferencesMap);

        public static EditorConfigData<bool> PreferMethodGroupConversion = new BooleanEditorConfigData("csharp_style_prefer_method_group_conversion",
                                                                                                       CSharpCompilerExtensionsResources.Prefer_method_group_conversion);

        public static EditorConfigData<bool> PreferTopLevelStatements = new BooleanEditorConfigData("csharp_style_prefer_top_level_statements",
                                                                                                    CSharpCompilerExtensionsResources.Prefer_top_level_statements);
        #endregion

        #region Expression Options
        public static EditorConfigData<bool> PreferSwitchExpression = new BooleanEditorConfigData("csharp_style_prefer_switch_expression",
                                                                                                  CSharpCompilerExtensionsResources.Prefer_switch_expression);

        public static EditorConfigData<bool> PreferSimpleDefaultExpression = new BooleanEditorConfigData("csharp_prefer_simple_default_expression",
                                                                                                         CSharpCompilerExtensionsResources.Prefer_simple_default_expression);

        public static EditorConfigData<bool> PreferLocalOverAnonymousFunction = new BooleanEditorConfigData("csharp_style_prefer_local_over_anonymous_function",
                                                                                                            CSharpCompilerExtensionsResources.Prefer_local_function_over_anonymous_function);

        public static EditorConfigData<bool> PreferIndexOperator = new BooleanEditorConfigData("csharp_style_prefer_index_operator",
                                                                                               CSharpCompilerExtensionsResources.Prefer_index_operator);

        public static EditorConfigData<bool> PreferRangeOperator = new BooleanEditorConfigData("csharp_style_prefer_range_operator",
                                                                                               CSharpCompilerExtensionsResources.Prefer_range_operator);

        public static EditorConfigData<bool> ImplicitObjectCreationWhenTypeIsApparent = new BooleanEditorConfigData("csharp_style_implicit_object_creation_when_type_is_apparent",
                                                                                                                    CSharpCompilerExtensionsResources.Prefer_implicit_object_creation_when_type_is_apparent);

        public static EditorConfigData<bool> PreferTupleSwap = new BooleanEditorConfigData("csharp_style_prefer_tuple_swap",
                                                                                           CSharpCompilerExtensionsResources.Prefer_tuple_swap);

        public static EditorConfigData<bool> PreferUtf8StringLiterals = new BooleanEditorConfigData("csharp_style_prefer_utf8_string_literals",
                                                                                                    CSharpCompilerExtensionsResources.Prefer_Utf8_string_literals);
        #endregion

        #region Pattern Matching Options
        public static EditorConfigData<bool> PreferPatternMatching = new BooleanEditorConfigData("csharp_style_prefer_pattern_matching",
                                                                                                 CSharpCompilerExtensionsResources.Prefer_pattern_matching);

        public static EditorConfigData<bool> PreferPatternMatchingOverIsWithCastCheck = new BooleanEditorConfigData("csharp_style_pattern_matching_over_is_with_cast_check",
                                                                                                                    CSharpCompilerExtensionsResources.Prefer_pattern_matching_over_is_with_cast_check);

        public static EditorConfigData<bool> PreferPatternMatchingOverAsWithNullCheck = new BooleanEditorConfigData("csharp_style_pattern_matching_over_as_with_null_check",
                                                                                                                    CSharpCompilerExtensionsResources.Prefer_pattern_matching_over_as_with_null_check);

        public static EditorConfigData<bool> PreferNotPattern = new BooleanEditorConfigData("csharp_style_prefer_not_pattern",
                                                                                            CSharpCompilerExtensionsResources.Prefer_pattern_matching_over_mixed_type_check);

        public static EditorConfigData<bool> PreferExtendedPropertyPattern = new BooleanEditorConfigData("csharp_style_prefer_extended_property_pattern",
                                                                                                         CSharpCompilerExtensionsResources.Prefer_extended_property_pattern);
        #endregion

        #region Variable Options
        public static EditorConfigData<bool> PreferInlinedVariableDeclaration = new BooleanEditorConfigData("csharp_style_inlined_variable_declaration",
                                                                                                            CSharpCompilerExtensionsResources.Prefer_inlined_variable_declaration);

        public static EditorConfigData<bool> PreferDeconstructedVariableDeclaration = new BooleanEditorConfigData("csharp_style_deconstructed_variable_declaration",
                                                                                                                  CSharpCompilerExtensionsResources.Prefer_deconstructed_variable_declaration);
        #endregion

        #region Expression Body Options
        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedTest = new EnumEditorConfigData<ExpressionBodyPreference>("BodyExpressionTest",
                                                                                                                                                 "Use expression body test",
                                                                                                                                                 ExpressionBodyPreferenceMap,
                                                                                                                                                 nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedMethods = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_methods",
                                                                                                                                                    CSharpCompilerExtensionsResources.Use_expression_body_for_methods,
                                                                                                                                                    ExpressionBodyPreferenceMap,
                                                                                                                                                    nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedConstructors = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_constructors",
                                                                                                                                                         CSharpCompilerExtensionsResources.Use_expression_body_for_constructors,
                                                                                                                                                         ExpressionBodyPreferenceMap,
                                                                                                                                                         nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedOperators = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_operators",
                                                                                                                                                      CSharpCompilerExtensionsResources.Use_expression_body_for_operators,
                                                                                                                                                      ExpressionBodyPreferenceMap,
                                                                                                                                                      nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedProperties = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_properties",
                                                                                                                                                       CSharpCompilerExtensionsResources.Use_expression_body_for_properties,
                                                                                                                                                       ExpressionBodyPreferenceMap,
                                                                                                                                                       nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedIndexers = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_indexers",
                                                                                                                                                     CSharpCompilerExtensionsResources.Use_expression_body_for_indexers,
                                                                                                                                                     ExpressionBodyPreferenceMap,
                                                                                                                                                     nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedAccessors = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_accessors",
                                                                                                                                                      CSharpCompilerExtensionsResources.Use_expression_body_for_accessors,
                                                                                                                                                      ExpressionBodyPreferenceMap,
                                                                                                                                                      nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedLambdas = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_lambdas",
                                                                                                                                                    CSharpCompilerExtensionsResources.Use_expression_body_for_lambdas,
                                                                                                                                                    ExpressionBodyPreferenceMap,
                                                                                                                                                    nullable: true);

        public static EditorConfigData<ExpressionBodyPreference> PreferExpressionBodiedLocalFunctions = new EnumEditorConfigData<ExpressionBodyPreference>("csharp_style_expression_bodied_local_functions",
                                                                                                                                                           CSharpCompilerExtensionsResources.Use_expression_body_for_local_functions,
                                                                                                                                                           ExpressionBodyPreferenceMap,
                                                                                                                                                           nullable: true);
        #endregion

        #region Unused Value Options
        public static EditorConfigData<UnusedValuePreference> UnusedValueAssignment = new EnumEditorConfigData<UnusedValuePreference>("csharp_style_unused_value_assignment_preference",
                                                                                                                                      CSharpCompilerExtensionsResources.Avoid_unused_value_assignments,
                                                                                                                                      UnusedValuePreferenceMap);

        public static EditorConfigData<UnusedValuePreference> UnusedValueExpressionStatement = new EnumEditorConfigData<UnusedValuePreference>("csharp_style_unused_value_expression_statement_preference",
                                                                                                                                               CSharpCompilerExtensionsResources.Avoid_expression_statements_that_implicitly_ignore_value,
                                                                                                                                               UnusedValuePreferenceMap);

        public static EditorConfigData<bool> AllowEmbeddedStatementsOnSameLine = new BooleanEditorConfigData("csharp_style_allow_embedded_statements_on_same_line_experimental",
                                                                                                             CSharpCompilerExtensionsResources.Allow_embedded_statements_on_same_line);

        public static EditorConfigData<bool> AllowBlankLinesBetweenConsecutiveBraces = new BooleanEditorConfigData("csharp_style_allow_blank_lines_between_consecutive_braces_experimental",
                                                                                                                   CSharpCompilerExtensionsResources.Allow_blank_lines_between_consecutive_braces);

        public static EditorConfigData<bool> AllowBlankLineAfterColonInConstructorInitializer = new BooleanEditorConfigData("csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental",
                                                                                                                            CSharpCompilerExtensionsResources.Allow_bank_line_after_colon_in_constructor_initializer);
        #endregion
    }
}

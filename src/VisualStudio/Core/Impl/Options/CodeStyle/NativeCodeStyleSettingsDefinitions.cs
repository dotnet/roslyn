// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal static class NativeCodeStyleSettingsDefinitions
{
    private static readonly (string ConfigName, string Group)[] s_cSharp =
    [
        ("dotnet_style_qualification_for_field", "memberQualification"),
        ("dotnet_style_qualification_for_property", "memberQualification"),
        ("dotnet_style_qualification_for_method", "memberQualification"),
        ("dotnet_style_qualification_for_event", "memberQualification"),
        ("dotnet_style_predefined_type_for_locals_parameters_members", "predefinedTypes"),
        ("dotnet_style_predefined_type_for_member_access", "predefinedTypes"),
        ("csharp_style_var_for_built_in_types", "varPreferences"),
        ("csharp_style_var_when_type_is_apparent", "varPreferences"),
        ("csharp_style_var_elsewhere", "varPreferences"),
        ("dotnet_style_prefer_auto_properties", "codeBlocks"),
        ("csharp_prefer_simple_using_statement", "codeBlocks"),
        ("dotnet_prefer_system_hash_code", "codeBlocks"),
        ("csharp_style_prefer_method_group_conversion", "codeBlocks"),
        ("csharp_style_prefer_top_level_statements", "codeBlocks"),
        ("dotnet_style_object_initializer", "expressions"),
        ("dotnet_style_collection_initializer", "expressions"),
        ("dotnet_style_prefer_simplified_boolean_expressions", "expressions"),
        ("csharp_style_prefer_switch_expression", "expressions"),
        ("dotnet_style_prefer_conditional_expression_over_assignment", "expressions"),
        ("dotnet_style_prefer_conditional_expression_over_return", "expressions"),
        ("dotnet_style_explicit_tuple_names", "expressions"),
        ("csharp_prefer_simple_default_expression", "expressions"),
        ("dotnet_style_prefer_inferred_tuple_names", "expressions"),
        ("dotnet_style_prefer_inferred_anonymous_type_member_names", "expressions"),
        ("csharp_style_prefer_local_over_anonymous_function", "expressions"),
        ("dotnet_style_prefer_compound_assignment", "expressions"),
        ("csharp_style_implicit_object_creation_when_type_is_apparent", "expressions"),
        ("csharp_style_prefer_index_operator", "expressions"),
        ("csharp_style_prefer_range_operator", "expressions"),
        ("csharp_style_prefer_tuple_swap", "expressions"),
        ("csharp_style_prefer_pattern_matching", "patternMatching"),
        ("csharp_style_pattern_matching_over_is_with_cast_check", "patternMatching"),
        ("csharp_style_pattern_matching_over_as_with_null_check", "patternMatching"),
        ("csharp_style_prefer_not_pattern", "patternMatching"),
        ("csharp_style_inlined_variable_declaration", "variables"),
        ("csharp_style_deconstructed_variable_declaration", "variables"),
        ("csharp_style_throw_expression", "nullChecking"),
        ("csharp_style_conditional_delegate_call", "nullChecking"),
        ("dotnet_style_coalesce_expression", "nullChecking"),
        ("dotnet_style_null_propagation", "nullChecking"),
        ("dotnet_style_prefer_is_null_check_over_reference_equality_method", "nullChecking"),
        ("csharp_style_prefer_null_check_over_type_check", "nullChecking"),
        ("csharp_using_directive_placement", "usings"),
        ("dotnet_style_readonly_field", "modifiers"),
        ("csharp_style_prefer_readonly_struct", "modifiers"),
        ("csharp_prefer_static_local_function", "modifiers"),
        ("csharp_prefer_static_anonymous_function", "modifiers"),
        ("dotnet_style_allow_multiple_blank_lines_experimental", "newLines"),
        ("csharp_style_allow_embedded_statements_on_same_line_experimental", "newLines"),
        ("csharp_style_allow_blank_lines_between_consecutive_braces_experimental", "newLines"),
        ("dotnet_style_allow_statement_immediately_after_block_experimental", "newLines"),
        ("csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental", "newLines"),
        ("csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental", "newLines"),
        ("csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental", "newLines"),
        ("csharp_prefer_braces", "codeBlocks"),
        ("csharp_style_namespace_declarations", "codeBlocks"),
        ("csharp_style_expression_bodied_methods", "expressions"),
        ("csharp_style_expression_bodied_constructors", "expressions"),
        ("csharp_style_expression_bodied_operators", "expressions"),
        ("csharp_style_expression_bodied_properties", "expressions"),
        ("csharp_style_expression_bodied_indexers", "expressions"),
        ("csharp_style_expression_bodied_accessors", "expressions"),
        ("csharp_style_expression_bodied_lambdas", "expressions"),
        ("csharp_style_expression_bodied_local_functions", "expressions"),
        ("csharp_style_unused_value_assignment_preference", "expressions"),
        ("csharp_style_unused_value_expression_statement_preference", "expressions"),
    ];

    private static readonly (string ConfigName, string Group)[] s_visualBasic =
    [
        ("dotnet_style_qualification_for_field", "memberQualification"),
        ("dotnet_style_qualification_for_property", "memberQualification"),
        ("dotnet_style_qualification_for_method", "memberQualification"),
        ("dotnet_style_qualification_for_event", "memberQualification"),
        ("dotnet_style_predefined_type_for_locals_parameters_members", "predefinedTypes"),
        ("dotnet_style_predefined_type_for_member_access", "predefinedTypes"),
        ("dotnet_style_parentheses_in_arithmetic_binary_operators", "parentheses"),
        ("dotnet_style_parentheses_in_other_binary_operators", "parentheses"),
        ("dotnet_style_parentheses_in_relational_binary_operators", "parentheses"),
        ("dotnet_style_parentheses_in_other_operators", "parentheses"),
        ("dotnet_style_prefer_auto_properties", "codeBlocks"),
        ("dotnet_prefer_system_hash_code", "codeBlocks"),
        ("dotnet_style_object_initializer", "expressions"),
        ("dotnet_style_collection_initializer", "expressions"),
        ("dotnet_style_prefer_simplified_boolean_expressions", "expressions"),
        ("dotnet_style_explicit_tuple_names", "expressions"),
        ("dotnet_style_prefer_inferred_tuple_names", "expressions"),
        ("dotnet_style_prefer_inferred_anonymous_type_member_names", "expressions"),
        ("dotnet_style_prefer_conditional_expression_over_assignment", "expressions"),
        ("dotnet_style_prefer_conditional_expression_over_return", "expressions"),
        ("dotnet_style_prefer_compound_assignment", "expressions"),
        ("visual_basic_style_prefer_isnot_expression", "expressions"),
        ("visual_basic_style_prefer_simplified_object_creation", "expressions"),
        ("visual_basic_style_unused_value_assignment_preference", "expressions"),
        ("visual_basic_style_unused_value_expression_statement_preference", "expressions"),
        ("dotnet_style_coalesce_expression", "nothingChecking"),
        ("dotnet_style_null_propagation", "nothingChecking"),
        ("dotnet_style_prefer_is_null_check_over_reference_equality_method", "nothingChecking"),
        ("dotnet_style_readonly_field", "modifiers"),
        ("dotnet_code_quality_unused_parameters", "parameters"),
        ("dotnet_style_allow_multiple_blank_lines_experimental", "newLines"),
        ("dotnet_style_allow_statement_immediately_after_block_experimental", "newLines"),
    ];

    private static IEnumerable<(string ConfigName, string Group)> GetDefinitions(string language)
        => language switch
        {
            LanguageNames.CSharp => s_cSharp,
            LanguageNames.VisualBasic => s_visualBasic,
            _ => throw new ArgumentException("Unsupported code style language.", nameof(language)),
        };

    internal static IEnumerable<string> GetConfigNames(string language)
        => GetDefinitions(language).Select(definition => definition.ConfigName);

    internal static string GetRowMoniker(string language, string configName)
    {
        var definition = GetDefinitions(language).Single(definition => definition.ConfigName == configName);
        return "languages." + (language == LanguageNames.CSharp ? "csharp" : "basic") + ".codeStyle.general." +
            definition.Group + "." + CodeStyleSetting.ToUnifiedScalarMoniker(configName);
    }
}

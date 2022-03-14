# How editorconfig, options, and diagnostics ids all fit together

Turns out this is kinda complicated. At the bottom there is a table that maps an editorconfig option to its associated Diagnostic Ids. In some cases a single Id is reported for multiple analyzer options, in other a single editorconfig option can have two diagnostic Ids.

Items that start with `dotnet_` and `file_header_template` are defined in [CodeStyleOptions2](../../../../../Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/CodeStyle/CodeStyleOptions2.cs) and exported for the code cleanup UI in [CommonCodeCleanUpFixerDiagnosticIds](CommonCodeCleanUpFixerDiagnosticIds.cs).

Items that start with `csharp_` are defined in [CSharpCodeStyleOptions](../../../../../Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/CodeStyle/CSharpCodeStyleOptions.cs) and exported for the code cleanup UI in [CSharpCodeCleanUpFixerDiagnosticIds](../../../../CSharp/Impl/LanguageService/CSharpCodeCleanupFixerDiagnosticIds.cs)

Items that start with `visual_basic_` are defined in [VisualBasicCodeStyleOptions](../../../../../Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/CodeStyle/VisualBasicCodeStyleOptions.vb) amd exported for the code cleanup UI in [VisualBasicCodeCleanUpFixerDiagnosticIds](../../../../VisualBasic/Impl/LanguageService/VisualBasicCodeCleanupFixerDiagnosticIds.vb)

For editorconfig items that are handled by the formatter they are not exported for the code cleanup UI as our code cleanup service as a single yes/no option for formatting that determines if these are read and fixed.

| **editorconfig**                                                                  | **Diagnostic Ids**       |
|-----------------------------------------------------------------------------------|--------------------------|
| dotnet_code_quality_unused_parameters                                             | IDE0060                  |
| dotnet_remove_unnecessary_suppression_exclusions                                  | IDE0079                  |
| dotnet_separate_import_directive_groups                                           | IDE0065                  |
| dotnet_sort_system_directives_first                                               | IDE0065                  |
| dotnet_style_allow_multiple_blank_lines_experimental                              | IDE2000                  |
| dotnet_style_allow_statement_immediately_after_block_experimental                 | IDE2003                  |
| dotnet_style_coalesce_expression                                                  | IDE0029                  |
| dotnet_style_collection_initializer                                               | IDE0028                  |
| dotnet_style_explicit_tuple_names                                                 | IDE0033                  |
| dotnet_style_namespace_match_folder                                               | IDE0130                  |
| dotnet_style_null_propagation                                                     | IDE0031                  |
| dotnet_style_object_initializer                                                   | IDE0017                  |
| dotnet_style_operator_placement_when_wrapping                                     | N/A handled by formatter |
| dotnet_style_parentheses_in_arithmetic_binary_operators                           | IDE0047/IDE0048          |
| dotnet_style_parentheses_in_other_binary_operators                                | IDE0047/IDE0048          |
| dotnet_style_parentheses_in_other_operators                                       | IDE0047/IDE0048          |
| dotnet_style_parentheses_in_relational_binary_operators                           | IDE0047/IDE0048          |
| dotnet_style_predefined_type_for_locals_parameters_members                        | IDE0049                  |
| dotnet_style_predefined_type_for_member_access                                    | IDE0049                  |
| dotnet_style_prefer_auto_properties                                               | IDE0032                  |
| dotnet_style_prefer_compound_assignment                                           | IDE0054/IDE0074          |
| dotnet_style_prefer_conditional_expression_over_assignment                        | IDE0045                  |
| dotnet_style_prefer_conditional_expression_over_return                            | IDE0046                  |
| dotnet_style_prefer_inferred_anonymous_type_member_names                          | IDE0037                  |
| dotnet_style_prefer_inferred_tuple_names                                          | IDE0037                  |
| dotnet_style_prefer_is_null_check_over_reference_equality_method                  | IDE0041                  |
| dotnet_style_prefer_simplified_boolean_expressions                                | IDE0075                  |
| dotnet_style_prefer_simplified_interpolation                                      | IDE0071                  |
| dotnet_style_qualification_for_event                                              | IDE0003/IDE0009          |
| dotnet_style_qualification_for_field                                              | IDE0003/IDE0009          |
| dotnet_style_qualification_for_method                                             | IDE0003/IDE0009          |
| dotnet_style_qualification_for_property                                           | IDE0003/IDE0009          |
| dotnet_style_readonly_field                                                       | IDE0044                  |
| dotnet_style_require_accessibility_modifiers                                      | IDE0040                  |
| csharp_indent_block_contents                                                      | N/A handled by formatter |
| csharp_indent_braces                                                              | N/A handled by formatter |
| csharp_indent_case_contents                                                       | N/A handled by formatter |
| csharp_indent_case_contents_when_block                                            | N/A handled by formatter |
| csharp_indent_labels                                                              | N/A handled by formatter |
| csharp_indent_switch_labels                                                       | N/A handled by formatter |
| csharp_new_line_before_catch                                                      | N/A handled by formatter |
| csharp_new_line_before_else                                                       | N/A handled by formatter |
| csharp_new_line_before_finally                                                    | N/A handled by formatter |
| csharp_new_line_before_members_in_anonymous_types                                 | N/A handled by formatter |
| csharp_new_line_before_members_in_object_initializers                             | N/A handled by formatter |
| csharp_new_line_before_open_brace                                                 | N/A handled by formatter |
| csharp_new_line_between_query_expression_clauses                                  | N/A handled by formatter |
| csharp_prefer_braces                                                              | IDE0011                  |
| csharp_prefer_simple_default_expression                                           | IDE0034                  |
| csharp_prefer_simple_using_statement                                              | IDE0063                  |
| csharp_prefer_static_local_function                                               | IDE0062                  |
| csharp_preferred_modifier_order                                                   | IDE0036                  |
| csharp_preserve_single_line_blocks                                                | N/A handled by formatter |
| csharp_preserve_single_line_statements                                            | N/A handled by formatter |
| csharp_space_after_cast                                                           | N/A handled by formatter |
| csharp_space_after_colon_in_inheritance_clause                                    | N/A handled by formatter |
| csharp_space_after_comma                                                          | N/A handled by formatter |
| csharp_space_after_dot                                                            | N/A handled by formatter |
| csharp_space_after_keywords_in_control_flow_statements                            | N/A handled by formatter |
| csharp_space_after_semicolon_in_for_statement                                     | N/A handled by formatter |
| csharp_space_around_binary_operators                                              | N/A handled by formatter |
| csharp_space_around_declaration_statements                                        | N/A handled by formatter |
| csharp_space_before_colon_in_inheritance_clause                                   | N/A handled by formatter |
| csharp_space_before_comma                                                         | N/A handled by formatter |
| csharp_space_before_dot                                                           | N/A handled by formatter |
| csharp_space_before_open_square_brackets                                          | N/A handled by formatter |
| csharp_space_before_semicolon_in_for_statement                                    | N/A handled by formatter |
| csharp_space_between_empty_square_brackets                                        | N/A handled by formatter |
| csharp_space_between_method_call_empty_parameter_list_parentheses                 | N/A handled by formatter |
| csharp_space_between_method_call_name_and_opening_parenthesis                     | N/A handled by formatter |
| csharp_space_between_method_call_parameter_list_parentheses                       | N/A handled by formatter |
| csharp_space_between_method_declaration_empty_parameter_list_parentheses          | N/A handled by formatter |
| csharp_space_between_method_declaration_name_and_open_parenthesis                 | N/A handled by formatter |
| csharp_space_between_method_declaration_parameter_list_parentheses                | N/A handled by formatter |
| csharp_space_between_parentheses                                                  | N/A handled by formatter |
| csharp_space_between_square_brackets                                              | N/A handled by formatter |
| csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental | IDE2004                  |
| csharp_style_allow_blank_lines_between_consecutive_braces_experimental            | IDE2002                  |
| csharp_style_allow_embedded_statements_on_same_line_experimental                  | IDE2001                  |
| csharp_style_conditional_delegate_call                                            | IDE1005                  |
| csharp_style_deconstructed_variable_declaration                                   | IDE0042                  |
| csharp_style_expression_bodied_accessors                                          | IDE0027                  |
| csharp_style_expression_bodied_constructors                                       | IDE0021                  |
| csharp_style_expression_bodied_indexers                                           | IDE0026                  |
| csharp_style_expression_bodied_lambdas                                            | IDE0053                  |
| csharp_style_expression_bodied_local_functions                                    | IDE0061                  |
| csharp_style_expression_bodied_methods                                            | IDE0022                  |
| csharp_style_expression_bodied_operators                                          | IDE0023/IDE0024          |
| csharp_style_expression_bodied_properties                                         | IDE0025                  |
| csharp_style_implicit_object_creation_when_type_is_apparent                       | IDE0090                  |
| csharp_style_inlined_variable_declaration                                         | IDE0018                  |
| csharp_style_namespace_declarations                                               | IDE0161                  |
| csharp_style_pattern_matching_over_as_with_null_check                             | IDE0019                  |
| csharp_style_pattern_matching_over_is_with_cast_check                             | IDE0020                  |
| csharp_style_prefer_extended_property_pattern                                     | IDE0170                  |
| csharp_style_prefer_index_operator                                                | IDE0056                  |
| csharp_style_prefer_local_over_anonymous_function                                 | IDE0039                  |
| csharp_style_prefer_method_group_conversion                                       | IDE0200                  |
| csharp_style_prefer_not_pattern                                                   | IDE0083                  |
| csharp_style_prefer_null_check_over_type_check                                    | IDE0150                  |
| csharp_style_prefer_null_check_over_type_check                                    | IDE0150                  |
| csharp_style_prefer_parameter_null_checking                                       | IDE0190                  |
| csharp_style_prefer_pattern_matching                                              | IDE0078                  |
| csharp_style_prefer_range_operator                                                | IDE0057                  |
| csharp_style_prefer_switch_expression                                             | IDE0066                  |
| csharp_style_prefer_tuple_swap                                                    | IDE0180                  |
| csharp_style_throw_expression                                                     | IDE0016                  |
| csharp_style_unused_value_assignment_preference                                   | IDE0059                  |
| csharp_style_unused_value_expression_statement_preference                         | IDE0058                  |
| csharp_style_var_elsewhere                                                        | IDE0007/IDE0008          |
| csharp_style_var_for_built_in_types                                               | IDE0007/IDE0008          |
| csharp_style_var_when_type_is_apparent                                            | IDE0007/IDE0008          |
| csharp_using_directive_placement                                                  | IDE0065                  |
| file_header_template                                                              | IDE0073                  |
| visual_basic_preferred_modifier_order                                             | IDE0036                  |
| visual_basic_style_prefer_isnot_expression                                        | IDE0084                  |
| visual_basic_style_prefer_simplified_object_creation                              | IDE0140                  |
| visual_basic_style_unused_value_assignment_preference                             | IDE0059                  |
| visual_basic_style_unused_value_expression_statement_preference                   | IDE0058                  |
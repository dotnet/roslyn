' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    <[UseExportProvider]>
    Public Class BasicEditorConfigGeneratorTests
        Inherits TestBase

        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub TestEditorConfigGeneratorDefault()
            Using workspace = TestWorkspace.CreateVisualBasic("")
                Dim expectedText = "# Remove the line below if you want to inherit .editorconfig settings from higher directories
root = true

# Visual Basic files
[*.vb]

#### Core EditorConfig Options ####

# Indentation and spacing
indent_size = 4
indent_style = space
tab_width = 4

# New line preferences
end_of_line = crlf
insert_final_newline = false

#### .NET Coding Conventions ####

# Organize usings
dotnet_separate_import_directive_groups = false
dotnet_sort_system_directives_first = true
file_header_template = unset

# this. and Me. preferences
dotnet_style_qualification_for_event = false
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_method = false
dotnet_style_qualification_for_property = false

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true
dotnet_style_predefined_type_for_member_access = true

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members

# Expression-level preferences
dotnet_prefer_system_hash_code = true
dotnet_style_coalesce_expression = true
dotnet_style_collection_initializer = true
dotnet_style_explicit_tuple_names = true
dotnet_style_namespace_match_folder = true
dotnet_style_null_propagation = true
dotnet_style_object_initializer = true
dotnet_style_operator_placement_when_wrapping = beginning_of_line
dotnet_style_prefer_auto_properties = true
dotnet_style_prefer_collection_expression = when_types_loosely_match
dotnet_style_prefer_compound_assignment = true
dotnet_style_prefer_conditional_expression_over_assignment = true
dotnet_style_prefer_conditional_expression_over_return = true
dotnet_style_prefer_foreach_explicit_cast_in_source = when_strongly_typed
dotnet_style_prefer_inferred_anonymous_type_member_names = true
dotnet_style_prefer_inferred_tuple_names = true
dotnet_style_prefer_is_null_check_over_reference_equality_method = true
dotnet_style_prefer_simplified_boolean_expressions = true
dotnet_style_prefer_simplified_interpolation = true

# Field preferences
dotnet_style_readonly_field = true

# Parameter preferences
dotnet_code_quality_unused_parameters = all

# Suppression preferences
dotnet_remove_unnecessary_suppression_exclusions = none

# New line preferences
dotnet_style_allow_multiple_blank_lines_experimental = true
dotnet_style_allow_statement_immediately_after_block_experimental = true

#### VB Coding Conventions ####

# Modifier preferences
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# Expression-level preferences
visual_basic_style_prefer_isnot_expression = true
visual_basic_style_prefer_simplified_object_creation = true
visual_basic_style_unused_value_assignment_preference = unused_local_variable
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable

#### Naming styles ####

# Naming rules

dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_field_members_should_be_pascal_case.symbols = non_field_members
dotnet_naming_rule.non_field_members_should_be_pascal_case.style = pascal_case

# Symbol specifications

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.interface.required_modifiers = 

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.types.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.types.required_modifiers = 

dotnet_naming_symbols.non_field_members.applicable_kinds = property, event, method
dotnet_naming_symbols.non_field_members.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.non_field_members.required_modifiers = 

# Naming styles

dotnet_naming_style.pascal_case.required_prefix = 
dotnet_naming_style.pascal_case.required_suffix = 
dotnet_naming_style.pascal_case.word_separator = 
dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.required_suffix = 
dotnet_naming_style.begins_with_i.word_separator = 
dotnet_naming_style.begins_with_i.capitalization = pascal_case
"
                ' Use the default options 
                Dim options = New OptionStore(workspace.GlobalOptions)
                Dim editorConfigOptions = workspace.GetService(Of EditorConfigOptionsEnumerator).GetOptions(LanguageNames.VisualBasic)
                Dim actualText = EditorConfigFileGenerator.Generate(editorConfigOptions, options, LanguageNames.VisualBasic)
                AssertEx.EqualOrDiff(expectedText, actualText)
            End Using
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub TestEditorConfigGeneratorToggleOptions()
            Using workspace = TestWorkspace.CreateVisualBasic("")
                Dim options = New OptionStore(workspace.GlobalOptions)
                options.SetOption(CodeStyleOptions2.PreferExplicitTupleNames, LanguageNames.VisualBasic, New CodeStyleOption2(Of Boolean)(False, NotificationOption2.[Error]))
                Dim expectedText = "# Remove the line below if you want to inherit .editorconfig settings from higher directories
root = true

# Visual Basic files
[*.vb]

#### Core EditorConfig Options ####

# Indentation and spacing
indent_size = 4
indent_style = space
tab_width = 4

# New line preferences
end_of_line = crlf
insert_final_newline = false

#### .NET Coding Conventions ####

# Organize usings
dotnet_separate_import_directive_groups = false
dotnet_sort_system_directives_first = true
file_header_template = unset

# this. and Me. preferences
dotnet_style_qualification_for_event = false
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_method = false
dotnet_style_qualification_for_property = false

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true
dotnet_style_predefined_type_for_member_access = true

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members

# Expression-level preferences
dotnet_prefer_system_hash_code = true
dotnet_style_coalesce_expression = true
dotnet_style_collection_initializer = true
dotnet_style_explicit_tuple_names = false:error
dotnet_style_namespace_match_folder = true
dotnet_style_null_propagation = true
dotnet_style_object_initializer = true
dotnet_style_operator_placement_when_wrapping = beginning_of_line
dotnet_style_prefer_auto_properties = true
dotnet_style_prefer_collection_expression = when_types_loosely_match
dotnet_style_prefer_compound_assignment = true
dotnet_style_prefer_conditional_expression_over_assignment = true
dotnet_style_prefer_conditional_expression_over_return = true
dotnet_style_prefer_foreach_explicit_cast_in_source = when_strongly_typed
dotnet_style_prefer_inferred_anonymous_type_member_names = true
dotnet_style_prefer_inferred_tuple_names = true
dotnet_style_prefer_is_null_check_over_reference_equality_method = true
dotnet_style_prefer_simplified_boolean_expressions = true
dotnet_style_prefer_simplified_interpolation = true

# Field preferences
dotnet_style_readonly_field = true

# Parameter preferences
dotnet_code_quality_unused_parameters = all

# Suppression preferences
dotnet_remove_unnecessary_suppression_exclusions = none

# New line preferences
dotnet_style_allow_multiple_blank_lines_experimental = true
dotnet_style_allow_statement_immediately_after_block_experimental = true

#### VB Coding Conventions ####

# Modifier preferences
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# Expression-level preferences
visual_basic_style_prefer_isnot_expression = true
visual_basic_style_prefer_simplified_object_creation = true
visual_basic_style_unused_value_assignment_preference = unused_local_variable
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable

#### Naming styles ####

# Naming rules

dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_field_members_should_be_pascal_case.symbols = non_field_members
dotnet_naming_rule.non_field_members_should_be_pascal_case.style = pascal_case

# Symbol specifications

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.interface.required_modifiers = 

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.types.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.types.required_modifiers = 

dotnet_naming_symbols.non_field_members.applicable_kinds = property, event, method
dotnet_naming_symbols.non_field_members.applicable_accessibilities = public, friend, private, protected, protected_friend, private_protected
dotnet_naming_symbols.non_field_members.required_modifiers = 

# Naming styles

dotnet_naming_style.pascal_case.required_prefix = 
dotnet_naming_style.pascal_case.required_suffix = 
dotnet_naming_style.pascal_case.word_separator = 
dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.required_suffix = 
dotnet_naming_style.begins_with_i.word_separator = 
dotnet_naming_style.begins_with_i.capitalization = pascal_case
"
                Dim editorConfigOptions = workspace.GetService(Of EditorConfigOptionsEnumerator).GetOptions(LanguageNames.VisualBasic)
                Dim actualText = EditorConfigFileGenerator.Generate(editorConfigOptions, options, LanguageNames.VisualBasic)
                AssertEx.EqualOrDiff(expectedText, actualText)
            End Using
        End Sub
    End Class
End Namespace

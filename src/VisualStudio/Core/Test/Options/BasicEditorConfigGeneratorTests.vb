' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
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

# this. and Me. preferences
dotnet_style_qualification_for_event = false:silent
dotnet_style_qualification_for_field = false:silent
dotnet_style_qualification_for_method = false:silent
dotnet_style_qualification_for_property = false:silent

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:silent
dotnet_style_predefined_type_for_member_access = true:silent

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# Expression-level preferences
csharp_style_deconstructed_variable_declaration = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion
csharp_style_throw_expression = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_prefer_auto_properties = true:silent
dotnet_style_prefer_compound_assignment = true:suggestion
dotnet_style_prefer_conditional_expression_over_assignment = true:silent
dotnet_style_prefer_conditional_expression_over_return = true:silent
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# Field preferences
dotnet_style_readonly_field = true:suggestion

# Parameter preferences
dotnet_code_quality_unused_parameters = all:suggestion

#### VB Coding Conventions ####

# Modifier preferences
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# Expression-level preferences
visual_basic_style_unused_value_assignment_preference = unused_local_variable:suggestion
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable:silent

"
                Dim editorConfigOptions = VisualBasic.Options.Formatting.CodeStylePage.TestAccessor.GetEditorConfigOptions()
                Dim actualText = EditorConfigFileGenerator.Generate(editorConfigOptions, workspace.Options, LanguageNames.VisualBasic)
                Assert.Equal(expectedText, actualText)
            End Using
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub TestEditorConfigGeneratorToggleOptions()
            Using workspace = TestWorkspace.CreateVisualBasic("")
                Dim changedOptions = workspace.Options.WithChangedOption(New OptionKey(CodeStyleOptions.PreferExplicitTupleNames, LanguageNames.VisualBasic),
                                                                         New CodeStyleOption(Of Boolean)(False, NotificationOption.[Error]))
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

# this. and Me. preferences
dotnet_style_qualification_for_event = false:silent
dotnet_style_qualification_for_field = false:silent
dotnet_style_qualification_for_method = false:silent
dotnet_style_qualification_for_property = false:silent

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:silent
dotnet_style_predefined_type_for_member_access = true:silent

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# Expression-level preferences
csharp_style_deconstructed_variable_declaration = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion
csharp_style_throw_expression = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = false:error
dotnet_style_null_propagation = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_prefer_auto_properties = true:silent
dotnet_style_prefer_compound_assignment = true:suggestion
dotnet_style_prefer_conditional_expression_over_assignment = true:silent
dotnet_style_prefer_conditional_expression_over_return = true:silent
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# Field preferences
dotnet_style_readonly_field = true:suggestion

# Parameter preferences
dotnet_code_quality_unused_parameters = all:suggestion

#### VB Coding Conventions ####

# Modifier preferences
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# Expression-level preferences
visual_basic_style_unused_value_assignment_preference = unused_local_variable:suggestion
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable:silent

"
                Dim editorConfigOptions = VisualBasic.Options.Formatting.CodeStylePage.TestAccessor.GetEditorConfigOptions()
                Dim actualText = EditorConfigFileGenerator.Generate(editorConfigOptions, changedOptions, LanguageNames.VisualBasic)
                Assert.Equal(expectedText, actualText)
            End Using
        End Sub
    End Class
End Namespace

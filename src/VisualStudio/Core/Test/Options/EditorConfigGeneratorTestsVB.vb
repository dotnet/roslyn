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
    Public Class EditorConfigGeneratorTestsVB
        Inherits TestBase

        <WpfFact>
        Public Sub TestEditorConfigGeneratorDefault()
            Using workspace = TestWorkspace.CreateVisualBasic("")
                Dim expectedText = "# Core EditorConfig Options
# Comment the line below if you want to inherit parent .editorconfig settings.
root = true

# Basic files
[*.vb]
indent_style = space
indent_size = 4
insert_final_newline = false

# .NET Coding Conventions
# Organize usings:
dotnet_sort_system_directives_first = true

# Me. preferences:
dotnet_style_qualification_for_field = false:refactoring_only
dotnet_style_qualification_for_property = false:refactoring_only
dotnet_style_qualification_for_method = false:refactoring_only
dotnet_style_qualification_for_event = false:refactoring_only

# Language keywords vs BCL types preferences:
dotnet_style_predefined_type_for_locals_parameters_members = true:refactoring_only
dotnet_style_predefined_type_for_member_access = true:refactoring_only

# Parentheses preferences:
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:refactoring_only

# Modifier preferences:
dotnet_style_require_accessibility_modifiers = for_non_interface_members:refactoring_only
dotnet_style_readonly_field = true:suggestion

# Expression-level preferences:
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:refactoring_only
dotnet_style_prefer_conditional_expression_over_assignment = true:refactoring_only
dotnet_style_prefer_conditional_expression_over_return = true:refactoring_only

# VB Coding Conventions
# Modifier preferences:
visual_basic_preferred_modifier_order = Partial,Default,Private,Protected,Public,Friend,NotOverridable,Overridable,MustOverride,Overloads,Overrides,MustInherit,NotInheritable,Static,Shared,Shadows,ReadOnly,WriteOnly,Dim,Const,WithEvents,Widening,Narrowing,Custom,Async,Iterator:refactoring_only
"
                Dim actualText = New StringBuilder()
                VisualBasic.Options.Formatting.CodeStylePage.Generate_Editorconfig(workspace.Options, LanguageNames.VisualBasic, actualText)
                Assert.Equal(expectedText, actualText.ToString())
            End Using
        End Sub

        <WpfFact>
        Public Sub TestEditorConfigGeneratorToggleOptions()
            Using workspace = TestWorkspace.CreateVisualBasic("")
                Dim changedOptions = workspace.Options.WithChangedOption(New OptionKey(CodeStyleOptions.PreferExplicitTupleNames, LanguageNames.VisualBasic),
                                                                         New CodeStyleOption(Of Boolean)(False, NotificationOption.[Error]))
                Dim expectedText = "# Core EditorConfig Options
# Comment the line below if you want to inherit parent .editorconfig settings.
root = true

# Basic files
[*.vb]
indent_style = space
indent_size = 4
insert_final_newline = false

# .NET Coding Conventions
# Organize usings:
dotnet_sort_system_directives_first = true

# Me. preferences:
dotnet_style_qualification_for_field = false:refactoring_only
dotnet_style_qualification_for_property = false:refactoring_only
dotnet_style_qualification_for_method = false:refactoring_only
dotnet_style_qualification_for_event = false:refactoring_only

# Language keywords vs BCL types preferences:
dotnet_style_predefined_type_for_locals_parameters_members = true:refactoring_only
dotnet_style_predefined_type_for_member_access = true:refactoring_only

# Parentheses preferences:
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:refactoring_only
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:refactoring_only

# Modifier preferences:
dotnet_style_require_accessibility_modifiers = for_non_interface_members:refactoring_only
dotnet_style_readonly_field = true:suggestion

# Expression-level preferences:
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = false:error
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:refactoring_only
dotnet_style_prefer_conditional_expression_over_assignment = true:refactoring_only
dotnet_style_prefer_conditional_expression_over_return = true:refactoring_only

# VB Coding Conventions
# Modifier preferences:
visual_basic_preferred_modifier_order = Partial,Default,Private,Protected,Public,Friend,NotOverridable,Overridable,MustOverride,Overloads,Overrides,MustInherit,NotInheritable,Static,Shared,Shadows,ReadOnly,WriteOnly,Dim,Const,WithEvents,Widening,Narrowing,Custom,Async,Iterator:refactoring_only
"
                Dim actualText = New StringBuilder()
                VisualBasic.Options.Formatting.CodeStylePage.Generate_Editorconfig(changedOptions, LanguageNames.VisualBasic, actualText)
                Assert.Equal(expectedText, actualText.ToString())
            End Using
        End Sub
    End Class
End Namespace

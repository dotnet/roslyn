' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            Return New GridOptionPreviewControl(serviceProvider, Function(o, s) New StyleViewModel(o, s), AddressOf GetCurrentEditorConfigOptionsString)
        End Function

        Friend Shared Function GetCurrentEditorConfigOptionsString(ByVal optionSet As OptionSet) As String
            Dim editorconfig = New StringBuilder()

            ' Core EditorConfig Options
            editorconfig.AppendLine("###############################")
            editorconfig.AppendLine("# Core EditorConfig Options   #")
            editorconfig.AppendLine("###############################")
            editorconfig.AppendLine("# You can uncomment the next line if this is your top-most .editorconfig file.")
            editorconfig.AppendLine("# root = true")
            editorconfig.AppendLine()
            editorconfig.AppendLine("# Basic files")
            editorconfig.AppendLine("[*.vb]")
            editorconfig.Append("indent_style = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, FormattingOptions.UseTabs))
            editorconfig.Append("indent_size = ")
            editorconfig.AppendLine(optionSet.GetOption(FormattingOptions.IndentationSize, "csharp").ToString())
            editorconfig.Append("insert_final_newline = ")
            editorconfig.AppendLine(optionSet.GetOption(FormattingOptions.InsertFinalNewLine).ToString().ToLower())

            editorconfig.AppendLine()
            ' .NET Coding Conventions
            editorconfig.AppendLine("###############################")
            editorconfig.AppendLine("# .NET Coding Conventions     #")
            editorconfig.AppendLine("###############################")
            editorconfig.AppendLine("# Organize usings")
            editorconfig.Append("dotnet_sort_system_directives_first = ")
            editorconfig.AppendLine(optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, "csharp").ToString().ToLower())

            editorconfig.AppendLine()
            ' this. preferences
            editorconfig.AppendLine("# this. preferences")
            editorconfig.Append("dotnet_style_qualification_for_field = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyFieldAccess))
            editorconfig.Append("dotnet_style_qualification_for_property = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyPropertyAccess))
            editorconfig.Append("dotnet_style_qualification_for_method = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyMethodAccess))
            editorconfig.Append("dotnet_style_qualification_for_event = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyPropertyAccess))

            editorconfig.AppendLine()
            ' Language keywords vs. BCL types preferences
            editorconfig.AppendLine("# Language keywords vs BCL types preferences")
            editorconfig.Append("dotnet_style_predefined_type_for_locals_parameters_members = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration))
            editorconfig.Append("dotnet_style_predefined_type_for_member_access = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess))
            editorconfig.AppendLine()

            ' Parentheses preferences
            editorconfig.AppendLine("# Parentheses preferences")
            editorconfig.Append("dotnet_style_parentheses_in_arithmetic_binary_operators = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.ArithmeticBinaryParentheses))
            editorconfig.Append("dotnet_style_parentheses_in_relational_binary_operators = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RelationalBinaryParentheses))
            editorconfig.Append("dotnet_style_parentheses_in_other_binary_operators = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherBinaryParentheses))
            editorconfig.Append("dotnet_style_parentheses_in_other_operators = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherParentheses))

            editorconfig.AppendLine()
            ' Modifier preferences
            editorconfig.AppendLine("# Modifier preferences")
            editorconfig.Append("dotnet_style_require_accessibility_modifiers = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RequireAccessibilityModifiers))
            editorconfig.Append("dotnet_style_readonly_field = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferReadonly))

            editorconfig.AppendLine()
            ' Expression-level preferences
            editorconfig.AppendLine("# Expression-level preferences")
            editorconfig.Append("dotnet_style_object_initializer = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferObjectInitializer))
            editorconfig.Append("dotnet_style_collection_initializer = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCollectionInitializer))
            editorconfig.Append("dotnet_style_explicit_tuple_names = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferExplicitTupleNames))
            editorconfig.Append("dotnet_style_null_propagation = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferNullPropagation))
            editorconfig.Append("dotnet_style_coalesce_expression = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCoalesceExpression))
            editorconfig.Append("dotnet_style_prefer_is_null_check_over_reference_equality_method = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod))
            editorconfig.Append("dotnet_prefer_inferred_tuple_names = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredTupleNames))
            editorconfig.Append("dotnet_prefer_inferred_anonymous_type_member_names = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredAnonymousTypeMemberNames))
            editorconfig.Append("dotnet_style_prefer_auto_properties = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferAutoProperties))
            editorconfig.Append("dotnet_style_prefer_conditional_expression_over_assignment = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverAssignment))
            editorconfig.Append("dotnet_style_prefer_conditional_expression_over_return = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverReturn))

            editorconfig.AppendLine()
            ' VB Coding Conventions
            editorconfig.AppendLine("###############################")
            editorconfig.AppendLine("# VB Coding Conventions       #")
            editorconfig.AppendLine("###############################")
            editorconfig.Append("visual_basic_preferred_modifier_order = ")
            editorconfig.AppendLine(BasicCodeStyleOptions_GenerateEditorconfig(optionSet, VisualBasicCodeStyleOptions.PreferredModifierOrder))

            Return editorconfig.ToString()
        End Function

        Private Shared Function BasicCodeStyleOptions_GenerateEditorconfig(optionSet As OptionSet, [option] As [Option](Of CodeStyleOption(Of String))) As String
            Dim curSetting = optionSet.GetOption([option])
            Return curSetting.Value + ":" + curSetting.Notification.ToString().ToLower()
        End Function

        Private Shared Function BasicCodeStyleOptions_GenerateEditorconfig(ByVal optionSet As OptionSet, ByVal [option] As PerLanguageOption(Of Boolean)) As String
            Dim curSetting = optionSet.GetOption([option], LanguageNames.VisualBasic)
            If curSetting Then
                Return "tab"
            Else
                Return "space"
            End If
        End Function

        Private Shared Function BasicCodeStyleOptions_GenerateEditorconfig(ByVal optionSet As OptionSet, ByVal [option] As PerLanguageOption(Of CodeStyleOption(Of AccessibilityModifiersRequired))) As String
            Dim curSetting = optionSet.GetOption([option], LanguageNames.VisualBasic)
            Dim editorconfig = ""
            If curSetting.Value = AccessibilityModifiersRequired.ForNonInterfaceMembers Then
                editorconfig += "for_non_interface_members:" & curSetting.Notification.ToString().ToLower()
            ElseIf curSetting.Value = AccessibilityModifiersRequired.OmitIfDefault Then
                editorconfig += "omit_if_default:" & curSetting.Notification.ToString().ToLower()
            Else
                editorconfig += (curSetting.Value & ":" + curSetting.Notification.ToString()).ToLower()
            End If
            Return editorconfig
        End Function

        Private Shared Function BasicCodeStyleOptions_GenerateEditorconfig(ByVal optionSet As OptionSet, ByVal [option] As PerLanguageOption(Of CodeStyleOption(Of Boolean))) As String
            Dim curSetting = optionSet.GetOption([option], LanguageNames.VisualBasic)
            Dim editorconfig = curSetting.Value & ":" + curSetting.Notification.ToString()
            Return editorconfig.ToLower()
        End Function

        Private Shared Function BasicCodeStyleOptions_GenerateEditorconfig(ByVal optionSet As OptionSet, ByVal [option] As PerLanguageOption(Of CodeStyleOption(Of ParenthesesPreference))) As String
            Dim curSetting = optionSet.GetOption([option], LanguageNames.VisualBasic)
            Dim editorconfig = ""
            If curSetting.Value = ParenthesesPreference.AlwaysForClarity Then
                editorconfig += "always_for_clarity:" & curSetting.Notification.ToString().ToLower()
            Else
                editorconfig += "never_if_unnecessary:" & curSetting.Notification.ToString().ToLower()
            End If
            Return editorconfig
        End Function
    End Class
End Namespace

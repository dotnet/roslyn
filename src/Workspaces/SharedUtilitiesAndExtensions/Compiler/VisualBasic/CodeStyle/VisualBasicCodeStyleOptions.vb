' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend NotInheritable Class VisualBasicCodeStyleOptions
        Private Shared ReadOnly s_allOptionsBuilder As ImmutableArray(Of IOption2).Builder = ImmutableArray.CreateBuilder(Of IOption2)

        Shared Sub New()
            AllOptions = s_allOptionsBuilder.ToImmutable()
        End Sub

        Private Shared Function CreateOption(Of T)(group As OptionGroup, name As String, defaultValue As T, storageLocation As EditorConfigStorageLocation(Of T)) As Option2(Of T)
            Return CodeStyleHelpers.CreateOption(group, name, defaultValue, s_allOptionsBuilder, storageLocation, LanguageNames.VisualBasic)
        End Function

        Private Shared Function CreateOption(group As OptionGroup, defaultValue As CodeStyleOption2(Of Boolean), name As String) As Option2(Of CodeStyleOption2(Of Boolean))
            Return CreateOption(group, name, defaultValue, EditorConfigStorageLocation.ForBoolCodeStyleOption(defaultValue))
        End Function

        Private Shared Function CreateOption(group As OptionGroup, defaultValue As CodeStyleOption2(Of String), name As String) As Option2(Of CodeStyleOption2(Of String))
            Return CreateOption(group, name, defaultValue, EditorConfigStorageLocation.ForStringCodeStyleOption(defaultValue))
        End Function

        Public Shared ReadOnly Property AllOptions As ImmutableArray(Of IOption2)

        Public Shared ReadOnly PreferredModifierOrder As Option2(Of CodeStyleOption2(Of String)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.Modifier, VisualBasicIdeCodeStyleOptions.Default.PreferredModifierOrder,
            "visual_basic_preferred_modifier_order")

        Public Shared ReadOnly PreferIsNotExpression As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences, VisualBasicIdeCodeStyleOptions.Default.PreferIsNotExpression,
            "visual_basic_style_prefer_isnot_expression")

        Public Shared ReadOnly PreferSimplifiedObjectCreation As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences, VisualBasicIdeCodeStyleOptions.Default.PreferSimplifiedObjectCreation,
            "visual_basic_style_prefer_simplified_object_creation")

        Public Shared ReadOnly UnusedValueExpressionStatement As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                editorConfigName:="visual_basic_style_unused_value_expression_statement_preference",
                defaultValue:=VisualBasicIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
                optionsBuilder:=s_allOptionsBuilder,
                languageName:=LanguageNames.VisualBasic)

        Public Shared ReadOnly UnusedValueAssignment As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                editorConfigName:="visual_basic_style_unused_value_assignment_preference",
                defaultValue:=VisualBasicIdeCodeStyleOptions.Default.UnusedValueAssignment,
                optionsBuilder:=s_allOptionsBuilder,
                languageName:=LanguageNames.VisualBasic)
    End Class

    Friend NotInheritable Class VisualBasicCodeStyleOptionGroups
        Public Shared ReadOnly Modifier As New OptionGroup(CompilerExtensionsResources.Modifier_preferences, priority:=1, parent:=CodeStyleOptionGroups.CodeStyle)
        Public Shared ReadOnly ExpressionLevelPreferences As New OptionGroup(CompilerExtensionsResources.Expression_level_preferences, priority:=2, parent:=CodeStyleOptionGroups.CodeStyle)
    End Class
End Namespace

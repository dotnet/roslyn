' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend NotInheritable Class VisualBasicCodeStyleOptions
        Private Shared ReadOnly s_allOptionsBuilder As ImmutableArray(Of IOption2).Builder = ImmutableArray.CreateBuilder(Of IOption2)

        Private Shared Function CreateOption(Of T)(
            group As OptionGroup,
            name As String,
            defaultValue As CodeStyleOption2(Of T),
            Optional serializerFactory As Func(Of CodeStyleOption2(Of T), EditorConfigStorageLocation(Of CodeStyleOption2(Of T))) = Nothing) As Option2(Of CodeStyleOption2(Of T))
            Return s_allOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, LanguageNames.VisualBasic, serializerFactory)
        End Function

        Public Shared ReadOnly PreferredModifierOrder As Option2(Of CodeStyleOption2(Of String)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.Modifier,
            "visual_basic_preferred_modifier_order",
            VisualBasicIdeCodeStyleOptions.Default.PreferredModifierOrder)

        Public Shared ReadOnly PreferIsNotExpression As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
            "visual_basic_style_prefer_isnot_expression",
            VisualBasicIdeCodeStyleOptions.Default.PreferIsNotExpression)

        Public Shared ReadOnly PreferSimplifiedObjectCreation As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
            "visual_basic_style_prefer_simplified_object_creation",
            VisualBasicIdeCodeStyleOptions.Default.PreferSimplifiedObjectCreation)

        Public Shared ReadOnly UnusedValueExpressionStatement As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
            "visual_basic_style_unused_value_expression_statement_preference",
            VisualBasicIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
            AddressOf CodeStyleHelpers.GetUnusedValuePreferenceSerializer)

        Public Shared ReadOnly UnusedValueAssignment As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
            "visual_basic_style_unused_value_assignment_preference",
            VisualBasicIdeCodeStyleOptions.Default.UnusedValueAssignment,
            AddressOf CodeStyleHelpers.GetUnusedValuePreferenceSerializer)

        Public Shared ReadOnly Property AllOptions As ImmutableArray(Of IOption2) = s_allOptionsBuilder.ToImmutable()
    End Class

    Friend NotInheritable Class VisualBasicCodeStyleOptionGroups
        Public Shared ReadOnly Modifier As New OptionGroup(CompilerExtensionsResources.Modifier_preferences, priority:=1, parent:=CodeStyleOptionGroups.CodeStyle)
        Public Shared ReadOnly ExpressionLevelPreferences As New OptionGroup(CompilerExtensionsResources.Expression_level_preferences, priority:=2, parent:=CodeStyleOptionGroups.CodeStyle)
    End Class
End Namespace

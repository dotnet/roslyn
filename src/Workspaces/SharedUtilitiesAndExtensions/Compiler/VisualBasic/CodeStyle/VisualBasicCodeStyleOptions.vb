' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.EditorConfigSettings
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend NotInheritable Class VisualBasicCodeStyleOptions
        Private Shared ReadOnly s_allOptionsBuilder As ImmutableArray(Of IOption2).Builder = ImmutableArray.CreateBuilder(Of IOption2)

        Shared Sub New()
            AllOptions = s_allOptionsBuilder.ToImmutable()
        End Sub

        Private Shared Function CreateOption(Of T)(group As OptionGroup, name As String, defaultValue As T, storageLocation As OptionStorageLocation2) As Option2(Of T)
            Return CodeStyleHelpers.CreateOption(group, NameOf(VisualBasicCodeStyleOptions), name, defaultValue, s_allOptionsBuilder, storageLocation, LanguageNames.VisualBasic)
        End Function

        Private Shared Function CreateOption(Of T)(group As OptionGroup, name As String, defaultValue As T, storageLocation1 As OptionStorageLocation2, storageLocation2 As OptionStorageLocation2) As Option2(Of T)
            Return CodeStyleHelpers.CreateOption(group, NameOf(VisualBasicCodeStyleOptions), name, defaultValue, s_allOptionsBuilder, storageLocation1, storageLocation2, LanguageNames.VisualBasic)
        End Function

        Private Shared Function CreateOption(group As OptionGroup, name As String, defaultValue As CodeStyleOption2(Of Boolean), editorConfigData As EditorConfigData(Of Boolean), roamingProfileStorageKeyName As String) As Option2(Of CodeStyleOption2(Of Boolean))
            Return CreateOption(group, name, defaultValue, EditorConfigStorageLocation.ForBoolCodeStyleOption(editorConfigData, defaultValue), New RoamingProfileStorageLocation(roamingProfileStorageKeyName))
        End Function

        Private Shared Function CreateOption(group As OptionGroup, name As String, defaultValue As CodeStyleOption2(Of String), editorConfigData As EditorConfigData(Of String), roamingProfileStorageKeyName As String) As Option2(Of CodeStyleOption2(Of String))
            Return CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForStringCodeStyleOption(editorConfigData, defaultValue),
                New RoamingProfileStorageLocation(roamingProfileStorageKeyName))
        End Function

        Public Shared ReadOnly Property AllOptions As ImmutableArray(Of IOption2)

        Public Shared ReadOnly PreferredModifierOrder As Option2(Of CodeStyleOption2(Of String)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.Modifier, NameOf(PreferredModifierOrder),
            VisualBasicIdeCodeStyleOptions.Default.PreferredModifierOrder,
            EditorConfigSettingsData.VBPreferredModifierOrder,
            $"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferredModifierOrder)}")

        Public Shared ReadOnly PreferIsNotExpression As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences, NameOf(PreferIsNotExpression),
            VisualBasicIdeCodeStyleOptions.Default.PreferIsNotExpression,
            EditorConfigSettingsData.PreferIsNotExpression,
            $"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferIsNotExpression)}")

        Public Shared ReadOnly PreferSimplifiedObjectCreation As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences, NameOf(PreferSimplifiedObjectCreation),
            VisualBasicIdeCodeStyleOptions.Default.PreferSimplifiedObjectCreation,
            EditorConfigSettingsData.PreferSimplifiedObjectCreation,
            $"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferSimplifiedObjectCreation)}")

        Public Shared ReadOnly UnusedValueExpressionStatement As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueExpressionStatement),
                editorConfigData:=EditorConfigSettingsData.VBUnusedValueExpressionStatement,
                defaultValue:=VisualBasicIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
                optionsBuilder:=s_allOptionsBuilder,
                languageName:=LanguageNames.VisualBasic)

        Public Shared ReadOnly UnusedValueAssignment As Option2(Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueAssignment),
                editorConfigData:=EditorConfigSettingsData.VBUnusedValueAssignment,
                defaultValue:=VisualBasicIdeCodeStyleOptions.Default.UnusedValueAssignment,
                optionsBuilder:=s_allOptionsBuilder,
                languageName:=LanguageNames.VisualBasic)
    End Class

    Friend NotInheritable Class VisualBasicCodeStyleOptionGroups
        Public Shared ReadOnly Modifier As New OptionGroup(CompilerExtensionsResources.Modifier_preferences, priority:=1)
        Public Shared ReadOnly ExpressionLevelPreferences As New OptionGroup(CompilerExtensionsResources.Expression_level_preferences, priority:=2)
    End Class
End Namespace

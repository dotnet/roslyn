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

        Private Shared Function CreateOption(Of T)(group As OptionGroup, name As String, defaultValue As T, ParamArray storageLocations As OptionStorageLocation2()) As [Option2](Of T)
            Return CodeStyleHelpers.CreateOption(group, NameOf(VisualBasicCodeStyleOptions), name, defaultValue, s_allOptionsBuilder, storageLocations)
        End Function

        Public Shared ReadOnly Property AllOptions As ImmutableArray(Of IOption2)

        Public Shared ReadOnly PreferredModifierOrderDefault As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.PartialKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword, SyntaxKind.FriendKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.OverridesKeyword, SyntaxKind.MustInheritKeyword,
                SyntaxKind.NotInheritableKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword,
                SyntaxKind.ReadOnlyKeyword, SyntaxKind.WriteOnlyKeyword, SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword,
                SyntaxKind.WithEventsKeyword, SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword, SyntaxKind.CustomKeyword,
                SyntaxKind.AsyncKeyword, SyntaxKind.IteratorKeyword)

        Public Shared ReadOnly PreferredModifierOrder As Option2(Of CodeStyleOption2(Of String)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.Modifier, NameOf(PreferredModifierOrder),
            defaultValue:=New CodeStyleOption2(Of String)(String.Join(",", PreferredModifierOrderDefault.Select(AddressOf SyntaxFacts.GetText)), NotificationOption2.Silent),
            EditorConfigStorageLocation.ForStringCodeStyleOption("visual_basic_preferred_modifier_order"),
            New RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferredModifierOrder)}"))

        Public Shared ReadOnly PreferIsNotExpression As Option2(Of CodeStyleOption2(Of Boolean)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences, NameOf(PreferIsNotExpression),
            defaultValue:=New CodeStyleOption2(Of Boolean)(True, NotificationOption2.Suggestion),
            EditorConfigStorageLocation.ForBoolCodeStyleOption("visual_basic_style_prefer_isnot_expression"),
            New RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferIsNotExpression)}"))

        Public Shared ReadOnly UnusedValueExpressionStatement As [Option2](Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueExpressionStatement),
                editorConfigName:="visual_basic_style_unused_value_expression_statement_preference",
                defaultValue:=New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent),
                optionsBuilder:=s_allOptionsBuilder)

        Public Shared ReadOnly UnusedValueAssignment As [Option2](Of CodeStyleOption2(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueAssignment),
                editorConfigName:="visual_basic_style_unused_value_assignment_preference",
                defaultValue:=New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion),
                optionsBuilder:=s_allOptionsBuilder)
    End Class

    Friend NotInheritable Class VisualBasicCodeStyleOptionGroups
        Public Shared ReadOnly Modifier As New OptionGroup(CompilerExtensionsResources.Modifier_preferences, priority:=1)
        Public Shared ReadOnly ExpressionLevelPreferences As New OptionGroup(CompilerExtensionsResources.Expression_level_preferences, priority:=2)
    End Class
End Namespace

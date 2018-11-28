' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend NotInheritable Class VisualBasicCodeStyleOptions
        Private Shared ReadOnly s_allOptionsBuilder As ImmutableArray(Of IOption).Builder = ImmutableArray.CreateBuilder(Of IOption)

        Shared Sub New()
            AllOptions = s_allOptionsBuilder.ToImmutable()
        End Sub

        Private Shared Function CreateOption(Of T)(group As OptionGroup, name As String, defaultValue As T, ParamArray storageLocations As OptionStorageLocation()) As [Option](Of T)
            Return CodeStyleHelpers.CreateOption(group, NameOf(VisualBasicCodeStyleOptions), name, defaultValue, s_allOptionsBuilder, storageLocations)
        End Function

        Public Shared ReadOnly Property AllOptions As ImmutableArray(Of IOption)

        Public Shared ReadOnly PreferredModifierOrderDefault As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.PartialKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword, SyntaxKind.FriendKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.OverridesKeyword, SyntaxKind.MustInheritKeyword,
                SyntaxKind.NotInheritableKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword,
                SyntaxKind.ReadOnlyKeyword, SyntaxKind.WriteOnlyKeyword, SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword,
                SyntaxKind.WithEventsKeyword, SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword, SyntaxKind.CustomKeyword,
                SyntaxKind.AsyncKeyword, SyntaxKind.IteratorKeyword)

        Public Shared ReadOnly PreferredModifierOrder As [Option](Of CodeStyleOption(Of String)) = CreateOption(
            VisualBasicCodeStyleOptionGroups.Modifier, NameOf(PreferredModifierOrder),
            defaultValue:=New CodeStyleOption(Of String)(String.Join(",", PreferredModifierOrderDefault.Select(AddressOf SyntaxFacts.GetText)), NotificationOption.Silent),
            EditorConfigStorageLocation.ForStringCodeStyleOption("visual_basic_preferred_modifier_order"),
            New RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferredModifierOrder)}"))

        Public Shared ReadOnly UnusedValueExpressionStatement As [Option](Of CodeStyleOption(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueExpressionStatement),
                editorConfigName:="visual_basic_style_unused_value_expression_statement_preference",
                defaultValue:=New CodeStyleOption(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption.Silent),
                optionsBuilder:=s_allOptionsBuilder)

        Public Shared ReadOnly UnusedValueAssignment As [Option](Of CodeStyleOption(Of UnusedValuePreference)) =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                group:=VisualBasicCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature:=NameOf(VisualBasicCodeStyleOptions),
                name:=NameOf(UnusedValueAssignment),
                editorConfigName:="visual_basic_style_unused_value_assignment_preference",
                defaultValue:=New CodeStyleOption(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption.Suggestion),
                optionsBuilder:=s_allOptionsBuilder)
    End Class

    Friend NotInheritable Class VisualBasicCodeStyleOptionGroups
        Public Shared ReadOnly Modifier As New OptionGroup(WorkspacesResources.Modifier_preferences, priority:=1)
        Public Shared ReadOnly ExpressionLevelPreferences As New OptionGroup(WorkspacesResources.Expression_level_preferences, priority:=2)
    End Class
End Namespace

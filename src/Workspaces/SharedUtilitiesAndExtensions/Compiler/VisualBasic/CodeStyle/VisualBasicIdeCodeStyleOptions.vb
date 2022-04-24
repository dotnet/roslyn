' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <DataContract>
    Friend NotInheritable Class VisualBasicIdeCodeStyleOptions
        Inherits IdeCodeStyleOptions

        Private Shared ReadOnly s_unusedLocalVariableWithSilentEnforcement As New CodeStyleOption2(Of UnusedValuePreference)(
            UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent)

        Private Shared ReadOnly s_unusedLocalVariableWithSuggestionEnforcement As New CodeStyleOption2(Of UnusedValuePreference)(
            UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion)

        Public Shared ReadOnly DefaultPreferredModifierOrder As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.PartialKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword, SyntaxKind.FriendKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.OverridesKeyword, SyntaxKind.MustInheritKeyword,
                SyntaxKind.NotInheritableKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword,
                SyntaxKind.ReadOnlyKeyword, SyntaxKind.WriteOnlyKeyword, SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword,
                SyntaxKind.WithEventsKeyword, SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword, SyntaxKind.CustomKeyword,
                SyntaxKind.AsyncKeyword, SyntaxKind.IteratorKeyword)

        Private Shared ReadOnly s_defaultModifierOrder As New CodeStyleOption2(Of String)(
            String.Join(",", DefaultPreferredModifierOrder.Select(AddressOf SyntaxFacts.GetText)), NotificationOption2.Silent)

        Public Shared ReadOnly [Default] As New VisualBasicIdeCodeStyleOptions()

        <DataMember(Order:=BaseMemberCount + 0)>
        Public ReadOnly PreferredModifierOrder As CodeStyleOption2(Of String)

        <DataMember(Order:=BaseMemberCount + 1)>
        Public ReadOnly PreferIsNotExpression As CodeStyleOption2(Of Boolean)

        <DataMember(Order:=BaseMemberCount + 2)>
        Public ReadOnly PreferSimplifiedObjectCreation As CodeStyleOption2(Of Boolean)

        <DataMember(Order:=BaseMemberCount + 3)>
        Public ReadOnly UnusedValueExpressionStatement As CodeStyleOption2(Of UnusedValuePreference)

        <DataMember(Order:=BaseMemberCount + 4)>
        Public ReadOnly UnusedValueAssignment As CodeStyleOption2(Of UnusedValuePreference)

#Disable Warning IDE1006 ' Record Naming Style
        Public Sub New(
            Optional Common As CommonOptions = Nothing,
            Optional PreferredModifierOrder As CodeStyleOption2(Of String) = Nothing,
            Optional PreferIsNotExpression As CodeStyleOption2(Of Boolean) = Nothing,
            Optional PreferSimplifiedObjectCreation As CodeStyleOption2(Of Boolean) = Nothing,
            Optional UnusedValueExpressionStatement As CodeStyleOption2(Of UnusedValuePreference) = Nothing,
            Optional UnusedValueAssignment As CodeStyleOption2(Of UnusedValuePreference) = Nothing)
#Enable Warning

            MyBase.New(Common)

            Me.PreferredModifierOrder = If(PreferredModifierOrder, s_defaultModifierOrder)
            Me.PreferIsNotExpression = If(PreferIsNotExpression, s_trueWithSuggestionEnforcement)
            Me.PreferSimplifiedObjectCreation = If(PreferSimplifiedObjectCreation, s_trueWithSuggestionEnforcement)
            Me.UnusedValueExpressionStatement = If(UnusedValueExpressionStatement, s_unusedLocalVariableWithSilentEnforcement)
            Me.UnusedValueAssignment = If(UnusedValueAssignment, s_unusedLocalVariableWithSuggestionEnforcement)
        End Sub
    End Class
End Namespace

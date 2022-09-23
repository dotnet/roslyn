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
        Implements IEquatable(Of VisualBasicIdeCodeStyleOptions)

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

        Public Shared ReadOnly [Default] As New VisualBasicIdeCodeStyleOptions(CommonOptions.Default)

        <DataMember> Public ReadOnly PreferredModifierOrder As CodeStyleOption2(Of String)
        <DataMember> Public ReadOnly PreferIsNotExpression As CodeStyleOption2(Of Boolean)
        <DataMember> Public ReadOnly PreferSimplifiedObjectCreation As CodeStyleOption2(Of Boolean)
        <DataMember> Public ReadOnly UnusedValueExpressionStatement As CodeStyleOption2(Of UnusedValuePreference)
        <DataMember> Public ReadOnly UnusedValueAssignment As CodeStyleOption2(Of UnusedValuePreference)

#Disable Warning IDE1006 ' Parameter names must match field names for serialization
        Public Sub New(
            Common As CommonOptions,
            Optional PreferredModifierOrder As CodeStyleOption2(Of String) = Nothing,
            Optional PreferIsNotExpression As CodeStyleOption2(Of Boolean) = Nothing,
            Optional PreferSimplifiedObjectCreation As CodeStyleOption2(Of Boolean) = Nothing,
            Optional UnusedValueExpressionStatement As CodeStyleOption2(Of UnusedValuePreference) = Nothing,
            Optional UnusedValueAssignment As CodeStyleOption2(Of UnusedValuePreference) = Nothing)
#Enable Warning

            Me.Common = Common
            Me.PreferredModifierOrder = If(PreferredModifierOrder, s_defaultModifierOrder)
            Me.PreferIsNotExpression = If(PreferIsNotExpression, s_trueWithSuggestionEnforcement)
            Me.PreferSimplifiedObjectCreation = If(PreferSimplifiedObjectCreation, s_trueWithSuggestionEnforcement)
            Me.UnusedValueExpressionStatement = If(UnusedValueExpressionStatement, s_unusedLocalVariableWithSilentEnforcement)
            Me.UnusedValueAssignment = If(UnusedValueAssignment, s_unusedLocalVariableWithSuggestionEnforcement)
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, VisualBasicIdeCodeStyleOptions))
        End Function

        Public Overloads Function Equals(other As VisualBasicIdeCodeStyleOptions) As Boolean Implements IEquatable(Of VisualBasicIdeCodeStyleOptions).Equals
            Return other IsNot Nothing AndAlso
                   Common.Equals(other.Common) AndAlso
                   PreferredModifierOrder.Equals(other.PreferredModifierOrder) AndAlso
                   PreferIsNotExpression.Equals(other.PreferIsNotExpression) AndAlso
                   PreferSimplifiedObjectCreation.Equals(other.PreferSimplifiedObjectCreation) AndAlso
                   UnusedValueExpressionStatement.Equals(other.UnusedValueExpressionStatement) AndAlso
                   UnusedValueAssignment.Equals(other.UnusedValueAssignment)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Common,
                   Hash.Combine(PreferredModifierOrder,
                   Hash.Combine(PreferIsNotExpression,
                   Hash.Combine(PreferSimplifiedObjectCreation,
                   Hash.Combine(UnusedValueExpressionStatement,
                   Hash.Combine(UnusedValueAssignment, 0))))))
        End Function
    End Class
End Namespace

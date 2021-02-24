' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend Module ModifierFlagsExtensions
        Private ReadOnly s_modifierDefinitions As New SortedList(Of ModifierFlags, SyntaxKind) From {
            {ModifierFlags.Partial, SyntaxKind.PartialKeyword},
            {ModifierFlags.Default, SyntaxKind.DefaultKeyword},
            {ModifierFlags.Private, SyntaxKind.PrivateKeyword},
            {ModifierFlags.Protected, SyntaxKind.ProtectedKeyword},
            {ModifierFlags.Public, SyntaxKind.PublicKeyword},
            {ModifierFlags.Friend, SyntaxKind.FriendKeyword},
            {ModifierFlags.MustOverride, SyntaxKind.MustOverrideKeyword},
            {ModifierFlags.Overridable, SyntaxKind.OverridableKeyword},
            {ModifierFlags.NotOverridable, SyntaxKind.NotOverridableKeyword},
            {ModifierFlags.Overrides, SyntaxKind.OverridesKeyword},
            {ModifierFlags.MustInherit, SyntaxKind.MustInheritKeyword},
            {ModifierFlags.NotInheritable, SyntaxKind.NotInheritableKeyword},
            {ModifierFlags.Static, SyntaxKind.StaticKeyword},
            {ModifierFlags.Shared, SyntaxKind.SharedKeyword},
            {ModifierFlags.Shadows, SyntaxKind.ShadowsKeyword},
            {ModifierFlags.ReadOnly, SyntaxKind.ReadOnlyKeyword},
            {ModifierFlags.WriteOnly, SyntaxKind.WriteOnlyKeyword},
            {ModifierFlags.Dim, SyntaxKind.DimKeyword},
            {ModifierFlags.Const, SyntaxKind.ConstKeyword},
            {ModifierFlags.WithEvents, SyntaxKind.WithEventsKeyword},
            {ModifierFlags.Widening, SyntaxKind.WideningKeyword},
            {ModifierFlags.Narrowing, SyntaxKind.NarrowingKeyword},
            {ModifierFlags.Custom, SyntaxKind.CustomKeyword},
            {ModifierFlags.ByVal, SyntaxKind.ByValKeyword},
            {ModifierFlags.ByRef, SyntaxKind.ByRefKeyword},
            {ModifierFlags.Optional, SyntaxKind.OptionalKeyword},
            {ModifierFlags.ParamArray, SyntaxKind.ParamArrayKeyword}
        }

        <Extension>
        Public Function GetModifierFlags(member As StatementSyntax) As ModifierFlags
            Dim result As ModifierFlags = 0

            For Each modifier In member.GetModifiers()
                Select Case modifier.Kind
                    Case SyntaxKind.PartialKeyword
                        result = result Or ModifierFlags.Partial
                    Case SyntaxKind.DefaultKeyword
                        result = result Or ModifierFlags.Default
                    Case SyntaxKind.PrivateKeyword
                        result = result Or ModifierFlags.Private
                    Case SyntaxKind.ProtectedKeyword
                        result = result Or ModifierFlags.Protected
                    Case SyntaxKind.PublicKeyword
                        result = result Or ModifierFlags.Public
                    Case SyntaxKind.FriendKeyword
                        result = result Or ModifierFlags.Friend
                    Case SyntaxKind.MustOverrideKeyword
                        result = result Or ModifierFlags.MustOverride
                    Case SyntaxKind.OverridableKeyword
                        result = result Or ModifierFlags.Overridable
                    Case SyntaxKind.NotOverridableKeyword
                        result = result Or ModifierFlags.NotOverridable
                    Case SyntaxKind.OverridesKeyword
                        result = result Or ModifierFlags.Overrides
                    Case SyntaxKind.MustInheritKeyword
                        result = result Or ModifierFlags.MustInherit
                    Case SyntaxKind.NotInheritableKeyword
                        result = result Or ModifierFlags.NotInheritable
                    Case SyntaxKind.StaticKeyword
                        result = result Or ModifierFlags.Static
                    Case SyntaxKind.SharedKeyword
                        result = result Or ModifierFlags.Shared
                    Case SyntaxKind.ShadowsKeyword
                        result = result Or ModifierFlags.Shadows
                    Case SyntaxKind.ReadOnlyKeyword
                        result = result Or ModifierFlags.ReadOnly
                    Case SyntaxKind.WriteOnlyKeyword
                        result = result Or ModifierFlags.WriteOnly
                    Case SyntaxKind.DimKeyword
                        result = result Or ModifierFlags.Dim
                    Case SyntaxKind.ConstKeyword
                        result = result Or ModifierFlags.Const
                    Case SyntaxKind.WithEventsKeyword
                        result = result Or ModifierFlags.WithEvents
                    Case SyntaxKind.WideningKeyword
                        result = result Or ModifierFlags.Widening
                    Case SyntaxKind.NarrowingKeyword
                        result = result Or ModifierFlags.Narrowing
                    Case SyntaxKind.CustomKeyword
                        result = result Or ModifierFlags.Custom
                End Select
            Next

            Return result
        End Function

        <Extension>
        Public Function GetModifierFlags(parameter As ParameterSyntax) As ModifierFlags
            Dim result As ModifierFlags = 0

            For Each modifier In parameter.Modifiers
                Select Case modifier.Kind
                    Case SyntaxKind.ByValKeyword
                        result = result Or ModifierFlags.ByVal
                    Case SyntaxKind.ByRefKeyword
                        result = result Or ModifierFlags.ByRef
                    Case SyntaxKind.OptionalKeyword
                        result = result Or ModifierFlags.Optional
                    Case SyntaxKind.ParamArrayKeyword
                        result = result Or ModifierFlags.ParamArray
                End Select
            Next

            Return result
        End Function

        <Extension>
        Public Function UpdateModifiers(member As StatementSyntax, flags As ModifierFlags) As StatementSyntax
            ' The starting token for this member may change, so we need to save
            ' the leading trivia and reattach it after updating the modifiers.
            ' We also need to remove it here to avoid duplicates.
            Dim leadingTrivia = member.GetLeadingTrivia()
            member = member.WithoutLeadingTrivia()

            Dim newModifierList = New List(Of SyntaxToken)
            For Each modifierDefinition In s_modifierDefinitions
                If (flags And modifierDefinition.Key) <> 0 Then
                    newModifierList.Add(SyntaxFactory.Token(modifierDefinition.Value))
                End If
            Next

            Dim newModifiers = SyntaxFactory.TokenList(newModifierList)

            Return member.WithModifiers(SyntaxFactory.TokenList(newModifierList)) _
                         .WithLeadingTrivia(leadingTrivia)
        End Function

        <Extension>
        Public Function UpdateModifiers(parameter As ParameterSyntax, flags As ModifierFlags) As ParameterSyntax
            ' The starting token for this member may change, so we need to save
            ' the leading trivia and reattach it after updating the modifiers.
            ' We also need to remove it here to avoid duplicates.
            Dim leadingTrivia = parameter.GetLeadingTrivia()
            parameter = parameter.WithoutLeadingTrivia()

            Dim newModifierList = New List(Of SyntaxToken)
            For Each modifierDefinition In s_modifierDefinitions
                If (flags And modifierDefinition.Key) <> 0 Then
                    newModifierList.Add(SyntaxFactory.Token(modifierDefinition.Value))
                End If
            Next

            Dim newModifiers = SyntaxFactory.TokenList(newModifierList)

            Return parameter.WithModifiers(SyntaxFactory.TokenList(newModifierList)) _
                            .WithLeadingTrivia(leadingTrivia)
        End Function

    End Module
End Namespace

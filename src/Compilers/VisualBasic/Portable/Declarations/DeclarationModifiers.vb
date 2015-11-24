' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    <Flags()>
    Friend Enum DeclarationModifiers
        None = 0

        [Private] = 1
        [Protected] = 1 << 1
        [Friend] = 1 << 2
        [Public] = 1 << 3
        AllAccessibilityModifiers = [Private] Or [Friend] Or [Protected] Or [Public]

        [Shared] = 1 << 4

        [ReadOnly] = 1 << 5
        [WriteOnly] = 1 << 6
        AllWriteabilityModifiers = [ReadOnly] Or [WriteOnly]

        [Overrides] = 1 << 7

        [Overridable] = 1 << 8
        [MustOverride] = 1 << 9
        [NotOverridable] = 1 << 10
        AllOverrideModifiers = [Overridable] Or [MustOverride] Or [NotOverridable]

        [Overloads] = 1 << 11
        [Shadows] = 1 << 12
        AllShadowingModifiers = [Overloads] Or [Shadows]

        [Default] = 1 << 13
        [WithEvents] = 1 << 14

        [Widening] = 1 << 15
        [Narrowing] = 1 << 16
        AllConversionModifiers = [Widening] Or [Narrowing]

        [Partial] = 1 << 17
        [MustInherit] = 1 << 18
        [NotInheritable] = 1 << 19

        Async = 1 << 20
        Iterator = 1 << 21

        [Dim] = 1 << 22
        [Const] = 1 << 23
        [Static] = 1 << 24

        InvalidInNotInheritableClass = [Overridable] Or [NotOverridable] Or [MustOverride] Or [Default]
        InvalidInModule = [Protected] Or [Shared] Or [Default] Or [MustOverride] Or [Overridable] Or [Shadows] Or [Overrides]
        InvalidInInterface = AllAccessibilityModifiers Or [Shared]
    End Enum

    Friend Module DeclarationModifiersExtensions
        <Extension()>
        Friend Function ToAccessibility(modifiers As DeclarationModifiers) As Accessibility
            Select Case modifiers And DeclarationModifiers.AllAccessibilityModifiers
                Case DeclarationModifiers.Private : Return Accessibility.Private
                Case DeclarationModifiers.Public : Return Accessibility.Public
                Case DeclarationModifiers.Protected : Return Accessibility.Protected
                Case DeclarationModifiers.Friend : Return Accessibility.Friend
                Case DeclarationModifiers.Friend Or DeclarationModifiers.Protected : Return Accessibility.ProtectedOrFriend
                Case DeclarationModifiers.Private Or DeclarationModifiers.Protected : Return Accessibility.ProtectedAndFriend
                Case Else
                    ' this method shouldn't be used if modifiers contain conflicting accessibility flags
                    Throw ExceptionUtilities.UnexpectedValue(modifiers)
            End Select
        End Function
    End Module
End Namespace

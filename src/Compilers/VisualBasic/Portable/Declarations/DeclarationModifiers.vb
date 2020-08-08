' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

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
End Namespace

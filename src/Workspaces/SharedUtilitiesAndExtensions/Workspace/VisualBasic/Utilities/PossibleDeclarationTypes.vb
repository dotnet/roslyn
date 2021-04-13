' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    <Flags()>
    Friend Enum PossibleDeclarationTypes As UInteger
        Method = 1 << 0
        [Class] = 1 << 1
        [Structure] = 1 << 2
        [Interface] = 1 << 3
        [Enum] = 1 << 4
        [Delegate] = 1 << 5
        [Module] = 1 << 6

        AllTypes = [Class] Or [Structure] Or [Interface] Or [Enum] Or [Delegate] Or [Module]

        [Operator] = 1 << 7
        [Property] = 1 << 8
        Field = 1 << 9
        [Event] = 1 << 10
        ExternalMethod = 1 << 11
        ProtectedMember = 1 << 12
        OverridableMethod = 1 << 13
        Accessor = 1 << 14
        IteratorFunction = 1 << 15
        IteratorProperty = 1 << 16
    End Enum
End Namespace

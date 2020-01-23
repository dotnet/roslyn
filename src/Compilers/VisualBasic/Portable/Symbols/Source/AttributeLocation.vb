' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    <Flags>
    Friend Enum AttributeLocation
        None = 0

        [Assembly] = 1 << 0
        [Module] = 1 << 1
        Type = 1 << 2
        Method = 1 << 3
        Field = 1 << 4
        [Property] = 1 << 5
        [Event] = 1 << 6
        Parameter = 1 << 7
        [Return] = 1 << 8
        TypeParameter = 1 << 9
    End Enum
End Namespace

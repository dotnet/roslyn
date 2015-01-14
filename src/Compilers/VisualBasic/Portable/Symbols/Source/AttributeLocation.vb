' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Partial Friend Module ExpressionGenerator
        Private Enum StringPieceKind
            Normal
            NonPrintable
            Cr
            Lf
            CrLf
            NullChar
            Back
            FormFeed
            Tab
            VerticalTab
        End Enum
    End Module
End Namespace

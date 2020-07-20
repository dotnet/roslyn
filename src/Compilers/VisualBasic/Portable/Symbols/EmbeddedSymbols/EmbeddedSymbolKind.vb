' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    <Flags()>
    Friend Enum EmbeddedSymbolKind As Byte
        None = 0
        Unset = 1
        EmbeddedAttribute = 2
        VbCore = 4
        XmlHelper = 8
        All = (EmbeddedAttribute Or VbCore Or XmlHelper)

        LastValue = XmlHelper
    End Enum

End Namespace

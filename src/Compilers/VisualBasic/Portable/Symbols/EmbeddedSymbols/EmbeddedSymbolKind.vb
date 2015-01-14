' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

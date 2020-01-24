﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class LessThanGreaterThanBraceMatcher
        Inherits AbstractVisualBasicBraceMatcher

        <ImportingConstructor()>
        Public Sub New()
            MyBase.New(SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken)
        End Sub

        Protected Overrides Function AllowedForToken(token As SyntaxToken) As Boolean
            ' Note: we only need to look for XmlElementStartTag, since brace highlight looks for
            ' pairs of matching braces with the same parent.  In the case of other xml nodes, like
            ' ElementEndTag, we don't have matching tags.  Instead we have a LessThanSlash token for
            ' the </ for example.
            Dim tok = CType(token, SyntaxToken)
            Return tok.Parent.Kind <> SyntaxKind.XmlElementStartTag
        End Function
    End Class
End Namespace

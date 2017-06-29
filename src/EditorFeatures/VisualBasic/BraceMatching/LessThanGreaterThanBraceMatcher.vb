' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

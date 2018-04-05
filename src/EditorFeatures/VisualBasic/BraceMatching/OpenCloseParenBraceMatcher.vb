' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class OpenCloseParenBraceMatcher
        Inherits AbstractVisualBasicBraceMatcher

        <ImportingConstructor()>
        Public Sub New()
            MyBase.New(SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken)
        End Sub
    End Class
End Namespace

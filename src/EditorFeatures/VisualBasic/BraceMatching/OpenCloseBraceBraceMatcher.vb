' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class OpenCloseBraceBraceMatcher
        Inherits AbstractVisualBasicBraceMatcher

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        End Sub
    End Class
End Namespace

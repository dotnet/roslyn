' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.BraceMatching
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic), [Shared]>
    Friend Class OpenCloseParenBraceMatcher
        Inherits AbstractVisualBasicBraceMatcher

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken)
        End Sub
    End Class
End Namespace

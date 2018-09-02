' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
    <ExportLanguageService(GetType(IEmbeddedLanguagesProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguagesProvider
        Inherits AbstractEmbeddedLanguagesProvider

        Public Shared Instance As New VisualBasicEmbeddedLanguagesProvider()

        Public Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken,
                       SyntaxKind.InterpolatedStringTextToken,
                       VisualBasicSyntaxFactsService.Instance,
                       VisualBasicSemanticFactsService.Instance,
                       VisualBasicVirtualCharService.Instance)
        End Sub

        Friend Overrides Function EscapeText(text As String, token As SyntaxToken) As String
            ' VB has no need to escape any regex characters that would be passed in through this API.
            Return text
        End Function
    End Class
End Namespace

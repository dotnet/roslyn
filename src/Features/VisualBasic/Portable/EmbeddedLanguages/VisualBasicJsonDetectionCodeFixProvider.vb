' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.JsonDetection), [Shared]>
    Friend Class VisualBasicJsonDetectionCodeFixProvider
        Inherits AbstractJsonDetectionCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub

        Protected Overrides Sub AddComment(editor As CodeAnalysis.Editing.SyntaxEditor, stringLiteral As SyntaxToken, commentContents As String)
            EmbeddedLanguageUtilities.AddComment(editor, stringLiteral, commentContents)
        End Sub
    End Class
End Namespace

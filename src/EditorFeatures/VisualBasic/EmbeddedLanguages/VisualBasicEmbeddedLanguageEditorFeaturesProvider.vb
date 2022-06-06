' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    <ExportLanguageService(GetType(IEmbeddedLanguagesProvider), LanguageNames.VisualBasic, ServiceLayer.Editor), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageEditorFeaturesProvider
        Inherits AbstractEmbeddedLanguageEditorFeaturesProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub

        Public Overrides Function EscapeText(text As String, token As SyntaxToken) As String
            Return EmbeddedLanguageUtilities.EscapeText(text)
        End Function
    End Class
End Namespace

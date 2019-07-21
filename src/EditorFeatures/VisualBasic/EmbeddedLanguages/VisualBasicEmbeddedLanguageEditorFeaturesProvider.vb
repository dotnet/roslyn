' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
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

        Friend Overrides Function EscapeText(text As String, token As SyntaxToken) As String
            Return EmbeddedLanguageUtilities.EscapeText(text, token)
        End Function
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    <ExportLanguageService(GetType(IEmbeddedLanguageEditorFeaturesProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageEditorFeaturesProvider
        Inherits AbstractEmbeddedLanguageEditorFeaturesProvider

        Public Shared Shadows Instance As New VisualBasicEmbeddedLanguageEditorFeaturesProvider()

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub
    End Class
End Namespace

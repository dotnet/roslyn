' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    <ExportLanguageService(GetType(IEmbeddedLanguageFeaturesProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageFeaturesProvider
        Inherits AbstractEmbeddedLanguageFeaturesProvider

        Public Shared Shadows Instance As New VisualBasicEmbeddedLanguageFeaturesProvider()

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub
    End Class
End Namespace

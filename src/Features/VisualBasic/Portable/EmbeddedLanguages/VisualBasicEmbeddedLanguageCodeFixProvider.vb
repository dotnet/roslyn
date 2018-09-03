' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicEmbeddedLanguageCodeFixProvider)), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageCodeFixProvider
        Inherits AbstractEmbeddedLanguageCodeFixProvider

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguageFeaturesProvider.Instance)
        End Sub
    End Class
End Namespace

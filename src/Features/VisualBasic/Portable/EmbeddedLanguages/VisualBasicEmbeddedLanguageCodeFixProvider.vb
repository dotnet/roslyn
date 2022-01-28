' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    ' Order regex classification before json classification.  Json lights up on probable-json strings, but we don't
    ' want that to happen for APIs that are certain to be another language Like Regex.
    <ExtensionOrder(Before:=PredefinedEmbeddedLanguageClassifierNames.Json)>
    <ExportEmbeddedLanguageClassifierInternal(
        PredefinedEmbeddedLanguageClassifierNames.Regex, LanguageNames.VisualBasic, True, "Regex", "Regexp"), [Shared]>
    Friend Class VisualBasicRegexEmbeddedLanguageClassifier
        Inherits AbstractRegexEmbeddedLanguageClassifier

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub
    End Class
End Namespace

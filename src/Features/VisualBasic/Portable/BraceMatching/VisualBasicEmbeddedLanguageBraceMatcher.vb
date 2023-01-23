' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.BraceMatching
Imports Microsoft.CodeAnalysis.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageBraceMatcher
        Inherits AbstractEmbeddedLanguageBraceMatcher

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
                <ImportMany> services As IEnumerable(Of Lazy(Of IEmbeddedLanguageBraceMatcher, EmbeddedLanguageMetadata)))
            MyBase.New(LanguageNames.VisualBasic, VisualBasicEmbeddedLanguagesProvider.Info, VisualBasicSyntaxKinds.Instance, services)
        End Sub
    End Class
End Namespace

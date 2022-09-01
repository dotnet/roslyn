' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(AggregateEmbeddedLanguageCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(InternalsVisibleToCompletionProvider))>
    <[Shared]>
    Friend Class AggregateEmbeddedLanguageCompletionProvider
        Inherits AbstractAggregateEmbeddedLanguageCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(<ImportMany> languageServices As IEnumerable(Of Lazy(Of ILanguageService, LanguageServiceMetadata)))
            MyBase.New(languageServices, LanguageNames.VisualBasic)
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace

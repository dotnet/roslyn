' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets

    <ExportLanguageService(GetType(ISnippetExpansionClientFactory), LanguageNames.VisualBasic)>
    <[Shared]>
    Friend NotInheritable Class VisualBasicSnippetExpansionClientFactory
        Inherits AbstractSnippetExpansionClientFactory

        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _signatureHelpControllerProvider As SignatureHelpControllerProvider
        Private ReadOnly _editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory
        Private ReadOnly _editorAdaptersFactoryService As IVsEditorAdaptersFactoryService
        Private ReadOnly _argumentProviders As ImmutableArray(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata))
        Private ReadOnly _editorOptionsService As EditorOptionsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            threadingContext As IThreadingContext,
            signatureHelpControllerProvider As SignatureHelpControllerProvider,
            editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory,
            editorAdaptersFactoryService As IVsEditorAdaptersFactoryService,
            <ImportMany> argumentProviders As IEnumerable(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata)),
            editorOptionsService As EditorOptionsService)

            _threadingContext = threadingContext
            _signatureHelpControllerProvider = signatureHelpControllerProvider
            _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory
            _editorAdaptersFactoryService = editorAdaptersFactoryService
            _argumentProviders = argumentProviders.ToImmutableArray()
            _editorOptionsService = editorOptionsService
        End Sub

        Protected Overrides Function CreateSnippetExpansionClient(textView As ITextView, subjectBuffer As ITextBuffer) As AbstractSnippetExpansionClient
            Return New SnippetExpansionClient(
                _threadingContext,
                Guids.VisualBasicDebuggerLanguageId,
                textView,
                subjectBuffer,
                _signatureHelpControllerProvider,
                _editorCommandHandlerServiceFactory,
                _editorAdaptersFactoryService,
                _argumentProviders,
                _editorOptionsService)
        End Function
    End Class

End Namespace

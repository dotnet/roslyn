' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Remote
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.LanguageServer.Client
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.LanguageClient

    ' currently, platform doesn't allow multiple content types
    ' to be associated with 1 ILanguageClient forcing us to
    ' create multiple ILanguageClients for each content type
    ' https://devdiv.visualstudio.com/DevDiv/_workitems/edit/952373
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Export(GetType(ILanguageClient))>
    <ExportMetadata("Capabilities", "WorkspaceStreamingSymbolProvider")>
    Friend Class VisualBasicLanguageServerClient
        Inherits AbstractLanguageServerClient

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext,
                       workspace As VisualStudioWorkspace,
                       <ImportMany> lazyOptions As IEnumerable(Of Lazy(Of IOptionPersister)),
                       eventListener As LanguageServerClientEventListener,
                       listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext,
                       workspace,
                       lazyOptions,
                       eventListener,
                       listenerProvider,
                       languageServerName:=WellKnownServiceHubServices.VisualBasicLanguageServer,
                       serviceHubClientName:="ManagedLanguage.IDE.VisualBasicLanguageServer")
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return BasicVSResources.Visual_Basic_language_server_client
            End Get
        End Property
    End Class
End Namespace

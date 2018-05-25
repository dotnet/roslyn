' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports System.Composition

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    ' HACK: The Export attribute (As ISnippetInfoService) is used by EditorTestApp to create this
    ' SnippetInfoService on the UI thread.
    <Export(GetType(ISnippetInfoService))>
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSnippetInfoService
        Inherits AbstractSnippetInfoService

        <ImportingConstructor>
        Public Sub New(serviceProvider As SVsServiceProvider, listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(serviceProvider, Guids.VisualBasicDebuggerLanguageId, listenerProvider)
        End Sub
    End Class
End Namespace

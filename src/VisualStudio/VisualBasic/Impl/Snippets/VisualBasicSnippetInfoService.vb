' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    ' HACK: The Export attribute (As ISnippetInfoService) is used by EditorTestApp to create this
    ' SnippetInfoService on the UI thread.
    <Export(GetType(ISnippetInfoService))>
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSnippetInfoService
        Inherits AbstractSnippetInfoService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, serviceProvider As SVsServiceProvider, listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, serviceProvider, Guids.VisualBasicDebuggerLanguageId, listenerProvider)
        End Sub
    End Class
End Namespace

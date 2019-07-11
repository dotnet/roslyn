' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class TestVisualBasicSnippetInfoService
        Inherits VisualBasicSnippetInfoService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, Nothing, listenerProvider)
        End Sub

        Friend Sub SetSnippetShortcuts(newSnippetShortcuts As String())
            SyncLock cacheGuard
                snippets = newSnippetShortcuts.Select(Function(shortcut) New SnippetInfo(shortcut, "title", "description", "path")).ToImmutableArray()
                snippetShortcuts = GetShortcutsHashFromSnippets(snippets)
            End SyncLock
        End Sub
    End Class
End Namespace


' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class TestVisualBasicSnippetInfoService
        Inherits VisualBasicSnippetInfoService

        <ImportingConstructor>
        Friend Sub New(<ImportMany> asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
            MyBase.New(Nothing, asyncListeners)
        End Sub

        Friend Async Function SetSnippetShortcuts(newSnippetShortcuts As String()) As Task
            Await InitialCachePopulationTask

            SyncLock cacheGuard
                snippets = newSnippetShortcuts.Select(Function(shortcut) New SnippetInfo(shortcut, "title", "description", "path")).ToList()
                snippetShortcuts = GetShortcutsHashFromSnippets(snippets)
            End SyncLock
        End Function
    End Class
End Namespace


' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Snippets

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.CSharp, ServiceLayer.Test), [Shared], PartNotDiscoverable>
    Friend Class TestCSharpSnippetInfoService
        Inherits CSharpSnippetInfoService

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

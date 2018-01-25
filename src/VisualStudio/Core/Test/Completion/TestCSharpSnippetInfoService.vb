﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.CSharp), [Shared]>
    Friend Class TestCSharpSnippetInfoService
        Inherits CSharpSnippetInfoService

        <ImportingConstructor>
        Friend Sub New(<ImportMany> asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
            MyBase.New(Nothing, asyncListeners)
        End Sub

        Friend Sub SetSnippetShortcuts(newSnippetShortcuts As String())
            SyncLock cacheGuard
                snippets = newSnippetShortcuts.Select(Function(shortcut) New SnippetInfo(shortcut, "title", "description", "path")).ToImmutableArray()
                snippetShortcuts = GetShortcutsHashFromSnippets(snippets)
            End SyncLock
        End Sub
    End Class
End Namespace


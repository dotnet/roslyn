' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.ProjectSystem
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Roslyn.Test.Utilities
Imports IVsAsyncFileChangeEx2 = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx2

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <UseExportProvider>
    Public NotInheritable Class FileChangeWatcherTests
        Implements IDisposable

        Private ReadOnly _tempPath As String

        Public Sub New()
            _tempPath = Path.Combine(TempRoot.Root, Path.GetRandomFileName())
            Directory.CreateDirectory(_tempPath)
        End Sub

        Private Sub Dispose() Implements IDisposable.Dispose
            Directory.Delete(_tempPath, recursive:=True)
        End Sub

        <WpfFact>
        Public Async Function WatchingMultipleContexts() As Task
            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))

                Dim context1 = fileChangeWatcher.CreateContext(ImmutableArray.Create(New WatchedDirectory(_tempPath, ImmutableArray(Of String).Empty)))
                Dim context2 = fileChangeWatcher.CreateContext(ImmutableArray.Create(New WatchedDirectory(_tempPath, ImmutableArray(Of String).Empty)))

                Dim handler1Called As Boolean = False
                Dim handler2Called As Boolean = False

                AddHandler context1.FileChanged, Sub(sender, args) handler1Called = True
                AddHandler context2.FileChanged, Sub(sender, args) handler2Called = True

                Dim watchedFile1 = context1.EnqueueWatchingFile("file1.txt")
                Dim watchedFile2 = context2.EnqueueWatchingFile("file2.txt")

                Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()

                fileChangeService.FireUpdate("file2.txt")

                Assert.False(handler1Called)
                Assert.True(handler2Called)
            End Using
        End Function
    End Class
End Namespace

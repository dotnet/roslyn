' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend Class MockVsFileChangeEx
        Implements IVsFileChangeEx
        Implements IVsAsyncFileChangeEx2

        Private ReadOnly _lock As New Object
        Private _watchedFiles As ImmutableList(Of WatchedEntity) = ImmutableList(Of WatchedEntity).Empty
        Private _watchedDirectories As ImmutableList(Of WatchedEntity) = ImmutableList(Of WatchedEntity).Empty
        Private _nextCookie As UInteger

        Public Function AdviseDirChange(pszDir As String, fWatchSubDir As Integer, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseDirChange
            If fWatchSubDir = 0 Then
                Throw New NotImplementedException("Watching a single directory but not subdirectories is not implemented in this mock.")
            End If

            pvsCookie = AdviseDirectoryOrFileChange(_watchedDirectories, pszDir, pFCE)
            Return VSConstants.S_OK
        End Function

        Public Function AdviseFileChange(pszMkDocument As String, grfFilter As UInteger, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseFileChange
            If (grfFilter And _VSFILECHANGEFLAGS.VSFILECHG_Time) <> _VSFILECHANGEFLAGS.VSFILECHG_Time Then
                Throw New NotImplementedException()
            End If

            pvsCookie = AdviseDirectoryOrFileChange(_watchedFiles, pszMkDocument, pFCE)
            Return VSConstants.S_OK
        End Function

        Private Function AdviseDirectoryOrFileChange(ByRef watchedList As ImmutableList(Of WatchedEntity),
                                                     pszMkDocument As String,
                                                     pFCE As IVsFileChangeEvents) As UInteger

            SyncLock _lock
                Dim cookie = _nextCookie
                watchedList = watchedList.Add(New WatchedEntity(cookie, pszMkDocument, DirectCast(pFCE, IVsFreeThreadedFileChangeEvents2)))
                _nextCookie += 1UI

                Return cookie
            End SyncLock
        End Function

        Public Function IgnoreFile(VSCOOKIE As UInteger, pszMkDocument As String, fIgnore As Integer) As Integer Implements IVsFileChangeEx.IgnoreFile
            Throw New NotImplementedException()
        End Function

        Public Function SyncFile(pszMkDocument As String) As Integer Implements IVsFileChangeEx.SyncFile
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseDirChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseDirChange
            SyncLock _lock
                _watchedDirectories = _watchedDirectories.RemoveAll(Function(t) t.Cookie = VSCOOKIE)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Function UnadviseFileChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseFileChange
            SyncLock _lock
                _watchedFiles = _watchedFiles.RemoveAll(Function(t) t.Cookie = VSCOOKIE)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Sub FireUpdate(filename As String)
            FireUpdate(filename, _watchedFiles, _watchedDirectories)
        End Sub

        ''' <summary>
        ''' Raises a file change that raised after <paramref name="unsubscribingAction"/> is ran.
        ''' </summary>
        ''' <remarks>
        ''' File change notifications are inherently asynchronous -- it's always possible that a file change
        ''' notification may have been started for something we unsubscribe a moment later -- there's always a window of time
        ''' between the final check and the entry into Roslyn code which is unavoidable as long as notifications aren't called
        ''' by the shell under a lock, which they aren't for deadlock reasons.
        ''' </remarks>
        Public Sub FireStaleUpdate(filename As String, unsubscribingAction As Action)
            Dim watchedFiles = _watchedFiles
            Dim watchedDirectories = _watchedDirectories

            unsubscribingAction()

            FireUpdate(filename, watchedFiles, watchedDirectories)
        End Sub

        Private Shared Sub FireUpdate(filename As String, watchedFiles As ImmutableList(Of WatchedEntity), watchedDirectories As ImmutableList(Of WatchedEntity))
            Dim actionsToFire As List(Of Action) = New List(Of Action)()

            For Each watchedFile In watchedFiles
                If String.Equals(watchedFile.Path, filename, StringComparison.OrdinalIgnoreCase) Then
                    actionsToFire.Add(Sub()
                                          watchedFile.Sink.FilesChanged(1, {watchedFile.Path}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
                                      End Sub)
                End If
            Next

            For Each watchedDirectory In watchedDirectories
                If FileNameMatchesFilter(filename, watchedDirectory) Then
                    actionsToFire.Add(Sub()
                                          watchedDirectory.Sink.DirectoryChangedEx2(watchedDirectory.Path, 1, {filename}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
                                      End Sub)
                End If
            Next

            If actionsToFire.Count > 0 Then
                For Each actionToFire In actionsToFire
                    actionToFire()
                Next
            Else
                Throw New InvalidOperationException($"There is no subscription for file {filename}. Is the test authored correctly?")
            End If
        End Sub

        Private Shared Function FileNameMatchesFilter(filename As String, watchedDirectory As WatchedEntity) As Boolean
            If Not filename.StartsWith(watchedDirectory.Path, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If

            If watchedDirectory.ExtensionFilters Is Nothing Then
                ' We have no extension filter, so good
                Return True
            End If

            For Each extension In watchedDirectory.ExtensionFilters
                If filename.EndsWith(extension) Then
                    Return True
                End If
            Next

            ' Didn't match the extension
            Return False
        End Function

        Public Function AdviseFileChangeAsync(filename As String, filter As _VSFILECHANGEFLAGS, sink As IVsFreeThreadedFileChangeEvents2, Optional cancellationToken As CancellationToken = Nothing) As Task(Of UInteger) Implements IVsAsyncFileChangeEx.AdviseFileChangeAsync
            Dim cookie As UInteger
            Marshal.ThrowExceptionForHR(AdviseFileChange(filename, CType(filter, UInteger), sink, cookie))
            Return Task.FromResult(cookie)
        End Function

        Public Function AdviseFileChangesAsync(filenames As IReadOnlyCollection(Of String), filter As _VSFILECHANGEFLAGS, sink As IVsFreeThreadedFileChangeEvents2, cancellationToken As CancellationToken) As Task(Of UInteger()) Implements IVsAsyncFileChangeEx2.AdviseFileChangesAsync
            Dim cookies As New List(Of UInteger)()

            SyncLock _lock
                For Each filename In filenames
                    Dim cookie As UInteger
                    Marshal.ThrowExceptionForHR(AdviseFileChange(filename, CType(filter, UInteger), sink, cookie))

                    cookies.Add(cookie)
                Next
            End SyncLock

            Return Task.FromResult(cookies.ToArray())
        End Function

        Public Function UnadviseFileChangeAsync(cookie As UInteger, Optional cancellationToken As CancellationToken = Nothing) As Task(Of String) Implements IVsAsyncFileChangeEx.UnadviseFileChangeAsync
            SyncLock _lock
                Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Cookie = cookie).Path

                Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                Return Task.FromResult(path)
            End SyncLock
        End Function

        Public Function UnadviseFileChangesAsync(cookies As IReadOnlyCollection(Of UInteger), Optional cancellationToken As CancellationToken = Nothing) As Task(Of String()) Implements IVsAsyncFileChangeEx.UnadviseFileChangesAsync
            Dim paths As New List(Of String)()

            SyncLock _lock
                For Each cookie In cookies
                    Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Cookie = cookie).Path

                    Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                    paths.Add(path)
                Next
            End SyncLock

            Return Task.FromResult(paths.ToArray())
        End Function

        Public Function AdviseDirChangeAsync(directory As String, watchSubdirectories As Boolean, sink As IVsFreeThreadedFileChangeEvents2, Optional cancellationToken As CancellationToken = Nothing) As Task(Of UInteger) Implements IVsAsyncFileChangeEx.AdviseDirChangeAsync
            Dim cookie As UInteger
            Marshal.ThrowExceptionForHR(AdviseDirChange(directory, If(watchSubdirectories, 1, 0), sink, cookie))
            Return Task.FromResult(cookie)
        End Function

        Public Function UnadviseDirChangeAsync(cookie As UInteger, Optional cancellationToken As CancellationToken = Nothing) As Task(Of String) Implements IVsAsyncFileChangeEx.UnadviseDirChangeAsync
            SyncLock _lock
                Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Cookie = cookie).Path

                Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                Return Task.FromResult(path)
            End SyncLock
        End Function

        Public Function UnadviseDirChangesAsync(cookies As IReadOnlyCollection(Of UInteger), Optional cancellationToken As CancellationToken = Nothing) As Task(Of String()) Implements IVsAsyncFileChangeEx.UnadviseDirChangesAsync
            Dim paths As New List(Of String)()

            SyncLock _lock
                For Each cookie In cookies
                    Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Cookie = cookie).Path

                    Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                    paths.Add(path)
                Next
            End SyncLock

            Return Task.FromResult(paths.ToArray())
        End Function

        Public Function SyncFileAsync(filename As String, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.SyncFileAsync
            Throw New NotImplementedException()
        End Function

        Public Function IgnoreFileAsync(cookie As UInteger, filename As String, ignore As Boolean, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.IgnoreFileAsync
            Throw New NotImplementedException()
        End Function

        Public Function IgnoreDirAsync(directory As String, ignore As Boolean, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.IgnoreDirAsync
            Throw New NotImplementedException()
        End Function

        Public Function FilterDirectoryChangesAsync(cookie As UInteger, extensions As String(), cancellationToken As CancellationToken) As Task Implements IVsAsyncFileChangeEx.FilterDirectoryChangesAsync
            _watchedDirectories.FirstOrDefault(Function(t) t.Cookie = cookie).ExtensionFilters = extensions
            Return Task.CompletedTask
        End Function

        Public ReadOnly Property WatchedFileCount As Integer
            Get
                SyncLock _lock
                    Return _watchedFiles.Count
                End SyncLock
            End Get
        End Property
    End Class

    Friend Class WatchedEntity
        Public ReadOnly Cookie As UInteger
        Public ReadOnly Path As String
        Public ReadOnly Sink As IVsFreeThreadedFileChangeEvents2
        Public ExtensionFilters As String()

        Public Sub New(cookie As UInteger, path As String, sink As IVsFreeThreadedFileChangeEvents2)
            Me.Cookie = cookie
            Me.Path = path
            Me.Sink = sink
        End Sub
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class MockVsFileChangeEx
        Implements IVsFileChangeEx
        Implements IVsAsyncFileChangeEx

        Private ReadOnly _lock As New Object
        Private ReadOnly _watchedFiles As New List(Of WatchedEntity)
        Private ReadOnly _watchedDirectories As New List(Of WatchedEntity)
        Private _nextCookie As UInteger = 0UI

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

        Private Function AdviseDirectoryOrFileChange(watchedList As List(Of WatchedEntity),
                                                     pszMkDocument As String,
                                                     pFCE As IVsFileChangeEvents) As UInteger

            SyncLock _lock
                Dim cookie = _nextCookie
                watchedList.Add(New WatchedEntity(cookie, pszMkDocument, DirectCast(pFCE, IVsFreeThreadedFileChangeEvents2)))
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
                _watchedDirectories.RemoveAll(Function(t) t.Cookie = VSCOOKIE)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Function UnadviseFileChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseFileChange
            SyncLock _lock
                _watchedFiles.RemoveAll(Function(t) t.Cookie = VSCOOKIE)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Sub FireUpdate(filename As String)
            Dim actionsToFire As List(Of Action) = New List(Of Action)()

            SyncLock _lock
                For Each watchedFile In _watchedFiles
                    If String.Equals(watchedFile.Path, filename, StringComparison.OrdinalIgnoreCase) Then
                        actionsToFire.Add(Sub()
                                              watchedFile.Sink.FilesChanged(1, {watchedFile.Path}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
                                          End Sub)
                    End If
                Next

                For Each watchedDirectory In _watchedDirectories
                    If FileNameMatchesFilter(filename, watchedDirectory) Then
                        actionsToFire.Add(Sub()
                                              watchedDirectory.Sink.DirectoryChangedEx2(watchedDirectory.Path, 1, {filename}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
                                          End Sub)
                    End If
                Next
            End SyncLock

            If actionsToFire.Count > 0 Then
                For Each actionToFire In actionsToFire
                    actionToFire()
                Next
            Else
                Throw New InvalidOperationException($"There is no subscription for file {filename}. Is the test authored correctly?")
            End If
        End Sub

        Private Function FileNameMatchesFilter(filename As String, watchedDirectory As WatchedEntity) As Boolean
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

        Public Function UnadviseFileChangeAsync(cookie As UInteger, Optional cancellationToken As CancellationToken = Nothing) As Task(Of String) Implements IVsAsyncFileChangeEx.UnadviseFileChangeAsync
            SyncLock _lock
                Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Cookie = cookie).Path

                Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                Return Task.FromResult(path)
            End SyncLock
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

        Public Function SyncFileAsync(filename As String, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.SyncFileAsync
            Throw New NotImplementedException()
        End Function

        Public Function IgnoreFileAsync(cookie As UInteger, filename As String, ignore As Boolean, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.IgnoreFileAsync
            Throw New NotImplementedException()
        End Function

        Public Function IgnoreDirAsync(directory As String, ignore As Boolean, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task Implements IVsAsyncFileChangeEx.IgnoreDirAsync
            Throw New NotImplementedException()
        End Function

#Disable Warning IDE0060 ' Remove unused parameter - Implements 'IVsAsyncFileChangeEx.FilterDirectoryChangesAsync', but this method is not yet defined in current reference to shell package.
        Public Function FilterDirectoryChangesAsync(cookie As UInteger, extensions As String(), Optional cancellationToken As CancellationToken = Nothing) As Task
#Enable Warning IDE0060 ' Remove unused parameter
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

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Private ReadOnly _watchedFiles As List(Of Tuple(Of UInteger, String, IVsFreeThreadedFileChangeEvents2)) = New List(Of Tuple(Of UInteger, String, IVsFreeThreadedFileChangeEvents2))
        Private _nextCookie As UInteger = 0UI

        Public Function AdviseDirChange(pszDir As String, fWatchSubDir As Integer, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseDirChange
            Return VSConstants.S_OK
        End Function

        Public Function AdviseFileChange(pszMkDocument As String, grfFilter As UInteger, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseFileChange
            If (grfFilter And _VSFILECHANGEFLAGS.VSFILECHG_Time) <> _VSFILECHANGEFLAGS.VSFILECHG_Time Then
                Throw New NotImplementedException()
            End If

            SyncLock _lock
                pvsCookie = _nextCookie
                _watchedFiles.Add(Tuple.Create(pvsCookie, pszMkDocument, DirectCast(pFCE, IVsFreeThreadedFileChangeEvents2)))
                _nextCookie += 1UI

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Function IgnoreFile(VSCOOKIE As UInteger, pszMkDocument As String, fIgnore As Integer) As Integer Implements IVsFileChangeEx.IgnoreFile
            Throw New NotImplementedException()
        End Function

        Public Function SyncFile(pszMkDocument As String) As Integer Implements IVsFileChangeEx.SyncFile
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseDirChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseDirChange
            Return VSConstants.S_OK
        End Function

        Public Function UnadviseFileChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseFileChange
            SyncLock _lock
                Dim index = _watchedFiles.FindIndex(Function(t) t.Item1 = VSCOOKIE)
                _watchedFiles.RemoveAt(index)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Sub FireUpdate(filename As String)
            Dim subscription As Tuple(Of UInteger, String, IVsFreeThreadedFileChangeEvents2) = Nothing

            SyncLock _lock
                subscription = _watchedFiles.First(Function(t) String.Equals(t.Item2, filename, StringComparison.OrdinalIgnoreCase))
            End SyncLock

            If subscription IsNot Nothing Then

                subscription.Item3.FilesChanged(1, {subscription.Item2}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
            Else
                Throw New InvalidOperationException("There is no subscription for file " + filename)
            End If
        End Sub

        Public Function AdviseFileChangeAsync(filename As String, filter As _VSFILECHANGEFLAGS, sink As IVsFreeThreadedFileChangeEvents2, Optional cancellationToken As CancellationToken = Nothing) As Task(Of UInteger) Implements IVsAsyncFileChangeEx.AdviseFileChangeAsync
            Dim cookie As UInteger
            Marshal.ThrowExceptionForHR(AdviseFileChange(filename, CType(filter, UInteger), sink, cookie))
            Return Task.FromResult(cookie)
        End Function

        Public Function UnadviseFileChangeAsync(cookie As UInteger, Optional cancellationToken As CancellationToken = Nothing) As Task(Of String) Implements IVsAsyncFileChangeEx.UnadviseFileChangeAsync
            SyncLock _lock
                Dim path = _watchedFiles.FirstOrDefault(Function(t) t.Item1 = cookie).Item2

                Marshal.ThrowExceptionForHR(UnadviseFileChange(cookie))

                Return Task.FromResult(path)
            End SyncLock
        End Function

        Public Function AdviseDirChangeAsync(directory As String, watchSubdirectories As Boolean, sink As IVsFreeThreadedFileChangeEvents2, Optional cancellationToken As CancellationToken = Nothing) As Task(Of UInteger) Implements IVsAsyncFileChangeEx.AdviseDirChangeAsync
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseDirChangeAsync(cookie As UInteger, Optional cancellationToken As CancellationToken = Nothing) As Task(Of String) Implements IVsAsyncFileChangeEx.UnadviseDirChangeAsync
            Throw New NotImplementedException()
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

        Public ReadOnly Property WatchedFileCount As Integer
            Get
                SyncLock _lock
                    Return _watchedFiles.Count
                End SyncLock
            End Get
        End Property
    End Class
End Namespace

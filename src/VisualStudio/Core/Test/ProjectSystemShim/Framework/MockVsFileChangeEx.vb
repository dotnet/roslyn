' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class MockVsFileChangeEx
        Implements IVsFileChangeEx

        Private ReadOnly _lock As New Object
        Private ReadOnly _watchedFiles As List(Of Tuple(Of UInteger, String, IVsFileChangeEvents)) = New List(Of Tuple(Of UInteger, String, IVsFileChangeEvents))
        Private _nextCookie As UInteger = 0UI

        Public Function AdviseDirChange(pszDir As String, fWatchSubDir As Integer, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseDirChange
            Throw New NotImplementedException()
        End Function

        Public Function AdviseFileChange(pszMkDocument As String, grfFilter As UInteger, pFCE As IVsFileChangeEvents, ByRef pvsCookie As UInteger) As Integer Implements IVsFileChangeEx.AdviseFileChange
            If (grfFilter And _VSFILECHANGEFLAGS.VSFILECHG_Time) <> _VSFILECHANGEFLAGS.VSFILECHG_Time Then
                Throw New NotImplementedException()
            End If

            SyncLock _lock
                pvsCookie = _nextCookie
                _watchedFiles.Add(Tuple.Create(pvsCookie, pszMkDocument, pFCE))
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
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseFileChange(VSCOOKIE As UInteger) As Integer Implements IVsFileChangeEx.UnadviseFileChange
            SyncLock _lock
                Dim index = _watchedFiles.FindIndex(Function(t) t.Item1 = VSCOOKIE)
                _watchedFiles.RemoveAt(index)

                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Sub FireUpdate(filename As String)
            Dim subscription As Tuple(Of UInteger, String, IVsFileChangeEvents) = Nothing

            SyncLock _lock
                subscription = _watchedFiles.First(Function(t) String.Equals(t.Item2, filename, StringComparison.OrdinalIgnoreCase))
            End SyncLock

            If subscription IsNot Nothing Then
                subscription.Item3.FilesChanged(1, {subscription.Item2}, {CType(_VSFILECHANGEFLAGS.VSFILECHG_Time, UInteger)})
            Else
                Throw New InvalidOperationException("There is no subscription for file " + filename)
            End If
        End Sub

        Public ReadOnly Property WatchedFileCount As Integer
            Get
                SyncLock _lock
                    Return _watchedFiles.Count
                End SyncLock
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    ''' ;VsStatusBarWrapper
    ''' <summary>
    ''' Wrapping IVsStatusbar to update IDE status bar.
    ''' </summary>
    Friend Class VsStatusBarWrapper

        Public Sub New(ByVal vsStatusBar As IVsStatusbar)
            Debug.Assert(vsStatusBar IsNot Nothing, "Must provide IVsStatusBar!")

            _vsStatusBar = vsStatusBar
        End Sub

        Public Sub StartProgress(ByVal label As String, ByVal total As Integer)
            Debug.Assert(total > 0, "total must > 0!")
            _vsStatusBarCookie = 0
            _completed = 0
            _total = total
            _vsStatusBar.Progress(_vsStatusBarCookie, 1, label, CUInt(_completed), CUInt(_total))
        End Sub

        Public Sub UpdateProgress(ByVal label As String)
            Debug.Assert(_vsStatusBarCookie > 0, "Haven't StartProgress!")
            If _vsStatusBarCookie = 0 Then
                Exit Sub
            End If

            _completed += 1
            Debug.Assert(_completed <= _total)
            If _completed <= _total Then
                _vsStatusBar.Progress(_vsStatusBarCookie, 1, label, CUInt(_completed), CUInt(_total))
            End If
        End Sub

        Public Sub StopProgress(ByVal label As String)
            Debug.Assert(_vsStatusBarCookie > 0, "Haven't StartProgress!")
            If _vsStatusBarCookie = 0 Then
                Exit Sub
            End If

            _vsStatusBar.Progress(_vsStatusBarCookie, 0, label, CUInt(_total), CUInt(_total))
        End Sub

        Public Sub SetText(ByVal text As String)
            _vsStatusBar.SetText(text)
        End Sub

        Private _vsStatusBar As IVsStatusbar

        Private _vsStatusBarCookie As UInteger = 0
        Private _total As Integer = 0
        Private _completed As Integer

    End Class
End Namespace

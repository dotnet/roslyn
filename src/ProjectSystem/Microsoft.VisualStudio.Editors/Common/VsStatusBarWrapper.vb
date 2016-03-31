Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    ''' ;VsStatusBarWrapper
    ''' <summary>
    ''' Wrapping IVsStatusbar to update IDE status bar.
    ''' </summary>
    Friend Class VsStatusBarWrapper

        Public Sub New(ByVal vsStatusBar As IVsStatusbar)
            Debug.Assert(vsStatusBar IsNot Nothing, "Must provide IVsStatusBar!")

            m_VsStatusBar = vsStatusBar
        End Sub

        Public Sub StartProgress(ByVal label As String, ByVal total As Integer)
            Debug.Assert(total > 0, "total must > 0!")
            m_VsStatusBarCookie = 0
            m_Completed = 0
            m_Total = total
            m_VsStatusBar.Progress(m_VsStatusBarCookie, 1, label, CUInt(m_Completed), CUInt(m_Total))
        End Sub

        Public Sub UpdateProgress(ByVal label As String)
            Debug.Assert(m_VsStatusBarCookie > 0, "Haven't StartProgress!")
            If m_VsStatusBarCookie = 0 Then
                Exit Sub
            End If

            m_Completed += 1
            Debug.Assert(m_Completed <= m_Total)
            If m_Completed <= m_Total Then
                m_VsStatusBar.Progress(m_VsStatusBarCookie, 1, label, CUInt(m_Completed), CUInt(m_Total))
            End If
        End Sub

        Public Sub StopProgress(ByVal label As String)
            Debug.Assert(m_VsStatusBarCookie > 0, "Haven't StartProgress!")
            If m_VsStatusBarCookie = 0 Then
                Exit Sub
            End If

            m_VsStatusBar.Progress(m_VsStatusBarCookie, 0, label, CUInt(m_Total), CUInt(m_Total))
        End Sub

        Public Sub SetText(ByVal text As String)
            m_VsStatusBar.SetText(text)
        End Sub

        Private m_VsStatusBar As IVsStatusbar

        Private m_VsStatusBarCookie As UInteger = 0
        Private m_Total As Integer = 0
        Private m_Completed As Integer

    End Class
End Namespace

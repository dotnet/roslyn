'******************************************************************************
'* ErrorControlCustomViewProvider.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Diagnostics
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' Provides a custom view for DesignerPanel that creates an Error control for its view.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ErrorControlCustomViewProvider
        Inherits CustomViewProvider

        Private m_View As ErrorControl 'The Error control as view
        Private m_ErrorText As String    'Error text, if given
        Private m_Exception As Exception 'Erorr exception, if given


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ErrorText">The error text to display in the error control.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ErrorText As String)
            m_ErrorText = ErrorText
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Exception">The exception to display in the error control.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal Exception As Exception)
            m_Exception = Exception
        End Sub


        ''' <summary>
        ''' Returns the view control (if already created)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property View() As Control
            Get
                Return m_View
            End Get
        End Property

        ''' <summary>
        ''' Creates the view control, if it doesn't already exist
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub CreateView()
            If m_View Is Nothing Then
                If m_Exception IsNot Nothing Then
                    m_View = New ErrorControl(m_Exception)
                Else
                    m_View = New ErrorControl(m_ErrorText)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Close the view control, if not already closed
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub CloseView()
            If m_View IsNot Nothing Then
                m_View.Dispose()
                m_View = Nothing
            End If
        End Sub


#Region "Dispose/IDisposable"

        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                ' Dispose managed resources.
                CloseView()
            End If
            MyBase.Dispose(Disposing)
        End Sub

#End Region

    End Class

End Namespace

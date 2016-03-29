'******************************************************************************
'* SpecialFileCustomViewProvider.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Diagnostics
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' A provider which can create views of the type SpecialFileCustomView.  See that
    '''   class' description for more information.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class SpecialFileCustomViewProvider
        Inherits CustomViewProvider

        Private m_View As Control
        Private m_LinkText As String
        Private m_DesignerView As ApplicationDesignerView
        Private m_DesignerPanel As ApplicationDesignerPanel
        Private m_SpecialFileId As Integer


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="DesignerView">The ApplicationDesignerView which owns this view.</param>
        ''' <param name="DesignerPanel">The ApplicationDesignerPanel in which this view will be displayed.</param>
        ''' <param name="SpecialFileId">The special file ID to create when the user clicks the link.</param>
        ''' <param name="LinkText">The text of the link message to display.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal DesignerView As ApplicationDesignerView, ByVal DesignerPanel As ApplicationDesignerPanel, ByVal SpecialFileId As Integer, ByVal LinkText As String)
            Debug.Assert(DesignerView IsNot Nothing)
            m_DesignerView = DesignerView
            Debug.Assert(DesignerPanel IsNot Nothing)
            m_DesignerPanel = DesignerPanel
            m_LinkText = LinkText
            m_SpecialFileId = SpecialFileId
        End Sub

        ''' <summary>
        ''' The text of the link message to display.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property LinkText() As String
            Get
                Return m_LinkText
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerView which owns this view.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property DesignerView() As ApplicationDesignerView
            Get
                Return m_DesignerView
            End Get
        End Property

        ''' <summary>
        ''' The special file ID to create when the user clicks the link.  Used by IVsProjectSpecialFiles.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property SpecialFileId() As Integer
            Get
                Return m_SpecialFileId
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerPanel in which this view will be displayed.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property DesignerPanel() As ApplicationDesignerPanel
            Get
                Return m_DesignerPanel
            End Get
        End Property


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
                Dim NewView As New SpecialFileCustomView
                NewView.LinkLabel.SetThemedColor(m_DesignerPanel.VsUIShell5)
                NewView.SetSite(Me)

                m_View = NewView
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



    ''' <summary>
    ''' Returns the document of a special file by calling through the IVsProjectSpecialFiles interface
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class SpecialFileCustomDocumentMonikerProvider
        Inherits CustomDocumentMonikerProvider

        Private m_SpecialFileId As Integer
        Private m_DesignerView As ApplicationDesignerView

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="DesignerView">The associated ApplicationDesignerView</param>
        ''' <param name="SpecialFileId">The special file ID for IVsProjectSpecialFiles that will be used to
        '''   obtain the document filename</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal DesignerView As ApplicationDesignerView, ByVal SpecialFileId As Integer)
            If DesignerView Is Nothing Then
                Throw New ArgumentNullException("DesignerView")
            End If

            m_SpecialFileId = SpecialFileId
            m_DesignerView = DesignerView

#If DEBUG Then
            Try
                Call GetDocumentMoniker()
            Catch ex As Exception
                AppDesCommon.RethrowIfUnrecoverable(ex)
                Debug.Fail("Shouldn't be creating a SpecialFileCustomDocumentMonikerProvider instance if the requested special file ID is not supported by the project")
            End Try
#End If
        End Sub

        Public Overrides Function GetDocumentMoniker() As String
            'Ask the project for the filename (do not create if it doesn't exist)
            Dim ItemId As UInteger
            Dim SpecialFilePath As String = Nothing
            Dim hr As Integer = m_DesignerView.SpecialFiles.GetFile(m_SpecialFileId, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
            If VSErrorHandler.Succeeded(hr) AndAlso SpecialFilePath <> "" Then
                'The file is supported (it doesn't necessarily mean that it exists yet)
                Return SpecialFilePath
            Else
                Debug.Fail("Why did the call to IVsProjectSpecialFiles fail?  We shouldn't have created a SpecialFileCustomDocumentMonikerProvider instance in the first place if the project didn't support this special file id" _
                    & vbCrLf & "Hr = 0x" & Hex(hr))
                Throw New InvalidOperationException(SR.GetString(SR.APPDES_SpecialFileNotSupported))
            End If
        End Function

    End Class


End Namespace

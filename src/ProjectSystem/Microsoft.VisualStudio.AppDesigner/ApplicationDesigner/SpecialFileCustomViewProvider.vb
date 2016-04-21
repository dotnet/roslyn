' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' A provider which can create views of the type SpecialFileCustomView.  See that
    '''   class' description for more information.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class SpecialFileCustomViewProvider
        Inherits CustomViewProvider

        Private _view As Control
        Private _linkText As String
        Private _designerView As ApplicationDesignerView
        Private _designerPanel As ApplicationDesignerPanel
        Private _specialFileId As Integer


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
            _designerView = DesignerView
            Debug.Assert(DesignerPanel IsNot Nothing)
            _designerPanel = DesignerPanel
            _linkText = LinkText
            _specialFileId = SpecialFileId
        End Sub

        ''' <summary>
        ''' The text of the link message to display.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property LinkText() As String
            Get
                Return _linkText
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerView which owns this view.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property DesignerView() As ApplicationDesignerView
            Get
                Return _designerView
            End Get
        End Property

        ''' <summary>
        ''' The special file ID to create when the user clicks the link.  Used by IVsProjectSpecialFiles.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property SpecialFileId() As Integer
            Get
                Return _specialFileId
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerPanel in which this view will be displayed.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property DesignerPanel() As ApplicationDesignerPanel
            Get
                Return _designerPanel
            End Get
        End Property


        ''' <summary>
        ''' Returns the view control (if already created)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property View() As Control
            Get
                Return _view
            End Get
        End Property

        ''' <summary>
        ''' Creates the view control, if it doesn't already exist
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub CreateView()
            If _view Is Nothing Then
                Dim NewView As New SpecialFileCustomView
                NewView.LinkLabel.SetThemedColor(_designerPanel.VsUIShell5)
                NewView.SetSite(Me)

                _view = NewView
            End If
        End Sub

        ''' <summary>
        ''' Close the view control, if not already closed
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub CloseView()
            If _view IsNot Nothing Then
                _view.Dispose()
                _view = Nothing
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

        Private _specialFileId As Integer
        Private _designerView As ApplicationDesignerView

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

            _specialFileId = SpecialFileId
            _designerView = DesignerView

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
            Dim hr As Integer = _designerView.SpecialFiles.GetFile(_specialFileId, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
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

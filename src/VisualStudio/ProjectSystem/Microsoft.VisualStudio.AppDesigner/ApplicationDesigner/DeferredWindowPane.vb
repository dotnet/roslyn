'//------------------------------------------------------------------------------
'// <copyright file="DeferredWindowPane.vb" company="Microsoft">
'     Copyright (c) Microsoft Corporation.  All rights reserved.
' </copyright>                                                                
'------------------------------------------------------------------------------

Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Shell
Imports System
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ' <devdoc>
    '     A "deferred" window pane.  This window pane
    '     implements IVsDeferredDocView.
    ' </devdoc>
    Friend NotInheritable Class DeferredWindowPane
        Inherits DesignerWindowPane
        Implements IVsDeferredDocView

        Private m_provider As DeferrableWindowPaneProviderService
        Private m_surface As DesignSurface
        Private m_realPane As DesignerWindowPane

        ' <devdoc>
        '     Create a new DeferredWindowPane
        ' </devdoc>
        Sub New(ByVal provider As DeferrableWindowPaneProviderService, ByVal surface As DesignSurface)
            MyBase.New(surface)
            m_provider = provider
            m_surface = surface
        End Sub

        ' <devdoc>
        '     We do not want the default command guid logic.  It will assert for us,
        '     warning that we are trying to create the view before we should.
        ' </devdoc>
        Public Overrides ReadOnly Property CommandGuid() As Guid
            Get
                Return Guid.Empty
            End Get
        End Property

        ' <devdoc>
        '     Override for abstract Window property.  If we get this far, we are
        '     in deep trouble.  We could offer the UI, but we won't be able to do 
        '     other window pane stuff like toolbox support.  Why even try?
        ' </devdoc>
        Public Overrides ReadOnly Property Window() As System.Windows.Forms.IWin32Window
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public Sub get_CmdUIGuid(ByRef pGuidCmdId As System.Guid) Implements Shell.Interop.IVsDeferredDocView.get_CmdUIGuid
            EnsurePane()
            pGuidCmdId = m_realPane.CommandGuid
        End Sub

        Public Sub get_DocView(ByRef ppUnkDocView As System.IntPtr) Implements Shell.Interop.IVsDeferredDocView.get_DocView
            EnsurePane()
            Dim view As Object = m_realPane.EditorView
            ppUnkDocView = Marshal.GetIUnknownForObject(view)
        End Sub

        ' <devdoc>
        '     Override this because this window pane is not really
        '     providing services to the host.
        ' </devdoc>
        Protected Overrides Sub AddDefaultServices()
            '// don't call base.  We don't want the base to add any services.
        End Sub

        ' <devdoc>
        '     Returns the actual window pane.  This will throw if we could not discover the
        '     correct window pane.
        ' </devdoc>
        Private Sub EnsurePane()
            If (m_realPane Is Nothing) Then
                Dim host As IDesignerHost = TryCast(m_surface.GetService(GetType(IDesignerHost)), IDesignerHost)

                If (host IsNot Nothing AndAlso host.RootComponent Is Nothing AndAlso host.Loading) Then
                    Throw New NotSupportedException("SR.GetString(SR.DesignerLoader_NotDeferred)")
                End If

                Try
                    m_realPane = m_provider.CreateWindowPane(m_surface)
                Catch ex As Exception
                    m_realPane = New ErrorWindowPane(m_surface, ex)
                End Try
            End If
        End Sub

    End Class

    ' <devdoc>
    '     This window pane is used as the UI for cases where 
    '     the designer failed to load entirely.  If we don't display
    '     this the only alternative is an ugly message box.  We like
    '     to be pretty.
    ' </devdoc>
    Friend NotInheritable Class ErrorWindowPane
        Inherits DesignerWindowPane

        Private m_window As ErrorControl
        Private m_error As Exception

        Friend Sub New(ByVal surface As DesignSurface, ByVal ex As Exception)
            MyBase.New(surface)
            m_error = ex
        End Sub

        Public Overrides ReadOnly Property Window() As IWin32Window
            Get
                If (m_window Is Nothing) Then
                    m_window = New ErrorControl()
                    m_window.Text = m_error.Message
                End If
                Return m_window
            End Get
        End Property

        ' <devdoc>
        '     Called when our view is disposed.
        ' </devdoc>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If (disposing) Then
                If (m_window IsNot Nothing) Then
                    m_window.Dispose()
                    m_window = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

    End Class

End Namespace

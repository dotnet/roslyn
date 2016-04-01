' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' Host window for MSO toolbars
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DesignerToolbarPanel
        Inherits Panel
        Implements IVsToolWindowToolbar

        Private _toolbarHost As Microsoft.VisualStudio.Shell.Interop.IVsToolWindowToolbarHost

        ' GUID for hosted toolbar as specified in CTC file
        Private _guid As Guid

        ' ID of hosted toolbar as specified in CTC file
        Private _id As UInteger

        ' UI Shell service
        Private _uiShell As IVsUIShell

        ' If we set the toolbar before our handle is created, we have to remember to 
        ' associate the handle with the toolbar on handle create...
        Private _associateToolbarOnHandleCreate As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            MyBase.New()
            Me.Margin = New Padding(0)
            Me.Dock = DockStyle.Top
        End Sub

        ''' <summary>
        ''' Tell the shell that our toolbar should be the active toolbar
        ''' </summary>
        ''' <param name="h"></param>
        ''' <remarks></remarks>
        Public Sub Activate(ByVal h As IntPtr)
            ' It seems that designers don't set the active secondary toolbar when activated -
            ' this should take care of that!
            _toolbarHost.ProcessMouseActivation(h, win.WM_SETFOCUS, 0, 0)
        End Sub

        ''' <summary>
        ''' Set the toolbar that we want to host.
        ''' </summary>
        ''' <param name="uiShell"></param>
        ''' <param name="guid">GUID for the toolbar as specified in the CTC file</param>
        ''' <param name="id">Id for the toolbar as specified in the CTC file</param>
        ''' <remarks></remarks>
        Public Sub SetToolbar(ByVal uiShell As IVsUIShell, ByVal guid As Guid, ByVal id As UInteger)
            If uiShell Is Nothing Then
                Throw New ArgumentNullException()
            End If

            ' Keep these guys around for later...
            _guid = guid
            _id = id
            _uiShell = uiShell

            If Me.IsHandleCreated Then
                ' Fine - handle was already created, let's associate the toolbar with the handle
                InternalAssociateToolbarWithHandle()
            Else
                ' No handle created yet. Make note that we should associate the handle with the toolbar
                ' as soon as we have one!
                _associateToolbarOnHandleCreate = True
            End If
        End Sub

        ''' <summary>
        ''' If our window handle wasn't created when we called SetupToolbar, then we
        ''' need to assoicate the new window handle with the toolbar
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnHandleCreated(ByVal e As System.EventArgs)
            MyBase.OnHandleCreated(e)
            If _associateToolbarOnHandleCreate Then
                InternalAssociateToolbarWithHandle()
            End If
        End Sub

        ''' <summary>
        ''' If our window handle is destroyed, we have to disassociate the current handle
        ''' with the toolbar host (and delete the new toolbar host)
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnHandleDestroyed(ByVal e As System.EventArgs)
            MyBase.OnHandleDestroyed(e)
            If _toolbarHost IsNot Nothing Then
                ' We had a toolbar host (which should be associated with the old handle)
                ' Let's kill it and make note that if we get a new handle, we should 
                ' hook it up!
                _toolbarHost.Close(0)
                _toolbarHost = Nothing
                _associateToolbarOnHandleCreate = True
            End If
        End Sub

        ''' <summary>
        ''' Associate the current window handle with the toolbar host
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub InternalAssociateToolbarWithHandle()
            Debug.Assert(Me.IsHandleCreated, "No handle created when calling InternaleAssociateToolbarWithHandle")
            If _uiShell IsNot Nothing Then
                _uiShell.SetupToolbar(Me.Handle, Me, _toolbarHost)
            End If

            _toolbarHost.AddToolbar(VSTWT_LOCATION.VSTWT_TOP, _guid, _id)
            _toolbarHost.ShowHideToolbar(_guid, _id, 1)
            _associateToolbarOnHandleCreate = False
        End Sub

        ''' <summary>
        ''' Tell the toolbar that we host that it needs to update its size
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnSizeChanged(ByVal e As System.EventArgs)
            MyBase.OnSizeChanged(e)
            If _toolbarHost IsNot Nothing Then
                _toolbarHost.BorderChanged()
            End If
        End Sub


        ''' <summary>
        ''' WndProc for the DesignerToolbarPanel
        ''' </summary>
        ''' <param name="m"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            If m.Msg = win.WM_SETFOCUS Then
                'The DesignerToolbarPanel should never get focus, but the hosted
                '  toolbar tries to get it to us in certain situations.  
                'We want to give focus back to the control in the parent that had
                '  it before.  Go up the parent chain until we find a ContainerControl,
                '  and set WM_FOCUS to it - this causes it to respond by setting focus
                '  back to the last child control which had it.
                If Me.Parent IsNot Nothing Then
                    Dim c As Control = Me.Parent
                    While c IsNot Nothing AndAlso Not TypeOf c Is ContainerControl
                        c = c.Parent
                    End While
                    If c IsNot Nothing Then
                        NativeMethods.SetFocus(c.Handle)

                        'Skip normal processing of WM_SETFOCUS by the control
                        Return
                    End If
                End If
            End If
            MyBase.WndProc(m)
        End Sub


#Region "IVsToolWindowToolbar implementation"

        ''' <summary>
        ''' The toolbar's border is the same as our client rectangle
        ''' </summary>
        ''' <param name="borders"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetBorder(ByVal borders() As Microsoft.VisualStudio.OLE.Interop.RECT) As Integer Implements IVsToolWindowToolbar.GetBorder
            Dim rect As Drawing.Rectangle = Me.Bounds

            Debug.Assert(borders.Length = 1)
            borders(0).left = ClientRectangle.Left
            borders(0).top = ClientRectangle.Top
            borders(0).right = ClientRectangle.Right
            borders(0).bottom = ClientRectangle.Bottom

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Set our size based on what the toolbar wants
        ''' </summary>
        ''' <param name="borders"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function SetBorderSpace(ByVal borders() As Microsoft.VisualStudio.OLE.Interop.RECT) As Integer Implements IVsToolWindowToolbar.SetBorderSpace
            Debug.Assert(borders IsNot Nothing)
            Debug.Assert(borders.Length = 1)

            Me.Height = borders(0).top - borders(0).bottom

            Return NativeMethods.S_OK
        End Function

#End Region

        ''' <summary>
        ''' Cleanup
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If _toolbarHost IsNot Nothing Then
                    _toolbarHost.Close(0)
                    _toolbarHost = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

    End Class
End Namespace
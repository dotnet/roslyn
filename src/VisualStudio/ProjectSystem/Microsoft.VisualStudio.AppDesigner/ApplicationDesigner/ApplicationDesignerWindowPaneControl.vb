'******************************************************************************
'* ApplicationDesignerWindowPaneControl.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************
Imports System
Imports System.Drawing
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports win = Microsoft.VisualStudio.Editors.AppDesInterop.win
Imports Microsoft.VisualStudio.Shell.Design
Imports System.Windows.Forms
Imports System.ComponentModel.Design
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public NotInheritable Class ApplicationDesignerWindowPaneControl
        Inherits UserControl


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Selects the next available control and makes it the active control.
        ''' </summary>
        ''' <param name="forward"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ProcessTabKey(ByVal forward As Boolean) As Boolean
            Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "ApplicationDesignerWindowPaneControl.ProcessTabKey")

            If (SelectNextControl(ActiveControl, forward, True, True, False)) Then
                Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ...SelectNextControl handled it")
                Return True
            End If

            Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ...Not handled")
            Return False
        End Function

        Private Sub InitializeComponent()
            '
            'ApplicationDesignerWindowPaneControl
            '
            Me.Name = "ApplicationDesignerWindowPaneControl"
            Me.Text = "ApplicationDesignerWindowPaneControl" 'For debugging

            'We don't want scrollbars to show up on this window
            Me.AutoScroll = False

        End Sub

#If DEBUG Then
        Private Sub ApplicationDesignerWindowPaneControl_SizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.SizeChanged
            Common.Switches.TracePDFocus(TraceLevel.Info, "ApplicationDesignerWindowPaneControl_SizeChanged: " & Me.Size.ToString())
        End Sub
#End If

    End Class

End Namespace

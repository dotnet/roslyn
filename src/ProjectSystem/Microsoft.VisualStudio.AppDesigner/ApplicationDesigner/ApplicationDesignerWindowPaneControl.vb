' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports System.Windows.Forms

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

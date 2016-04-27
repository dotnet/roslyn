' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsExtensionCollisionDialog
        Private _lastFocus As Control
        Private _helpCallBack As IVBAddImportsDialogHelpCallback

        Public Sub New(ByVal [namespace] As String, ByVal identifier As String, ByVal minimallyQualifiedName As String, ByVal helpCAllback As IVBAddImportsDialogHelpCallback, ByVal isp As IServiceProvider)
            MyBase.New(isp)
            Me.SuspendLayout()
            Try
                _helpCallBack = helpCAllback
                InitializeComponent()
                txtMain_.Text = String.Format(My.Resources.AddImports.AddImportsExtensionMethodsMainFormatString, [namespace], identifier, minimallyQualifiedName)
                txtMain_.AutoSize = True

                pnlLayout_.AutoSize = True
                Me.AutoSize = True
            Finally
                Me.ResumeLayout()
            End Try
        End Sub

        Private Sub ButtonClick(ByVal sender As Object, ByVal e As EventArgs) Handles btnOk_.Click, btnCancel_.Click
            Me.Close()
        End Sub

        Private Function GetTextBoxWidth() As Integer
            Return CInt(pnlLayout_.Width - (pnlLayout_.ColumnStyles(0).Width + pnlLayout_.ColumnStyles(4).Width))
        End Function

        Private Sub ButtonGotFocus(ByVal sender As Object, ByVal e As EventArgs) Handles btnOk_.GotFocus, btnCancel_.GotFocus
            _lastFocus = CType(sender, Control)
        End Sub

        Private Sub LabelGotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtMain_.GotFocus
            _lastFocus.Focus()
        End Sub

        Private Sub ClickHelpButton(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            e.Cancel = True
            OnHelpRequested(New HelpEventArgs(Point.Empty))
        End Sub

        Private Sub RequestHelp(ByVal sender As Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles Me.HelpRequested
            If (_helpCallBack IsNot Nothing) Then
                _helpCallBack.InvokeHelp()
            End If
        End Sub
    End Class
End Namespace
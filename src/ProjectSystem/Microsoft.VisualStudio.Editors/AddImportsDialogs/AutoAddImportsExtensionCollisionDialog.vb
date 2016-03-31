Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsExtensionCollisionDialog
        Private m_lastFocus As Control
        Private m_helpCallBack As IVBAddImportsDialogHelpCallback

        Public Sub New(ByVal [namespace] As String, ByVal identifier As String, ByVal minimallyQualifiedName As String, ByVal helpCAllback As IVBAddImportsDialogHelpCallback, ByVal isp As IServiceProvider)
            MyBase.New(isp)
            Me.SuspendLayout()
            Try
                m_helpCallBack = helpCAllback
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
            m_lastFocus = CType(sender, Control)
        End Sub

        Private Sub LabelGotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtMain_.GotFocus
            m_lastFocus.Focus()
        End Sub

        Private Sub ClickHelpButton(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            e.Cancel = True
            OnHelpRequested(New HelpEventArgs(Point.Empty))
        End Sub

        Private Sub RequestHelp(ByVal sender As Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles Me.HelpRequested
            If (m_helpCallBack IsNot Nothing) Then
                m_helpCallBack.InvokeHelp()
            End If
        End Sub
    End Class
End Namespace
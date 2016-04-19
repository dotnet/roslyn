Namespace Microsoft.VisualStudio.Editors.AddImports
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class AutoAddImportsCollisionDialog
        Inherits AddImportDialogBase
        'Inherits System.Windows.Forms.Form

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AutoAddImportsCollisionDialog))
            Me.m_layoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.m_lblImportsAnyways = New System.Windows.Forms.Label
            Me.m_cancelButton = New System.Windows.Forms.Button
            Me.m_rbQualifyCurrentLine = New System.Windows.Forms.RadioButton
            Me.m_okButton = New System.Windows.Forms.Button
            Me.m_lblMain = New System.Windows.Forms.Label
            Me.m_lblQualifyCurrentLine = New System.Windows.Forms.Label
            Me.m_rbImportsAnyways = New System.Windows.Forms.RadioButton
            Me.m_layoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'm_layoutPanel
            '
            resources.ApplyResources(Me.m_layoutPanel, "m_layoutPanel")
            Me.m_layoutPanel.Controls.Add(Me.m_lblImportsAnyways, 3, 3)
            Me.m_layoutPanel.Controls.Add(Me.m_cancelButton, 5, 6)
            Me.m_layoutPanel.Controls.Add(Me.m_rbQualifyCurrentLine, 2, 4)
            Me.m_layoutPanel.Controls.Add(Me.m_okButton, 4, 6)
            Me.m_layoutPanel.Controls.Add(Me.m_lblMain, 1, 1)
            Me.m_layoutPanel.Controls.Add(Me.m_lblQualifyCurrentLine, 3, 4)
            Me.m_layoutPanel.Controls.Add(Me.m_rbImportsAnyways, 2, 3)
            Me.m_layoutPanel.Name = "m_layoutPanel"
            '
            'm_lblImportsAnyways
            '
            Me.m_layoutPanel.SetColumnSpan(Me.m_lblImportsAnyways, 4)
            resources.ApplyResources(Me.m_lblImportsAnyways, "m_lblImportsAnyways")
            Me.m_lblImportsAnyways.MaximumSize = New System.Drawing.Size(350, 0)
            Me.m_lblImportsAnyways.Name = "m_lblImportsAnyways"
            '
            'm_cancelButton
            '
            resources.ApplyResources(Me.m_cancelButton, "m_cancelButton")
            Me.m_layoutPanel.SetColumnSpan(Me.m_cancelButton, 2)
            Me.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.m_cancelButton.Name = "m_cancelButton"
            Me.m_cancelButton.UseVisualStyleBackColor = True
            '
            'm_rbQualifyCurrentLine
            '
            resources.ApplyResources(Me.m_rbQualifyCurrentLine, "m_rbQualifyCurrentLine")
            Me.m_rbQualifyCurrentLine.Name = "m_rbQualifyCurrentLine"
            Me.m_rbQualifyCurrentLine.UseVisualStyleBackColor = True
            '
            'm_okButton
            '
            resources.ApplyResources(Me.m_okButton, "m_okButton")
            Me.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.m_okButton.Name = "m_okButton"
            Me.m_okButton.UseVisualStyleBackColor = True
            '
            'm_lblMain
            '
            Me.m_layoutPanel.SetColumnSpan(Me.m_lblMain, 6)
            resources.ApplyResources(Me.m_lblMain, "m_lblMain")
            Me.m_lblMain.MaximumSize = New System.Drawing.Size(409, 0)
            Me.m_lblMain.Name = "m_lblMain"
            '
            'm_lblQualifyCurrentLine
            '
            Me.m_layoutPanel.SetColumnSpan(Me.m_lblQualifyCurrentLine, 4)
            resources.ApplyResources(Me.m_lblQualifyCurrentLine, "m_lblQualifyCurrentLine")
            Me.m_lblQualifyCurrentLine.MaximumSize = New System.Drawing.Size(350, 0)
            Me.m_lblQualifyCurrentLine.Name = "m_lblQualifyCurrentLine"
            '
            'm_rbImportsAnyways
            '
            resources.ApplyResources(Me.m_rbImportsAnyways, "m_rbImportsAnyways")
            Me.m_rbImportsAnyways.Checked = True
            Me.m_rbImportsAnyways.Name = "m_rbImportsAnyways"
            Me.m_rbImportsAnyways.TabStop = True
            Me.m_rbImportsAnyways.UseVisualStyleBackColor = True
            '
            'AutoAddImportsCollisionDialog
            '
            Me.AcceptButton = Me.m_okButton
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.m_cancelButton
            Me.Controls.Add(Me.m_layoutPanel)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "AutoAddImportsCollisionDialog"
            Me.ShowInTaskbar = False
            Me.m_layoutPanel.ResumeLayout(False)
            Me.m_layoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Protected WithEvents m_layoutPanel As System.Windows.Forms.TableLayoutPanel
        Protected WithEvents m_okButton As System.Windows.Forms.Button
        Protected WithEvents m_cancelButton As System.Windows.Forms.Button
        Protected WithEvents m_rbQualifyCurrentLine As System.Windows.Forms.RadioButton
        Protected WithEvents m_lblMain As System.Windows.Forms.Label
        Protected WithEvents m_lblImportsAnyways As System.Windows.Forms.Label
        Protected WithEvents m_lblQualifyCurrentLine As System.Windows.Forms.Label
        Protected WithEvents m_rbImportsAnyways As System.Windows.Forms.RadioButton

    End Class
End Namespace
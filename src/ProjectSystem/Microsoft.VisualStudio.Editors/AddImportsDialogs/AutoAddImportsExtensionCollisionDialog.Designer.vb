Namespace Microsoft.VisualStudio.Editors.AddImports
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class AutoAddImportsExtensionCollisionDialog
        'Inherits System.Windows.Forms.Form
        Inherits AddImportDialogBase

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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AutoAddImportsExtensionCollisionDialog))
            Me.pnlLayout_ = New System.Windows.Forms.TableLayoutPanel
            Me.txtMain_ = New System.Windows.Forms.Label
            Me.btnOk_ = New System.Windows.Forms.Button
            Me.btnCancel_ = New System.Windows.Forms.Button
            Me.pnlLayout_.SuspendLayout()
            Me.SuspendLayout()
            '
            'pnlLayout_
            '
            resources.ApplyResources(Me.pnlLayout_, "pnlLayout_")
            Me.pnlLayout_.Controls.Add(Me.txtMain_, 1, 1)
            Me.pnlLayout_.Controls.Add(Me.btnOk_, 2, 3)
            Me.pnlLayout_.Controls.Add(Me.btnCancel_, 3, 3)
            Me.pnlLayout_.MaximumSize = New System.Drawing.Size(412, 0)
            Me.pnlLayout_.Name = "pnlLayout_"
            '
            'txtMain_
            '
            Me.txtMain_.BackColor = System.Drawing.SystemColors.Control
            Me.pnlLayout_.SetColumnSpan(Me.txtMain_, 3)
            Me.txtMain_.Cursor = System.Windows.Forms.Cursors.Arrow
            resources.ApplyResources(Me.txtMain_, "txtMain_")
            Me.txtMain_.Name = "txtMain_"
            '
            'btnOk_
            '
            resources.ApplyResources(Me.btnOk_, "btnOk_")
            Me.btnOk_.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.btnOk_.Name = "btnOk_"
            Me.btnOk_.UseVisualStyleBackColor = True
            '
            'btnCancel_
            '
            resources.ApplyResources(Me.btnCancel_, "btnCancel_")
            Me.btnCancel_.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.btnCancel_.Name = "btnCancel_"
            Me.btnCancel_.UseVisualStyleBackColor = True
            '
            'AutoAddImportsExtensionCollisionDialog
            '
            Me.AcceptButton = Me.btnOk_
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.btnCancel_
            Me.Controls.Add(Me.pnlLayout_)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "AutoAddImportsExtensionCollisionDialog"
            Me.ShowInTaskbar = False
            Me.pnlLayout_.ResumeLayout(False)
            Me.pnlLayout_.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Protected WithEvents pnlLayout_ As System.Windows.Forms.TableLayoutPanel
        Protected WithEvents btnOk_ As System.Windows.Forms.Button
        Protected WithEvents btnCancel_ As System.Windows.Forms.Button
        Protected WithEvents txtMain_ As System.Windows.Forms.Label

    End Class
End Namespace
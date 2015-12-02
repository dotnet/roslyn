Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class WebUrlDialog
        'Inherits System.Windows.Forms.Form
        Inherits Microsoft.VisualStudio.Editors.XmlToSchema.XmlToSchemaForm

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
            Dim okButton As System.Windows.Forms.Button
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(WebUrlDialog))
            Dim cancelButton As System.Windows.Forms.Button
            Dim Label2 As System.Windows.Forms.Label
            Dim TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
            Me._urlComboBox = New System.Windows.Forms.ComboBox
            okButton = New System.Windows.Forms.Button
            cancelButton = New System.Windows.Forms.Button
            Label2 = New System.Windows.Forms.Label
            TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel
            TableLayoutPanel2.SuspendLayout()
            Me.SuspendLayout()
            '
            'okButton
            '
            resources.ApplyResources(okButton, "okButton")
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK
            okButton.Name = "okButton"
            '
            'cancelButton
            '
            resources.ApplyResources(cancelButton, "cancelButton")
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
            cancelButton.Name = "cancelButton"
            '
            'Label2
            '
            resources.ApplyResources(Label2, "Label2")
            Label2.Name = "Label2"
            '
            'TableLayoutPanel2
            '
            resources.ApplyResources(TableLayoutPanel2, "TableLayoutPanel2")
            TableLayoutPanel2.Controls.Add(cancelButton, 2, 2)
            TableLayoutPanel2.Controls.Add(Label2, 0, 0)
            TableLayoutPanel2.Controls.Add(Me._urlComboBox, 0, 1)
            TableLayoutPanel2.Controls.Add(okButton, 1, 2)
            TableLayoutPanel2.Name = "TableLayoutPanel2"
            '
            '_urlComboBox
            '
            Me._urlComboBox.AllowDrop = True
            Me._urlComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest
            Me._urlComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.RecentlyUsedList
            TableLayoutPanel2.SetColumnSpan(Me._urlComboBox, 3)
            resources.ApplyResources(Me._urlComboBox, "_urlComboBox")
            Me._urlComboBox.FormattingEnabled = True
            Me._urlComboBox.Name = "_urlComboBox"
            '
            'WebUrlDialog
            '
            Me.AcceptButton = okButton
            Me.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = cancelButton
            Me.Controls.Add(TableLayoutPanel2)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "WebUrlDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            TableLayoutPanel2.ResumeLayout(False)
            TableLayoutPanel2.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents _urlComboBox As System.Windows.Forms.ComboBox

    End Class
End Namespace

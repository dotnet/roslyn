
Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class PasteXmlDialog
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PasteXmlDialog))
            Dim cancelButton As System.Windows.Forms.Button
            Dim Label1 As System.Windows.Forms.Label
            Dim TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
            Me._xmlTextBox = New System.Windows.Forms.TextBox
            okButton = New System.Windows.Forms.Button
            cancelButton = New System.Windows.Forms.Button
            Label1 = New System.Windows.Forms.Label
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
            'Label1
            '
            resources.ApplyResources(Label1, "Label1")
            TableLayoutPanel2.SetColumnSpan(Label1, 4)
            Label1.Name = "Label1"
            '
            'TableLayoutPanel2
            '
            resources.ApplyResources(TableLayoutPanel2, "TableLayoutPanel2")
            TableLayoutPanel2.Controls.Add(cancelButton, 3, 2)
            TableLayoutPanel2.Controls.Add(Label1, 0, 0)
            TableLayoutPanel2.Controls.Add(okButton, 2, 2)
            TableLayoutPanel2.Controls.Add(Me._xmlTextBox, 0, 1)
            TableLayoutPanel2.Name = "TableLayoutPanel2"
            '
            '_xmlTextBox
            '
            Me._xmlTextBox.AcceptsReturn = True
            TableLayoutPanel2.SetColumnSpan(Me._xmlTextBox, 4)
            resources.ApplyResources(Me._xmlTextBox, "_xmlTextBox")
            Me._xmlTextBox.Name = "_xmlTextBox"
            '
            'PasteXmlDialog
            '
            Me.AcceptButton = okButton
            Me.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = cancelButton
            Me.Controls.Add(TableLayoutPanel2)
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "PasteXmlDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            TableLayoutPanel2.ResumeLayout(False)
            TableLayoutPanel2.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents _xmlTextBox As System.Windows.Forms.TextBox

    End Class
End Namespace
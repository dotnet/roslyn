Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
        Partial Class AppDotXamlErrorControl
        Inherits System.Windows.Forms.UserControl

        'UserControl overrides dispose to clean up the component list.
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
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            Me.EditXamlButton = New System.Windows.Forms.Button
            Me.ErrorControl = New Microsoft.VisualStudio.Editors.DesignerFramework.ErrorControl
            Me.TableLayoutPanel1.SuspendLayout()
            Me.SuspendLayout()
            '
            'TableLayoutPanel1
            '
            Me.TableLayoutPanel1.AutoSize = True
            Me.TableLayoutPanel1.ColumnCount = 1
            Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.TableLayoutPanel1.Controls.Add(Me.ErrorControl, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.EditXamlButton, 0, 1)
            Me.TableLayoutPanel1.Location = New System.Drawing.Point(3, 3)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            Me.TableLayoutPanel1.RowCount = 2
            Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.TableLayoutPanel1.Size = New System.Drawing.Size(177, 108)
            Me.TableLayoutPanel1.TabIndex = 0
            '
            'ErrorControl
            '
            Me.ErrorControl.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.ErrorControl.AutoSize = True
            Me.ErrorControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.ErrorControl.Location = New System.Drawing.Point(3, 3)
            Me.ErrorControl.Name = "ErrorControl"
            Me.ErrorControl.Padding = New System.Windows.Forms.Padding(17)
            Me.ErrorControl.Size = New System.Drawing.Size(171, 73)
            Me.ErrorControl.TabIndex = 1
            '
            'EditXamlButton
            '
            Me.EditXamlButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.EditXamlButton.AutoSize = True
            Me.EditXamlButton.Location = New System.Drawing.Point(107, 82)
            Me.EditXamlButton.Name = "EditXamlButton"
            Me.EditXamlButton.Size = New System.Drawing.Size(67, 23)
            Me.EditXamlButton.TabIndex = 0
            Me.EditXamlButton.Text = "&Edit XAML"
            Me.EditXamlButton.UseVisualStyleBackColor = True
            '
            'AppDotXamlErrorControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.AutoSize = True
            Me.Controls.Add(Me.TableLayoutPanel1)
            Me.Name = "AppDotXamlErrorControl"
            Me.Size = New System.Drawing.Size(182, 114)
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Protected WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Protected WithEvents ErrorControl As Microsoft.VisualStudio.Editors.DesignerFramework.ErrorControl
        Protected WithEvents EditXamlButton As System.Windows.Forms.Button

    End Class

End Namespace

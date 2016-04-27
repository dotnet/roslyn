Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

    '<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Public Class ErrorControl
        Inherits System.Windows.Forms.UserControl

        'UserControl overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Me.IconGlyph = New System.Windows.Forms.PictureBox
            Me.ErrorText = New System.Windows.Forms.TextBox
            CType(Me.IconGlyph, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'IconGlyph
            '
            Me.IconGlyph.Location = New System.Drawing.Point(17, 17)
            Me.IconGlyph.Name = "IconGlyph"
            Me.IconGlyph.Size = New System.Drawing.Size(32, 32)
            Me.IconGlyph.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
            Me.IconGlyph.TabIndex = 0
            Me.IconGlyph.TabStop = False
            '
            'ErrorText
            '
            Me.ErrorText.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.ErrorText.BackColor = System.Drawing.SystemColors.Control
            Me.ErrorText.BorderStyle = System.Windows.Forms.BorderStyle.None
            Me.ErrorText.Location = New System.Drawing.Point(66, 17)
            Me.ErrorText.Multiline = True
            Me.ErrorText.Name = "ErrorText"
            Me.ErrorText.ReadOnly = True
            Me.ErrorText.Size = New System.Drawing.Size(83, 30)
            Me.ErrorText.TabIndex = 1
            '
            'ErrorControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.Controls.Add(Me.ErrorText)
            Me.Controls.Add(Me.IconGlyph)
            Me.Name = "ErrorControl"
            Me.Padding = New System.Windows.Forms.Padding(17)
            Me.Size = New System.Drawing.Size(170, 64)
            CType(Me.IconGlyph, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Public WithEvents IconGlyph As System.Windows.Forms.PictureBox
        Public WithEvents ErrorText As System.Windows.Forms.TextBox

    End Class

End Namespace

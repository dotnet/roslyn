' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports System.Drawing

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend Class OpenFileWarningDialog
        Inherits BaseDialog
        'Inherits System.Windows.Forms.Form

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ServiceProvider As IServiceProvider, ByVal fileName As String)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            Me.alwaysCheckCheckBox.Checked = True
            Me.messageLabel.Text = String.Format(Me.messageLabel.Text, fileName)
            Me.messageLabel.PerformLayout()
            Me.dialogLayoutPanel.PerformLayout()

            Me.ClientSize = New Size(Me.ClientSize.Width, Me.dialogLayoutPanel.Size.Height + Me.Padding.Top * 2)
            AddHandler Me.dialogLayoutPanel.SizeChanged, AddressOf TableLayoutPanelSizeChanged
            F1Keyword = HelpIDs.Dlg_OpenFileWarning
        End Sub

        Private Sub TableLayoutPanelSizeChanged(ByVal sender As Object, ByVal e As EventArgs)
            Me.ClientSize = New Size(Me.ClientSize.Width, Me.dialogLayoutPanel.Size.Height + Me.Padding.Top * 2)
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso _components IsNot Nothing Then
                RemoveHandler Me.dialogLayoutPanel.SizeChanged, AddressOf TableLayoutPanelSizeChanged
                _components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        ''' <summary>
        ''' returns whether we need pop up a warning dialog for this extension again
        ''' </summary>
        Public ReadOnly Property AlwaysCheckForThisExtension() As Boolean
            Get
                Return Me.alwaysCheckCheckBox.Checked
            End Get
        End Property

        ''' <summary>
        ''' Click handler for the OK button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonOk_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonOK.Click
            Close()
        End Sub
        Friend WithEvents messageLabel2 As System.Windows.Forms.Label

        ''' <summary>
        ''' Click handler for the Help button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DialogQueryName_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub

        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OpenFileWarningDialog))
            Me.dialogLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.alwaysCheckCheckBox = New System.Windows.Forms.CheckBox
            Me.messageLabel = New System.Windows.Forms.Label
            Me.buttonOK = New System.Windows.Forms.Button
            Me.buttonCancel = New System.Windows.Forms.Button
            Me.messageLabel2 = New System.Windows.Forms.Label
            Me.dialogLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'dialogLayoutPanel
            '
            resources.ApplyResources(Me.dialogLayoutPanel, "dialogLayoutPanel")
            Me.dialogLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.dialogLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.dialogLayoutPanel.Controls.Add(Me.alwaysCheckCheckBox, 0, 2)
            Me.dialogLayoutPanel.Controls.Add(Me.messageLabel, 0, 0)
            Me.dialogLayoutPanel.Controls.Add(Me.buttonOK, 0, 3)
            Me.dialogLayoutPanel.Controls.Add(Me.buttonCancel, 1, 3)
            Me.dialogLayoutPanel.Controls.Add(Me.messageLabel2, 0, 1)
            Me.dialogLayoutPanel.Name = "dialogLayoutPanel"
            Me.dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'alwaysCheckCheckBox
            '
            resources.ApplyResources(Me.alwaysCheckCheckBox, "alwaysCheckCheckBox")
            Me.dialogLayoutPanel.SetColumnSpan(Me.alwaysCheckCheckBox, 2)
            Me.alwaysCheckCheckBox.Name = "alwaysCheckCheckBox"
            '
            'messageLabel
            '
            resources.ApplyResources(Me.messageLabel, "messageLabel")
            Me.dialogLayoutPanel.SetColumnSpan(Me.messageLabel, 2)
            Me.messageLabel.Name = "messageLabel"
            '
            'buttonOK
            '
            resources.ApplyResources(Me.buttonOK, "buttonOK")
            Me.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.buttonOK.Name = "buttonOK"
            '
            'buttonCancel
            '
            resources.ApplyResources(Me.buttonCancel, "buttonCancel")
            Me.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.buttonCancel.Name = "buttonCancel"
            '
            'messageLabel2
            '
            resources.ApplyResources(Me.messageLabel2, "messageLabel2")
            Me.dialogLayoutPanel.SetColumnSpan(Me.messageLabel2, 2)
            Me.messageLabel2.Name = "messageLabel2"
            '
            'OpenFileWarningDialog
            '
            Me.AcceptButton = Me.buttonOK
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.buttonCancel
            Me.Controls.Add(Me.dialogLayoutPanel)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "OpenFileWarningDialog"
            Me.ShowIcon = False
            Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
            Me.dialogLayoutPanel.ResumeLayout(False)
            Me.dialogLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents dialogLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents messageLabel As System.Windows.Forms.Label
        Friend WithEvents alwaysCheckCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents buttonOK As System.Windows.Forms.Button
        Friend WithEvents buttonCancel As System.Windows.Forms.Button

    End Class
End Namespace

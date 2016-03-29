Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.Package

    Partial Public Class EditorToolsOptionsPanel
        Inherits System.Windows.Forms.UserControl

        <System.Diagnostics.DebuggerNonUserCode()> _
        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

        End Sub

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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(EditorToolsOptionsPanel))
            Me.IndentingLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me._indentTypeLabel = New System.Windows.Forms.Label
            Me._indentTypeNoneRadioButton = New System.Windows.Forms.RadioButton
            Me._indentTypeBlockRadioButton = New System.Windows.Forms.RadioButton
            Me._indentTypeSmartRadioButton = New System.Windows.Forms.RadioButton
            Me._tabSizeLabel = New System.Windows.Forms.Label
            Me._tabSizeTextBox = New System.Windows.Forms.TextBox
            Me._indentSizeLabel = New System.Windows.Forms.Label
            Me._indentSizeTextBox = New System.Windows.Forms.TextBox
            Me._wordWrapCheckBox = New System.Windows.Forms.CheckBox
            Me._lineNumbersCheckBox = New System.Windows.Forms.CheckBox
            Me.IndentingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.IndentingGroupBox = New System.Windows.Forms.GroupBox
            Me.InteractionGroupBox = New System.Windows.Forms.GroupBox
            Me.InteractionTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.IndentingLayoutPanel.SuspendLayout()
            Me.IndentingTableLayoutPanel.SuspendLayout()
            Me.IndentingGroupBox.SuspendLayout()
            Me.InteractionGroupBox.SuspendLayout()
            Me.InteractionTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'IndentingLayoutPanel
            '
            resources.ApplyResources(Me.IndentingLayoutPanel, "IndentingLayoutPanel")
            Me.IndentingLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.IndentingLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.IndentingLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.IndentingLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.IndentingLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.IndentingLayoutPanel.Controls.Add(Me._indentTypeLabel, 0, 0)
            Me.IndentingLayoutPanel.Controls.Add(Me._indentTypeNoneRadioButton, 1, 0)
            Me.IndentingLayoutPanel.Controls.Add(Me._indentTypeBlockRadioButton, 1, 1)
            Me.IndentingLayoutPanel.Controls.Add(Me._indentTypeSmartRadioButton, 1, 2)
            Me.IndentingLayoutPanel.Controls.Add(Me._tabSizeLabel, 0, 3)
            Me.IndentingLayoutPanel.Controls.Add(Me._tabSizeTextBox, 1, 3)
            Me.IndentingLayoutPanel.Controls.Add(Me._indentSizeLabel, 0, 4)
            Me.IndentingLayoutPanel.Controls.Add(Me._indentSizeTextBox, 1, 4)
            Me.IndentingLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 3, 3, 15)
            Me.IndentingLayoutPanel.Name = "IndentingLayoutPanel"
            Me.IndentingLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.IndentingLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.IndentingLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.IndentingLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.IndentingLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            '_indentTypeLabel
            '
            resources.ApplyResources(Me._indentTypeLabel, "_indentTypeLabel")
            Me._indentTypeLabel.Margin = New System.Windows.Forms.Padding(3)
            Me._indentTypeLabel.Name = "_indentTypeLabel"
            '
            '_indentTypeNoneRadioButton
            '
            resources.ApplyResources(Me._indentTypeNoneRadioButton, "_indentTypeNoneRadioButton")
            Me._indentTypeNoneRadioButton.Margin = New System.Windows.Forms.Padding(3, 1, 3, 1)
            Me._indentTypeNoneRadioButton.Name = "_indentTypeNoneRadioButton"
            '
            '_indentTypeBlockRadioButton
            '
            resources.ApplyResources(Me._indentTypeBlockRadioButton, "_indentTypeBlockRadioButton")
            Me._indentTypeBlockRadioButton.Margin = New System.Windows.Forms.Padding(3, 1, 3, 1)
            Me._indentTypeBlockRadioButton.Name = "_indentTypeBlockRadioButton"
            '
            '_indentTypeSmartRadioButton
            '
            resources.ApplyResources(Me._indentTypeSmartRadioButton, "_indentTypeSmartRadioButton")
            Me._indentTypeSmartRadioButton.Margin = New System.Windows.Forms.Padding(3, 1, 3, 1)
            Me._indentTypeSmartRadioButton.Name = "_indentTypeSmartRadioButton"
            '
            '_tabSizeLabel
            '
            resources.ApplyResources(Me._tabSizeLabel, "_tabSizeLabel")
            Me._tabSizeLabel.Name = "_tabSizeLabel"
            '
            '_tabSizeTextBox
            '
            resources.ApplyResources(Me._tabSizeTextBox, "_tabSizeTextBox")
            Me._tabSizeTextBox.Margin = New System.Windows.Forms.Padding(3, 6, 3, 3)
            Me._tabSizeTextBox.Name = "_tabSizeTextBox"

           '
            '_indentSizeLabel
            '
            resources.ApplyResources(Me._indentSizeLabel, "_indentSizeLabel")
            Me._indentSizeLabel.Name = "_indentSizeLabel"
            '
            '_indentSizeTextBox
            '
            resources.ApplyResources(Me._indentSizeTextBox, "_indentSizeTextBox")
            Me._indentSizeTextBox.Margin = New System.Windows.Forms.Padding(3, 6, 3, 3)
            Me._indentSizeTextBox.Name = "_indentSizeTextBox"

            '
            '_wordWrapCheckBox
            '
            resources.ApplyResources(Me._wordWrapCheckBox, "_wordWrapCheckBox")
            Me._wordWrapCheckBox.Margin = New System.Windows.Forms.Padding(3, 1, 3, 1)
            Me._wordWrapCheckBox.Name = "_wordWrapCheckBox"
            '
            '_lineNumbersCheckBox
            '
            resources.ApplyResources(Me._lineNumbersCheckBox, "_lineNumbersCheckBox")
            Me._lineNumbersCheckBox.Margin = New System.Windows.Forms.Padding(3, 1, 3, 1)
            Me._lineNumbersCheckBox.Name = "_lineNumbersCheckBox"
            '
            'IndentingTableLayoutPanel
            '
            resources.ApplyResources(Me.IndentingTableLayoutPanel, "IndentingTableLayoutPanel")
            Me.IndentingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.IndentingTableLayoutPanel.Controls.Add(Me.IndentingGroupBox, 0, 0)
            Me.IndentingTableLayoutPanel.Controls.Add(Me.InteractionGroupBox, 0, 1)
            Me.IndentingTableLayoutPanel.Name = "IndentingTableLayoutPanel"
            Me.IndentingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.IndentingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'IndentingGroupBox
            '
            resources.ApplyResources(Me.IndentingGroupBox, "IndentingGroupBox")
            Me.IndentingGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.IndentingGroupBox.Controls.Add(Me.IndentingLayoutPanel)
            Me.IndentingGroupBox.Name = "IndentingGroupBox"
            Me.IndentingGroupBox.Padding = New System.Windows.Forms.Padding(3, 10, 3, 3)
            Me.IndentingGroupBox.TabStop = False
            '
            'InteractionGroupBox
            '
            resources.ApplyResources(Me.InteractionGroupBox, "InteractionGroupBox")
            Me.InteractionGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.InteractionGroupBox.Controls.Add(Me.InteractionTableLayoutPanel)
            Me.InteractionGroupBox.Name = "InteractionGroupBox"
            Me.InteractionGroupBox.TabStop = False
            '
            'InteractionTableLayoutPanel
            '
            resources.ApplyResources(Me.InteractionTableLayoutPanel, "InteractionTableLayoutPanel")
            Me.InteractionTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.InteractionTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.InteractionTableLayoutPanel.Controls.Add(Me._wordWrapCheckBox, 0, 0)
            Me.InteractionTableLayoutPanel.Controls.Add(Me._lineNumbersCheckBox, 0, 1)
            Me.InteractionTableLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 3, 3, 10)
            Me.InteractionTableLayoutPanel.Name = "InteractionTableLayoutPanel"
            Me.InteractionTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.InteractionTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'EditorToolsOptionsPanel
            '
            Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.Controls.Add(Me.IndentingTableLayoutPanel)
            Me.Name = "EditorToolsOptionsPanel"
            resources.ApplyResources(Me, "$this")
            Me.IndentingLayoutPanel.ResumeLayout(False)
            Me.IndentingLayoutPanel.PerformLayout()
            Me.IndentingTableLayoutPanel.ResumeLayout(False)
            Me.IndentingTableLayoutPanel.PerformLayout()
            Me.IndentingGroupBox.ResumeLayout(False)
            Me.IndentingGroupBox.PerformLayout()
            Me.InteractionGroupBox.ResumeLayout(False)
            Me.InteractionGroupBox.PerformLayout()
            Me.InteractionTableLayoutPanel.ResumeLayout(False)
            Me.InteractionTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
        Friend WithEvents IndentingLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents _indentTypeLabel As System.Windows.Forms.Label
        Friend WithEvents _indentTypeNoneRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents _indentTypeBlockRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents _indentTypeSmartRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents _indentSizeLabel As System.Windows.Forms.Label
        Friend WithEvents _tabSizeTextBox As System.Windows.Forms.TextBox
        Friend WithEvents _indentSizeTextBox As System.Windows.Forms.TextBox
        Friend WithEvents _wordWrapCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents _lineNumbersCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents _tabSizeLabel As System.Windows.Forms.Label
        Friend WithEvents IndentingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents IndentingGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents InteractionGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents InteractionTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

    End Class
End Namespace

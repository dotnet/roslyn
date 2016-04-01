' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms
Imports VSLangProj80
Imports VSLangProj90
Imports VsLangProj110

'This is the VB version of this page.  BuildPropPage.vb is the C#/J# version.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class CompilePropPage2
        Inherits BuildPropPageBase

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            _notifyError = SR.GetString(SR.PPG_Compile_Notification_Error)
            _notifyNone = SR.GetString(SR.PPG_Compile_Notification_None)
            _notifyWarning = SR.GetString(SR.PPG_Compile_Notification_Warning)
            MyBase.PageRequiresScaling = False
            MyBase.AutoScaleMode = AutoScaleMode.Font

            AddChangeHandlers()

            Dim optionStrictErrors As New System.Collections.ArrayList
            For Each ErrorInfo As ErrorInfo In _errorInfos
                If ErrorInfo.ErrorOnOptionStrict Then
                    optionStrictErrors.AddRange(ErrorInfo.ErrList)
                End If
            Next
            ReDim _optionStrictIDs(optionStrictErrors.Count - 1)
            optionStrictErrors.CopyTo(_optionStrictIDs)
            System.Array.Sort(_optionStrictIDs)

            NotificationColumn.Items.Add(_notifyNone)
            NotificationColumn.Items.Add(_notifyWarning)
            NotificationColumn.Items.Add(_notifyError)
        End Sub

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Friend WithEvents BuildOutputPathLabel As System.Windows.Forms.Label
        Friend WithEvents BuildOutputPathTextBox As System.Windows.Forms.TextBox
        Friend WithEvents BuildOutputPathButton As System.Windows.Forms.Button
        Friend WithEvents AdvancedOptionsButton As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents buildOutputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        Private _settingGenerateXmlDocumentation As Boolean
        Private _generateXmlDocumentation As Object
        Friend WithEvents CompileOptionsGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents CompileOptionsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents OptionExplicitLabel As System.Windows.Forms.Label
        Friend WithEvents DisableAllWarningsCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents WarningsAsErrorCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionExplicitComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents BuildEventsButton As System.Windows.Forms.Button
        Friend WithEvents RegisterForComInteropCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionCompareComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents GenerateXMLCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionStrictLabel As System.Windows.Forms.Label
        Friend WithEvents OptionStrictComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents OptionCompareLabel As System.Windows.Forms.Label
        Friend WithEvents OptionInferLabel As System.Windows.Forms.Label
        Friend WithEvents OptionInferComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents WarningsConfigurationsGridViewLabel As System.Windows.Forms.Label
        Friend WithEvents WarningsGridView As Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView
        Friend WithEvents ConditionColumn As System.Windows.Forms.DataGridViewTextBoxColumn
        Friend WithEvents NotificationColumn As System.Windows.Forms.DataGridViewComboBoxColumn
        Friend WithEvents TargetCPULabel As System.Windows.Forms.Label
        Friend WithEvents TargetCPUComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents Prefer32BitCheckBox As System.Windows.Forms.CheckBox

        ' Shared cache of raw and extended configuration objects
        Private _objectCache As FakeAllConfigurationsPropertyControlData.ConfigurationObjectCache
        Friend WithEvents AdvancedCompileOptionsLabelLine As System.Windows.Forms.Label

        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer
        'put this bock in
        'Me.WarningsGridView = New Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView

        'PERF: A note about the labels used as lines.  The 3D label is being set to 1 px high,
        '   so you’re really only using the grey part of it.  Using BorderStyle.Fixed3D seems
        '   to fire an extra resize OnHandleCreated.  The simple solution is to use BorderStyle.None
        '   and BackColor = SystemColors.ControlDark.

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CompilePropPage2))
            Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
            Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
            Me.BuildOutputPathLabel = New System.Windows.Forms.Label
            Me.BuildOutputPathTextBox = New System.Windows.Forms.TextBox
            Me.BuildOutputPathButton = New System.Windows.Forms.Button
            Me.AdvancedOptionsButton = New System.Windows.Forms.Button
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.buildOutputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.CompileOptionsGroupBox = New System.Windows.Forms.GroupBox
            Me.CompileOptionsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AdvancedCompileOptionsLabelLine = New System.Windows.Forms.Label
            Me.OptionExplicitLabel = New System.Windows.Forms.Label
            Me.DisableAllWarningsCheckBox = New System.Windows.Forms.CheckBox
            Me.WarningsAsErrorCheckBox = New System.Windows.Forms.CheckBox
            Me.OptionExplicitComboBox = New System.Windows.Forms.ComboBox
            Me.BuildEventsButton = New System.Windows.Forms.Button
            Me.RegisterForComInteropCheckBox = New System.Windows.Forms.CheckBox
            Me.OptionCompareComboBox = New System.Windows.Forms.ComboBox
            Me.GenerateXMLCheckBox = New System.Windows.Forms.CheckBox
            Me.OptionStrictLabel = New System.Windows.Forms.Label
            Me.OptionStrictComboBox = New System.Windows.Forms.ComboBox
            Me.OptionCompareLabel = New System.Windows.Forms.Label
            Me.OptionInferLabel = New System.Windows.Forms.Label
            Me.OptionInferComboBox = New System.Windows.Forms.ComboBox
            Me.WarningsConfigurationsGridViewLabel = New System.Windows.Forms.Label
            Me.WarningsGridView = New Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView
            Me.ConditionColumn = New System.Windows.Forms.DataGridViewTextBoxColumn
            Me.NotificationColumn = New System.Windows.Forms.DataGridViewComboBoxColumn
            Me.TargetCPULabel = New System.Windows.Forms.Label
            Me.TargetCPUComboBox = New System.Windows.Forms.ComboBox
            Me.Prefer32BitCheckBox = New System.Windows.Forms.CheckBox
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.buildOutputTableLayoutPanel.SuspendLayout()
            Me.CompileOptionsGroupBox.SuspendLayout()
            Me.CompileOptionsTableLayoutPanel.SuspendLayout()
            CType(Me.WarningsGridView, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'BuildOutputPathLabel
            '
            resources.ApplyResources(Me.BuildOutputPathLabel, "BuildOutputPathLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.BuildOutputPathLabel, 2)
            Me.BuildOutputPathLabel.Name = "BuildOutputPathLabel"
            '
            'BuildOutputPathTextBox
            '
            resources.ApplyResources(Me.BuildOutputPathTextBox, "BuildOutputPathTextBox")
            Me.BuildOutputPathTextBox.Name = "BuildOutputPathTextBox"
            '
            'BuildOutputPathButton
            '
            resources.ApplyResources(Me.BuildOutputPathButton, "BuildOutputPathButton")
            Me.BuildOutputPathButton.Name = "BuildOutputPathButton"
            '
            'AdvancedOptionsButton
            '
            resources.ApplyResources(Me.AdvancedOptionsButton, "AdvancedOptionsButton")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.AdvancedOptionsButton, 2)
            Me.AdvancedOptionsButton.Name = "AdvancedOptionsButton"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.BuildOutputPathLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.buildOutputTableLayoutPanel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompileOptionsGroupBox, 0, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'buildOutputTableLayoutPanel
            '
            resources.ApplyResources(Me.buildOutputTableLayoutPanel, "buildOutputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.buildOutputTableLayoutPanel, 2)
            Me.buildOutputTableLayoutPanel.Controls.Add(Me.BuildOutputPathTextBox, 0, 0)
            Me.buildOutputTableLayoutPanel.Controls.Add(Me.BuildOutputPathButton, 1, 0)
            Me.buildOutputTableLayoutPanel.Name = "buildOutputTableLayoutPanel"
            '
            'CompileOptionsGroupBox
            '
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CompileOptionsGroupBox, 2)
            Me.CompileOptionsGroupBox.Controls.Add(Me.CompileOptionsTableLayoutPanel)
            resources.ApplyResources(Me.CompileOptionsGroupBox, "CompileOptionsGroupBox")
            Me.CompileOptionsGroupBox.Name = "CompileOptionsGroupBox"
            Me.CompileOptionsGroupBox.TabStop = False
            '
            'CompileOptionsTableLayoutPanel
            '
            resources.ApplyResources(Me.CompileOptionsTableLayoutPanel, "CompileOptionsTableLayoutPanel")
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.AdvancedCompileOptionsLabelLine, 0, 14)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionExplicitLabel, 0, 0)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.DisableAllWarningsCheckBox, 0, 10)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsAsErrorCheckBox, 0, 11)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionExplicitComboBox, 0, 1)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.BuildEventsButton, 1, 13)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.RegisterForComInteropCheckBox, 0, 13)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionCompareComboBox, 0, 3)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.GenerateXMLCheckBox, 0, 12)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionStrictLabel, 1, 0)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionStrictComboBox, 1, 1)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionCompareLabel, 0, 2)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionInferLabel, 1, 2)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionInferComboBox, 1, 3)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsConfigurationsGridViewLabel, 0, 7)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsGridView, 0, 8)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.AdvancedOptionsButton, 0, 15)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.TargetCPULabel, 0, 4)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.TargetCPUComboBox, 0, 5)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.Prefer32BitCheckBox, 0, 6)
            Me.CompileOptionsTableLayoutPanel.Name = "CompileOptionsTableLayoutPanel"
            '
            'AdvancedCompileOptionsLabelLine
            '
            Me.AdvancedCompileOptionsLabelLine.AccessibleRole = System.Windows.Forms.AccessibleRole.Graphic
            resources.ApplyResources(Me.AdvancedCompileOptionsLabelLine, "AdvancedCompileOptionsLabelLine")
            Me.AdvancedCompileOptionsLabelLine.BackColor = System.Drawing.SystemColors.ControlDark
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.AdvancedCompileOptionsLabelLine, 2)
            Me.AdvancedCompileOptionsLabelLine.Name = "AdvancedCompileOptionsLabelLine"
            '
            'OptionExplicitLabel
            '
            resources.ApplyResources(Me.OptionExplicitLabel, "OptionExplicitLabel")
            Me.OptionExplicitLabel.Name = "OptionExplicitLabel"
            '
            'DisableAllWarningsCheckBox
            '
            resources.ApplyResources(Me.DisableAllWarningsCheckBox, "DisableAllWarningsCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.DisableAllWarningsCheckBox, 2)
            Me.DisableAllWarningsCheckBox.Name = "DisableAllWarningsCheckBox"
            '
            'WarningsAsErrorCheckBox
            '
            resources.ApplyResources(Me.WarningsAsErrorCheckBox, "WarningsAsErrorCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.WarningsAsErrorCheckBox, 2)
            Me.WarningsAsErrorCheckBox.Name = "WarningsAsErrorCheckBox"
            '
            'OptionExplicitComboBox
            '
            resources.ApplyResources(Me.OptionExplicitComboBox, "OptionExplicitComboBox")
            Me.OptionExplicitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionExplicitComboBox.FormattingEnabled = True
            Me.OptionExplicitComboBox.Name = "OptionExplicitComboBox"
            '
            'BuildEventsButton
            '
            resources.ApplyResources(Me.BuildEventsButton, "BuildEventsButton")
            Me.BuildEventsButton.MinimumSize = New System.Drawing.Size(91, 0)
            Me.BuildEventsButton.Name = "BuildEventsButton"
            '
            'RegisterForComInteropCheckBox
            '
            resources.ApplyResources(Me.RegisterForComInteropCheckBox, "RegisterForComInteropCheckBox")
            Me.RegisterForComInteropCheckBox.Name = "RegisterForComInteropCheckBox"
            '
            'OptionCompareComboBox
            '
            resources.ApplyResources(Me.OptionCompareComboBox, "OptionCompareComboBox")
            Me.OptionCompareComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionCompareComboBox.FormattingEnabled = True
            Me.OptionCompareComboBox.Name = "OptionCompareComboBox"
            '
            'GenerateXMLCheckBox
            '
            resources.ApplyResources(Me.GenerateXMLCheckBox, "GenerateXMLCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.GenerateXMLCheckBox, 2)
            Me.GenerateXMLCheckBox.Name = "GenerateXMLCheckBox"
            '
            'OptionStrictLabel
            '
            resources.ApplyResources(Me.OptionStrictLabel, "OptionStrictLabel")
            Me.OptionStrictLabel.Name = "OptionStrictLabel"
            '
            'OptionStrictComboBox
            '
            resources.ApplyResources(Me.OptionStrictComboBox, "OptionStrictComboBox")
            Me.OptionStrictComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionStrictComboBox.FormattingEnabled = True
            Me.OptionStrictComboBox.Name = "OptionStrictComboBox"
            '
            'OptionCompareLabel
            '
            resources.ApplyResources(Me.OptionCompareLabel, "OptionCompareLabel")
            Me.OptionCompareLabel.Name = "OptionCompareLabel"
            '
            'OptionInferLabel
            '
            resources.ApplyResources(Me.OptionInferLabel, "OptionInferLabel")
            Me.OptionInferLabel.Name = "OptionInferLabel"
            '
            'OptionInferComboBox
            '
            resources.ApplyResources(Me.OptionInferComboBox, "OptionInferComboBox")
            Me.OptionInferComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionInferComboBox.FormattingEnabled = True
            Me.OptionInferComboBox.Name = "OptionInferComboBox"
            '
            'WarningsConfigurationsGridViewLabel
            '
            resources.ApplyResources(Me.WarningsConfigurationsGridViewLabel, "WarningsConfigurationsGridViewLabel")
            Me.WarningsConfigurationsGridViewLabel.Name = "WarningsConfigurationsGridViewLabel"
            '
            'WarningsGridView
            '
            Me.WarningsGridView.AllowUserToAddRows = False
            Me.WarningsGridView.AllowUserToDeleteRows = False
            Me.WarningsGridView.AllowUserToResizeRows = False
            resources.ApplyResources(Me.WarningsGridView, "WarningsGridView")
            Me.WarningsGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells
            Me.WarningsGridView.BackgroundColor = System.Drawing.SystemColors.Window
            Me.WarningsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.WarningsGridView.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ConditionColumn, Me.NotificationColumn})
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.WarningsGridView, 2)
            Me.WarningsGridView.MinimumSize = New System.Drawing.Size(0, 105)
            Me.WarningsGridView.MultiSelect = False
            Me.WarningsGridView.Name = "WarningsGridView"
            Me.WarningsGridView.RowHeadersVisible = False
            Me.CompileOptionsTableLayoutPanel.SetRowSpan(Me.WarningsGridView, 2)
            '
            'ConditionColumn
            '
            Me.ConditionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
            DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
            Me.ConditionColumn.DefaultCellStyle = DataGridViewCellStyle1
            Me.ConditionColumn.FillWeight = 65.0!
            resources.ApplyResources(Me.ConditionColumn, "ConditionColumn")
            Me.ConditionColumn.Name = "ConditionColumn"
            Me.ConditionColumn.ReadOnly = True
            Me.ConditionColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
            '
            'NotificationColumn
            '
            Me.NotificationColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
            DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
            Me.NotificationColumn.DefaultCellStyle = DataGridViewCellStyle2
            Me.NotificationColumn.FillWeight = 35.0!
            resources.ApplyResources(Me.NotificationColumn, "NotificationColumn")
            Me.NotificationColumn.Name = "NotificationColumn"
            '
            'TargetCPULabel
            '
            resources.ApplyResources(Me.TargetCPULabel, "TargetCPULabel")
            Me.TargetCPULabel.Name = "TargetCPULabel"
            '
            'TargetCPUComboBox
            '
            resources.ApplyResources(Me.TargetCPUComboBox, "TargetCPUComboBox")
            Me.TargetCPUComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetCPUComboBox.FormattingEnabled = True
            Me.TargetCPUComboBox.Name = "TargetCPUComboBox"
            '
            'Prefer32BitCheckBox
            '
            resources.ApplyResources(Me.Prefer32BitCheckBox, "Prefer32BitCheckBox")
            Me.Prefer32BitCheckBox.Name = "Prefer32BitCheckBox"
            '
            'CompilePropPage2
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.MinimumSize = New System.Drawing.Size(455, 437)
            Me.Name = "CompilePropPage2"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.buildOutputTableLayoutPanel.ResumeLayout(False)
            Me.buildOutputTableLayoutPanel.PerformLayout()
            Me.CompileOptionsGroupBox.ResumeLayout(False)
            Me.CompileOptionsGroupBox.PerformLayout()
            Me.CompileOptionsTableLayoutPanel.ResumeLayout(False)
            Me.CompileOptionsTableLayoutPanel.PerformLayout()
            CType(Me.WarningsGridView, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

        End Sub

#End Region


        ' The list of warning ids that are affected by option strict on/off
        Private _optionStrictIDs() As Integer

        ' List of warnings to ignore
        Private _noWarn() As Integer

        ' List of warnings to report as errors
        Private _specWarnAsError() As Integer

        Private _comVisible As Object

        'Localized error/warning strings for notify column
        Private _notifyError As String
        Private _notifyNone As String
        Private _notifyWarning As String
        Private Const s_conditionColumnIndex As Integer = 0
        Private Const s_notifyColumnIndex As Integer = 1
        Private Const s_conditionColumnWidthPercentage As Integer = 35 'non-resizable column
        Private Const s_notifyColumnWidthPercentage As Integer = 100

        'Minimum scrolling widths - widths below which resizing the settings designer will cause a horizontal
        '  scrollbar to appear rather than sizing the column below this size
        Private Const s_conditionColumnMinScrollingWidth As Integer = 100
        Private Const s_notifyColumnMinScrollingWidth As Integer = 100 'non-resizable column

        ' Cached extended objects for all configurations...
        Private _cachedExtendedObjects() As Object

        Private _optionStrictCustomText As String
        Private _optionStrictOnText As String
        Private _optionStrictOffText As String
        Private _refreshingWarningsList As Boolean

        ' Since the option strict combobox value depends
        ' on a combination of the noWarn, specWarnAsError and
        ' option strict properties, which may all change because
        ' of an undo or load operation, and we have no way to
        ' put all of these updates in a transaction, or have them
        ' ordered in a consistent way, we queue an update of the UI
        ' on the IDLE so that it happens after all the settings have
        ' been set...
        Private _optionStrictComboBoxUpdateQueued As Boolean

#Region "Queued update of text in option strict combobox"
        Private Delegate Sub QueueUpdateOptionStrictComboBoxDelegate()

        ''' <summary>
        ''' Whenever we programatically change the noWarn,specWarnAsError or OptionStrict
        ''' properties, we need to make sure that we have the right items/display text in
        ''' the option strict combobox
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub QueueUpdateOptionStrictComboBox()
            If _optionStrictComboBoxUpdateQueued Then
                Return
            End If

            Me.BeginInvoke(New QueueUpdateOptionStrictComboBoxDelegate(AddressOf Me.UpdateOptionStrictComboBox))
            _optionStrictComboBoxUpdateQueued = True
        End Sub


        ''' <summary>
        ''' Update the text (and possibly the contents) of the option strict combobox
        ''' This method does *not* change the underlying property, it only updates the
        ''' UI.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateOptionStrictComboBox()
            Try
                If IsOptionStrictOn() Then
                    ' On means that we should remove the "Custom" from the drop-down
                    OptionStrictComboBox.Items.Remove(_optionStrictCustomText)
                ElseIf IsOptionStrictCustom() Then
                    ' If we are showing "Custom", but the current settings are the same as
                    ' "Off", remove the "Custom" entry from the combobox and change current selection
                    ' to "Off"
                    If Not IsSameAsOptionStrictCustom() Then
                        OptionStrictComboBox.Items.Remove(_optionStrictCustomText)
                        OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOffText)
                    End If
                ElseIf IsOptionStrictOff() Then
                    ' Off may actually mean "Custom"
                    If Not IsSameAsOptionStrictOff() Then
                        ' Change from showing "Off" to "Custom" in combobox
                        Dim newIndex As Integer = OptionStrictComboBox.Items.IndexOf(_optionStrictCustomText)
                        If newIndex = -1 Then
                            ' Add the option strict custom text since it wasn't already in there...
                            newIndex = OptionStrictComboBox.Items.Add(_optionStrictCustomText)
                        End If
                        OptionStrictComboBox.SelectedIndex = newIndex
                    End If
                End If
            Finally
                _optionStrictComboBoxUpdateQueued = False
            End Try
        End Sub
#End Region

        ''' <summary>
        ''' Returns true if the Register for COM Interop checkbox makes sense in this project context
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function RegisterForComInteropSupported() As Boolean
            If MultiProjectSelect Then
                Return False
            End If

            Try
                Dim value As Object = Nothing
                If Not GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, "OutputTypeEx", value) Then
                    Return False
                End If

                Return CUInt(value) = VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Library _
                    AndAlso Not GetPropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop).IsMissing
            Catch ex As Exception
                Common.Utils.RethrowIfUnrecoverable(ex)

                'If the project doesn't support this property, the answer is no.
                Return False
            End Try
        End Function

#Region "Enable / disable controls helpers"
        Protected Overrides Sub EnableAllControls(ByVal _enabled As Boolean)
            MyBase.EnableAllControls(_enabled)

            GetPropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile).EnableControls(_enabled)
            Me.AdvancedOptionsButton.Enabled = _enabled
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath).EnableControls(_enabled)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop).EnableControls(_enabled AndAlso RegisterForComInteropSupported())

            EnableDisableWarningControls(_enabled)
        End Sub

        Private Sub EnableDisableWarningControls(ByVal _enabled As Boolean)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_WarningLevel).EnableControls(_enabled) 'DisableAllWarningsCheckBox
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors).EnableControls(_enabled AndAlso Not DisableAllWarnings())

            EnableDisableGridView(_enabled)
        End Sub

        Private Sub EnableDisableGridView(ByVal _enabled As Boolean)

            If GetPropertyControlData(VsProjPropId2.VBPROJPROPID_NoWarn).IsMissing _
            OrElse GetPropertyControlData(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors).IsMissing Then
                'Not much sense in having the grid enabled if these properties aren't supported by the flavor
                _enabled = False
            End If

            Dim NotifyColumn As DataGridViewComboBoxColumn = CType(Me.WarningsGridView.Columns.Item(s_notifyColumnIndex), DataGridViewComboBoxColumn)
            If _enabled AndAlso DisableAllWarningsCheckBox.CheckState = CheckState.Unchecked AndAlso Me.WarningsAsErrorCheckBox.CheckState = CheckState.Unchecked Then
                For Each column As DataGridViewColumn In WarningsGridView.Columns
                    column.DefaultCellStyle.BackColor = WarningsGridView.DefaultCellStyle.BackColor
                Next
                If IndeterminateWarningsState Then
                    ' If we don't set the current cell to nothing, changing the read-only mode may
                    ' cause us to go into edit mode, with the subsequent prompt about resetting all the
                    ' changes...
                    WarningsGridView.CurrentCell = Nothing
                End If
                WarningsGridView.Enabled = True
            Else
                For Each column As DataGridViewColumn In WarningsGridView.Columns
                    column.DefaultCellStyle.BackColor = Me.BackColor
                Next
                WarningsGridView.Enabled = False
            End If
        End Sub
#End Region

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If _objectCache IsNot Nothing Then
                    _objectCache.Reset(ProjectHierarchy, ServiceProvider, False)
                End If

                If m_ControlData Is Nothing Then
                    'Note: "TreatSpecificWarningsAsErrors - For the grid that contains the ability to turn warnings on and off for specific warnings,
                    '  we use a hidden textbox with the name "SpecWarnAsErrorTextBox".  In this, we build up the list of warnings to treat
                    '  individually.
                    _objectCache = New FakeAllConfigurationsPropertyControlData.ConfigurationObjectCache(ProjectHierarchy, ServiceProvider)

                    m_ControlData = New PropertyControlData() { _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId2.VBPROJPROPID_NoWarn, "NoWarn", Nothing, AddressOf Me.NoWarnSet, AddressOf Me.NoWarnGet, ControlDataFlags.UserHandledEvents, Nothing), _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, "TreatSpecificWarningsAsErrors", Nothing, AddressOf Me.SpecWarnAsErrorSet, AddressOf Me.SpecWarnAsErrorGet, ControlDataFlags.UserHandledEvents, Nothing), _
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionExplicit, "OptionExplicit", Me.OptionExplicitComboBox, New Control() {Me.OptionExplicitLabel}), _
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionStrict, "OptionStrict", Me.OptionStrictComboBox, AddressOf Me.OptionStrictSet, AddressOf Me.OptionStrictGet, ControlDataFlags.UserHandledEvents, New Control() {Me.OptionStrictLabel}), _
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionCompare, "OptionCompare", Me.OptionCompareComboBox, New Control() {Me.OptionCompareLabel}), _
                        New PropertyControlData(VBProjPropId90.VBPROJPROPID_OptionInfer, "OptionInfer", Me.OptionInferComboBox, New Control() {Me.OptionInferLabel}), _
                        New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release, _
                            VsProjPropId.VBPROJPROPID_OutputPath, "OutputPath", Me.BuildOutputPathTextBox, Nothing, AddressOf Me.OutputPathGet, ControlDataFlags.None, New Control() {Me.BuildOutputPathLabel}), _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_DocumentationFile, "DocumentationFile", Nothing, AddressOf Me.DocumentationFileNameSet, AddressOf Me.DocumentationFileNameGet, ControlDataFlags.UserHandledEvents, New Control() {Me.GenerateXMLCheckBox}), _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_WarningLevel, "WarningLevel", Me.DisableAllWarningsCheckBox, AddressOf Me.WarningLevelSet, AddressOf Me.WarningLevelGet, ControlDataFlags.UserHandledEvents, Nothing), _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors, "TreatWarningsAsErrors", Me.WarningsAsErrorCheckBox, Nothing, Nothing, ControlDataFlags.UserHandledEvents, Nothing), _
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_RegisterForComInterop, "RegisterForComInterop", Me.RegisterForComInteropCheckBox, Nothing, Nothing, ControlDataFlags.UserHandledEvents, Nothing), _
                        New PropertyControlData(VsProjPropId80.VBPROJPROPID_ComVisible, "ComVisible", Nothing, AddressOf Me.ComVisibleSet, AddressOf Me.ComVisibleGet, ControlDataFlags.Hidden Or ControlDataFlags.PersistedInAssemblyInfoFile), _
                        New PropertyControlData(VsProjPropId80.VBPROJPROPID_PlatformTarget, "PlatformTarget", Me.TargetCPUComboBox, AddressOf PlatformTargetSet, AddressOf PlatformTargetGet, ControlDataFlags.None, New Control() {TargetCPULabel}), _
                        New PropertyControlData(VsProjPropId110.VBPROJPROPID_Prefer32Bit, "Prefer32Bit", Me.Prefer32BitCheckBox, AddressOf Prefer32BitSet, AddressOf Prefer32BitGet) _
                    }
                End If
                Return m_ControlData
            End Get
        End Property

#Region "Custom property getters/setters"
#Region "Documentation filename getter and setter"
        Private Function DocumentationFileNameGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Select Case GenerateXMLCheckBox.CheckState
                Case CheckState.Checked
                    If String.IsNullOrEmpty(TryCast(_generateXmlDocumentation, String)) Then
                        _generateXmlDocumentation = ProjectProperties.AssemblyName & ".xml"
                    End If
                    value = _generateXmlDocumentation
                Case CheckState.Unchecked
                    value = ""
                Case Else
                    ' Why are we called to get an indeterminate value?
                    value = PropertyControlData.Indeterminate
            End Select
            Return True
        End Function

        Private Function DocumentationFileNameSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If _settingGenerateXmlDocumentation Then
                Return False
            End If
            _settingGenerateXmlDocumentation = True
            Try
                If PropertyControlData.IsSpecialValue(value) Then
                    GenerateXMLCheckBox.CheckState = CheckState.Indeterminate
                ElseIf String.IsNullOrEmpty(TryCast(value, String)) Then
                    GenerateXMLCheckBox.CheckState = CheckState.Unchecked
                Else
                    GenerateXMLCheckBox.CheckState = CheckState.Checked
                End If

                ' Store this value off for later...
                _generateXmlDocumentation = value
                Return True
            Finally
                _settingGenerateXmlDocumentation = False
            End Try
        End Function

#End Region

#Region "NoWarn getter and setter"
        ''' <summary>
        ''' Custom handling of the NoWarn property. We don't have a single control that is associated with this
        ''' property - it is merged with the TreatSpecificWarningsAsErrors and the Option Strict and displayed
        ''' in the warnings grid view
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function NoWarnGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If _noWarn IsNot Nothing Then
                value = ConcatenateNumbers(_noWarn)
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Custom handling of the NoWarn property. We don't have a single control that is associated with this
        ''' property - it is merged with the TreatSpecificWarningsAsErrors and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function NoWarnSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate OrElse value Is PropertyControlData.MissingProperty Then
                _noWarn = Nothing
            Else
                If Not TypeOf value Is String Then
                    Debug.Fail("Expected a string value for property NoWarn")
                    Throw Common.CreateArgumentException("value")
                End If
                _noWarn = SplitToNumbers(DirectCast(value, String))
            End If
            If Not m_fInsideInit Then
                ' Settings this require us to update the warnings grid view...
                UpdateWarningList()
            End If
            Return True
        End Function
#End Region

#Region "TreatSpecificWarningsAsErrors getter and setter"
        ''' <summary>
        ''' Custom handling of the TreatSpecificWarningsAsErrors property. We don't have a single control that is associated with this
        ''' property - it is merged with the NoWarn and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SpecWarnAsErrorGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Debug.Assert(_specWarnAsError IsNot Nothing)
            value = ConcatenateNumbers(_specWarnAsError)
            Return True
        End Function

        ''' <summary>
        ''' Custom handling of the TreatSpecificWarningsAsErrors property. We don't have a single control that is associated with this
        ''' property - it is merged with the NoWarn and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SpecWarnAsErrorSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate OrElse value Is PropertyControlData.MissingProperty Then
                _specWarnAsError = Nothing
            Else
                If Not TypeOf value Is String Then
                    Debug.Fail("Expected a string value for property SpecWarnAsError")
                    Throw Common.CreateArgumentException("value")
                End If
                _specWarnAsError = SplitToNumbers(DirectCast(value, String))
            End If
            If Not m_fInsideInit Then
                ' Changing this property requires us to update the warnings grid view...
                UpdateWarningList()
            End If
            Return True
        End Function
#End Region

#Region "WarningLevel getter and setter"
        ''' <summary>
        ''' Property getter for warning level
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function WarningLevelGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Select Case Me.DisableAllWarningsCheckBox.CheckState
                Case CheckState.Checked
                    value = VSLangProj.prjWarningLevel.prjWarningLevel0 'Warning Level 0 = off
                    Return True
                Case CheckState.Unchecked
                    value = VSLangProj.prjWarningLevel.prjWarningLevel1 'Warning Level 1 = on
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Property setter for warning level
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function WarningLevelSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate Then
                DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate
            Else
                If CType(value, VSLangProj.prjWarningLevel) = VSLangProj.prjWarningLevel.prjWarningLevel0 Then
                    DisableAllWarningsCheckBox.CheckState = CheckState.Checked
                Else
                    DisableAllWarningsCheckBox.CheckState = CheckState.Unchecked
                End If
            End If
            Return True
        End Function
#End Region

#Region "OptionStrict getter and setter"
        Private Function OptionStrictGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim strValue As String = CStr(OptionStrictComboBox.SelectedItem)
            If _optionStrictCustomText.Equals(strValue, System.StringComparison.Ordinal) Then
                value = VSLangProj.prjOptionStrict.prjOptionStrictOff
            Else
                value = prop.Converter.ConvertFrom(strValue)
            End If
            Return True
        End Function

        Private Function OptionStrictSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value IsNot Nothing Then
                Try
                    If Not PropertyControlData.IsSpecialValue(value) Then
                        Dim strValue As String = prop.Converter.ConvertToString(value)
                        OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(strValue)
                    Else
                        OptionStrictComboBox.SelectedIndex = -1
                    End If
                    If Not m_fInsideInit Then
                        QueueUpdateOptionStrictComboBox()
                        UpdateWarningList()
                    End If
                    Return True
                Catch ex As Exception
                    Debug.Fail(String.Format("Failed to convert {0} to string ({1})", value, ex))
                    Common.RethrowIfUnrecoverable(ex)
                End Try
            Else
                Debug.Fail("Why did we get a NULL value for option strict?")
            End If
            Return False
        End Function
#End Region

#Region "OutputPath getter"
        Private Function OutputPathGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = GetProjectRelativeDirectoryPath(Trim(Me.BuildOutputPathTextBox.Text))
            Return True
        End Function
#End Region
#End Region

        ''' <summary>
        ''' We need to reset our cached extended objects every time someone calls SetObjects
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub SetObjects(ByVal objects() As Object)
            If _objectCache IsNot Nothing Then
                _objectCache.Reset(ProjectHierarchy, ServiceProvider, True)
            End If
            MyBase.SetObjects(objects)
        End Sub


#Region "Pre/post init page"

        ''' <summary>
        ''' For some reason, AnyCPU includes as space when returned by the IVsConfigProvider.Get*PlatformNames
        ''' but should *not* include a space when passed to the compiler/set the property value
        ''' </summary>
        ''' <remarks></remarks>
        Private Const s_anyCPUPropertyValue As String = "AnyCPU"
        Private Const s_anyCPUPlatformName As String = "Any CPU"

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again.
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            MyBase.PreInitPage()
            'Add any special init code here
            Me.Dock = DockStyle.Fill

            Dim data As PropertyControlData = GetPropertyControlData("OptionStrict")
            Dim _TypeConverter As TypeConverter = data.PropDesc.Converter

            If _TypeConverter IsNot Nothing Then
                'Get the localized text for On/Off
                OptionStrictComboBox.Items.Clear()
                For Each o As Object In _TypeConverter.GetStandardValues()
                    If CInt(o) = VSLangProj.prjOptionStrict.prjOptionStrictOn Then
                        _optionStrictOnText = _TypeConverter.ConvertToString(o)
                        OptionStrictComboBox.Items.Add(_optionStrictOnText)
                    ElseIf CInt(o) = VSLangProj.prjOptionStrict.prjOptionStrictOff Then
                        _optionStrictOffText = _TypeConverter.ConvertToString(o)
                        OptionStrictComboBox.Items.Add(_optionStrictOffText)
                    End If
                Next
            End If


            _optionStrictCustomText = SR.GetString(SR.PPG_Compile_OptionStrict_Custom)

            Dim PlatformEntries As New List(Of String)

            ' Let's try to sniff the supported platforms from our hiearchy (if any)
            TargetCPUComboBox.Items.Clear()
            If Me.ProjectHierarchy IsNot Nothing Then
                Dim oCfgProv As Object = Nothing
                Dim hr As Integer
                hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, oCfgProv)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim cfgProv As IVsCfgProvider2 = TryCast(oCfgProv, IVsCfgProvider2)
                    If cfgProv IsNot Nothing Then
                        Dim actualPlatformCount(0) As UInteger
                        hr = cfgProv.GetSupportedPlatformNames(0, Nothing, actualPlatformCount)
                        If VSErrorHandler.Succeeded(hr) Then
                            Dim platformCount As UInteger = actualPlatformCount(0)
                            Dim platforms(CInt(platformCount)) As String
                            hr = cfgProv.GetSupportedPlatformNames(platformCount, platforms, actualPlatformCount)
                            If VSErrorHandler.Succeeded(hr) Then
                                For platformNo As Integer = 0 To CInt(platformCount - 1)
                                    If s_anyCPUPlatformName.Equals(platforms(platformNo), StringComparison.Ordinal) Then
                                        PlatformEntries.Add(s_anyCPUPropertyValue)
                                    Else
                                        PlatformEntries.Add(platforms(platformNo))
                                    End If
                                Next
                            End If
                        End If
                    End If
                End If
            End If

            ' ...and if we couldn't get 'em from the project system, let's add a hard-coded list of platforms...
            If PlatformEntries.Count = 0 Then
                Debug.Fail("Unable to get platform list from configuration manager")
                PlatformEntries.AddRange(New String() {"AnyCPU", "x86", "x64", "Itanium"})
            End If
            If VSProductSKU.ProductSKU < VSProductSKU.VSASKUEdition.Enterprise Then
                'For everything lower than VSTS (SKU# = Enterprise), don't target Itanium
                PlatformEntries.Remove("Itanium")
            End If

            ' ... Finally, add the entries to the combobox
            Me.TargetCPUComboBox.Items.AddRange(PlatformEntries.ToArray())

        End Sub

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again.
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()

            'OutputPath browse button should only be enabled when the text box is enabled and Not ReadOnly
            Me.BuildOutputPathButton.Enabled = (Me.BuildOutputPathTextBox.Enabled AndAlso Not Me.BuildOutputPathTextBox.ReadOnly)
            EnableControl(RegisterForComInteropCheckBox, RegisterForComInteropSupported())

            'Populate Error/Warnings list
            PopulateErrorList()
            QueueUpdateOptionStrictComboBox()
            EnableAllControls(Me.Enabled)

            'Hide all non-Express controls
            If VSProductSKU.IsExpress Then
                Me.BuildEventsButton.Visible = False
            End If

            ' Only show the separator/all configurations label if we have the
            ' ShowAllConfigurations setting on...
            Dim SimplifiedConfigMode As Boolean = False
            Dim ConfigurationState As PropPageDesigner.ConfigurationState = Nothing
            ConfigurationState = TryCast(GetServiceFromPropertyPageSite(GetType(PropPageDesigner.ConfigurationState)), PropPageDesigner.ConfigurationState)
            Debug.Assert(ConfigurationState IsNot Nothing, "Couldn't QS for ConfigurationState")
            If ConfigurationState IsNot Nothing Then
                SimplifiedConfigMode = ConfigurationState.IsSimplifiedConfigMode
            End If

            RefreshEnabledStatusForPrefer32Bit(Me.Prefer32BitCheckBox)

            Me.MinimumSize = Me.GetPreferredSize(System.Drawing.Size.Empty)
        End Sub
#End Region

        Public Enum ErrorNotification
            None
            Warning
            [Error]
        End Enum

        Private Class ErrorInfo
            Public ReadOnly Title As String
            Public ReadOnly Numbers As String
            Public ReadOnly Notification As ErrorNotification
            Public ReadOnly ErrorOnOptionStrict As Boolean
            Public Index As Integer
            Public ReadOnly ErrList As Integer()
            Public Sub New(ByVal Title As String, ByVal Numbers As String, ByVal Notification As ErrorNotification, ByVal ErrorOnOptionStrict As Boolean, ByVal ErrList As Integer())
                Me.Title = Title
                Me.Numbers = Numbers
                Me.Notification = Notification
                Me.ErrorOnOptionStrict = ErrorOnOptionStrict
                Me.ErrList = ErrList
                System.Array.Sort(Me.ErrList)
            End Sub
        End Class

        Private _errorInfos As ErrorInfo() = { _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42016), "42016,41999", ErrorNotification.None, True, New Integer() {42016, 41999}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42017_42018_42019), "42017,42018,42019,42032,42036", ErrorNotification.None, True, New Integer() {42017, 42018, 42019, 42032, 42036}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42020), "42020,42021,42022", ErrorNotification.None, True, New Integer() {42020, 42021, 42022}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42104), "42104,42108,42109,42030", ErrorNotification.None, False, New Integer() {42104, 42108, 42109, 42030}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42105_42106_42107), "42105,42106,42107", ErrorNotification.None, False, New Integer() {42105, 42106, 42107}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42353_42354_42355), "42353,42354,42355", ErrorNotification.None, False, New Integer() {42353, 42354, 42355}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42024), "42024,42099", ErrorNotification.None, False, New Integer() {42024, 42099}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42025), "42025", ErrorNotification.None, False, New Integer() {42025}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42004), "41998,42004,42026,", ErrorNotification.None, False, New Integer() {41998, 42004, 42026}), _
            New ErrorInfo(SR.GetString(SR.PPG_Compile_42029), "42029,42031", ErrorNotification.None, False, New Integer() {42029, 42031})}

        Private Sub PopulateErrorList()
            Dim NotificationColumn As DataGridViewComboBoxColumn = CType(Me.WarningsGridView.Columns.Item(s_notifyColumnIndex), DataGridViewComboBoxColumn)
            Dim ConditionColumn As DataGridViewTextBoxColumn = CType(Me.WarningsGridView.Columns.Item(s_conditionColumnIndex), DataGridViewTextBoxColumn)
            Dim Index As Integer
            Dim row As DataGridViewRow

            With Me.WarningsGridView
                .Rows.Clear()
                .ScrollBars = ScrollBars.Vertical

                For Each ErrorInfo As ErrorInfo In _errorInfos
                    Index = .Rows.Add(ErrorInfo.Title) ', NotificationText)
                    row = .Rows.Item(Index)
                    ErrorInfo.Index = Index
                Next

                .AutoResizeColumn(s_conditionColumnIndex, DataGridViewAutoSizeColumnMode.DisplayedCells)
                .AutoResizeColumn(s_notifyColumnIndex, DataGridViewAutoSizeColumnMode.DisplayedCells)
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
                .RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            End With

            'Now flip the toggles whether the user has toggled the warning/error
            UpdateWarningList()

        End Sub

#Region "Helper methods to map UI values to properties"

        Private Function IsOptionStrictOn() As Boolean
            Return (Me._optionStrictOnText.Equals(CStr(Me.OptionStrictComboBox.SelectedItem), System.StringComparison.Ordinal))
        End Function

        Private Function IsOptionStrictOff() As Boolean
            Return (Me._optionStrictOffText.Equals(CStr(Me.OptionStrictComboBox.SelectedItem), System.StringComparison.Ordinal))
        End Function

        Private Function IsOptionStrictCustom() As Boolean
            Return (Me._optionStrictCustomText.Equals(CStr(Me.OptionStrictComboBox.SelectedItem), System.StringComparison.Ordinal))
        End Function

        Private Function TreatAllWarningsAsErrors() As Boolean
            Return Me.WarningsAsErrorCheckBox.CheckState = CheckState.Checked
        End Function

        Private Function DisableAllWarnings() As Boolean
            Return Me.DisableAllWarningsCheckBox.CheckState = CheckState.Checked
        End Function

        ''' <summary>
        ''' We are in an indeterminate state if we have conflicting settings in
        ''' different configurations
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' We shouldn't be in this situation unless the user has messed around manually with
        ''' the project file...
        ''' </remarks>
        Private ReadOnly Property IndeterminateWarningsState() As Boolean
            Get
                If WarningsAsErrorCheckBox.CheckState = CheckState.Indeterminate Then
                    Return True
                End If

                If Me.DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate Then
                    Return True
                End If

                If _noWarn Is Nothing Then
                    Return True
                End If

                If _specWarnAsError Is Nothing Then
                    Return True
                End If

                Return False
            End Get
        End Property


#End Region

        Private Sub DisableAllWarningsCheckBox_Checked(ByVal sender As Object, ByVal e As System.EventArgs) Handles DisableAllWarningsCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate Then
                UpdateWarningList()
                EnableDisableWarningControls(Me.Enabled)
                SetDirty(DisableAllWarningsCheckBox, True)
            End If
        End Sub

        ''' <summary>
        ''' We use an empty cell to indicate that levels conflict
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub WarningsGridView_CellFormatting(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellFormattingEventArgs) Handles WarningsGridView.CellFormatting
            If e.ColumnIndex = s_notifyColumnIndex Then
                ' If either this is in an indeterminate state because we have different warning levels 
                ' in different configurations, or if the current value is indeterminate (DBNull) because
                ' only a subset of the values the make up this row's set of warning id's were included in
                ' the string(s), we paint the current cell blank...
                Dim isBlankCell As Boolean
                If e.Value Is DBNull.Value Then
                    isBlankCell = True
                ElseIf IndeterminateWarningsState Then
                    If Not _errorInfos(e.RowIndex).ErrorOnOptionStrict OrElse IsOptionStrictCustom() Then
                        isBlankCell = True
                    End If
                End If
                If isBlankCell Then
                    e.Value = ""
                    e.FormattingApplied = True
                End If
            End If
        End Sub

        Private Sub WarningsGridView_EditingControlShowing(ByVal sender As Object, ByVal e As DataGridViewEditingControlShowingEventArgs) Handles WarningsGridView.EditingControlShowing
            With e.CellStyle
                .BackColor = WarningsGridView.BackgroundColor
                .ForeColor = WarningsGridView.ForeColor
            End With
        End Sub

        Private Sub WarningsAsErrorCheckBox_Checked(ByVal sender As Object, ByVal e As System.EventArgs) Handles WarningsAsErrorCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not WarningsAsErrorCheckBox.CheckState = CheckState.Indeterminate Then
                UpdateWarningList()
                EnableDisableWarningControls(Me.Enabled)
                SetDirty(WarningsAsErrorCheckBox, True)
            End If
        End Sub

        ''' <summary>
        ''' Make sure we set the Register for COM interop property whenever the
        ''' user checkes the corresponding checkbox on the property page
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub RegisterForComInteropCheckBox_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RegisterForComInteropCheckBox.CheckedChanged
            If Not m_fInsideInit Then
                If RegisterForComInteropCheckBox.Checked Then
                    ' Whenever the user checks the register for Com interop, we should also set the COM visible property
                    _comVisible = True
                    SetDirty(VsProjPropId80.VBPROJPROPID_ComVisible, False)
                End If
                SetDirty(VsProjPropId.VBPROJPROPID_RegisterForComInterop, True)
            End If
        End Sub

        ''' <summary>
        ''' Get the value for ComVisible. 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns>
        ''' </returns>
        ''' <remarks>
        ''' </remarks>
        Private Function ComVisibleGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = _comVisible
            Return True
        End Function

        ''' <summary>
        ''' Set the current value for the COM Visible property
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ComVisibleSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            _comVisible = value
            Return True
        End Function

        ''' <summary>
        ''' Called whenever the property page detects that a property defined on this property page is changed in the
        '''   project system.  Property changes made directly by an apply or through PropertyControlData will not come
        '''   through this method.
        ''' </summary>
        Protected Overrides Sub OnExternalPropertyChanged(ByVal DISPID As Integer, ByVal Source As PropertyChangeSource)
            MyBase.OnExternalPropertyChanged(DISPID, Source)

            'If the project's OutputType has changed, the Register for COM Interop control's visibility might need to change
            If Source <> PropertyChangeSource.Direct AndAlso (DISPID = DISPID_UNKNOWN OrElse DISPID = VsProjPropId.VBPROJPROPID_OutputType) Then
                EnableControl(RegisterForComInteropCheckBox, RegisterForComInteropSupported())

                ' Changes to the OutputType may affect whether 'Prefer32Bit' is enabled
                RefreshEnabledStatusForPrefer32Bit(Me.Prefer32BitCheckBox)
            End If
        End Sub


        ''' <summary>
        ''' Disables warnings which are not generated when Option Strict is on
        ''' (Option Strict On will generate error ids, not the warning ids)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateWarningList()
            ' Depending on the order that we are populating the controls,
            ' we may get called to update the warnings list before we have
            ' added any rows (i.e. setting option strict will cause this)
            If WarningsGridView.RowCount = 0 Then
                ' This should only happen during init!
                Debug.Assert(m_fInsideInit, "Why didn't we have any rows in the warnings grid view outside of init?")
                Exit Sub
            End If

            Dim savedRefreshingWarningsList As Boolean = _refreshingWarningsList
            If savedRefreshingWarningsList Then
                Debug.Fail("Recursive update of warnings list...")
            End If
            Try
                _refreshingWarningsList = True
                If WarningsGridView.IsCurrentCellInEditMode Then
                    WarningsGridView.CancelEdit()
                    WarningsGridView.CurrentCell = Nothing
                End If

                Dim rows As DataGridViewRowCollection = Me.WarningsGridView.Rows
                Dim ComboboxCell As DataGridViewComboBoxCell

                For Each ErrorInfo As ErrorInfo In _errorInfos
                    Dim row As DataGridViewRow = rows.Item(ErrorInfo.Index)

                    ComboboxCell = DirectCast(row.Cells(s_notifyColumnIndex), DataGridViewComboBoxCell)

                    'Check for this error in NoWarn.Text or SpecWarnAsErrorTextBox.Text
                    If IsOptionStrictOn() AndAlso ErrorInfo.ErrorOnOptionStrict Then
                        ' Option Strict ON overrides everything below
                        ComboboxCell.Value = _notifyError
                    ElseIf DisableAllWarnings() Then
                        ' If the DisableAllWarnings checkbox is checked we will set this guy to NotifyNone
                        ' and not care about warning levels for specific warnings...
                        ComboboxCell.Value = _notifyNone
                    ElseIf TreatAllWarningsAsErrors() AndAlso _noWarn IsNot Nothing AndAlso AreNumbersInList(_noWarn, ErrorInfo.ErrList) = TriState.False Then
                        ' If the TreatWarningsAsErrors checkbox is checked we will set this guy to NotifyError
                        ' (since we already know that DisableAllWarnings wasn't checked)
                        ComboboxCell.Value = _notifyError
                    Else
                        ' If none of the above, we have to check the lists of specific errors to
                        ' ignore/report as errors
                        Dim IsNoWarn, IsWarnAsError As TriState
                        If _noWarn IsNot Nothing Then
                            IsNoWarn = AreNumbersInList(_noWarn, ErrorInfo.ErrList)
                        Else
                            IsNoWarn = TriState.UseDefault
                        End If

                        If _specWarnAsError IsNot Nothing Then
                            IsWarnAsError = AreNumbersInList(_specWarnAsError, ErrorInfo.ErrList)
                        Else
                            IsWarnAsError = TriState.UseDefault
                        End If

                        'NOTE: Order of test is important
                        If IsNoWarn = TriState.True Then
                            ComboboxCell.Value = _notifyNone
                        ElseIf IsWarnAsError = TriState.True AndAlso IsNoWarn <> TriState.UseDefault Then
                            ComboboxCell.Value = _notifyError
                        ElseIf IsNoWarn = TriState.False AndAlso IsWarnAsError = TriState.False Then
                            ComboboxCell.Value = _notifyWarning
                        Else
                            ComboboxCell.Value = System.DBNull.Value
                        End If
                    End If
                Next

                QueueUpdateOptionStrictComboBox()

            Finally
                _refreshingWarningsList = savedRefreshingWarningsList
            End Try
        End Sub

#Region "Set related functions (join/intersect/union)"
        ''' <summary>
        ''' Concatenate an array of integers into a comma-separated string of numbers
        ''' </summary>
        ''' <param name="numbers"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ConcatenateNumbers(ByVal numbers() As Integer) As String
            Dim strNumbers(numbers.Length - 1) As String
            For i As Integer = 0 To numbers.Length - 1
                strNumbers(i) = numbers(i).ToString()
            Next
            Return String.Join(",", strNumbers)
        End Function

        ''' <summary>
        ''' Split a comma-separated string into a sorted array of numbers
        ''' </summary>
        ''' <param name="numberString"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SplitToNumbers(ByVal numberString As String) As Integer()
            If numberString Is Nothing Then
                Debug.Fail("NULL Argument")
                Throw New ArgumentNullException()
            End If

            Dim result As New List(Of Integer)

            For Each strNumber As String In numberString.Split(","c)
                Dim Number As Double
                If Double.TryParse(strNumber, Number) Then
                    If Number >= 0 AndAlso Number < System.Int32.MaxValue Then
                        result.Add(CInt(Number))
                    End If
                End If
            Next
            result.Sort()
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Return the intersection of the two *sorted* arrays set1 and set2
        ''' </summary>
        ''' <param name="set1"></param>
        ''' <param name="set2"></param>
        ''' <returns></returns>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Function Intersect(ByVal set1() As Integer, ByVal set2() As Integer) As Integer()
            Dim indexSet1 As Integer = 0
            Dim indexSet2 As Integer = 0

            Dim result As New List(Of Integer)
            Do While indexSet1 < set1.Length AndAlso indexSet2 < set2.Length
                ' Walk while the items in set1 are less than the item we are looking
                ' at in set2
                While set1(indexSet1) < set2(indexSet2)
                    indexSet1 += 1
                    If indexSet1 >= set1.Length Then Exit Do
                End While

                ' If the items are equal, add and move to next
                If set1(indexSet1) = set2(indexSet2) Then
                    result.Add(set1(indexSet1))
                    indexSet1 += 1
                End If
                indexSet2 += 1
            Loop
            Return result.ToArray()
        End Function


        ''' <summary>
        ''' Return the union of the two *sorted* arrays set1 and set2
        ''' </summary>
        ''' <param name="set1"></param>
        ''' <param name="set2"></param>
        ''' <returns></returns>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Function Union(ByVal set1() As Integer, ByVal set2() As Integer) As Integer()
            Dim indexSet1 As Integer = 0
            Dim indexSet2 As Integer = 0

            Dim result As New List(Of Integer)
            If set1 IsNot Nothing AndAlso set2 IsNot Nothing Then
                Do While indexSet1 < set1.Length AndAlso indexSet2 < set2.Length
                    ' Add all numbers from set1 that are less than the currently selected
                    ' item in set2
                    While set1(indexSet1) < set2(indexSet2)
                        result.Add(set1(indexSet1))
                        indexSet1 += 1
                        If indexSet1 >= set1.Length Then Exit Do
                    End While

                    ' We should only add one of the items if
                    ' the currently selected item in set1 and set2
                    ' are equal - make sure of that by bumping the index
                    ' for set1 up one notch!
                    If set1(indexSet1) = set2(indexSet2) Then
                        indexSet1 += 1
                    End If
                    result.Add(set2(indexSet2))
                    indexSet2 += 1
                Loop

                ' Add the remaining items
                For i As Integer = indexSet1 To set1.Length - 1
                    result.Add(set1(i))
                Next

                For i As Integer = indexSet2 To set2.Length - 1
                    result.Add(set2(i))
                Next
            End If
            Return result.ToArray()
        End Function


        ''' <summary>
        ''' Remove any items in itemsToRmove from completeSet
        ''' </summary>
        ''' <param name="completeSet"></param>
        ''' <param name="itemsToRemove"></param>
        ''' <returns></returns>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Function RemoveItems(ByVal completeSet() As Integer, ByVal itemsToRemove() As Integer) As Integer()
            Dim indexCompleteSet As Integer = 0
            Dim indexItemsToRemove As Integer = 0

            Dim result As New List(Of Integer)
            If completeSet IsNot Nothing Then
                If itemsToRemove Is Nothing Then
                    itemsToRemove = New Integer() {}
                End If

                Do While indexCompleteSet < completeSet.Length AndAlso indexItemsToRemove < itemsToRemove.Length
                    ' Walk while the items in the set to remove is less than the items in the
                    ' complete set
                    While itemsToRemove(indexItemsToRemove) < completeSet(indexCompleteSet)
                        indexItemsToRemove += 1
                        If indexItemsToRemove >= itemsToRemove.Length Then Exit Do
                    End While

                    ' If we have a match, we should skip this item from adding to the result set
                    If itemsToRemove(indexItemsToRemove) = completeSet(indexCompleteSet) Then
                        indexItemsToRemove += 1
                    Else
                        result.Add(completeSet(indexCompleteSet))
                    End If
                    indexCompleteSet += 1
                Loop

                ' Add the remaining items from the complete set
                For i As Integer = indexCompleteSet To completeSet.Length - 1
                    result.Add(completeSet(i))
                Next
            End If
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Check if the numbers specified in SearchForNumbers are all included in the CompleteList
        ''' </summary>
        ''' <param name="CompleteList"></param>
        ''' <param name="SearchForNumbers"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function AreNumbersInList(ByVal CompleteList As Integer(), ByVal SearchForNumbers As Integer()) As TriState
            Dim foundNumbers As Integer = Intersect(CompleteList, SearchForNumbers).Length
            Dim numberOfItemsToFind As Integer = SearchForNumbers.Length

            If foundNumbers = numberOfItemsToFind Then
                Return TriState.True
            ElseIf foundNumbers = 0 Then
                Return TriState.False
            Else
                Return TriState.UseDefault
            End If
        End Function
#End Region

        Private Sub AdvancedOptionsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AdvancedOptionsButton.Click
            ShowChildPage(SR.GetString(SR.PPG_AdvancedCompilerSettings_Title), GetType(AdvCompilerSettingsPropPage), HelpKeywords.VBProjPropAdvancedCompile)
        End Sub

        Private Sub BuildEventsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles BuildEventsButton.Click
            ShowChildPage(SR.GetString(SR.PPG_BuildEventsTitle), GetType(BuildEventsPropPage))
        End Sub

        Private Sub BuildOutputPathButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles BuildOutputPathButton.Click
            Dim value As String = Nothing
            If GetDirectoryViaBrowseRelativeToProject(Me.BuildOutputPathTextBox.Text, SR.GetString(SR.PPG_SelectOutputPathTitle), value) Then
                Me.BuildOutputPathTextBox.Text = value
                SetDirty(BuildOutputPathTextBox, True)
            End If
        End Sub

        Private Sub GenerateXMLCheckBox_CheckStateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles GenerateXMLCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not _settingGenerateXmlDocumentation Then
                Me.SetDirty(VsProjPropId.VBPROJPROPID_DocumentationFile, True)
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropCompile
        End Function

        Private Function PlatformTargetSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                Me.TargetCPUComboBox.SelectedIndex = -1
            Else
                If (IsNothing(TryCast(value, String)) OrElse TryCast(value, String) = "") Then
                    Me.TargetCPUComboBox.SelectedItem = s_anyCPUPropertyValue
                Else
                    Me.TargetCPUComboBox.SelectedItem = TryCast(value, String)
                End If
            End If

            Return True
        End Function

        Private Function PlatformTargetGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = Me.TargetCPUComboBox.SelectedItem
            Return True
        End Function

#Region "Check if the current warning level settings correspond to option strict on, off or custom"
        ''' <summary>
        ''' Check to see if the warnings lists exactly correspond to the
        ''' Option Strict OFF settings
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsSameAsOptionStrictOff() As Boolean
            If _specWarnAsError IsNot Nothing AndAlso _
               _noWarn IsNot Nothing AndAlso _
               AreNumbersInList(_noWarn, _optionStrictIDs) = TriState.True AndAlso _
               AreNumbersInList(_specWarnAsError, _optionStrictIDs) = TriState.False _
            Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Check to see if the warnings lists exactly correspond to the
        ''' Option Strict ON settings
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsSameAsOptionStrictOn() As Boolean
            If _specWarnAsError IsNot Nothing AndAlso _
               _noWarn IsNot Nothing AndAlso _
               AreNumbersInList(_specWarnAsError, _optionStrictIDs) = TriState.True AndAlso _
               AreNumbersInList(_noWarn, _optionStrictIDs) = TriState.False _
            Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is this option strict custom?
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsSameAsOptionStrictCustom() As Boolean
            Return Not IsSameAsOptionStrictOn() AndAlso Not IsSameAsOptionStrictOff()
        End Function
#End Region

        Private Sub UpdatePropertiesFromCurrentState()
            ' If we are inside init, we are in the process of updating the list or if we have set
            ' a "global" treatment of (disable all warnings/treat all warnings as errors) we skip the actual set...
            If Not (m_fInsideInit OrElse _refreshingWarningsList OrElse TreatAllWarningsAsErrors() OrElse DisableAllWarnings()) Then
                'Get/Set the entire property set
                'Enumerate the rows and get the values to write
                Dim cell As DataGridViewCell
                Dim CellValue As String

                Dim ErrorsList As New List(Of Integer)
                Dim NoNotifyList As New List(Of Integer)

                cell = Me.WarningsGridView.CurrentCell

                For Index As Integer = 0 To WarningsGridView.Rows.Count - 1
                    cell = Me.WarningsGridView.Rows.Item(Index).Cells.Item(1)
                    CellValue = DirectCast(cell.EditedFormattedValue, String)
                    Dim Numbers As String = _errorInfos(Index).Numbers
                    If Numbers <> "" Then
                        If CellValue.Equals(_notifyNone) Then
                            For Each err As Integer In _errorInfos(Index).ErrList
                                NoNotifyList.Add(err)
                            Next
                        ElseIf CellValue.Equals(_notifyError) Then
                            For Each err As Integer In _errorInfos(Index).ErrList
                                ErrorsList.Add(err)
                            Next
                        ElseIf CellValue = "" Then
                            ' This is an indeterminate value - we should keep whatever we have in there
                            ' from before...
                            If _noWarn Is Nothing Then
                                Debug.Fail("Why did we try to update properties from current set with an empty noWarn?")
                                _noWarn = New Integer() {}
                            End If
                            For Each err As Integer In Intersect(_errorInfos(Index).ErrList, _noWarn)
                                NoNotifyList.Add(err)
                            Next
                            If _specWarnAsError Is Nothing Then
                                Debug.Fail("Why did we try to update properties from current set with an empty specWarnAsError?")
                                _specWarnAsError = New Integer() {}
                            End If
                            For Each err As Integer In Intersect(_errorInfos(Index).ErrList, _specWarnAsError)
                                ErrorsList.Add(err)
                            Next
                        End If
                    End If
                Next

                _noWarn = NoNotifyList.ToArray()
                _specWarnAsError = ErrorsList.ToArray()

                System.Array.Sort(_noWarn)
                System.Array.Sort(_specWarnAsError)

                ' Update option strict combobox...
                Dim optionStrictChanged As Boolean = False
                If (Not IsSameAsOptionStrictOn()) AndAlso IsOptionStrictOn() Then
                    OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOffText)
                    optionStrictChanged = True
                    ' Potentially update option strict to "custom"
                ElseIf IsSameAsOptionStrictOn() AndAlso (Not IsOptionStrictOn()) Then
                    optionStrictChanged = True
                    OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOnText)
                End If

                QueueUpdateOptionStrictComboBox()
                SetDirty(VsProjPropId2.VBPROJPROPID_NoWarn, False)
                SetDirty(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, False)
                If optionStrictChanged Then
                    SetDirty(VsProjPropId.VBPROJPROPID_OptionStrict, False)
                End If
                SetDirty(True)
            End If
        End Sub

        ''' <summary>
        ''' Set the warnings to ignore/warnings to report as error to correspond to the
        ''' option strictness that we have...
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <remarks></remarks>
        Private Sub ResetOptionStrictness(ByVal Value As String)
            OptionStrictComboBox.SelectedItem = Value
            Select Case Value
                Case _optionStrictOnText
                    _noWarn = RemoveItems(_noWarn, _optionStrictIDs)
                    _specWarnAsError = Union(_specWarnAsError, _optionStrictIDs)
                Case _optionStrictOffText
                    _specWarnAsError = RemoveItems(_specWarnAsError, _optionStrictIDs)
                    _noWarn = Union(_noWarn, _optionStrictIDs)
                Case _optionStrictCustomText
                    ' Just leave things as they are...
                Case Else
                    Debug.Fail("Unknown option strict level: " & Value)
            End Select

            SetDirty(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, False)
            SetDirty(VsProjPropId2.VBPROJPROPID_NoWarn, False)
            SetDirty(OptionStrictComboBox, False)
            SetDirty(True)
            If ProjectReloadedDuringCheckout Then
                Return
            End If
            UpdateWarningList()
        End Sub

        ''' <summary>
        ''' Whenever the user changes the option strict combobox in the UI, we have to
        ''' update the corresponding project properties
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub OptionStrictComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles OptionStrictComboBox.SelectionChangeCommitted
            If Not m_fInsideInit Then
                ResetOptionStrictness(TryCast(OptionStrictComboBox.SelectedItem, String))
            End If
        End Sub

        Private Sub TargetCPUComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles TargetCPUComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            ' Changes to the TargetCPU may affect whether 'Prefer32Bit' is enabled
            RefreshEnabledStatusForPrefer32Bit(Me.Prefer32BitCheckBox)
        End Sub

        ''' <summary>
        ''' Override PreApplyPageChanges to validate and potentially warn the user about untrusted output
        ''' path.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub PreApplyPageChanges()
            If Me.GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath).IsDirty Then
                Try
                    Dim absPath As String = Path.Combine(GetProjectPath(), GetProjectRelativeDirectoryPath(Trim(BuildOutputPathTextBox.Text)))
                    If Not CheckPath(absPath) Then
                        If DesignerFramework.DesignerMessageBox.Show(ServiceProvider, _
                                                                    SR.GetString(SR.PPG_OutputPathNotSecure), _
                                                                    DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), _
                                                                    MessageBoxButtons.OKCancel, _
                                                                    MessageBoxIcon.Warning) = DialogResult.Cancel _
                        Then
                            ' Set the focus back to the offending control!
                            BuildOutputPathTextBox.Focus()
                            BuildOutputPathTextBox.Clear()
                            Throw New System.Runtime.InteropServices.COMException("", Interop.win.OLE_E_PROMPTSAVECANCELLED)
                        End If
                    End If
                Catch ex As System.ApplicationException
                    ' The old behavior was to assume a secure path if exceptio occured...
                End Try
            End If
            MyBase.PreApplyPageChanges()
        End Sub

        ''' <summary>
        ''' Check if the path is a trusted path or not
        ''' </summary>
        ''' <param name="path"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This code was ported from langutil.cpp (function LuCheckSecurityLevel)
        ''' If that code ever changes, we've gotta update this as well...
        ''' </remarks>
        Private Function CheckPath(ByVal path As String) As Boolean
            If path Is Nothing Then
                Throw New System.ArgumentNullException("path")
            End If


            If Not System.IO.Path.IsPathRooted(path) Then
                Throw Common.CreateArgumentException("path")
            End If

            ' Some additional verification is done by Path.GetFullPath...
            Dim absPath As String = System.IO.Path.GetFullPath(path)

            Dim internetSecurityManager As Interop.IInternetSecurityManager = Nothing

            ' We've got to get a fresh instance of the InternetSecurityManager, since it seems that the instance we
            ' can get from our ServiceProvider can't Map URLs to zones...
            Dim localReg As ILocalRegistry2 = TryCast(ServiceProvider.GetService(GetType(ILocalRegistry)), ILocalRegistry2)
            If localReg IsNot Nothing Then
                Dim ObjectPtr As System.IntPtr = IntPtr.Zero
                Try
                    Static CLSID_InternetSecurityManager As New System.Guid("7b8a2d94-0ac9-11d1-896c-00c04fb6bfc4")
                    VSErrorHandler.ThrowOnFailure(localReg.CreateInstance(CLSID_InternetSecurityManager, Nothing, Interop.NativeMethods.IID_IUnknown, Interop.win.CLSCTX_INPROC_SERVER, ObjectPtr))
                    internetSecurityManager = TryCast(System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(ObjectPtr), Interop.IInternetSecurityManager)
                Catch Ex As Exception When Not Common.IsUnrecoverable(Ex)
                    Debug.Fail(String.Format("Failed to create Interop.IInternetSecurityManager: {0}", Ex))
                Finally
                    If ObjectPtr <> IntPtr.Zero Then
                        System.Runtime.InteropServices.Marshal.Release(ObjectPtr)
                    End If
                End Try
            End If

            If internetSecurityManager Is Nothing Then
                Debug.Fail("Failed to create an InternetSecurityManager")
                Throw New System.ApplicationException
            End If

            Dim zone As Integer
            Dim hr As Integer = internetSecurityManager.MapUrlToZone(absPath, zone, 0)

            If VSErrorHandler.Failed(hr) Then
                ' If we can't map the absolute path to a zone, we silently fail...
                Return True
            End If

            Dim folderEvidence As System.Security.Policy.Evidence = New System.Security.Policy.Evidence()
            folderEvidence.AddHostEvidence(New System.Security.Policy.Url("file:///" & absPath))
            folderEvidence.AddHostEvidence(New System.Security.Policy.Zone(CType(zone, System.Security.SecurityZone)))
            Dim folderPSet As System.Security.PermissionSet = System.Security.SecurityManager.GetStandardSandbox(folderEvidence)

            ' Get permission set that is granted to local code running on the local machine.
            Dim localEvidence As New System.Security.Policy.Evidence()
            localEvidence.AddHostEvidence(New System.Security.Policy.Zone(System.Security.SecurityZone.MyComputer))

            Dim localPSet As System.Security.PermissionSet = System.Security.SecurityManager.GetStandardSandbox(localEvidence)
            localPSet.RemovePermission((New System.Security.Permissions.ZoneIdentityPermission(System.Security.SecurityZone.MyComputer)).GetType())

            ' Return true if permission set that would be granted to code in
            ' target folder is equal (or greater than) that granted to local code.
            If localPSet.IsSubsetOf(folderPSet) Then
                Return True
            Else
                Return False
            End If
        End Function


        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComboBoxes_DropDown(ByVal sender As Object, ByVal e As EventArgs) Handles OptionStrictComboBox.DropDown
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Event handler for value changed events fired from the warnings grid view
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub NotificationLevelChanged(ByVal sender As Object, ByVal e As DataGridViewCellEventArgs) Handles WarningsGridView.CellValueChanged
            If Not m_fInsideInit AndAlso Not _refreshingWarningsList AndAlso e.RowIndex >= 0 AndAlso e.ColumnIndex = s_notifyColumnIndex Then
                UpdatePropertiesFromCurrentState()
            End If
        End Sub

        ''' <summary>
        ''' If we have indeterminate values for either the noWarn or specWarnAsError, we have got to
        ''' reset the properties in a known state before we can make any changes.
        '''
        ''' Let's prompt the user so (s)he can make this decision.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub EnsureNotConflictingSettings(ByVal sender As Object, ByVal e As DataGridViewCellCancelEventArgs) Handles WarningsGridView.CellBeginEdit
            If IndeterminateWarningsState Then
                'Prompt user for resetting settings...
                If DesignerFramework.DesignUtil.ShowMessage(ServiceProvider, SR.GetString(SR.PPG_Compile_ResetIndeterminateWarningLevels), DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) = DialogResult.OK Then
                    _noWarn = _optionStrictIDs
                    _specWarnAsError = New Integer() {}
                    UpdateWarningList()
                Else
                    e.Cancel = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' This is a workaround for event notification not being sent by the datagridview when
        ''' the combobox sends SelectionChangeCommitted
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class InternalDataGridView
            Inherits DesignerFramework.DesignerDataGridView

            Public Overrides Sub NotifyCurrentCellDirty(ByVal dirty As Boolean)
                MyBase.NotifyCurrentCellDirty(dirty)

                If dirty Then
                    Me.CommitEdit(DataGridViewDataErrorContexts.Commit)
                End If
            End Sub

        End Class

        ''' <summary>
        ''' PropertyControlData that always acts as if you have selected the "all configurations/all platforms"
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class FakeAllConfigurationsPropertyControlData
            Inherits PropertyControlData

            ''' <summary>
            ''' Since it is expensive to get the extended objects, and all PropertyControlDatas on the same
            ''' page share the same set, we keep a shared cache around... All we need is a service provider
            ''' and someone to Reset us when the SetObjects is called...
            ''' </summary>
            Friend Class ConfigurationObjectCache
                ' Cached properties for the extended and raw config objects
                Private _extendedObjects() As Object
                Private _rawObjects() As Object

                ' Cached instance of our IVsCfgProvider2 instance
                Private _vscfgprovider As IVsCfgProvider2

                ' Cached hierarchy
                Private _hierarchy As IVsHierarchy

                ' Cached service provider
                Private _serviceProvider As IServiceProvider

                ''' <summary>
                ''' Create a new instance of
                ''' </summary>
                Friend Sub New(ByVal Hierarchy As IVsHierarchy, ByVal ServiceProvider As IServiceProvider)
                    _hierarchy = Hierarchy
                    _serviceProvider = ServiceProvider
                End Sub

                ''' <summary>
                ''' Reset our cached values if we have a new hierarchy and/or serviceprovider
                ''' </summary>
                Friend Sub Reset(ByVal Hierarchy As IVsHierarchy, ByVal ServiceProvider As IServiceProvider, ByVal forceReset As Boolean)
                    If forceReset OrElse _hierarchy IsNot Hierarchy OrElse _serviceProvider IsNot ServiceProvider Then
                        _hierarchy = Hierarchy
                        _serviceProvider = ServiceProvider
                        _extendedObjects = Nothing
                        _rawObjects = Nothing
                        _vscfgprovider = Nothing
                    End If
                End Sub

                ''' <summary>
                ''' Private getter for the IVsCfgProvider2 for the associated proppage's hierarchy
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Private ReadOnly Property VsCfgProvider() As IVsCfgProvider2
                    Get
                        If _vscfgprovider Is Nothing Then
                            Dim Value As Object = Nothing
                            VSErrorHandler.ThrowOnFailure(_hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, Value))
                            _vscfgprovider = TryCast(Value, IVsCfgProvider2)
                        End If
                        Debug.Assert(_vscfgprovider IsNot Nothing, "Failed to get config provider")
                        Return _vscfgprovider
                    End Get
                End Property

                ''' <summary>
                ''' Getter for the raw config objects. We override this to always return the properties for all
                ''' configurations to make this property look like a config independent-ish property
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Friend ReadOnly Property ConfigRawPropertiesObjects() As Object()
                    Get
                        Dim tmpRawObjects() As IVsCfg
                        Dim ConfigCount As UInteger() = New UInteger(0) {} 'Interop declaration requires us to use an array
                        VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(0, Nothing, ConfigCount, Nothing))
                        Debug.Assert(ConfigCount(0) > 0, "Why no configs?")
                        tmpRawObjects = New IVsCfg(CInt(ConfigCount(0)) - 1) {}
                        Dim ActualCount As UInteger() = New UInteger(0) {}
                        VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(CUInt(tmpRawObjects.Length), tmpRawObjects, ActualCount, Nothing))
                        Debug.Assert(ActualCount(0) = ConfigCount(0), "Unexpected # of configs returned")
                        Dim rawObjects(tmpRawObjects.Length - 1) As Object
                        tmpRawObjects.CopyTo(rawObjects, 0)
                        Return rawObjects
                    End Get
                End Property

                ''' <summary>
                ''' Getter for the extended config objects. We override this to always return the properties for all
                ''' configurations to make this property look like a config independent-ish property
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Friend ReadOnly Property ConfigExtendedPropertiesObjects() As Object()
                    Get
                        If _extendedObjects Is Nothing Then
                            Dim aem As Microsoft.VisualStudio.Editors.PropertyPages.AutomationExtenderManager
                            aem = Microsoft.VisualStudio.Editors.PropertyPages.AutomationExtenderManager.GetAutomationExtenderManager(_serviceProvider)
                            _extendedObjects = aem.GetExtendedObjects(ConfigRawPropertiesObjects)
                            Debug.Assert(_extendedObjects IsNot Nothing, "Extended objects unavailable")
                        End If
                        Return _extendedObjects
                    End Get
                End Property
            End Class

            ' Shared cache of raw & extended configuration objects
            Private _configurationObjectCache As ConfigurationObjectCache

            ' Create a new instance
            Public Sub New(ByVal ConfigurationObjectCache As ConfigurationObjectCache, ByVal id As Integer, ByVal name As String, ByVal control As Control, ByVal setter As SetDelegate, ByVal getter As GetDelegate, ByVal flags As ControlDataFlags, ByVal AssocControls As System.Windows.Forms.Control())
                MyBase.New(id, name, control, setter, getter, flags, AssocControls)
                _configurationObjectCache = ConfigurationObjectCache
            End Sub

            ''' <summary>
            ''' Getter for the raw config objects. We override this to always return the properties for all
            ''' configurations to make this property look like a config independent-ish property
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Overrides ReadOnly Property RawPropertiesObjects() As Object()
                Get
                    Return _configurationObjectCache.ConfigRawPropertiesObjects()
                End Get
            End Property

            ''' <summary>
            ''' Getter for the extended config objects. We override this to always return the properties for all
            ''' configurations to make this property look like a config independent-ish property
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Overrides ReadOnly Property ExtendedPropertiesObjects() As Object()
                Get
                    Return _configurationObjectCache.ConfigExtendedPropertiesObjects()
                End Get
            End Property

        End Class

    End Class

End Namespace

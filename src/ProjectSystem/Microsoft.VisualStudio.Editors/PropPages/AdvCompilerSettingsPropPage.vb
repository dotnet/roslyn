'******************************************************************************
'* AdvCompilerSettingsPropPage.vb
'*
'* Copyright (C) 1999-2004 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************
'
'This is the advanced compiler options page for VB only.


Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.Runtime.Versioning
Imports System.Windows.Forms
Imports VB = Microsoft.VisualBasic
Imports VBStrings = Microsoft.VisualBasic.Strings
Imports VSLangProj80
Imports VslangProj90
Imports VslangProj100

Imports Microsoft.VisualStudio.Editors.Common
Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class AdvCompilerSettingsPropPage
        Inherits PropPageUserControlBase
        'Inherits UserControl

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            'We don't want this localized, and the WinForms designer will do that automatically if
            '  we have it in InitializeComponent.
            Me.DebugInfoComboBox.Items.AddRange(New Object() {"None", "Full", "pdb-only"})

            Me.MinimumSize = Me.PreferredSize()

            AddChangeHandlers()
            MyBase.PageRequiresScaling = False
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Friend WithEvents RemoveIntegerChecks As System.Windows.Forms.CheckBox
        Friend WithEvents Optimize As System.Windows.Forms.CheckBox
        Friend WithEvents DefineDebug As System.Windows.Forms.CheckBox
        Friend WithEvents DefineTrace As System.Windows.Forms.CheckBox
        Friend WithEvents DllBaseLabel As System.Windows.Forms.Label
        Friend WithEvents CustomConstantsExampleLabel As System.Windows.Forms.Label
        Friend WithEvents DefineConstantsTextbox As System.Windows.Forms.TextBox
        Friend WithEvents OptimizationsSeparatorLabel As System.Windows.Forms.Label
        Friend WithEvents CompilationConstantsLabel As System.Windows.Forms.Label
        Friend WithEvents OptimizationsLabel As System.Windows.Forms.Label
        Friend WithEvents DllBaseTextbox As System.Windows.Forms.TextBox
        Friend WithEvents ConstantsSeparatorLabel As System.Windows.Forms.Label
        Friend WithEvents GenerateSerializationAssembliesLabel As System.Windows.Forms.Label
        Friend WithEvents GenerateSerializationAssemblyComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents optimizationTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents compilationConstantsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents GenerateDebugInfoLabel As System.Windows.Forms.Label
        Friend WithEvents DebugInfoComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents CustomConstantsLabel As System.Windows.Forms.Label
        Friend WithEvents CompileWithDotNetNative As System.Windows.Forms.CheckBox
        Friend WithEvents EnableGatekeeperAnAlysis As System.Windows.Forms.CheckBox

        'PERF: A note about the labels used as lines.  The 3D label is being set to 1 px high,
        '   so you're really only using the grey part of it.  Using BorderStyle.Fixed3D seems
        '   to fire an extra resize OnHandleCreated.  The simple solution is to use BorderStyle.None 
        '   and BackColor = SystemColors.ControlDark.

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvCompilerSettingsPropPage))
            Me.OptimizationsLabel = New System.Windows.Forms.Label
            Me.RemoveIntegerChecks = New System.Windows.Forms.CheckBox
            Me.Optimize = New System.Windows.Forms.CheckBox
            Me.DllBaseLabel = New System.Windows.Forms.Label
            Me.DllBaseTextbox = New System.Windows.Forms.TextBox
            Me.DefineDebug = New System.Windows.Forms.CheckBox
            Me.DefineTrace = New System.Windows.Forms.CheckBox
            Me.CustomConstantsExampleLabel = New System.Windows.Forms.Label
            Me.DefineConstantsTextbox = New System.Windows.Forms.TextBox
            Me.CustomConstantsLabel = New System.Windows.Forms.Label
            Me.OptimizationsSeparatorLabel = New System.Windows.Forms.Label
            Me.CompilationConstantsLabel = New System.Windows.Forms.Label
            Me.ConstantsSeparatorLabel = New System.Windows.Forms.Label
            Me.GenerateSerializationAssembliesLabel = New System.Windows.Forms.Label
            Me.GenerateSerializationAssemblyComboBox = New System.Windows.Forms.ComboBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.compilationConstantsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.optimizationTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.GenerateDebugInfoLabel = New System.Windows.Forms.Label
            Me.DebugInfoComboBox = New System.Windows.Forms.ComboBox
            Me.CompileWithDotNetNative = New System.Windows.Forms.CheckBox()
            Me.EnableGatekeeperAnAlysis = New System.Windows.Forms.CheckBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.compilationConstantsTableLayoutPanel.SuspendLayout()
            Me.optimizationTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'OptimizationsLabel
            '
            resources.ApplyResources(Me.OptimizationsLabel, "OptimizationsLabel")
            Me.OptimizationsLabel.Name = "OptimizationsLabel"
            '
            'RemoveIntegerChecks
            '
            resources.ApplyResources(Me.RemoveIntegerChecks, "RemoveIntegerChecks")
            Me.RemoveIntegerChecks.Name = "RemoveIntegerChecks"

            '
            ' CompileWithDotNetNative
            '
            resources.ApplyResources(Me.CompileWithDotNetNative, "CompileWithDotNetNative")
            Me.CompileWithDotNetNative.Name = "CompileWithDotNetNative"
            resources.ApplyResources(Me.EnableGatekeeperAnAlysis, "EnableGatekeeperAnalysis")
            Me.EnableGatekeeperAnAlysis.Name = "EnableGatekeeperAnalysis"

            '
            'Optimize
            '
            resources.ApplyResources(Me.Optimize, "Optimize")
            Me.Optimize.Name = "Optimize"
            '
            'DllBaseLabel
            '
            resources.ApplyResources(Me.DllBaseLabel, "DllBaseLabel")
            Me.DllBaseLabel.Name = "DllBaseLabel"
            '
            'DllBaseTextbox
            '
            resources.ApplyResources(Me.DllBaseTextbox, "DllBaseTextbox")
            Me.DllBaseTextbox.Name = "DllBaseTextbox"
            '
            'DefineDebug
            '
            resources.ApplyResources(Me.DefineDebug, "DefineDebug")
            Me.DefineDebug.Name = "DefineDebug"
            '
            'DefineTrace
            '
            resources.ApplyResources(Me.DefineTrace, "DefineTrace")
            Me.DefineTrace.Name = "DefineTrace"
            '
            'CustomConstantsExampleLabel
            '
            resources.ApplyResources(Me.CustomConstantsExampleLabel, "CustomConstantsExampleLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CustomConstantsExampleLabel, 2)
            Me.CustomConstantsExampleLabel.Name = "CustomConstantsExampleLabel"
            '
            'DefineConstantsTextbox
            '
            resources.ApplyResources(Me.DefineConstantsTextbox, "DefineConstantsTextbox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.DefineConstantsTextbox, 2)
            Me.DefineConstantsTextbox.Name = "DefineConstantsTextbox"
            '
            'CustomConstantsLabel
            '
            resources.ApplyResources(Me.CustomConstantsLabel, "CustomConstantsLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CustomConstantsLabel, 2)
            Me.CustomConstantsLabel.Name = "CustomConstantsLabel"
            '
            'OptimizationsSeparatorLabel
            '
            Me.OptimizationsSeparatorLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.OptimizationsSeparatorLabel, "OptimizationsSeparatorLabel")
            Me.OptimizationsSeparatorLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.OptimizationsSeparatorLabel.Name = "OptimizationsSeparatorLabel"
            '
            'CompilationConstantsLabel
            '
            resources.ApplyResources(Me.CompilationConstantsLabel, "CompilationConstantsLabel")
            Me.CompilationConstantsLabel.Name = "CompilationConstantsLabel"
            '
            'ConstantsSeparatorLabel
            '
            Me.ConstantsSeparatorLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.ConstantsSeparatorLabel, "ConstantsSeparatorLabel")
            Me.ConstantsSeparatorLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.ConstantsSeparatorLabel.Name = "ConstantsSeparatorLabel"
            '
            'GenerateSerializationAssembliesLabel
            '
            resources.ApplyResources(Me.GenerateSerializationAssembliesLabel, "GenerateSerializationAssembliesLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.GenerateSerializationAssembliesLabel, 2)
            Me.GenerateSerializationAssembliesLabel.Name = "GenerateSerializationAssembliesLabel"
            '
            'GenerateSerializationAssemblyComboBox
            '
            resources.ApplyResources(Me.GenerateSerializationAssemblyComboBox, "GenerateSerializationAssemblyComboBox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.GenerateSerializationAssemblyComboBox, 2)
            Me.GenerateSerializationAssemblyComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.GenerateSerializationAssemblyComboBox.FormattingEnabled = True
            Me.GenerateSerializationAssemblyComboBox.Name = "GenerateSerializationAssemblyComboBox"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CustomConstantsExampleLabel, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateSerializationAssemblyComboBox, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineConstantsTextbox, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateSerializationAssembliesLabel, 0, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CustomConstantsLabel, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.compilationConstantsTableLayoutPanel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DllBaseLabel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.optimizationTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoveIntegerChecks, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineDebug, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateDebugInfoLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DebugInfoComboBox, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineTrace, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Optimize, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DllBaseTextbox, 1, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompileWithDotNetNative, 0, 12)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.EnableGatekeeperAnAlysis, 0, 13)

            '
            'compilationConstantsTableLayoutPanel
            '
            resources.ApplyResources(Me.compilationConstantsTableLayoutPanel, "compilationConstantsTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.compilationConstantsTableLayoutPanel, 2)
            Me.compilationConstantsTableLayoutPanel.Controls.Add(Me.CompilationConstantsLabel, 0, 0)
            Me.compilationConstantsTableLayoutPanel.Controls.Add(Me.ConstantsSeparatorLabel, 1, 0)
            Me.compilationConstantsTableLayoutPanel.Name = "compilationConstantsTableLayoutPanel"
            '
            'optimizationTableLayoutPanel
            '
            resources.ApplyResources(Me.optimizationTableLayoutPanel, "optimizationTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.optimizationTableLayoutPanel, 2)
            Me.optimizationTableLayoutPanel.Controls.Add(Me.OptimizationsLabel, 0, 0)
            Me.optimizationTableLayoutPanel.Controls.Add(Me.OptimizationsSeparatorLabel, 1, 0)
            Me.optimizationTableLayoutPanel.Name = "optimizationTableLayoutPanel"
            '
            'GenerateDebugInfoLabel
            '
            resources.ApplyResources(Me.GenerateDebugInfoLabel, "GenerateDebugInfoLabel")
            Me.GenerateDebugInfoLabel.Name = "GenerateDebugInfoLabel"
            '
            'DebugInfoComboBox
            '
            resources.ApplyResources(Me.DebugInfoComboBox, "DebugInfoComboBox")
            Me.DebugInfoComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.DebugInfoComboBox.FormattingEnabled = True
            Me.DebugInfoComboBox.Name = "DebugInfoComboBox"
            '
            'AdvCompilerSettingsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AdvCompilerSettingsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.compilationConstantsTableLayoutPanel.ResumeLayout(False)
            Me.compilationConstantsTableLayoutPanel.PerformLayout()
            Me.optimizationTableLayoutPanel.ResumeLayout(False)
            Me.optimizationTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

        Enum TreatWarningsSetting
            WARNINGS_ALL
            WARNINGS_SPECIFIC
            WARNINGS_NONE
        End Enum

        'The state of the DebugSymbols value - true or false
        '  This is automatically set to true whenever the value in the DebugInfo combobox is set to something else
        '  than None, and false otherwise
        Private m_DebugSymbols As Object

        Private Const DEBUGINFO_NONE As String = ""
        Private Const DEBUGINFO_FULL As String = "full"

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(
                        VsProjPropId.VBPROJPROPID_RemoveIntegerChecks, "RemoveIntegerChecks", Me.RemoveIntegerChecks),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_Optimize, "Optimize", Me.Optimize),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_BaseAddress, "BaseAddress", Me.DllBaseTextbox, AddressOf Me.SetBaseAddress, AddressOf Me.GetBaseAddress, ControlDataFlags.None, New Control() {Me.DllBaseLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release, VsProjPropId.VBPROJPROPID_DebugSymbols, "DebugSymbols", Nothing, AddressOf Me.DebugSymbolsSet, AddressOf Me.DebugSymbolsGet),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_DebugInfo, "DebugInfo", DebugInfoComboBox, AddressOf DebugInfoSet, AddressOf DebugInfoGet, ControlDataFlags.UserHandledEvents, New Control() {Me.GenerateDebugInfoLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release, VsProjPropId.VBPROJPROPID_DefineDebug, "DefineDebug", Me.DefineDebug),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineTrace, "DefineTrace", Me.DefineTrace),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants, "DefineConstants", Me.DefineConstantsTextbox, New Control() {Me.CustomConstantsLabel, Me.CustomConstantsExampleLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_GenerateSerializationAssemblies, "GenerateSerializationAssemblies", Me.GenerateSerializationAssemblyComboBox, AssocControls:=New Control() {GenerateSerializationAssembliesLabel}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_OutputType, "OutputType", Nothing, ControlDataFlags.Hidden),
                    New HiddenIfMissingPropertyControlData(1, "UseDotNetNativeToolchain", Me.CompileWithDotNetNative),
                    New HiddenIfMissingPropertyControlData(1, "RunGatekeeperAudit", Me.EnableGatekeeperAnAlysis)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

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

            ' Only enable the dll base adress for library type projects...
            Dim dllBaseEnabled As Boolean = False
            Dim pcd As PropertyControlData = Me.GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputType)
            If pcd IsNot Nothing Then
                Dim oOutputType As Object = pcd.TryGetPropertyValueNative(m_ExtendedObjects)
                If Not PropertyControlData.IsSpecialValue(oOutputType) Then
                    Dim prjOutputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                    If prjOutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                        dllBaseEnabled = True
                    End If
                End If
            End If
            Me.DllBaseTextbox.Enabled = dllBaseEnabled
        End Sub

        ''' <summary>
        ''' Format baseaddress value into VB hex notation
        ''' </summary>
        ''' <param name="BaseAddress"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ToHexAddress(ByVal BaseAddress As UInt64) As String
            Debug.Assert(BaseAddress >= 0 AndAlso BaseAddress <= UInt32.MaxValue, "Invalid baseaddress value")

            Return "&H" & String.Format("{0:X8}", CUInt(BaseAddress))
        End Function

        ''' <summary>
        ''' Converts BaseAddress property to VB hext format for UI
        ''' Called by base class code through delegate
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SetBaseAddress(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal obj As Object) As Boolean
            control.Text = "&H" & String.Format("{0:X8}", obj)
            Return True
        End Function

        ''' <summary>
        ''' Converts the string BaseAddress text to the native property type of UInt32
        ''' Called by base class code through delegate
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetBaseAddress(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef obj As Object) As Boolean
            obj = GetBaseAddressFromControl(control)
            Return True
        End Function

        ''' <summary>
        ''' Converts the string BaseAddress text to the native property type of UInt32
        ''' Called by base class code through delegate
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetBaseAddressFromControl(ByVal control As Control) As UInteger
            Dim StringValue As String = Trim(control.Text)
            Dim LongValue As ULong = 0

            'DLL Baseaddress must be &Hxxxxxxxx format
            If String.Compare(VBStrings.Left(StringValue, 2), "&H", StringComparison.OrdinalIgnoreCase) = 0 AndAlso IsNumeric(StringValue) Then
                Try
                    LongValue = CULng(StringValue)
                    If LongValue < UInt32.MaxValue Then
                        Return CUInt(LongValue)
                    End If
                Catch ex As Exception
                    'Let throw below
                End Try
            End If
            Throw New FormatException(SR.GetString(SR.PPG_InvalidHexString))
        End Function

        ''' <summary>
        ''' Get the debug symbols flag. 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function DebugSymbolsGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If TypeOf m_DebugSymbols Is Boolean Then
                value = CType(m_DebugSymbols, Boolean)
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Set the debug symbols flag
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function DebugSymbolsSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            m_DebugSymbols = value
            Return True
        End Function

        ''' <summary>
        ''' Gets the DebugInfo property (DebugType in the proj file).  This is either None, 
        '''   Full or PDB-Only.
        '''   In the VB property pages, the user is given only the choice of whether
        '''   to generate debug info or not.  But setting only that property on/off
        '''   without also changing the DebugInfo property can lead to confusion in the
        '''   build engine (esp. if the DebugType is also set in the proj file).  So we
        '''   change this property when the DebugSymbols property is set.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function DebugInfoSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                Me.DebugInfoComboBox.SelectedIndex = -1
            Else
                Dim stValue As String = TryCast(value, String)
                If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then

                    '// Need to special case pdb-only becuase it's stored in the property without the dash but it's
                    '// displayed in the dialog with a dash.

                    If (String.Compare(stValue, "pdbonly", StringComparison.OrdinalIgnoreCase) <> 0) Then
                        Me.DebugInfoComboBox.Text = stValue
                    Else
                        Me.DebugInfoComboBox.Text = "pdb-only"
                    End If
                Else
                    Me.DebugInfoComboBox.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function DebugInfoGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            ' Need to special case pdb-only because the display name has a dash while the actual property value
            ' doesn't have the dash.
            If String.Equals(Me.DebugInfoComboBox.Text, "pdb-only", StringComparison.OrdinalIgnoreCase) Then
                value = "pdbonly"
            Else
                value = Me.DebugInfoComboBox.Text
            End If
            Return True
        End Function

        ''' <summary>
        ''' Whenever the user changes the selection in the debug info combobox, we have to update both the
        ''' DebugInfo and DebugSymbols properties...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DebugInfoComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As EventArgs) Handles DebugInfoComboBox.SelectionChangeCommitted
            If DebugInfoComboBox.SelectedIndex = 0 Then
                ' Index 0 corresponds to "None" 
                m_DebugSymbols = False
            Else
                m_DebugSymbols = True
            End If
            SetDirty(VsProjPropId80.VBPROJPROPID_DebugInfo, False)
            SetDirty(VsProjPropId.VBPROJPROPID_DebugSymbols, False)
            SetDirty(True)
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAdvancedCompile
        End Function

        ''' <summary>
        ''' Validation method for BaseAddress
        ''' no cancellation, just normalizes value if not an error condition
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub BaseAddress_Validating(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles DllBaseTextbox.Validating
            Dim StringValue As String = Trim(Me.DllBaseTextbox.Text)

            Const DEFAULT_DLLBASEADDRESS As String = "&H11000000"

            If StringValue = "" Then
                Me.DllBaseTextbox.Text = DEFAULT_DLLBASEADDRESS

            ElseIf String.Compare(VBStrings.Left(StringValue, 2), "&H", StringComparison.OrdinalIgnoreCase) = 0 AndAlso IsNumeric(StringValue) Then
                Dim LongValue As ULong = CULng(StringValue)
                If LongValue < UInt32.MaxValue Then
                    'Reformat into clean
                    DllBaseTextbox.Text = ToHexAddress(LongValue)
                Else
                    'Cancel here prevents swithing to another window
                    'e.Cancel = True
                    'Throw New Exception(SR.GetString(SR.PPG_InvalidHexString))
                End If

            Else
                'Should we put up a UI glyph beside the textbox showing the error?
                'Status bar error text?

                'e.Cancel = True
            End If
        End Sub

        ''' <summary>
        ''' Validation properties
        ''' </summary>
        ''' <param name="controlData"></param>
        ''' <param name="message"></param>
        ''' <param name="returnControl"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ValidateProperty(ByVal controlData As PropertyControlData, ByRef message As String, ByRef returnControl As System.Windows.Forms.Control) As ValidationResult
            If controlData.FormControl Is DllBaseTextbox Then
                Try
                    GetBaseAddressFromControl(DllBaseTextbox)
                Catch ex As FormatException
                    message = ex.Message
                    Return ValidationResult.Failed
                End Try
            End If
            Return ValidationResult.Succeeded
        End Function
    End Class

End Namespace


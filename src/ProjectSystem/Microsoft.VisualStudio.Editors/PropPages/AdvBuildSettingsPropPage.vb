Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Globalization
Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.PlatformUI
Imports VBStrings = Microsoft.VisualBasic.Strings
Imports VSLangProj80
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class AdvBuildSettingsPropPage
        'Inherits System.Windows.Forms.UserControl
        ' If you want to be able to use the forms designer to edit this file,
        ' change the base class from PropPageUserControlBase to UserControl
        Inherits PropPageUserControlBase


#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            ' Scale the width of the overarching table layout panel
            Me.overarchingTableLayoutPanel.Width = DpiHelper.LogicalToDeviceUnitsX(overarchingTableLayoutPanel.Width)

            Me.MinimumSize = Me.PreferredSize()

            AddChangeHandlers()

            Me.AutoScaleMode = AutoScaleMode.Font
            MyBase.PageRequiresScaling = False
        End Sub

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Friend WithEvents lblLanguageVersion As System.Windows.Forms.Label
        Friend WithEvents lblReportCompilerErrors As System.Windows.Forms.Label
        Friend WithEvents chkOverflow As System.Windows.Forms.CheckBox
        Friend WithEvents cboLanguageVersion As System.Windows.Forms.ComboBox
        Friend WithEvents cboReportCompilerErrors As System.Windows.Forms.ComboBox
        Friend WithEvents lblDebugInfo As System.Windows.Forms.Label
        Friend WithEvents lblFileAlignment As System.Windows.Forms.Label
        Friend WithEvents lblDLLBase As System.Windows.Forms.Label
        Friend WithEvents cboDebugInfo As System.Windows.Forms.ComboBox
        Friend WithEvents cboFileAlignment As System.Windows.Forms.ComboBox
        Friend WithEvents txtDLLBase As System.Windows.Forms.TextBox
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalLabel As System.Windows.Forms.Label
        Friend WithEvents generalLineLabel As System.Windows.Forms.Label
        Friend WithEvents outputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents outputLabel As System.Windows.Forms.Label
        Friend WithEvents outputLineLabel As System.Windows.Forms.Label

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'PERF: A note about the labels used as lines.  The 3D label is being set to 1 px high,
        '   so you’re really only using the grey part of it.  Using BorderStyle.Fixed3D seems
        '   to fire an extra resize OnHandleCreated.  The simple solution is to use BorderStyle.None 
        '   and BackColor = SystemColors.ControlDark.

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvBuildSettingsPropPage))
            Me.lblLanguageVersion = New System.Windows.Forms.Label
            Me.lblReportCompilerErrors = New System.Windows.Forms.Label
            Me.cboLanguageVersion = New System.Windows.Forms.ComboBox
            Me.cboReportCompilerErrors = New System.Windows.Forms.ComboBox
            Me.lblDebugInfo = New System.Windows.Forms.Label
            Me.cboDebugInfo = New System.Windows.Forms.ComboBox
            Me.lblFileAlignment = New System.Windows.Forms.Label
            Me.cboFileAlignment = New System.Windows.Forms.ComboBox
            Me.lblDLLBase = New System.Windows.Forms.Label
            Me.txtDLLBase = New System.Windows.Forms.TextBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.outputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.outputLabel = New System.Windows.Forms.Label
            Me.outputLineLabel = New System.Windows.Forms.Label
            Me.generalTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.generalLabel = New System.Windows.Forms.Label
            Me.generalLineLabel = New System.Windows.Forms.Label
            Me.chkOverflow = New System.Windows.Forms.CheckBox
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.outputTableLayoutPanel.SuspendLayout()
            Me.generalTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblLanguageVersion
            '
            resources.ApplyResources(Me.lblLanguageVersion, "lblLanguageVersion")
            Me.lblLanguageVersion.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblLanguageVersion.Name = "lblLanguageVersion"
            '
            'cboLanguageVersion
            '
            resources.ApplyResources(Me.cboLanguageVersion, "cboLanguageVersion")
            Me.cboLanguageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboLanguageVersion.FormattingEnabled = True
            Me.cboLanguageVersion.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboLanguageVersion.Name = "cboLanguageVersion"
            '
            'lblReportCompilerErrors
            '
            resources.ApplyResources(Me.lblReportCompilerErrors, "lblReportCompilerErrors")
            Me.lblReportCompilerErrors.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblReportCompilerErrors.Name = "lblReportCompilerErrors"
            '
            'cboReportCompilerErrors
            '
            resources.ApplyResources(Me.cboReportCompilerErrors, "cboReportCompilerErrors")
            Me.cboReportCompilerErrors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboReportCompilerErrors.FormattingEnabled = True
            Me.cboReportCompilerErrors.Items.AddRange(New Object() {"none", "prompt", "send", "queue"})
            Me.cboReportCompilerErrors.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboReportCompilerErrors.Name = "cboReportCompilerErrors"
            '
            'lblDebugInfo
            '
            resources.ApplyResources(Me.lblDebugInfo, "lblDebugInfo")
            Me.lblDebugInfo.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblDebugInfo.Name = "lblDebugInfo"
            '
            'cboDebugInfo
            '
            resources.ApplyResources(Me.cboDebugInfo, "cboDebugInfo")
            Me.cboDebugInfo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboDebugInfo.FormattingEnabled = True
            Me.cboDebugInfo.Items.AddRange(New Object() {"none", "full", "pdb-only"})
            Me.cboDebugInfo.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboDebugInfo.Name = "cboDebugInfo"
            '
            'lblFileAlignment
            '
            resources.ApplyResources(Me.lblFileAlignment, "lblFileAlignment")
            Me.lblFileAlignment.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblFileAlignment.Name = "lblFileAlignment"
            '
            'cboFileAlignment
            '
            resources.ApplyResources(Me.cboFileAlignment, "cboFileAlignment")
            Me.cboFileAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboFileAlignment.FormattingEnabled = True
            Me.cboFileAlignment.Items.AddRange(New Object() {"512", "1024", "2048", "4096", "8192"})
            Me.cboFileAlignment.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboFileAlignment.Name = "cboFileAlignment"
            '
            'lblDLLBase
            '
            resources.ApplyResources(Me.lblDLLBase, "lblDLLBase")
            Me.lblDLLBase.Margin = New System.Windows.Forms.Padding(9, 3, 3, 0)
            Me.lblDLLBase.Name = "lblDLLBase"
            '
            'txtDLLBase
            '
            resources.ApplyResources(Me.txtDLLBase, "txtDLLBase")
            Me.txtDLLBase.Margin = New System.Windows.Forms.Padding(3, 3, 0, 0)
            Me.txtDLLBase.Name = "txtDLLBase"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.Controls.Add(Me.outputTableLayoutPanel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtDLLBase, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.generalTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDLLBase, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblFileAlignment, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboDebugInfo, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboFileAlignment, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDebugInfo, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboReportCompilerErrors, 1, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboLanguageVersion, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkOverflow, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblReportCompilerErrors, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblLanguageVersion, 0, 1)
            Me.overarchingTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'outputTableLayoutPanel
            '
            resources.ApplyResources(Me.outputTableLayoutPanel, "outputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.outputTableLayoutPanel, 2)
            Me.outputTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.outputTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLabel)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLineLabel)
            Me.outputTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 3, 0, 3)
            Me.outputTableLayoutPanel.Name = "outputTableLayoutPanel"
            Me.outputTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'outputLabel
            '
            resources.ApplyResources(Me.outputLabel, "outputLabel")
            Me.outputLabel.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.outputLabel.Name = "outputLabel"
            '
            'outputLineLabel
            '
            resources.ApplyResources(Me.outputLineLabel, "outputLineLabel")
            Me.outputLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.outputLineLabel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.outputLineLabel.Name = "outputLineLabel"
            '
            'generalTableLayoutPanel
            '
            resources.ApplyResources(Me.generalTableLayoutPanel, "generalTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.generalTableLayoutPanel, 2)
            Me.generalTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.generalTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLabel, 0, 0)
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLineLabel, 1, 0)
            Me.generalTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 0, 0, 3)
            Me.generalTableLayoutPanel.Name = "generalTableLayoutPanel"
            Me.generalTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'generalLabel
            '
            resources.ApplyResources(Me.generalLabel, "generalLabel")
            Me.generalLabel.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.generalLabel.Name = "generalLabel"
            '
            'generalLineLabel
            '
            resources.ApplyResources(Me.generalLineLabel, "generalLineLabel")
            Me.generalLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.generalLineLabel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.generalLineLabel.Name = "generalLineLabel"
            '
            'chkOverflow
            '
            resources.ApplyResources(Me.chkOverflow, "chkOverflow")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkOverflow, 2)
            Me.chkOverflow.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.chkOverflow.Name = "chkOverflow"
            '
            'AdvBuildSettingsPropPage
            '
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AdvBuildSettingsPropPage"
            resources.ApplyResources(Me, "$this")
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.outputTableLayoutPanel.ResumeLayout(False)
            Me.outputTableLayoutPanel.PerformLayout()
            Me.generalTableLayoutPanel.ResumeLayout(False)
            Me.generalTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region


        Protected m_bDebugSymbols As Boolean = False


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(CSharpProjPropId.CSPROJPROPID_LanguageVersion, "LanguageVersion", Me.cboLanguageVersion, AddressOf LanguageVersionSet, AddressOf LanguageVersionGet, ControlDataFlags.None, New Control() {Me.lblLanguageVersion}),
                    New PropertyControlData(CSharpProjPropId.CSPROJPROPID_ErrorReport, "ErrorReport", Me.cboReportCompilerErrors, AddressOf ErrorReportSet, AddressOf ErrorReportGet, ControlDataFlags.None, New Control() {Me.lblReportCompilerErrors}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_CheckForOverflowUnderflow, "CheckForOverflowUnderflow", Me.chkOverflow, AddressOf OverflowUnderflowSet, AddressOf OverflowUnderflowGet),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_FileAlignment, "FileAlignment", Me.cboFileAlignment, AddressOf FileAlignmentSet, AddressOf FileAlignmentGet, ControlDataFlags.None, New Control() {Me.lblFileAlignment}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_BaseAddress, "BaseAddress", Me.txtDLLBase, AddressOf BaseAddressSet, AddressOf BaseAddressGet, ControlDataFlags.None, New Control() {Me.lblDLLBase}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_DebugInfo, "DebugInfo", Me.cboDebugInfo, AddressOf DebugInfoSet, AddressOf DebugInfoGet, ControlDataFlags.None, New Control() {Me.lblDebugInfo}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DebugSymbols, "DebugSymbols", Nothing, AddressOf DebugSymbolsSet, AddressOf DebugSymbolsGet)}
                End If
                Return m_ControlData
            End Get
        End Property

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

            Me.cboLanguageVersion.Items.Clear()

            Me.cboLanguageVersion.Items.AddRange(CSharpLanguageVersionUtilities.GetAllLanguageVersions())
            Me.cboLanguageVersion.SelectedIndex = 0

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
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function LanguageVersionSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            Me.cboLanguageVersion.SelectedIndex = -1

            If PropertyControlData.IsSpecialValue(value) Then
                'Leave it unselected
            Else
                Dim stValue As String = CType(value, String)
                If stValue = "" Then
                    stValue = CSharpLanguageVersion.Default.Value
                End If

                For Each entry As CSharpLanguageVersion In Me.cboLanguageVersion.Items
                    If entry.Value = stValue Then
                        Me.cboLanguageVersion.SelectedItem = entry
                        Exit For
                    End If
                Next

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
        Private Function LanguageVersionGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim currentVersion As CSharpLanguageVersion = CType(CType(control, ComboBox).SelectedItem, CSharpLanguageVersion)
            If currentVersion IsNot Nothing Then
                value = currentVersion.Value
                Return True
            End If

            Debug.Fail("The combobox should not have still been unselected yet be dirty")
            Return False

        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function ErrorReportSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    Me.cboReportCompilerErrors.Text = stValue
                Else
                    Me.cboReportCompilerErrors.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
                Return True
            Else
                Me.cboReportCompilerErrors.SelectedIndex = -1        '// Indeterminate state
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function ErrorReportGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.cboReportCompilerErrors.SelectedIndex <> -1) Then
                value = Me.cboReportCompilerErrors.Text
                Return True
            Else
                Return False         '// Indeterminate - let the architecture handle it
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function DebugSymbolsSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate/IsMissing 
                m_bDebugSymbols = False
            Else
                m_bDebugSymbols = CType(value, Boolean)
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
        Private Function DebugSymbolsGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = m_bDebugSymbols
            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function BaseAddressSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (IsExeProject()) Then
                '// EXE's don't support base addresses so just disable the control and set the disabled text to the default for 
                '// EXE's.

                Me.txtDLLBase.Enabled = False
                Me.txtDLLBase.Text = "0x00400000"
            Else
                '// The default for DLL projects is 0x11000000
                Me.txtDLLBase.Enabled = True

                Dim iBaseAddress As UInteger

                If (TypeOf (value) Is UInteger) Then
                    iBaseAddress = CUInt(value)
                Else
                    '// Since it's bogus just use the default for DLLs
                    iBaseAddress = &H11000000   '// 0x11000000
                End If

                Dim stHexValue As String = "0x" & iBaseAddress.ToString("x", CultureInfo.CurrentUICulture)
                If value Is PropertyControlData.Indeterminate Then
                    stHexValue = ""
                End If
                Me.txtDLLBase.Text = stHexValue
            End If

            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        Private Function IsExeProject() As Boolean

            Dim obj As Object = Nothing
            Dim OutputType As VSLangProj.prjOutputType

            Try
                GetCurrentProperty(VsProjPropId.VBPROJPROPID_OutputType, "OutputType", obj)
                OutputType = CType(obj, VSLangProj.prjOutputType)
            Catch ex As InvalidCastException
                '// When all else fails assume dll (so they can edit it)
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            Catch ex As TargetInvocationException
                ' Property must be missing for this project flavor
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            End Try

            If (OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                Return False
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function BaseAddressGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim StringValue As String = Trim(control.Text)

            'DLL Baseaddress must be 0xNNNNNNNN format
            If String.Compare(VBStrings.Left(StringValue, 2), "0x", StringComparison.OrdinalIgnoreCase) = 0 Then
                StringValue = "&h" + VBStrings.Mid(StringValue, 3)
                If IsNumeric(StringValue) Then
                    Dim LongValue As ULong
                    Try
                        LongValue = CULng(StringValue)
                        If LongValue < UInt32.MaxValue Then
                            value = CUInt(LongValue)
                            Return True
                        End If
                    Catch ex As Exception
                        'Let throw below
                    End Try
                End If
            End If
            Throw New Exception(SR.GetString(SR.PPG_AdvancedBuildSettings_InvalidBaseAddress))

        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function DebugInfoSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                Me.cboDebugInfo.SelectedIndex = -1
            Else
                Dim stValue As String = TryCast(value, String)
                If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then

                    '// Need to special case pdb-only becuase it's stored in the property without the dash but it's
                    '// displayed in the dialog with a dash.

                    If (String.Compare(stValue, "pdbonly", StringComparison.OrdinalIgnoreCase) <> 0) Then
                        Me.cboDebugInfo.Text = stValue
                    Else
                        Me.cboDebugInfo.Text = "pdb-only"
                    End If
                Else
                    Me.cboDebugInfo.SelectedIndex = 0        '// Zero is the (none) entry in the list
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

            '// Need to special case pdb-only because the display name has a dash while the actual property value
            '// doesn't have the dash.
            If (String.Compare(Me.cboDebugInfo.Text, "pdb-only", StringComparison.OrdinalIgnoreCase) <> 0) Then
                value = Me.cboDebugInfo.Text
            Else
                value = "pdbonly"
            End If
            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DebugInfo_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboDebugInfo.SelectedIndexChanged
            If Me.cboDebugInfo.SelectedIndex = 0 Then
                '// user selcted none
                m_bDebugSymbols = False
            Else
                m_bDebugSymbols = True
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_DebugSymbols, False)
            SetDirty(VsProjPropId80.VBPROJPROPID_DebugInfo, False)
            SetDirty(True)
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function FileAlignmentSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                Me.cboFileAlignment.SelectedIndex = -1
            Else
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    Me.cboFileAlignment.Text = stValue
                Else
                    Me.cboFileAlignment.SelectedIndex = 0        '// Zero is the (none) entry in the list
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
        Private Function FileAlignmentGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = Me.cboFileAlignment.Text
            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Function OverflowUnderflowSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate Then
                Me.chkOverflow.CheckState = CheckState.Indeterminate
            Else
                Me.chkOverflow.Checked = CType(value, Boolean)
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
        Private Function OverflowUnderflowGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = Me.chkOverflow.Checked
            Return True
        End Function

    End Class

End Namespace

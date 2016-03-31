Imports System
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks></remarks>

    Friend NotInheritable Class BuildEventsPropPage
        Inherits PropPageUserControlBase
        'Inherits System.Windows.Forms.UserControl
        ' If you want to be able to use the forms designer to edit this file,
        ' change the base class from PropPageUserControlBase to UserControl

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
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

        Friend WithEvents lblPreBuildEventCommandLine As System.Windows.Forms.Label
        Friend WithEvents lblPostBuildEventCommandLine As System.Windows.Forms.Label
        Friend WithEvents lblRunPostBuildEvent As System.Windows.Forms.Label
        Friend WithEvents txtPreBuildEventCommandLine As System.Windows.Forms.TextBox
        Friend WithEvents txtPostBuildEventCommandLine As System.Windows.Forms.TextBox
        Friend WithEvents cboRunPostBuildEvent As System.Windows.Forms.ComboBox
        Friend WithEvents btnPreBuildBuilder As System.Windows.Forms.Button
        Friend WithEvents btnPostBuildBuilder As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BuildEventsPropPage))
            Me.lblPreBuildEventCommandLine = New System.Windows.Forms.Label()
            Me.txtPreBuildEventCommandLine = New System.Windows.Forms.TextBox()
            Me.btnPreBuildBuilder = New System.Windows.Forms.Button()
            Me.lblPostBuildEventCommandLine = New System.Windows.Forms.Label()
            Me.txtPostBuildEventCommandLine = New System.Windows.Forms.TextBox()
            Me.btnPostBuildBuilder = New System.Windows.Forms.Button()
            Me.lblRunPostBuildEvent = New System.Windows.Forms.Label()
            Me.cboRunPostBuildEvent = New System.Windows.Forms.ComboBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblPreBuildEventCommandLine
            '
            resources.ApplyResources(Me.lblPreBuildEventCommandLine, "lblPreBuildEventCommandLine")
            Me.lblPreBuildEventCommandLine.Name = "lblPreBuildEventCommandLine"
            '
            'txtPreBuildEventCommandLine
            '
            Me.txtPreBuildEventCommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.txtPreBuildEventCommandLine, "txtPreBuildEventCommandLine")
            Me.txtPreBuildEventCommandLine.Name = "txtPreBuildEventCommandLine"
            '
            'btnPreBuildBuilder
            '
            resources.ApplyResources(Me.btnPreBuildBuilder, "btnPreBuildBuilder")
            Me.btnPreBuildBuilder.Name = "btnPreBuildBuilder"
            '
            'lblPostBuildEventCommandLine
            '
            resources.ApplyResources(Me.lblPostBuildEventCommandLine, "lblPostBuildEventCommandLine")
            Me.lblPostBuildEventCommandLine.Name = "lblPostBuildEventCommandLine"
            '
            'txtPostBuildEventCommandLine
            '
            Me.txtPostBuildEventCommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.txtPostBuildEventCommandLine, "txtPostBuildEventCommandLine")
            Me.txtPostBuildEventCommandLine.Name = "txtPostBuildEventCommandLine"
            '
            'btnPostBuildBuilder
            '
            resources.ApplyResources(Me.btnPostBuildBuilder, "btnPostBuildBuilder")
            Me.btnPostBuildBuilder.Name = "btnPostBuildBuilder"
            '
            'lblRunPostBuildEvent
            '
            resources.ApplyResources(Me.lblRunPostBuildEvent, "lblRunPostBuildEvent")
            Me.lblRunPostBuildEvent.Name = "lblRunPostBuildEvent"
            '
            'cboRunPostBuildEvent
            '
            resources.ApplyResources(Me.cboRunPostBuildEvent, "cboRunPostBuildEvent")
            Me.cboRunPostBuildEvent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboRunPostBuildEvent.FormattingEnabled = True
            Me.cboRunPostBuildEvent.Items.AddRange(New Object() {resources.GetString("cboRunPostBuildEvent.Items"), resources.GetString("cboRunPostBuildEvent.Items1"), resources.GetString("cboRunPostBuildEvent.Items2")})
            Me.cboRunPostBuildEvent.Name = "cboRunPostBuildEvent"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblPreBuildEventCommandLine, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtPostBuildEventCommandLine, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboRunPostBuildEvent, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtPreBuildEventCommandLine, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblRunPostBuildEvent, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblPostBuildEventCommandLine, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnPostBuildBuilder, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnPreBuildBuilder, 0, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'BuildEventsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "BuildEventsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

        Enum Tokens
            OutDir = 0
            ConfigurationName
            ProjectName
            TargetName
            TargetPath
            ProjectPath
            ProjectFileName
            TargetExt
            TargetFileName
            DevEnvDir
            TargetDir
            ProjectDir
            SolutionFileName
            SolutionPath
            SolutionDir
            SolutionName
            PlatformName
            ProjectExt
            SolutionExt
            Tokens_MAX
        End Enum

        Private Shared ReadOnly m_TokenNames() As String = { _
            "OutDir", _
            "ConfigurationName", _
            "ProjectName", _
            "TargetName", _
            "TargetPath", _
            "ProjectPath", _
            "ProjectFileName", _
            "TargetExt", _
            "TargetFileName", _
            "DevEnvDir", _
            "TargetDir", _
            "ProjectDir", _
            "SolutionFileName", _
            "SolutionPath", _
            "SolutionDir", _
            "SolutionName", _
            "PlatformName", _
            "ProjectExt", _
            "SolutionExt" _
        }

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() { _
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_PreBuildEvent, "PreBuildEvent", Me.txtPreBuildEventCommandLine, ControlDataFlags.None, New Control() {btnPreBuildBuilder, lblPreBuildEventCommandLine}), _
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_PostBuildEvent, "PostBuildEvent", Me.txtPostBuildEventCommandLine, ControlDataFlags.None, New Control() {btnPostBuildBuilder, lblPostBuildEventCommandLine}), _
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_RunPostBuildEvent, "RunPostBuildEvent", Me.cboRunPostBuildEvent, New Control() {Me.lblRunPostBuildEvent}) _
                    }
                End If

                Return m_ControlData
            End Get
        End Property

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsVBProject() Then
                Return HelpKeywords.VBProjPropBuildEvents
            Else
                Return HelpKeywords.CSProjPropBuildEvents
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PostBuildBuilderButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPostBuildBuilder.Click
            Dim CommandLineText As String
            CommandLineText = Me.txtPostBuildEventCommandLine.Text

            LaunchEventBuilder(Me, AddressOf Me.GetTokenValue, SR.GetString(SR.PPG_PostBuildCommandLineTitle), CommandLineText)
            Dim oldCommandLine As String = Me.txtPostBuildEventCommandLine.Text
            Me.txtPostBuildEventCommandLine.Text = CommandLineText
            If oldCommandLine <> CommandLineText Then
                SetDirty(txtPostBuildEventCommandLine, True)
            End If
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PreBuildBuilderButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreBuildBuilder.Click
            Dim CommandLineText As String
            CommandLineText = Me.txtPreBuildEventCommandLine.Text

            LaunchEventBuilder(Me, AddressOf Me.GetTokenValue, SR.GetString(SR.PPG_PreBuildCommandLineTitle), CommandLineText)
            Dim oldCommandLine As String = Me.txtPreBuildEventCommandLine.Text
            Me.txtPreBuildEventCommandLine.Text = CommandLineText
            If oldCommandLine <> CommandLineText Then
                SetDirty(txtPreBuildEventCommandLine, True)
            End If
        End Sub

        Friend Delegate Function GetTokenValueFunc(ByVal MacroName As String) As String


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="WindowTitleText"></param>
        ''' <param name="CommandLine"></param>
        ''' <remarks></remarks>
        Private Function LaunchEventBuilder(ByVal Parent As BuildEventsPropPage, ByVal valueHelper As GetTokenValueFunc, ByVal WindowTitleText As String, ByRef CommandLine As String) As Boolean

            Dim frm As New BuildEventCommandLineDialog
            Dim Values() As String = Nothing

            '// Initialize the title text
            frm.SetFormTitleText(WindowTitleText)


            '// Initialize the command line
            frm.EventCommandLine = CommandLine

            '// Set the page property
            frm.Page = Parent

            '// Set the Dte object for cmdline dialog
            ' VSWhidbey 163859 - help not able to retrieve DTE handle
            frm.DTE = Parent.DTE

            '// Initialize the token values

            GetTokenValues(Values, valueHelper)
            frm.SetTokensAndValues(m_TokenNames, Values)


            '// Show the form
            If (frm.ShowDialog(ServiceProvider) = System.Windows.Forms.DialogResult.OK) Then
                CommandLine = frm.EventCommandLine
            End If

            Return True
        End Function


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="Values"></param>
        ''' <remarks></remarks>        
        Friend Shared Function GetTokenValues(ByRef Values() As String, ByVal valueHelper As GetTokenValueFunc) As Boolean
            Dim i As Integer
            Values = CType(Array.CreateInstance(GetType(String), Tokens.Tokens_MAX), String())

            For i = 0 To Tokens.Tokens_MAX - 1
                Values(i) = valueHelper(m_TokenNames(i))
            Next

            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="MacroName"></param>
        ''' <remarks></remarks>
        Private Function GetTokenValue(ByVal MacroName As String) As String
            Dim MacroEval As IVsBuildMacroInfo
            Dim MacroValue As String = Nothing
            Dim Hier As IVsHierarchy = Nothing
            Dim ItemId As UInteger
            Dim ThisObj As Object = m_Objects(0)

            If TypeOf ThisObj Is IVsBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsBrowseObject).GetProjectItem(Hier, ItemId))
            ElseIf TypeOf ThisObj Is IVsCfgBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsCfgBrowseObject).GetProjectItem(Hier, ItemId))
            End If
            MacroEval = CType(Hier, IVsBuildMacroInfo)
            VSErrorHandler.ThrowOnFailure(MacroEval.GetBuildMacroValue(MacroName, MacroValue))

            Return MacroValue
        End Function

    End Class

End Namespace

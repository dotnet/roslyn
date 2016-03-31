Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports System.ComponentModel
Imports VSLangProj80
Imports VslangProj90
Imports VSLangProj110

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Not currently used directly (but it's inherited from)
    '''   - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    Friend Class ApplicationPropPage
        Inherits ApplicationPropPageInternalBase

        Protected Const Const_SubMain As String = "Sub Main"
        Protected Const Const_DefaultNamespace As String = "DefaultNamespace"
        Protected Const Const_StartupObject As String = "StartupObject"
        Protected Const Const_ApplicationIcon As String = "ApplicationIcon"
        Protected Const Const_ApplicationManifest As String = "ApplicationManifest"
        Friend Const Const_TargetFrameworkMoniker As String = "TargetFrameworkMoniker"
        Protected m_RootNamespace As String

        Friend WithEvents iconTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        Private m_OutputTypeDefaultValues As OutputTypeComboBoxValue()

        Protected Const INDEX_WINDOWSAPP As Integer = 0
        Protected Const INDEX_COMMANDLINEAPP As Integer = 1
        Protected Const INDEX_WINDOWSCLASSLIB As Integer = 2

        Private m_StartupObject As String
        Friend WithEvents TargetFramework As System.Windows.Forms.ComboBox
        Friend WithEvents TargetFrameworkLabel As System.Windows.Forms.Label
        Friend WithEvents overarchingLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ManifestExplanationLabel As System.Windows.Forms.TextBox
        Private m_controlGroup As Control()()

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            m_OutputTypeDefaultValues = New OutputTypeComboBoxValue(INDEX_WINDOWSCLASSLIB) {}
            m_OutputTypeDefaultValues(INDEX_WINDOWSAPP) = New OutputTypeComboBoxValue(INDEX_WINDOWSAPP)
            m_OutputTypeDefaultValues(INDEX_COMMANDLINEAPP) = New OutputTypeComboBoxValue(INDEX_COMMANDLINEAPP)
            m_OutputTypeDefaultValues(INDEX_WINDOWSCLASSLIB) = New OutputTypeComboBoxValue(INDEX_WINDOWSCLASSLIB)

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
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
        Friend WithEvents AssemblyName As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyInfoButton As System.Windows.Forms.Button
        Friend WithEvents OutputType As System.Windows.Forms.ComboBox
        Friend WithEvents StartupObject As System.Windows.Forms.ComboBox
        Friend WithEvents ApplicationIconLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationIcon As System.Windows.Forms.ComboBox
        Friend WithEvents AppIconBrowse As System.Windows.Forms.Button
        Friend WithEvents AppIconImage As System.Windows.Forms.PictureBox
        Friend WithEvents ApplicationManifestLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationManifest As System.Windows.Forms.ComboBox
        Friend WithEvents RootNameSpace As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyNameLabel As System.Windows.Forms.Label
        Friend WithEvents RootNamespaceLabel As System.Windows.Forms.Label
        Friend WithEvents OutputTypeLabel As System.Windows.Forms.Label
        Friend WithEvents ResourcesLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectLabel As System.Windows.Forms.Label
        Friend WithEvents ResourcesGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents IconRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents Win32ResourceRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents Win32ResourceFileBrowse As System.Windows.Forms.Button
        Friend WithEvents Win32ResourceFile As System.Windows.Forms.TextBox
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPage))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AssemblyNameLabel = New System.Windows.Forms.Label()
            Me.AssemblyName = New System.Windows.Forms.TextBox()
            Me.RootNamespaceLabel = New System.Windows.Forms.Label()
            Me.RootNameSpace = New System.Windows.Forms.TextBox()
            Me.OutputTypeLabel = New System.Windows.Forms.Label()
            Me.OutputType = New System.Windows.Forms.ComboBox()
            Me.StartupObjectLabel = New System.Windows.Forms.Label()
            Me.StartupObject = New System.Windows.Forms.ComboBox()
            Me.AssemblyInfoButton = New System.Windows.Forms.Button()
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label()
            Me.TargetFramework = New System.Windows.Forms.ComboBox()
            Me.ResourcesGroupBox = New System.Windows.Forms.GroupBox()
            Me.iconTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.ResourcesLabel = New System.Windows.Forms.Label()
            Me.IconRadioButton = New System.Windows.Forms.RadioButton()
            Me.ManifestExplanationLabel = New System.Windows.Forms.TextBox()
            Me.ApplicationIconLabel = New System.Windows.Forms.Label()
            Me.ApplicationIcon = New System.Windows.Forms.ComboBox()
            Me.AppIconBrowse = New System.Windows.Forms.Button()
            Me.AppIconImage = New System.Windows.Forms.PictureBox()
            Me.ApplicationManifestLabel = New System.Windows.Forms.Label()
            Me.ApplicationManifest = New System.Windows.Forms.ComboBox()
            Me.Win32ResourceRadioButton = New System.Windows.Forms.RadioButton()
            Me.Win32ResourceFile = New System.Windows.Forms.TextBox()
            Me.Win32ResourceFileBrowse = New System.Windows.Forms.Button()
            Me.overarchingLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.TopHalfLayoutPanel.SuspendLayout()
            Me.ResourcesGroupBox.SuspendLayout()
            Me.iconTableLayoutPanel.SuspendLayout()
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.overarchingLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyName, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNameSpace, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.OutputTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.OutputType, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectLabel, 0, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObject, 0, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyInfoButton, 1, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFramework, 0, 3)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
            '
            'AssemblyNameLabel
            '
            resources.ApplyResources(Me.AssemblyNameLabel, "AssemblyNameLabel")
            Me.AssemblyNameLabel.Name = "AssemblyNameLabel"
            '
            'AssemblyName
            '
            resources.ApplyResources(Me.AssemblyName, "AssemblyName")
            Me.AssemblyName.Name = "AssemblyName"
            '
            'RootNamespaceLabel
            '
            resources.ApplyResources(Me.RootNamespaceLabel, "RootNamespaceLabel")
            Me.RootNamespaceLabel.Name = "RootNamespaceLabel"
            '
            'RootNameSpace
            '
            resources.ApplyResources(Me.RootNameSpace, "RootNameSpace")
            Me.RootNameSpace.Name = "RootNameSpace"
            '
            'OutputTypeLabel
            '
            resources.ApplyResources(Me.OutputTypeLabel, "OutputTypeLabel")
            Me.OutputTypeLabel.Name = "OutputTypeLabel"
            '
            'OutputType
            '
            resources.ApplyResources(Me.OutputType, "OutputType")
            Me.OutputType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OutputType.FormattingEnabled = True
            Me.OutputType.Name = "OutputType"
            '
            'StartupObjectLabel
            '
            resources.ApplyResources(Me.StartupObjectLabel, "StartupObjectLabel")
            Me.StartupObjectLabel.Name = "StartupObjectLabel"
            '
            'StartupObject
            '
            resources.ApplyResources(Me.StartupObject, "StartupObject")
            Me.StartupObject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObject.FormattingEnabled = True
            Me.StartupObject.Name = "StartupObject"
            Me.StartupObject.Sorted = True
            '
            'AssemblyInfoButton
            '
            resources.ApplyResources(Me.AssemblyInfoButton, "AssemblyInfoButton")
            Me.AssemblyInfoButton.Name = "AssemblyInfoButton"
            '
            'TargetFrameworkLabel
            '
            resources.ApplyResources(Me.TargetFrameworkLabel, "TargetFrameworkLabel")
            Me.TargetFrameworkLabel.Name = "TargetFrameworkLabel"
            '
            'TargetFramework
            '
            resources.ApplyResources(Me.TargetFramework, "TargetFramework")
            Me.TargetFramework.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetFramework.FormattingEnabled = True
            Me.TargetFramework.Name = "TargetFramework"
            Me.TargetFramework.Sorted = True
            '
            'ResourcesGroupBox
            '
            resources.ApplyResources(Me.ResourcesGroupBox, "ResourcesGroupBox")
            Me.ResourcesGroupBox.Controls.Add(Me.iconTableLayoutPanel)
            Me.ResourcesGroupBox.Name = "ResourcesGroupBox"
            Me.ResourcesGroupBox.TabStop = False
            '
            'iconTableLayoutPanel
            '
            resources.ApplyResources(Me.iconTableLayoutPanel, "iconTableLayoutPanel")
            Me.iconTableLayoutPanel.Controls.Add(Me.ResourcesLabel, 0, 0)
            Me.iconTableLayoutPanel.Controls.Add(Me.IconRadioButton, 0, 1)
            Me.iconTableLayoutPanel.Controls.Add(Me.ManifestExplanationLabel, 0, 2)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationIconLabel, 0, 3)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationIcon, 0, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.AppIconBrowse, 1, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.AppIconImage, 2, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationManifestLabel, 0, 5)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationManifest, 0, 6)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceRadioButton, 0, 7)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceFile, 0, 8)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceFileBrowse, 1, 8)
            Me.iconTableLayoutPanel.Name = "iconTableLayoutPanel"
            '
            'ResourcesLabel
            '
            resources.ApplyResources(Me.ResourcesLabel, "ResourcesLabel")
            Me.ResourcesLabel.Name = "ResourcesLabel"
            '
            'IconRadioButton
            '
            resources.ApplyResources(Me.IconRadioButton, "IconRadioButton")
            Me.IconRadioButton.Name = "IconRadioButton"
            '
            'ManifestExplanationLabel
            '
            Me.ManifestExplanationLabel.BorderStyle = System.Windows.Forms.BorderStyle.None
            resources.ApplyResources(Me.ManifestExplanationLabel, "ManifestExplanationLabel")
            Me.ManifestExplanationLabel.HideSelection = False
            Me.ManifestExplanationLabel.Name = "ManifestExplanationLabel"
            Me.ManifestExplanationLabel.ReadOnly = True
            '
            'ApplicationIconLabel
            '
            resources.ApplyResources(Me.ApplicationIconLabel, "ApplicationIconLabel")
            Me.ApplicationIconLabel.Name = "ApplicationIconLabel"
            '
            'ApplicationIcon
            '
            resources.ApplyResources(Me.ApplicationIcon, "ApplicationIcon")
            Me.ApplicationIcon.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.ApplicationIcon.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.ApplicationIcon.FormattingEnabled = True
            Me.ApplicationIcon.Name = "ApplicationIcon"
            '
            'AppIconBrowse
            '
            resources.ApplyResources(Me.AppIconBrowse, "AppIconBrowse")
            Me.AppIconBrowse.Name = "AppIconBrowse"
            '
            'AppIconImage
            '
            resources.ApplyResources(Me.AppIconImage, "AppIconImage")
            Me.AppIconImage.Name = "AppIconImage"
            Me.AppIconImage.TabStop = False
            '
            'ApplicationManifestLabel
            '
            resources.ApplyResources(Me.ApplicationManifestLabel, "ApplicationManifestLabel")
            Me.ApplicationManifestLabel.Name = "ApplicationManifestLabel"
            '
            'ApplicationManifest
            '
            resources.ApplyResources(Me.ApplicationManifest, "ApplicationManifest")
            Me.ApplicationManifest.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.ApplicationManifest.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.ApplicationManifest.FormattingEnabled = True
            Me.ApplicationManifest.Name = "ApplicationManifest"
            '
            'Win32ResourceRadioButton
            '
            resources.ApplyResources(Me.Win32ResourceRadioButton, "Win32ResourceRadioButton")
            Me.Win32ResourceRadioButton.Name = "Win32ResourceRadioButton"
            '
            'Win32ResourceFile
            '
            resources.ApplyResources(Me.Win32ResourceFile, "Win32ResourceFile")
            Me.Win32ResourceFile.Name = "Win32ResourceFile"
            '
            'Win32ResourceFileBrowse
            '
            resources.ApplyResources(Me.Win32ResourceFileBrowse, "Win32ResourceFileBrowse")
            Me.Win32ResourceFileBrowse.Name = "Win32ResourceFileBrowse"
            '
            'overarchingLayoutPanel
            '
            resources.ApplyResources(Me.overarchingLayoutPanel, "overarchingLayoutPanel")
            Me.overarchingLayoutPanel.Controls.Add(Me.ResourcesGroupBox, 0, 1)
            Me.overarchingLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingLayoutPanel.Name = "overarchingLayoutPanel"
            '
            'ApplicationPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingLayoutPanel)
            Me.Name = "ApplicationPropPage"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            Me.ResourcesGroupBox.ResumeLayout(False)
            Me.ResourcesGroupBox.PerformLayout()
            Me.iconTableLayoutPanel.ResumeLayout(False)
            Me.iconTableLayoutPanel.PerformLayout()
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).EndInit()
            Me.overarchingLayoutPanel.ResumeLayout(False)
            Me.overarchingLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
#End Region


        Private Sub AssemblyInfoButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(SR.GetString(SR.PPG_AssemblyInfo_Title), GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    m_TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData( _
                            VslangProj100.VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker, Const_TargetFrameworkMoniker, _
                            TargetFramework, _
                            AddressOf SetTargetFrameworkMoniker, AddressOf GetTargetFrameworkMoniker, _
                            ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout, _
                            New Control() {Me.TargetFrameworkLabel})

                    'StartupObject must be kept at the end of the list because it depends on the initialization of "OutputType" values
                    'm_ControlData = New PropertyControlData()
                    m_ControlData = New PropertyControlData() {}
                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", Me.AssemblyName, New Control() {Me.AssemblyNameLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyName)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_DefaultNamespace, Const_DefaultNamespace, Me.RootNameSpace, New Control() {Me.RootNamespaceLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_RootNamespace)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", Me.ApplicationIcon, AddressOf Me.ApplicationIconSet, AddressOf Me.ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {Me.AppIconImage, Me.AppIconBrowse, Me.IconRadioButton, Me.ApplicationIconLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_ApplicationIcon)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, Me.OutputType, AddressOf Me.OutputTypeSet, AddressOf Me.OutputTypeGet, ControlDataFlags.UserHandledEvents, New Control() {Me.OutputTypeLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, "StartupObject", Me.StartupObject, AddressOf Me.StartupObjectSet, AddressOf Me.StartupObjectGet, ControlDataFlags.UserHandledEvents, New Control() {Me.StartupObjectLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_StartupObject)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_Win32ResourceFile, "Win32ResourceFile", Me.Win32ResourceFile, AddressOf Me.Win32ResourceSet, AddressOf Me.Win32ResourceGet, ControlDataFlags.None, New Control() {Me.Win32ResourceFileBrowse, Me.Win32ResourceRadioButton})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", Me.ApplicationManifest, AddressOf Me.ApplicationManifestSet, AddressOf Me.ApplicationManifestGet, ControlDataFlags.UserHandledEvents, New Control() {Me.ApplicationManifest, Me.ApplicationManifestLabel})
                    datalist.Add(data)
                    datalist.Add(m_TargetFrameworkPropertyControlData)
                    m_ControlData = datalist.ToArray()

                End If
                Return m_ControlData
            End Get
        End Property

        Protected Overrides ReadOnly Property ValidationControlGroups() As Control()()
            Get
                If m_controlGroup Is Nothing Then
                    m_controlGroup = New Control()() { _
                        New Control() {IconRadioButton, Win32ResourceRadioButton, ApplicationIcon, ApplicationManifest, Win32ResourceFile, AppIconBrowse, Win32ResourceFileBrowse} _
                        }
                End If
                Return m_controlGroup
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="OutputType"></param>
        ''' <remarks></remarks>
        Private Sub PopulateControlSet(ByVal OutputType As UInteger)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupported(OutputType), False)
        End Sub

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="StartUpObjectSupported">If false, (None) will be the only entry in the list.</param>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        ''' <remarks></remarks>
        Protected Overridable Sub PopulateStartupObject(ByVal StartUpObjectSupported As Boolean, ByVal PopulateDropdown As Boolean)
            'overridable to support the csharpapplication page (Sub Main isn't used by C#)

            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData("StartupObject")

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedItem = .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))
                        .Text = SR.GetString(SR.PPG_Application_StartupObjectNotSet)
                        .SelectedIndex = 0  '// Set it to NotSet
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        Me.StartupObject.Enabled = False
                        Me.StartupObjectLabel.Enabled = False
                    End If
                Else

                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc

                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDown
                        .Items.Clear()

                        ' (Not Set) should always be available in the list
                        .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))

                        If PopulateDropdown Then
                            RefreshPropertyStandardValues()

                            'Certain project types may not support standard values
                            If prop.Converter.GetStandardValuesSupported() Then
                                Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                Using New WaitCursor
                                    For Each o As Object In prop.Converter.GetStandardValues()
                                        .Items.Add(RemoveRootNamespace(prop.Converter.ConvertToString(o)))
                                    Next
                                End Using
                            End If
                        End If

                        Dim SelectedItemText As String = RemoveRootNamespace(CStr(StartupObjectPropertyControlData.InitialValue))
                        .SelectedItem = SelectedItemText
                        If .SelectedItem Is Nothing Then
                            .Items.Add(SelectedItemText)
                            'CONSIDER: Can we use the object returned by .Items.Add to set the selection?
                            .SelectedItem = SelectedItemText
                        End If
                        'If "Sub Main" is not in the list, then add it.
                        If .Items.IndexOf(Const_SubMain) < 0 Then
                            .Items.Add(Const_SubMain)
                        End If
                    End With
                End If
            Finally
                'Restore previous state
                m_fInsideInit = InsideInitSave
            End Try
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub EnableControlSet()
            UpdateIconImage(False)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function OutputTypeGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Me.OutputType.SelectedIndex = -1 Then
                ' We're indeterminate. Just let the architecture handle it
                Return False
            End If

            Dim currentValue As OutputTypeComboBoxValue = TryCast(Me.OutputType.SelectedItem, OutputTypeComboBoxValue)

            If currentValue Is Nothing Then
                Return False
            End If

            value = currentValue.Value
            Return True

        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function OutputTypeSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            Dim didSelectItem As Boolean = False

            If Not PropertyControlData.IsSpecialValue(value) Then

                Dim uIntValue As UInteger = CUInt(value)
                didSelectItem = SelectItemInOutputTypeComboBox(Me.OutputType, uIntValue)

                If didSelectItem Then
                    PopulateControlSet(uIntValue)
                End If
            End If

            If Not didSelectItem Then
                '// We're indeterminate 
                Me.OutputType.SelectedIndex = -1

                '// Set the startup object to indeterminate as well
                StartupObject.SelectedIndex = -1
            End If
            Return True
        End Function

        Private Function ApplicationIconSupported() As Boolean
            Return Not GetPropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon).IsMissing
        End Function

        Private Function Win32ResourceFileSupported() As Boolean
            Return Not GetPropertyControlData(VsProjPropId80.VBPROJPROPID_Win32ResourceFile).IsMissing
        End Function

        Function SetIconAndWin32ResourceFile() As Boolean
            Dim obj As Object
            Dim propWin32ResourceFile As PropertyDescriptor
            Dim stWin32ResourceFile As String = Nothing

            Dim propApplicationIcon As PropertyDescriptor
            Dim stApplicationIcon As String = Nothing

            Dim propApplicationManifest As PropertyDescriptor
            Dim stApplicationManifest As String = Nothing

            propApplicationIcon = GetPropertyDescriptor("ApplicationIcon")
            propApplicationManifest = GetPropertyDescriptor("ApplicationManifest")
            propWin32ResourceFile = GetPropertyDescriptor("Win32ResourceFile")

            obj = TryGetNonCommonPropertyValue(propApplicationIcon)
            If Not PropertyControlData.IsSpecialValue(obj) Then

                stApplicationIcon = TryCast(obj, String)

                If (Trim(stApplicationIcon) = "") Then
                    If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                        stApplicationIcon = SR.GetString(SR.PPG_Application_DefaultIconText)
                    Else
                        '// ApplicationIcon can be empty for dlls
                    End If
                End If
            End If

            obj = TryGetNonCommonPropertyValue(propApplicationManifest)
            If Not PropertyControlData.IsSpecialValue(obj) Then

                stApplicationManifest = TryCast(obj, String)
                stApplicationManifest = Trim(stApplicationManifest)

                If String.Equals(stApplicationManifest, prjApplicationManifestValues.prjApplicationManifest_Default, StringComparison.OrdinalIgnoreCase) Then
                    stApplicationManifest = SR.GetString(SR.PPG_Application_DefaultManifestText)
                ElseIf String.Equals(stApplicationManifest, prjApplicationManifestValues.prjApplicationManifest_NoManifest, StringComparison.OrdinalIgnoreCase) Then
                    stApplicationManifest = SR.GetString(SR.PPG_Application_NoManifestText)
                ElseIf String.IsNullOrEmpty(stApplicationManifest) Then
                    If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                        stApplicationManifest = SR.GetString(SR.PPG_Application_DefaultManifestText)
                    Else
                        '// ApplicationManifest can be empty for dlls
                    End If
                End If
            End If

            obj = TryGetNonCommonPropertyValue(propWin32ResourceFile)
            If Not PropertyControlData.IsSpecialValue(obj) Then
                stWin32ResourceFile = TryCast(obj, String)
            End If

            If stApplicationIcon Is Nothing AndAlso stWin32ResourceFile Is Nothing Then
                '// indeterminate
                If Not IconEntryIsDefault(Me.ApplicationIcon.Text) Then
                    Me.ApplicationIcon.Text = ""
                End If
                EnableControl(Me.AppIconBrowse, ApplicationIconSupported())
                EnableControl(Me.ApplicationIcon, ApplicationIconSupported())
                EnableControl(Me.ApplicationIconLabel, ApplicationIconSupported())
                Me.IconRadioButton.Checked = False
                If Not ApplicationManifestEntryIsDefault(Me.ApplicationManifest.Text) Then
                    Me.ApplicationManifest.Text = String.Empty
                End If
                If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                    EnableControl(Me.ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(Me.ApplicationManifest, ApplicationManifestSupported())
                Else
                    Me.ApplicationManifestLabel.Enabled = False
                    Me.ApplicationManifest.Enabled = False
                End If
                Me.Win32ResourceFile.Text = ""
                EnableControl(Me.Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Me.Win32ResourceFileBrowse, Win32ResourceFileSupported())
                Me.Win32ResourceRadioButton.Checked = False

            ElseIf (Not (IsNothing(stWin32ResourceFile)) AndAlso stWin32ResourceFile <> "") Then

                Me.Win32ResourceFile.Text = stWin32ResourceFile
                EnableControl(Me.Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Me.Win32ResourceFileBrowse, Win32ResourceFileSupported())
                Me.Win32ResourceRadioButton.Checked = True

                Me.ApplicationIcon.Text = ""
                Me.AppIconBrowse.Enabled = False
                Me.ApplicationIcon.Enabled = False
                Me.ApplicationIconLabel.Enabled = False
                Me.IconRadioButton.Checked = False
                Me.ApplicationManifest.Text = String.Empty
                Me.ApplicationManifestLabel.Enabled = False
                Me.ApplicationManifest.Enabled = False

            Else

                Me.ApplicationIcon.Text = stApplicationIcon
                EnableControl(Me.ApplicationIconLabel, ApplicationIconSupported())
                EnableControl(Me.ApplicationIcon, ApplicationIconSupported())
                EnableControl(Me.AppIconBrowse, ApplicationIconSupported())
                Me.IconRadioButton.Checked = True
                If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                    Me.ApplicationManifest.Text = stApplicationManifest
                    EnableControl(Me.ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(Me.ApplicationManifest, ApplicationManifestSupported())
                Else
                    Me.ApplicationManifest.Text = String.Empty
                    Me.ApplicationManifestLabel.Enabled = False
                    Me.ApplicationManifest.Enabled = False
                End If
                Me.Win32ResourceFile.Text = ""
                Me.Win32ResourceFile.Enabled = False
                Me.Win32ResourceFileBrowse.Enabled = False
                Me.Win32ResourceRadioButton.Checked = False

            End If
            Return True

        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Shadows Function ApplicationIconGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.IconRadioButton.Checked = True) Then
                If (Me.ApplicationIcon.Text.Equals(SR.GetString(SR.PPG_Application_DefaultIconText), StringComparison.OrdinalIgnoreCase)) Then
                    value = ""
                Else
                    value = Me.ApplicationIcon.Text
                End If
                Return True
            ElseIf (Me.Win32ResourceRadioButton.Checked = True) Then
                value = ""
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function ApplicationIconSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Shadows Function ApplicationManifestGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.IconRadioButton.Checked = True) Then
                If (Me.ApplicationManifest.Text.Equals(SR.GetString(SR.PPG_Application_DefaultManifestText), StringComparison.CurrentCultureIgnoreCase)) Then
                    value = prjApplicationManifestValues.prjApplicationManifest_Default
                ElseIf (Me.ApplicationManifest.Text.Equals(SR.GetString(SR.PPG_Application_NoManifestText), StringComparison.CurrentCultureIgnoreCase)) Then
                    value = prjApplicationManifestValues.prjApplicationManifest_NoManifest
                Else
                    value = Me.ApplicationManifest.Text.Trim()
                End If
                Return True
            ElseIf (Me.Win32ResourceRadioButton.Checked = True) Then
                ' Reset it to default.
                value = String.Empty
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function ApplicationManifestSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function Win32ResourceGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.Win32ResourceRadioButton.Checked = True) Then
                value = Me.Win32ResourceFile.Text
                Return True
            ElseIf (Me.IconRadioButton.Checked = True) Then
                value = ""
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function Win32ResourceSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub IconResourceFile_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconRadioButton.CheckedChanged, Win32ResourceRadioButton.CheckedChanged
            If (Me.IconRadioButton.Checked = True) Then
                EnableControl(Me.ApplicationIconLabel, ApplicationIconSupported())
                EnableControl(Me.ApplicationIcon, ApplicationIconSupported())
                EnableControl(Me.AppIconBrowse, ApplicationIconSupported())
                If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                    EnableControl(Me.ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(Me.ApplicationManifest, ApplicationManifestSupported())
                Else
                    Me.ApplicationManifestLabel.Enabled = False
                    Me.ApplicationManifest.Enabled = False
                End If
                Me.Win32ResourceFile.Enabled = False
                Me.Win32ResourceFileBrowse.Enabled = False
            ElseIf (Me.Win32ResourceRadioButton.Checked = True) Then
                Me.ApplicationIconLabel.Enabled = False
                Me.ApplicationIcon.Enabled = False
                Me.AppIconBrowse.Enabled = False
                Me.ApplicationManifestLabel.Enabled = False
                Me.ApplicationManifest.Enabled = False
                EnableControl(Me.Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Me.Win32ResourceFileBrowse, Win32ResourceFileSupported())
            End If

            UpdateIconImage(False)

            SetDirty(ApplicationIcon, False)
            SetDirty(ApplicationManifest, False)
            SetDirty(Win32ResourceFile, True)
        End Sub

        ''' <summary>
        ''' validate a property
        ''' </summary>
        ''' <param name="controlData"></param>
        ''' <param name="message"></param>
        ''' <param name="returnControl"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ValidateProperty(ByVal controlData As PropertyControlData, ByRef message As String, ByRef returnControl As Control) As ValidationResult
            Select Case controlData.DispId
                Case VsProjPropId.VBPROJPROPID_ApplicationIcon
                    If IconRadioButton.Checked Then
                        If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                            If Trim(ApplicationIcon.Text).Length = 0 Then
                                message = SR.GetString(SR.PPG_Application_BadIcon)
                                Return ValidationResult.Warning
                            ElseIf Trim(ApplicationIcon.Text).Equals(SR.GetString(SR.PPG_Application_DefaultIconText), StringComparison.OrdinalIgnoreCase) Then
                                '// This is valid
                                Return ValidationResult.Succeeded
                            End If
                        Else
                            '// We allow empty string for class libraries so don't display error
                        End If
                    End If
                Case VsProjPropId90.VBPROJPROPID_ApplicationManifest
                    If IconRadioButton.Checked Then
                        If (ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                            If String.IsNullOrEmpty(Trim(ApplicationManifest.Text)) Then
                                message = SR.GetString(SR.PPG_Application_BadManifest)
                                Return ValidationResult.Warning
                            Else
                                '// This is valid
                                Return ValidationResult.Succeeded
                            End If
                        Else
                            '// We allow empty string for class libraries so don't display error
                        End If
                    End If
                Case VsProjPropId80.VBPROJPROPID_Win32ResourceFile
                    If Win32ResourceRadioButton.Checked Then
                        If Trim(Win32ResourceFile.Text).Length = 0 Then
                            message = SR.GetString(SR.PropPage_NeedResFile)
                            Return ValidationResult.Warning
                        ElseIf Not File.Exists(Win32ResourceFile.Text) Then
                            message = SR.GetString(SR.PropPage_ResourceFileNotExist)
                            Return ValidationResult.Warning
                        End If
                    End If
            End Select
            Return ValidationResult.Succeeded
        End Function


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function StartupObjectGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            'overridable to support the csharpapplication page (C# doesn't use root namespace)
            If Not StartUpObjectSupported() Then
                value = ""
            Else
                'Append the RootNamespace to the startup object name
                Dim StringValue As String = DirectCast(GetControlValue(Const_StartupObject), String)
                m_RootNamespace = DirectCast(GetControlValue(Const_DefaultNamespace), String)
                If m_RootNamespace <> "" AndAlso StringValue <> Const_SubMain Then
                    value = m_RootNamespace & "." & StringValue
                End If
            End If
            Return True
        End Function

        Protected Overridable Function StartupObjectSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'overridable to support the csharpapplication page (C# doesn't use root namespace)

            Dim OutputTypeControlData As PropertyControlData = GetPropertyControlData(Const_OutputTypeEx)

            If OutputTypeControlData.IsMissing OrElse Not SupportsOutputTypeProperty() Then
                'Property is not supported by this project type
                ' hide associated fields
                Me.OutputType.Enabled = False
                Me.OutputTypeLabel.Enabled = False

                'Populate
                PopulateStartupObject(True, False)
            Else
                '(Okay to use OutputTypeControlData.InitialValue because we checked IsMissing above)
                Me.PopulateControlSet(CUInt(OutputTypeControlData.InitialValue))
                Me.EnableControlSet()
                Return True
            End If
            Return True
        End Function

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

            Me.OutputType.Items.Clear()

            If Not SupportsOutputTypeProperty() Then

                Me.OutputType.Enabled = False
                Me.OutputTypeLabel.Enabled = False

            ElseIf Not PopulateOutputTypeComboBoxFromProjectProperty(Me.OutputType) Then

                Me.OutputType.Items.AddRange(m_OutputTypeDefaultValues)

            End If

            'Populate the target framework combobox
            PopulateTargetFrameworkComboBox(Me.TargetFramework)
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

            EnableControlSet()

            PopulateIconList(False)
            PopulateManifestList(False)
            UpdateIconImage(False)

            'VSWhidbey 206085
            'In J#, this should be Default package
            If IsJSProject() Then
                RootNamespaceLabel.Text = SR.GetString(SR.PPG_Application_RootNamespaceJSharp)
            End If
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function RemoveRootNamespace(ByVal value As String) As String
            Dim root As String
            Dim RootLength As Integer

            If m_RootNamespace Is Nothing Then
                m_RootNamespace = Trim(TryCast(GetPropertyControlData(Const_DefaultNamespace).InitialValue, String)) 'TryCast because InitialValue will be an object if RootNamespace property not supported
            End If

            root = m_RootNamespace

            If root IsNot Nothing Then
                'Append period for comparison check
                root = root & "."
                RootLength = root.Length
            End If

            If value Is Nothing Then
                value = ""
            End If

            If RootLength > 0 AndAlso value.Length > RootLength Then
                If String.Compare(root, 0, value, 0, RootLength) = 0 Then
                    'Now check that we have a period '.' following the name
                    value = value.Substring(RootLength)
                End If
            End If
            Return value
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub OutputType_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles OutputType.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            Dim OutputType As UInteger = CUInt(GetControlValueNative(Const_OutputTypeEx))

            Me.EnableControlSet()

            SetDirty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, False)
            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, False)
            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, False)
            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)
            SetDirty(True) 'True forces Apply
            If ProjectReloadedDuringCheckout Then
                Return
            End If

            Me.PopulateControlSet(OutputType)

            SetIconAndWin32ResourceFile()
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsJSProject() Then
                Return HelpKeywords.JSProjPropApplication
            Else
                Debug.Assert(IsCSProject, "Unknown project type")
                Return HelpKeywords.CSProjPropApplication
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Win32ResourceFileBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Win32ResourceFileBrowse.Click

            SkipValidating(Win32ResourceFile)   ' skip this because we will pop up dialog to edit it...
            ProcessDelayValidationQueue(False)

            Dim sInitialDirectory As String = Nothing
            Dim sFileName As String

            If sInitialDirectory = "" Then
                sFileName = ""
                sInitialDirectory = ""
            Else
                sFileName = System.IO.Path.GetFileName(sInitialDirectory)
                sInitialDirectory = System.IO.Path.GetDirectoryName(sInitialDirectory)
            End If

            Dim fileNames As ArrayList = Utils.GetFilesViaBrowse(ServiceProvider, Me.Handle, sInitialDirectory, SR.GetString(SR.PPG_AddWin32ResourceTitle), _
                    Common.CombineDialogFilters( _
                        Common.CreateDialogFilter(SR.GetString(SR.PPG_AddWin32ResourceFilter), "res"), _
                        Common.Utils.GetAllFilesDialogFilter() _
                        ), _
                        0, False, sFileName)
            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                sFileName = CStr(fileNames(0))
                If System.IO.File.Exists(sFileName) Then
                    Me.Win32ResourceFile.Text = sFileName
                    SetDirty(Win32ResourceFile, True)
                Else
                    DelayValidate(Win32ResourceFile)
                End If
            Else
                DelayValidate(Win32ResourceFile)
            End If
        End Sub

        'Update the list of available items whenever the start-up object combobox is opened.
        Private Sub StartupObject_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles StartupObject.DropDown
            PopulateStartupObject(StartUpObjectSupported(), PopulateDropdown:=True)
            Common.SetComboBoxDropdownWidth(StartupObject)
        End Sub

        Private Sub StartupObject_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles StartupObject.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, True)
        End Sub

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComboBoxes_DropDown(ByVal sender As Object, ByVal e As EventArgs) Handles OutputType.DropDown
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub


#Region "Application icon"

        ''' <summary>
        ''' Populates the given application icon combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindIconsInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        ''' <remarks></remarks>
        Private Overloads Sub PopulateIconList(ByVal FindIconsInProject As Boolean)
            PopulateIconList(FindIconsInProject, ApplicationIcon, CType(GetControlValueNative(Const_ApplicationIcon), String))
        End Sub


        ''' <summary>
        ''' Update the image displayed for the currently-selected application icon
        ''' </summary>
        ''' <remarks></remarks>
        Private Overloads Sub UpdateIconImage(ByVal AddToProject As Boolean)
            UpdateIconImage(ApplicationIcon, AppIconImage, AddToProject)
        End Sub


        Private Sub ApplicationIcon_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationIcon.DropDown
            If GetPropertyControlData(Const_ApplicationIcon).IsDirty() Then
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If

            'When the icon combobox is dropped down, update it with all current entries from the project
            PopulateIconList(True)
            Common.SetComboBoxDropdownWidth(ApplicationIcon)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ApplicationIcon_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationIcon.LostFocus
            If m_fInsideInit Then
                Return
            End If

            If GetPropertyControlData(Const_ApplicationIcon).IsDirty() Then
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If
        End Sub

        Private Sub ApplicationIcon_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationIcon.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            UpdateIconImage(True)
            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
        End Sub

        Private Sub ApplicationIcon_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationIcon.TextChanged
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, False)
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub AppIconBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AppIconBrowse.Click
            BrowseForAppIcon(ApplicationIcon, AppIconImage)
        End Sub

#End Region

#Region "Application Manifest"

        ''' <summary>
        ''' Populates the given application manifest combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindManifestInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        ''' <remarks></remarks>
        Private Overloads Sub PopulateManifestList(ByVal FindManifestInProject As Boolean)
            PopulateManifestList(FindManifestInProject, ApplicationManifest, CType(GetControlValueNative(Const_ApplicationManifest), String))
        End Sub

        Private Sub ApplicationManifest_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationManifest.DropDown
            If GetPropertyControlData(Const_ApplicationManifest).IsDirty() Then
                SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
            End If

            'When the icon combobox is dropped down, update it with all current entries from the project
            PopulateManifestList(True)
            Common.SetComboBoxDropdownWidth(ApplicationManifest)
        End Sub

        '@ <summary>
        '@ 
        '@ </summary>
        '@ <param name="sender"></param>
        '@ <param name="e"></param>
        '@ <remarks></remarks>
        Private Sub ApplicationManifest_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationManifest.LostFocus
            If m_fInsideInit Then
                Return
            End If

            If GetPropertyControlData(Const_ApplicationManifest).IsDirty() Then
                SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
            End If
        End Sub

        Private Sub ApplicationManifest_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationManifest.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
        End Sub

        Private Sub ApplicationManifest_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationManifest.TextChanged
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, False)
        End Sub


#End Region

        Private Sub iconTableLayoutPanel_Paint(sender As Object, e As PaintEventArgs) Handles iconTableLayoutPanel.Paint

        End Sub
    End Class


End Namespace

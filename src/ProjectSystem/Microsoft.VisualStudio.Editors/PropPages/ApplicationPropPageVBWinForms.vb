Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports System.Windows.Forms
Imports System.ComponentModel
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSLangProj80
Imports VslangProj90
Imports VSLangProj110

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The application property page for VB WinForms apps
    ''' - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ApplicationPropPageVBWinForms
        Inherits ApplicationPropPageVBBase
        'Inherits UserControl

#Region " Windows Form Designer generated code "

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If

            End If
            MyBase.Dispose(disposing)
        End Sub

        Friend WithEvents SaveMySettingsCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents AuthenticationModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AuthenticationModeLabel As System.Windows.Forms.Label
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyNameLabel As System.Windows.Forms.Label
        Friend WithEvents RootNamespaceLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents RootNamespaceTextBox As System.Windows.Forms.TextBox
        Friend WithEvents TargetFrameworkLabel As System.Windows.Forms.Label
        Friend WithEvents TargetFrameworkComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ApplicationTypeLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationTypeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AssemblyInfoButton As System.Windows.Forms.Button
        Friend WithEvents StartupObjectLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents UseApplicationFrameworkCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents IconLabel As System.Windows.Forms.Label
        Friend WithEvents EnableXPThemesCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents SingleInstanceCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents ShutdownModeLabel As System.Windows.Forms.Label
        Friend WithEvents ShutdownModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ViewCodeButton As System.Windows.Forms.Button
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents WindowsAppGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents BottomHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents IconCombobox As System.Windows.Forms.ComboBox
        Friend WithEvents IconPicturebox As System.Windows.Forms.PictureBox
        Friend WithEvents SplashScreenLabel As System.Windows.Forms.Label
        Friend WithEvents SplashScreenComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ViewUACSettingsButton As System.Windows.Forms.Button
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPageVBWinForms))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyNameLabel = New System.Windows.Forms.Label
            Me.AssemblyNameTextBox = New System.Windows.Forms.TextBox
            Me.RootNamespaceLabel = New System.Windows.Forms.Label
            Me.RootNamespaceTextBox = New System.Windows.Forms.TextBox
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label
            Me.TargetFrameworkComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeComboBox = New System.Windows.Forms.ComboBox
            Me.IconLabel = New System.Windows.Forms.Label
            Me.IconPicturebox = New System.Windows.Forms.PictureBox
            Me.IconCombobox = New System.Windows.Forms.ComboBox
            Me.UseApplicationFrameworkCheckBox = New System.Windows.Forms.CheckBox
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyInfoButton = New System.Windows.Forms.Button
            Me.ViewUACSettingsButton = New System.Windows.Forms.Button
            Me.WindowsAppGroupBox = New System.Windows.Forms.GroupBox
            Me.BottomHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.EnableXPThemesCheckBox = New System.Windows.Forms.CheckBox
            Me.SingleInstanceCheckBox = New System.Windows.Forms.CheckBox
            Me.SaveMySettingsCheckbox = New System.Windows.Forms.CheckBox
            Me.AuthenticationModeLabel = New System.Windows.Forms.Label
            Me.AuthenticationModeComboBox = New System.Windows.Forms.ComboBox
            Me.ShutdownModeLabel = New System.Windows.Forms.Label
            Me.ShutdownModeComboBox = New System.Windows.Forms.ComboBox
            Me.SplashScreenLabel = New System.Windows.Forms.Label
            Me.SplashScreenComboBox = New System.Windows.Forms.ComboBox
            Me.ViewCodeButton = New System.Windows.Forms.Button
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.TopHalfLayoutPanel.SuspendLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.TableLayoutPanel1.SuspendLayout()
            Me.WindowsAppGroupBox.SuspendLayout()
            Me.BottomHalfLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameTextBox, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceTextBox, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkComboBox, 0, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeComboBox, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectComboBox, 0, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectLabel, 0, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconLabel, 1, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconPicturebox, 2, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconCombobox, 1, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.UseApplicationFrameworkCheckBox, 0, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TableLayoutPanel1, 0, 6)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
            '
            'AssemblyNameLabel
            '
            resources.ApplyResources(Me.AssemblyNameLabel, "AssemblyNameLabel")
            Me.AssemblyNameLabel.Name = "AssemblyNameLabel"
            '
            'AssemblyNameTextBox
            '
            resources.ApplyResources(Me.AssemblyNameTextBox, "AssemblyNameTextBox")
            Me.AssemblyNameTextBox.Name = "AssemblyNameTextBox"
            '
            'RootNamespaceLabel
            '
            resources.ApplyResources(Me.RootNamespaceLabel, "RootNamespaceLabel")
            Me.RootNamespaceLabel.Name = "RootNamespaceLabel"
            '
            'RootNamespaceTextBox
            '
            resources.ApplyResources(Me.RootNamespaceTextBox, "RootNamespaceTextBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.RootNamespaceTextBox, 2)
            Me.RootNamespaceTextBox.Name = "RootNamespaceTextBox"
            '
            'TargetFrameworkLabel
            '
            resources.ApplyResources(Me.TargetFrameworkLabel, "TargetFrameworkLabel")
            Me.TargetFrameworkLabel.Name = "TargetFrameworkLabel"
            '
            'TargetFrameworkComboBox
            '
            resources.ApplyResources(Me.TargetFrameworkComboBox, "TargetFrameworkComboBox")
            Me.TargetFrameworkComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetFrameworkComboBox.FormattingEnabled = True
            Me.TargetFrameworkComboBox.Name = "TargetFrameworkComboBox"
            '
            'StartupObjectComboBox
            '
            resources.ApplyResources(Me.StartupObjectComboBox, "StartupObjectComboBox")
            Me.StartupObjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObjectComboBox.FormattingEnabled = True
            Me.StartupObjectComboBox.Name = "StartupObjectComboBox"
            '
            'StartupObjectLabel
            '
            resources.ApplyResources(Me.StartupObjectLabel, "StartupObjectLabel")
            Me.StartupObjectLabel.Name = "StartupObjectLabel"
            '
            'ApplicationTypeLabel
            '
            resources.ApplyResources(Me.ApplicationTypeLabel, "ApplicationTypeLabel")
            Me.ApplicationTypeLabel.Name = "ApplicationTypeLabel"
            '
            'ApplicationTypeComboBox
            '
            resources.ApplyResources(Me.ApplicationTypeComboBox, "ApplicationTypeComboBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.ApplicationTypeComboBox, 2)
            Me.ApplicationTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ApplicationTypeComboBox.FormattingEnabled = True
            Me.ApplicationTypeComboBox.Name = "ApplicationTypeComboBox"
            '
            'IconLabel
            '
            resources.ApplyResources(Me.IconLabel, "IconLabel")
            Me.IconLabel.Name = "IconLabel"
            '
            'IconPicturebox
            '
            resources.ApplyResources(Me.IconPicturebox, "IconPicturebox")
            Me.IconPicturebox.Name = "IconPicturebox"
            Me.IconPicturebox.TabStop = False
            '
            'IconCombobox
            '
            resources.ApplyResources(Me.IconCombobox, "IconCombobox")
            Me.IconCombobox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
            Me.IconCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.IconCombobox.FormattingEnabled = True
            Me.IconCombobox.Name = "IconCombobox"
            '
            'UseApplicationFrameworkCheckBox
            '
            resources.ApplyResources(Me.UseApplicationFrameworkCheckBox, "UseApplicationFrameworkCheckBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.UseApplicationFrameworkCheckBox, 2)
            Me.UseApplicationFrameworkCheckBox.Name = "UseApplicationFrameworkCheckBox"
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.TableLayoutPanel1, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.AssemblyInfoButton, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.ViewUACSettingsButton, 1, 0)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            'AssemblyInfoButton
            '
            resources.ApplyResources(Me.AssemblyInfoButton, "AssemblyInfoButton")
            Me.AssemblyInfoButton.Name = "AssemblyInfoButton"
            '
            'ViewUACSettingsButton
            '
            resources.ApplyResources(Me.ViewUACSettingsButton, "ViewUACSettingsButton")
            Me.ViewUACSettingsButton.Name = "ViewUACSettingsButton"
            '
            'WindowsAppGroupBox
            '
            resources.ApplyResources(Me.WindowsAppGroupBox, "WindowsAppGroupBox")
            Me.WindowsAppGroupBox.Controls.Add(Me.BottomHalfLayoutPanel)
            Me.WindowsAppGroupBox.Name = "WindowsAppGroupBox"
            Me.WindowsAppGroupBox.TabStop = False
            '
            'BottomHalfLayoutPanel
            '
            resources.ApplyResources(Me.BottomHalfLayoutPanel, "BottomHalfLayoutPanel")
            Me.BottomHalfLayoutPanel.Controls.Add(Me.EnableXPThemesCheckBox, 0, 0)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SingleInstanceCheckBox, 0, 1)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SaveMySettingsCheckbox, 0, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.AuthenticationModeLabel, 0, 3)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.AuthenticationModeComboBox, 0, 4)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeLabel, 0, 5)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeComboBox, 0, 6)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SplashScreenLabel, 0, 7)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SplashScreenComboBox, 0, 8)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ViewCodeButton, 1, 8)
            Me.BottomHalfLayoutPanel.Name = "BottomHalfLayoutPanel"
            '
            'EnableXPThemesCheckBox
            '
            resources.ApplyResources(Me.EnableXPThemesCheckBox, "EnableXPThemesCheckBox")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.EnableXPThemesCheckBox, 2)
            Me.EnableXPThemesCheckBox.Name = "EnableXPThemesCheckBox"
            '
            'SingleInstanceCheckBox
            '
            resources.ApplyResources(Me.SingleInstanceCheckBox, "SingleInstanceCheckBox")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.SingleInstanceCheckBox, 2)
            Me.SingleInstanceCheckBox.Name = "SingleInstanceCheckBox"
            '
            'SaveMySettingsCheckbox
            '
            resources.ApplyResources(Me.SaveMySettingsCheckbox, "SaveMySettingsCheckbox")
            Me.SaveMySettingsCheckbox.Name = "SaveMySettingsCheckbox"
            '
            'AuthenticationModeLabel
            '
            resources.ApplyResources(Me.AuthenticationModeLabel, "AuthenticationModeLabel")
            Me.AuthenticationModeLabel.Name = "AuthenticationModeLabel"
            '
            'AuthenticationModeComboBox
            '
            resources.ApplyResources(Me.AuthenticationModeComboBox, "AuthenticationModeComboBox")
            Me.AuthenticationModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.AuthenticationModeComboBox.FormattingEnabled = True
            Me.AuthenticationModeComboBox.Name = "AuthenticationModeComboBox"
            '
            'ShutdownModeLabel
            '
            resources.ApplyResources(Me.ShutdownModeLabel, "ShutdownModeLabel")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.ShutdownModeLabel, 2)
            Me.ShutdownModeLabel.Name = "ShutdownModeLabel"
            '
            'ShutdownModeComboBox
            '
            resources.ApplyResources(Me.ShutdownModeComboBox, "ShutdownModeComboBox")
            Me.ShutdownModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ShutdownModeComboBox.FormattingEnabled = True
            Me.ShutdownModeComboBox.Name = "ShutdownModeComboBox"
            '
            'SplashScreenLabel
            '
            resources.ApplyResources(Me.SplashScreenLabel, "SplashScreenLabel")
            Me.SplashScreenLabel.Name = "SplashScreenLabel"
            '
            'SplashScreenComboBox
            '
            resources.ApplyResources(Me.SplashScreenComboBox, "SplashScreenComboBox")
            Me.SplashScreenComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.SplashScreenComboBox.FormattingEnabled = True
            Me.SplashScreenComboBox.Name = "SplashScreenComboBox"
            '
            'ViewCodeButton
            '
            resources.ApplyResources(Me.ViewCodeButton, "ViewCodeButton")
            Me.ViewCodeButton.Name = "ViewCodeButton"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.WindowsAppGroupBox, 0, 1)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'ApplicationPropPageVBWinForms
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "ApplicationPropPageVBWinForms"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).EndInit()
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.WindowsAppGroupBox.ResumeLayout(False)
            Me.WindowsAppGroupBox.PerformLayout()
            Me.BottomHalfLayoutPanel.ResumeLayout(False)
            Me.BottomHalfLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

        'Backing storage for the current MainForm value (without the root namespace)
        Protected MainFormTextboxNoRootNS As New TextBox

        Protected Const Const_SubMain As String = "Sub Main"
        Protected Const Const_MyApplicationEntryPoint As String = "My.MyApplication"
        Protected Const Const_MyApplication As String = "MyApplication"


        Private m_ShutdownModeStringValues As String()
        Private m_AuthenticationModeStringValues As String()
        Private m_NoneText As String
        Private m_MyType As String
        Private m_StartupObjectLabelText As String 'This one is in the form's resx when initialized
        Private m_StartupFormLabelText As String 'This one we pull from resources

        'This is the (cached) MyApplication.MyApplicationProperties object returned by the project system
        Private m_MyApplicationPropertiesCache As IMyApplicationPropertiesInternal
        Private WithEvents m_MyApplicationPropertiesNotifyPropertyChanged As INotifyPropertyChanged

        'Set to true if we have tried to cache the MyApplication properties value.  If this is True and
        '  m_MyApplicationPropertiesCache is Nothing, it indicates that the MyApplication property is not
        '  supported in this project system (which may mean the project flavor has turned off this support)
        Private m_IsMyApplicationPropertiesCached As Boolean

        'Cache whether MyType is one of the disabled values so we don't have to fetch it constantly
        '  from the project properties
        Private m_IsMyTypeDisabled As Boolean
        Private m_IsMyTypeDisabledCached As Boolean

        ' If set, we are using my application types as the 'output type'.  Otherwise, we are using
        ' output types provided by the project system
        Private m_UsingMyApplicationTypes As Boolean = True

        Protected Const Const_EnableVisualStyles As String = "EnableVisualStyles"
        Protected Const Const_AuthenticationMode As String = "AuthenticationMode"
        Protected Const Const_SingleInstance As String = "SingleInstance"
        Protected Const Const_ShutdownMode As String = "ShutdownMode"
        Protected Const Const_SplashScreenNoRootNS As String = "SplashScreen" 'we persist this without the root namespace
        Protected Const Const_CustomSubMain As String = "CustomSubMain"
        Protected Const Const_MainFormNoRootNS As String = "MainForm" 'we persist this without the root namespace
        Protected Const Const_MyType As String = "MyType"
        Protected Const Const_SaveMySettingsOnExit As String = "SaveMySettingsOnExit"

        ' Shared list of all known application types and their properties...
        Private Shared s_applicationTypes As New Generic.List(Of ApplicationTypeInfo)

        Private m_settingApplicationType As Boolean

        ''' <summary>
        '''  Set up shared state...
        ''' </summary>
        ''' <remarks></remarks>
        Shared Sub New()
            ' Populate shared list of all known application types allowed on this page
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsApp, SR.GetString(SR.PPG_WindowsFormsApp), True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsClassLib, SR.GetString(SR.PPG_WindowsClassLib), True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.CommandLineApp, SR.GetString(SR.PPG_CommandLineApp), True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsService, SR.GetString(SR.PPG_WindowsService), False))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WebControl, SR.GetString(SR.PPG_WebControlLib), False))
        End Sub

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            SetCommonControls()
            AddChangeHandlers()

            'Remember original text of the Start-up object label text
            m_StartupObjectLabelText = Me.StartupObjectLabel.Text

            'Get text for the forms case from resources
            m_StartupFormLabelText = SR.GetString(SR.PPG_Application_StartupFormLabelText)

            m_NoneText = SR.GetString(SR.PPG_ComboBoxSelect_None)

            'Ordering of strings here determines value stored in MyApplication.myapp
            m_ShutdownModeStringValues = New String() {SR.GetString(SR.PPG_MyApplication_StartupMode_FormCloses), SR.GetString(SR.PPG_MyApplication_StartupMode_AppExits)}
            m_AuthenticationModeStringValues = New String() {SR.GetString(SR.PPG_MyApplication_AuthenMode_Windows), SR.GetString(SR.PPG_MyApplication_AuthenMode_ApplicationDefined)}
            MyBase.PageRequiresScaling = False
        End Sub

        ''' <summary>
        ''' Let the base class know which control instances correspond to shared controls
        '''   between this inherited class and the base vb application property page class.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetCommonControls()
            m_CommonControls = New CommonControls( _
                Me.IconCombobox, Me.IconLabel, Me.IconPicturebox)
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                Dim ControlsThatDependOnStartupObjectProperty As Control() = { _
                    StartupObjectLabel, UseApplicationFrameworkCheckBox, WindowsAppGroupBox _
                }
                Dim ControlsThatDependOnOutputTypeProperty As Control() = { _
                    ApplicationTypeComboBox, ApplicationTypeLabel _
                }

                If m_ControlData Is Nothing Then
                    'StartupObject must be kept after OutputType because it depends on the initialization of "OutputType" values
                    ' Custom sub main must come before MainForm, because it will ASSERT on the enable frameowrk checkbox
                    ' StartupObject must be kept after MainForm, because it needs the main form name...
                    ' MyApplication should be kept before all other MyAppDISPIDs properties to make sure that everyting in there
                    ' is initialized correctly...
                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)

                    Dim data As PropertyControlData = New PropertyControlData(VBProjPropId.VBPROJPROPID_MyApplication, Const_MyApplication, Nothing, AddressOf Me.MyApplicationSet, AddressOf Me.MyApplicationGet, ControlDataFlags.UserHandledEvents)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.CustomSubMain, Const_CustomSubMain, Me.UseApplicationFrameworkCheckBox, AddressOf CustomSubMainSet, AddressOf CustomSubMainGet, ControlDataFlags.UserPersisted Or ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RootNamespace, Const_RootNamespace, Me.RootNamespaceTextBox, New Control() {RootNamespaceLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_RootNamespace)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, Nothing, AddressOf Me.OutputTypeSet, AddressOf Me.OutputTypeGet, ControlDataFlags.None, ControlsThatDependOnOutputTypeProperty)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.MainForm, Const_MainFormNoRootNS, Me.MainFormTextboxNoRootNS, AddressOf MainFormNoRootNSSet, Nothing, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, Const_StartupObject, Me.StartupObjectComboBox, AddressOf Me.StartupObjectSet, AddressOf Me.StartupObjectGet, ControlDataFlags.UserHandledEvents, ControlsThatDependOnStartupObjectProperty)
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_StartupObject)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", Me.AssemblyNameTextBox, New Control() {AssemblyNameLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyName)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", Me.IconCombobox, AddressOf MyBase.ApplicationIconSet, AddressOf MyBase.ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {Me.IconLabel, Me.IconPicturebox})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_ApplicationIcon)
                    datalist.Add(data)
                    data = New PropertyControlData(VBProjPropId.VBPROJPROPID_MyType, Const_MyType, Nothing, AddressOf Me.MyTypeSet, AddressOf Me.MyTypeGet)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.EnableVisualStyles, Const_EnableVisualStyles, Me.EnableXPThemesCheckBox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.AuthenticationMode, Const_AuthenticationMode, Me.AuthenticationModeComboBox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.SingleInstance, Const_SingleInstance, Me.SingleInstanceCheckBox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.ShutdownMode, Const_ShutdownMode, Me.ShutdownModeComboBox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile, New Control() {ShutdownModeLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.SplashScreen, Const_SplashScreenNoRootNS, Me.SplashScreenComboBox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile, New Control() {SplashScreenLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(MyAppDISPIDs.SaveMySettingsOnExit, Const_SaveMySettingsOnExit, Me.SaveMySettingsCheckbox, ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInVBMyAppFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", Nothing, ControlDataFlags.Hidden)
                    datalist.Add(data)

                    m_TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData(
                            VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker,
                            ApplicationPropPage.Const_TargetFrameworkMoniker,
                            TargetFrameworkComboBox,
                            AddressOf SetTargetFrameworkMoniker,
                            AddressOf GetTargetFrameworkMoniker,
                            ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout,
                            New Control() {Me.TargetFrameworkLabel})

                    datalist.Add(m_TargetFrameworkPropertyControlData)

                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property


        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub CleanupCOMReferences()
            MyBase.CleanupCOMReferences()

            m_MyApplicationPropertiesCache = Nothing
            m_MyApplicationPropertiesNotifyPropertyChanged = Nothing
            m_IsMyApplicationPropertiesCached = False
        End Sub


        Private ReadOnly Property MyApplicationPropertiesSupported() As Boolean
            Get
                Return MyApplicationProperties IsNot Nothing
            End Get
        End Property


        ''' <summary>
        ''' Gets the MyApplication.MyApplicationProperties object returned by the project system (which the project system creates by calling into us)
        ''' </summary>
        ''' <value>The value of the MyApplication property, or else Nothing if it is not supported.</value>
        ''' <remarks></remarks>
        Private ReadOnly Property MyApplicationProperties() As IMyApplicationPropertiesInternal
            Get
                Debug.Assert(Implies(m_MyApplicationPropertiesCache IsNot Nothing, m_IsMyApplicationPropertiesCached))
                Debug.Assert(Implies(m_MyApplicationPropertiesNotifyPropertyChanged IsNot Nothing, m_IsMyApplicationPropertiesCached))
                If Not m_IsMyApplicationPropertiesCached Then
                    'Set a flag so we don't keep trying to query for this property
                    m_IsMyApplicationPropertiesCached = True

                    Dim ApplicationProperties As Object = Nothing
                    If GetProperty(VBProjPropId.VBPROJPROPID_MyApplication, ApplicationProperties) Then
                        m_MyApplicationPropertiesCache = TryCast(ApplicationProperties, IMyApplicationPropertiesInternal)
                        m_MyApplicationPropertiesNotifyPropertyChanged = TryCast(ApplicationProperties, INotifyPropertyChanged)
                    Else
                        'MyApplication property is not supported in this project system
                        m_MyApplicationPropertiesCache = Nothing
                        m_MyApplicationPropertiesNotifyPropertyChanged = Nothing
                    End If
                End If

                Return m_MyApplicationPropertiesCache
            End Get
        End Property


        ''' <summary>
        ''' Attempts to run the custom tool for the .myapp file.  If an exception
        '''   is thrown, it is displayed to the user and swallowed.
        ''' </summary>
        ''' <returns>True on success.</returns>
        ''' <remarks></remarks>
        Private Function TryRunCustomToolForMyApplication() As Boolean
            If MyApplicationProperties IsNot Nothing Then
                Try
                    MyApplicationProperties.RunCustomTool()
                Catch ex As Exception
                    ShowErrorMessage(ex)
                End Try
            End If

            Return True
        End Function


        ''' <summary>
        ''' This is a readonly property, so don't return anything
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MyApplicationGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = MyApplicationProperties
            Return True
        End Function

        ''' <summary>
        ''' Value given us for "MyApplication" property 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MyApplicationSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'Nothing for us to do
            Return True
        End Function

        ''' <summary>
        ''' Returns the value stored in the UI for the MyType property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MyTypeGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = m_MyType
            Return True
        End Function

        ''' <summary>
        ''' Value given us for "MyType" property 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MyTypeSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            Dim stValue As String = CType(value, String)

            If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then
                m_MyType = stValue
            Else
                m_MyType = Nothing
            End If

            UpdateApplicationTypeUI()
            If Not m_fInsideInit Then
                ' We've got to make sure that we run the custom tool whenever we change
                ' the "application type"
                TryRunCustomToolForMyApplication()
            End If

            Return True
        End Function


        ''' <summary>
        ''' Gets the output type from the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks>OutputType is obtained from the value in the Application Type field</remarks>
        Protected Function OutputTypeGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If m_UsingMyApplicationTypes Then
                Dim AppType As ApplicationTypes

                If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                    AppType = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo).ApplicationType
                Else
                    AppType = ApplicationTypes.WindowsApp
                End If
                value = MyApplication.MyApplicationProperties.OutputTypeFromApplicationType(AppType)
            Else
                If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                    value = DirectCast(ApplicationTypeComboBox.SelectedItem, OutputTypeComboBoxValue).Value
                Else
                    value = VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_WinExe
                End If
            End If

            Return True
        End Function

        Protected Function OutputTypeSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            If m_UsingMyApplicationTypes Then
                'No UI for OutputType, ApplicationType provides our UI selection
                UpdateApplicationTypeUI()

                If Not m_fInsideInit Then
                    ' We've got to make sure that we run the custom tool whenever we change
                    ' the "application type"
                    TryRunCustomToolForMyApplication()
                End If
            Else
                Dim uIntValue As UInteger = CUInt(value)

                If SelectItemInOutputTypeComboBox(Me.ApplicationTypeComboBox, uIntValue) Then
                    PopulateStartupObject(StartUpObjectSupported(uIntValue), PopulateDropdown:=False)
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Make sure the application type combobox is showing the appropriate 
        ''' value
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateApplicationTypeUI()
            If m_settingApplicationType Then
                Return
            End If

            Dim oOutputType As Object = Nothing
            Dim oMyType As Object = Nothing
            If Me.GetProperty(VBProjPropId.VBPROJPROPID_MyType, oMyType) AndAlso oMyType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oMyType) _
                AndAlso Me.GetProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) _
            Then
                Dim AppType As MyApplication.ApplicationTypes = MyApplication.MyApplicationProperties.ApplicationTypeFromOutputType(CUInt(oOutputType), CStr(oMyType))
                Me.ApplicationTypeComboBox.SelectedItem = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(AppType))
                Me.EnableControlSet(AppType)
                Me.PopulateControlSet(AppType)
            Else
                Me.ApplicationTypeComboBox.SelectedIndex = -1
                EnableIconComboBox(False)
                EnableUseApplicationFrameworkCheckBox(False)
            End If
        End Sub


        ''' <summary>
        ''' Getter for the "CustSubMain" property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' The UI checkbox's logic is reversed from the property ("Enable application frameworks" = Not CustomSubMain).  However, because the property
        '''   is specified as CustomSubMain and I don't want to change it at this point, and the property change notification is based on the
        '''   CustomSubMain property ID, I didn't want to change the PropertyControlData to use a custom property.  So we reverse the logic in
        '''   a custom getter/setter
        ''' </remarks>
        Protected Function CustomSubMainGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If UseApplicationFrameworkCheckBox.CheckState <> CheckState.Indeterminate Then
                value = Not UseApplicationFrameworkCheckBox.Checked 'reversed
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Setter for the "CustSubMain" property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' The UI checkbox's logic is reversed from the property ("Enable application frameworks" = Not CustomSubMain).  However, because the property
        '''   is specified as CustomSubMain and I don't want to change it at this point, and the property change notification is based on the
        '''   CustomSubMain property ID, I didn't want to change the PropertyControlData to use a custom property.  So we reverse the logic in
        '''   a custom getter/setter
        ''' </remarks>
        Protected Function CustomSubMainSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                Me.UseApplicationFrameworkCheckBox.CheckState = CheckState.Indeterminate
            Else
                Me.UseApplicationFrameworkCheckBox.CheckState = Common.IIf(Not CBool(value), CheckState.Checked, CheckState.Unchecked) 'reversed
            End If

            'Toggle whether the application framework properties are enabled
            Me.WindowsAppGroupBox.Enabled = MyApplicationFrameworkEnabled()

            Return True
        End Function

        Private Function IsClassLibrary(ByVal AppType As ApplicationTypes) As Boolean
            If (AppType = ApplicationTypes.WindowsClassLib OrElse AppType = ApplicationTypes.WebControl) Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Enables the "Enable application framework" checkbox (if Enable=True), but only if it is supported in this project with current settings
        ''' </summary>
        ''' <param name="Enable"></param>
        ''' <remarks></remarks>
        Private Sub EnableUseApplicationFrameworkCheckBox(ByVal Enable As Boolean)
            If Enable Then
                Dim useApplicationFrameworkEnabled As Boolean = MyApplicationFrameworkSupported()
                UseApplicationFrameworkCheckBox.Enabled = useApplicationFrameworkEnabled
                Debug.Assert(Not MyApplicationPropertiesSupported OrElse UseApplicationFrameworkCheckBox.Checked = Not MyApplicationProperties.CustomSubMainRaw)

                'The groupbox with My-related properties on the page should only be
                '  enabled if the custom sub main checkbox is enabled but not
                '  checked.
                Debug.Assert(Implies(useApplicationFrameworkEnabled, MyApplicationProperties IsNot Nothing))
                WindowsAppGroupBox.Enabled = useApplicationFrameworkEnabled AndAlso Not MyApplicationProperties.CustomSubMainRaw 'Be sure to use CustomSubMainRaw instead of CustomSubMain - application type might not be set correctly yet
            Else
                UseApplicationFrameworkCheckBox.Enabled = False
                WindowsAppGroupBox.Enabled = False
            End If
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function StartupObjectGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Not StartUpObjectSupported() Then
                value = ""
                Return True
            End If

            Dim StringValue As String

            'Value in the combobox does not contain the root namespace
            StringValue = NothingToEmptyString(DirectCast(Me.StartupObjectComboBox.SelectedItem, String))

            If MyApplicationFrameworkEnabled() Then
                'Check that the main form is actually a form
                Dim IsAForm As Boolean = False
                Dim FormEntryPoints() As String = GetFormEntryPoints(IncludeSplashScreen:=False)

                If IsNoneText(StringValue) OrElse Const_SubMain.Equals(StringValue, StringComparison.OrdinalIgnoreCase) Then
                    'Not a form
                Else
                    Dim StringValueWithNamespace As String = AddCurrentRootNamespace(StringValue)
                    For Each FormName As String In FormEntryPoints
                        If String.Equals(FormName, StringValueWithNamespace, StringComparison.OrdinalIgnoreCase) Then
                            IsAForm = True
                            Exit For
                        End If
                    Next
                End If

                If Not IsAForm Then
                    If StringValue <> "" AndAlso StringValue.Equals(MyApplicationProperties.SplashScreenNoRootNS, StringComparison.OrdinalIgnoreCase) Then
                        'We couldn't find it because it's the same as the splash screen.  That's not allowed.
                        ShowErrorMessage(SR.GetString(SR.PPG_Application_SplashSameAsStart))
                    Else
                        'When the application framework is enabled, there must be a start-up form selected (MainForm) or there will
                        '  be a compile error or run-time error.  We avoid this when possible by picking the first available
                        '  form.  Also show a messagebox to let the user know about the problem (but don't throw an exception, because
                        '  that would cause problems in applying the other properties on the page).
                        ShowErrorMessage(SR.GetString(SR.PPG_Application_InvalidSubMainStartup))
                    End If

                    If FormEntryPoints IsNot Nothing AndAlso FormEntryPoints.Length() > 0 Then
                        'Change to an arbitrary start-up form and continue...
                        StringValue = RemoveCurrentRootNamespace(FormEntryPoints(0))
                    Else
                        'There is no start-up form available.  To keep from getting a compile or run-time error, we need to turn
                        '  off the application framework.
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                        SetDirty(MyAppDISPIDs.CustomSubMain, False)
                        value = ""
                        MainFormTextboxNoRootNS.Text = ""
                        SetDirty(MyAppDISPIDs.MainForm, False)
                        Return True
                    End If
                End If
            End If

            'If this is a WindowsApplication with My, then the value in the combobox is what we want
            '  to be the main form - this gets placed into MainFormTextboxNoRootNS and will get persisted
            '  out to MyApplicationProperties.MainFormNoRootNS.  The start-up object must be returned
            '  as a pointer to the start-up method in the My application framework stuff.
            If MyApplicationFrameworkEnabled() Then
                Debug.Assert(Not IsNoneText(StringValue), "None should not have been supported with the My stuff enabled")
                MainFormTextboxNoRootNS.Text = StringValue
                SetDirty(MyAppDISPIDs.MainForm, False)

                'Start-up object needs the root namespace
                value = AddCurrentRootNamespace(Const_MyApplicationEntryPoint)
            Else
                'My framework not enabled, add the root namespace to the raw value in the combobox, and that's the
                '  start-up object (unless it's (None)).
                If Not IsNoneText(StringValue) And Not Const_SubMain.Equals(StringValue, StringComparison.OrdinalIgnoreCase) Then
                    StringValue = AddCurrentRootNamespace(StringValue)
                End If

                value = StringValue
            End If

            Return True
        End Function

        ''' <summary>
        ''' Called by base to set update the UI
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function StartupObjectSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'This is handled by the ApplicationType set, so do nothing here
            'CONSIDER: The start-up object/MainForm-handling code needs to be reworked - it makes undo/redo/external property changes 
            '  more difficult than they should be.  Get code should not be changing the value of other properties.

            If Not m_fInsideInit Then
                'Property has been changed, refresh.
                PopulateStartupObject(StartUpObjectSupported(), False)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Setter for MainForm.  We handle this so that we also get notified when the property
        '''   has changed.
        ''' </summary>
        ''' <param name="conrol"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MainFormNoRootNSSet(ByVal conrol As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                MainFormTextboxNoRootNS.Text = DirectCast(value, String)

                'When this changes, we need to update the start-up object combobox
                If Not m_fInsideInit Then
                    PopulateStartupObject(StartUpObjectSupported(), PopulateDropdown:=False)
                End If
            Else
                MainFormTextboxNoRootNS.Text = ""
            End If

            Return True
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="OutputType"></param>
        ''' <remarks></remarks>
        Private Sub PopulateControlSet(ByVal OutputType As UInteger)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupported(OutputType), False)
        End Sub

        Private Sub PopulateControlSet(ByVal AppType As ApplicationTypes)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupportedForApplicationType(AppType), False)
        End Sub

        ''' <summary>
        ''' Populates the splash screen combobox's text and optionally dropdown entries
        ''' </summary>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        ''' <remarks></remarks>
        Protected Sub PopulateSplashScreenList(ByVal PopulateDropdown As Boolean)
            'Use the same list as StartupObject, but no sub main

            Dim StartupObjectControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)
            Dim SplashScreenControlData As PropertyControlData = GetPropertyControlData(Const_SplashScreenNoRootNS)

            If Not MyApplicationPropertiesSupported OrElse StartupObjectControlData.IsMissing OrElse SplashScreenControlData.IsMissing Then
                Debug.Assert(Me.SplashScreenComboBox.Enabled = False) 'Should have been disabled via PropertyControlData mechanism
                Debug.Assert(Me.SplashScreenLabel.Enabled = False) 'Should have been disabled via PropertyControlData mechanism
            Else
                With Me.SplashScreenComboBox
                    .Items.Clear()
                    .Items.Add(m_NoneText)

                    If PopulateDropdown Then
                        Switches.TracePDPerf("*** Populating splash screen list from the project [may be slow for a large project]")
                        Debug.Assert(Not m_fInsideInit, "PERFORMANCE ALERT: We shouldn't be populating the screen screen dropdown list during page initialization, it should be done later if needed.")
                        Using New WaitCursor
                            Dim CurrentMainForm As String = MyApplicationProperties.MainFormNoRootNamespace

                            For Each FullName As String In GetFormEntryPoints(IncludeSplashScreen:=True)
                                Dim SplashForm As String = RemoveCurrentRootNamespace(FullName)
                                'Only add forms to this list, skip 'Sub Main'
                                If (Not SplashForm.Equals(Const_MyApplicationEntryPoint, StringComparison.OrdinalIgnoreCase)) AndAlso _
                                    (Not SplashForm.Equals(Const_SubMain, StringComparison.OrdinalIgnoreCase)) Then
                                    'We don't allow the splash form and main form to be the same, so don't
                                    '  put the main into the splash form list
                                    If Not SplashForm.Equals(CurrentMainForm, StringComparison.OrdinalIgnoreCase) Then
                                        .Items.Add(SplashForm)
                                    End If
                                End If
                            Next
                        End Using
                    End If

                    If MyApplicationProperties.SplashScreenNoRootNS = "" Then
                        'Set to (None)
                        .SelectedIndex = 0
                    Else
                        .SelectedItem = MyApplicationProperties.SplashScreenNoRootNS
                        If .SelectedItem Is Nothing Then
                            'Not in the list - add it
                            .SelectedIndex = .Items.Add(MyApplicationProperties.SplashScreenNoRootNS)
                        End If
                    End If
                End With
            End If
        End Sub


        ''' <summary>
        ''' Returns True iff the My Application framework should be supportable
        '''   in this project.  It does not necessarily mean that it's turned on,
        '''   just that it can be supported.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MyApplicationFrameworkSupported() As Boolean
            If Not MyApplicationPropertiesSupported Then
                Return False
            End If

            Dim StartupObjectControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)
            If StartupObjectControlData.IsMissing Then
                'This project type does not support the Startup-Object property, therefore it can't
                '  support the My application framework.
                Return False
            End If

            If MyTypeDisabled() Then
                Return False
            End If

            Return True
        End Function


        ''' <summary>
        ''' Returns True iff the My Application framework stuff is supported
        '''   in this project system *and* it is currently turned on by the
        '''   user.
        ''' This means, among other things, that we have a list of start-up *forms*
        '''   instead of objects.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MyApplicationFrameworkEnabled() As Boolean
            If Not MyApplicationFrameworkSupported() Then
                Return False
            End If

            If Not m_UsingMyApplicationTypes Then
                Return False
            End If

            Dim appType As ApplicationTypeInfo = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            If appType IsNot Nothing _
                AndAlso appType.ApplicationType = ApplicationTypes.WindowsApp _
                AndAlso Me.UseApplicationFrameworkCheckBox.CheckState = CheckState.Checked _
            Then
                Return True
            Else
                Return False
            End If
        End Function


        ''' <summary>
        ''' Retrieve the list of start-up forms (not start-up objects) from the VB compiler
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetFormEntryPoints(ByVal IncludeSplashScreen As Boolean) As String()
            Try
                Dim EntryPointProvider As Interop.IVBEntryPointProvider = CType(ServiceProvider.GetService(Interop.NativeMethods.VBCompilerGuid), Interop.IVBEntryPointProvider)
                If EntryPointProvider IsNot Nothing Then
                    Dim EntryPoints() As String = New String() {}
                    Dim cEntryPointsAvailable As UInteger

                    'First call gets estimated number of entrypoints
                    Dim hr As Integer = EntryPointProvider.GetFormEntryPointsList(ProjectHierarchy, 0, Nothing, cEntryPointsAvailable)
                    If VSErrorHandler.Failed(hr) Then
                        Debug.Fail("Failed to get VB Form entry points, hr=0x" & Hex(hr))
                    ElseIf cEntryPointsAvailable > 0 Then
                        'Keep repeating until we give them a large enough array (it's possible the
                        '  number of entry points available has increased since we made our first call)
                        While EntryPoints.Length < cEntryPointsAvailable
                            ReDim EntryPoints(CInt(cEntryPointsAvailable) - 1)
                            EntryPointProvider.GetFormEntryPointsList(ProjectHierarchy, CUInt(EntryPoints.Length), EntryPoints, cEntryPointsAvailable)
                        End While

                        'We might have ended up with fewer than originally estimated...
                        ReDim Preserve EntryPoints(CInt(cEntryPointsAvailable) - 1)

                        If Not IncludeSplashScreen Then
                            'Filter out the splash screen
                            Dim SplashScreen As String = MyApplicationProperties.SplashScreen
                            For i As Integer = 0 To EntryPoints.Length - 1
                                If EntryPoints(i).Equals(SplashScreen, StringComparison.OrdinalIgnoreCase) Then
                                    'Found it - remove it
                                    For j As Integer = i + 1 To EntryPoints.Length - 1
                                        EntryPoints(i) = EntryPoints(j)
                                    Next
                                    ReDim Preserve EntryPoints(EntryPoints.Length - 1 - 1) 'Reduce allocated number by one
                                    Return EntryPoints
                                End If
                            Next
                        End If

                        'And return 'em...
                        Return EntryPoints
                    End If
                Else
                    Debug.Fail("Failed to get IVBEntryPointProvider")
                End If

            Catch ex As System.Exception
                Common.RethrowIfUnrecoverable(ex)
                Debug.Fail("An exception occurred in GetStartupForms() - using empty list" & vbCrLf & ex.ToString)
            End Try

            Return New String() {}
        End Function

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="StartUpObjectSupported">If false, (None) will be the only entry in the list.</param>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        ''' <remarks></remarks>
        Protected Sub PopulateStartupObject(ByVal StartUpObjectSupported As Boolean, ByVal PopulateDropdown As Boolean)
            'overridable to support the csharpapplication page (Sub Main isn't used by C#)
            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObjectComboBox
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedIndex = .Items.Add(m_NoneText)
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        Me.StartupObjectComboBox.Enabled = False
                        Me.StartupObjectLabel.Enabled = False
                    End If
                Else
                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc
                    Dim SwapWithMyAppData As Boolean = Me.MyApplicationFrameworkEnabled()

                    With StartupObjectComboBox
                        .Items.Clear()

                        If PopulateDropdown Then
                            Using New WaitCursor
                                Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                Dim StartupObjects As ICollection = Nothing
                                If MyApplicationFrameworkEnabled() Then
                                    StartupObjects = GetFormEntryPoints(IncludeSplashScreen:=False)
                                Else
                                    RefreshPropertyStandardValues() 'Force us to see any new start-up objects in the project

                                    'Certain project types may not support standard values
                                    If prop.Converter.GetStandardValuesSupported() Then
                                        StartupObjects = prop.Converter.GetStandardValues()
                                    End If
                                End If

                                If StartupObjects IsNot Nothing Then
                                    For Each o As Object In StartupObjects
                                        Dim EntryPoint As String = RemoveCurrentRootNamespace(TryCast(o, String))
                                        'Remove "My.MyApplication" from the list
                                        If SwapWithMyAppData AndAlso Const_SubMain.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase) Then
                                            'Do not add 'Sub Main' for MY applications
                                        ElseIf Not Const_MyApplicationEntryPoint.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase) Then
                                            .Items.Add(EntryPoint)
                                        End If
                                    Next
                                End If
                            End Using
                        End If

                        '(Okay to use StartupObject's InitialValue because we checked it against IsMissing up above)
                        Dim SelectedItemText As String = RemoveCurrentRootNamespace(CStr(StartupObjectPropertyControlData.InitialValue))
                        If SwapWithMyAppData Then
                            'We're using the My application framework for start-up, so that means we need to show the MainForm from
                            '  our my application stuff instead of what's in the start-up object (which would set to the My application
                            '  start-up).
                            SelectedItemText = MainFormTextboxNoRootNS.Text
                        End If

                        .SelectedItem = SelectedItemText
                        If .SelectedItem Is Nothing AndAlso SelectedItemText <> "" Then
                            .SelectedIndex = .Items.Add(SelectedItemText)
                        End If

                        If PopulateDropdown Then
                            'If "Sub Main" is not in the list and this isn't a WindowsApplication with My, then add it.
                            Dim SubMainIndex As Integer = .Items.IndexOf(Const_SubMain)
                            If SwapWithMyAppData Then
                                'Remove "Sub Main" if this is a MY app
                                If SubMainIndex > 0 Then
                                    .Items.RemoveAt(SubMainIndex)
                                End If
                            ElseIf .Items.IndexOf(Const_SubMain) < 0 Then
                                .Items.Add(Const_SubMain)
                            End If
                        End If
                    End With
                End If
            Finally
                'Restore previous state
                m_fInsideInit = InsideInitSave
            End Try
        End Sub

        Private Sub EnableControlSet(ByVal AppType As ApplicationTypes)
            Select Case AppType
                Case ApplicationTypes.CommandLineApp, ApplicationTypes.WindowsService
                    EnableIconComboBox(True)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case ApplicationTypes.WindowsApp
                    EnableIconComboBox(True)
                    EnableUseApplicationFrameworkCheckBox(True)

                Case ApplicationTypes.WindowsClassLib
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case ApplicationTypes.WebControl
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case Else
                    Debug.Fail("Unexpected ApplicationType")
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)
            End Select

            EnableMyApplicationControlSet()
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(AppType))
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="OutputType"></param>
        ''' <remarks></remarks>
        Private Sub EnableControlSet(ByVal OutputType As VSLangProj.prjOutputType)
            EnableIconComboBox(OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary)
            EnableMyApplicationControlSet()
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(OutputType))
        End Sub

        ''' <summary>
        ''' Sets the visibility of the MyApplication-related properties
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub EnableMyApplicationControlSet()
            If Not MyApplicationPropertiesSupported Then
                'If MyApplication property not supported at all, then this project system flavor has disabled it,
                '  and we want to completely hide all my-related controls, so we don't confuse users.
                WindowsAppGroupBox.Visible = False
                UseApplicationFrameworkCheckBox.Visible = False
            Else
                WindowsAppGroupBox.Visible = True
                UseApplicationFrameworkCheckBox.Visible = True
                SaveMySettingsCheckbox.Enabled = MySettingsSupported()
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropApplication
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

            If Not SupportsOutputTypeProperty() Then

                Me.ApplicationTypeComboBox.Enabled = False
                Me.ApplicationTypeLabel.Enabled = False

            Else

                ' If the project specifies the output types, use the output types instead of the my application types
                m_UsingMyApplicationTypes = Not PopulateOutputTypeComboBoxFromProjectProperty(ApplicationTypeComboBox)

                If m_UsingMyApplicationTypes Then
                    MyBase.PopulateApplicationTypes(ApplicationTypeComboBox, s_applicationTypes)
                End If
            End If

            Me.ShutdownModeComboBox.Items.Clear()
            Me.ShutdownModeComboBox.Items.AddRange(m_ShutdownModeStringValues)

            Me.AuthenticationModeComboBox.Items.Clear()
            Me.AuthenticationModeComboBox.Items.AddRange(m_AuthenticationModeStringValues)

            Me.PopulateTargetFrameworkComboBox(Me.TargetFrameworkComboBox)

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

            If MyTypeDisabled() Then
                'If the MyType is disabled, we should turn on Custom Sub Main.  This will ensure we don't write any
                '  code for Application.Designer.vb, which would not compile for ApplicationType=WindowsForms.
                If MyApplicationProperties IsNot Nothing Then
                    Try
                        MyApplicationProperties.CustomSubMain = True
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                    Catch ex As Exception When Not Utils.IsUnrecoverable(ex)
                    End Try
                End If
            End If

            ' enable/disable controls based upon the current value of the project's
            '   OutputType (.exe, .dll...)
            EnableControlSet(ProjectProperties.OutputType)

            PopulateIconList(False)
            UpdateIconImage(False)
            SetStartupObjectLabelText()

            PopulateSplashScreenList(False)

        End Sub

        Public Overrides Function GetUserDefinedPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            If PropertyName = Const_EnableVisualStyles Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_AuthenticationMode Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SingleInstance Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_ShutdownMode Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))
            ElseIf PropertyName = Const_CustomSubMain Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Takes a value from the property store, and converts it into the UI-displayable form
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function ReadUserDefinedProperty(ByVal PropertyName As String, ByRef Value As Object) As Boolean

            If PropertyName = Const_EnableVisualStyles Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.EnableVisualStyles
                End If

            ElseIf PropertyName = Const_SingleInstance Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.SingleInstance
                End If

            ElseIf PropertyName = Const_ShutdownMode Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim index As Integer = MyApplicationProperties.ShutdownMode
                    If index < 0 OrElse index > 1 Then
                        'If user horked the values, default to form exit
                        index = 0
                    End If
                    Value = Me.m_ShutdownModeStringValues(index)
                End If

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    If MyApplicationProperties.SplashScreenNoRootNS = "" Then
                        Value = Me.m_NoneText
                    ElseIf IsNoneText(MyApplicationProperties.SplashScreenNoRootNS) Then
                        Debug.Fail("Splash screen should not have been saved as (None)")
                        Value = ""
                    Else
                        Value = MyApplicationProperties.SplashScreenNoRootNS
                    End If
                End If

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim MainForm As String = MyApplicationProperties.MainFormNoRootNamespace
                    Debug.Assert(Not IsNoneText(MainForm), "MainForm should not have been persisted as (None)")
                    If MainForm = "" Then
                        Value = Me.m_NoneText
                    ElseIf Not IsNoneText(MainForm) Then
                        Value = MainForm
                    End If
                End If

            ElseIf PropertyName = Const_CustomSubMain Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.CustomSubMainRaw
                End If

            ElseIf PropertyName = Const_AuthenticationMode Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim Index As Integer = MyApplicationProperties.AuthenticationMode
                    If Not System.Enum.IsDefined(GetType(ApplicationServices.AuthenticationMode), Index) Then
                        'If user horked the values, default to Windows authentication
                        Index = ApplicationServices.AuthenticationMode.Windows
                    End If

                    Value = Me.m_AuthenticationModeStringValues(Index)
                End If
            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.SaveMySettingsOnExit
                End If

            Else
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Takes a value from the UI, converts it and writes it into the property store
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function WriteUserDefinedProperty(ByVal PropertyName As String, ByVal Value As Object) As Boolean
            If PropertyName = Const_EnableVisualStyles Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.EnableVisualStyles = CBool(Value)

            ElseIf PropertyName = Const_SingleInstance Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.SingleInstance = CBool(Value)

            ElseIf PropertyName = Const_ShutdownMode Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim index As Integer
                If m_ShutdownModeStringValues(1).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    'If user horked the values, default to form exit
                    index = 1
                Else
                    index = 0
                End If
                MyApplicationProperties.ShutdownMode = index

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim SplashScreenNoRootNS As String = Trim(TryCast(Value, String))
                If IsNoneText(SplashScreenNoRootNS) Then
                    'When the splash screen is none, we save it as an empty string
                    SplashScreenNoRootNS = ""
                End If
                MyApplicationProperties.SplashScreenNoRootNS = SplashScreenNoRootNS

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim MainForm As String = Trim(TryCast(Value, String))
                If IsNoneText(MainForm) Then
                    MainForm = ""
                End If
                MyApplicationProperties.MainFormNoRootNamespace = MainForm

            ElseIf PropertyName = Const_CustomSubMain Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                MyApplicationProperties.CustomSubMain = CBool(Value)

            ElseIf PropertyName = Const_AuthenticationMode Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim Index As Integer
                If m_AuthenticationModeStringValues(AuthenticationMode.Windows).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    Index = AuthenticationMode.Windows
                ElseIf m_AuthenticationModeStringValues(AuthenticationMode.ApplicationDefined).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    Index = AuthenticationMode.ApplicationDefined
                Else
                    'If user horked the values, default to Windows
                    Index = AuthenticationMode.Windows
                End If
                MyApplicationProperties.AuthenticationMode = Index

            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.SaveMySettingsOnExit = CBool(Value)

            Else
                Return False
            End If

            Return True
        End Function



        ''' <summary>
        ''' Get the current value of MyType from the UI
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetMyTypeFromUI() As String

            Dim MyTypeObject As Object = Nothing
            MyTypeGet(Nothing, Nothing, MyTypeObject)

            Dim MyType As String
            MyType = TryCast(MyTypeObject, String)

            Return MyType
        End Function


        ''' <summary>
        ''' Get the current value of MyType from the project properties
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetMyTypeFromProject() As String
            Dim MyTypeObject As Object = Nothing
            If GetProperty(VBProjPropId.VBPROJPROPID_MyType, MyTypeObject) Then
                Return TryCast(MyTypeObject, String)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' If MyType is set to "Empty" or "Custom", then the property page should consider
        '''   the MyType function "disabled" and should not show the My application properties,
        '''   nor should it change the MyType to any other value.  This allows programmers to
        '''   effectively turn off My and leave it off.  Returns true if My has been disabled.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MyTypeDisabled() As Boolean
            If Not m_IsMyTypeDisabledCached Then
                m_IsMyTypeDisabledCached = True
                Dim MyType As String = GetMyTypeFromProject()

                m_IsMyTypeDisabled = MyType IsNot Nothing _
                    AndAlso (MyType.Equals(MyApplication.MyApplicationProperties.Const_MyType_Empty, StringComparison.OrdinalIgnoreCase) _
                                OrElse MyType.Equals(MyApplication.MyApplicationProperties.Const_MyType_Custom, StringComparison.OrdinalIgnoreCase))
            End If

            Return m_IsMyTypeDisabled
        End Function


        ''' <summary>
        ''' Sets the current value of MyType based on the application type
        ''' </summary>
        ''' <param name="AppType"></param>
        ''' <remarks></remarks>
        Private Sub SetMyType(ByVal AppType As ApplicationTypes, ByVal ReadyToApply As Boolean)
            Debug.Assert(UseApplicationFrameworkCheckBox.CheckState <> CheckState.Indeterminate OrElse Not MyApplicationFrameworkSupported() OrElse MyTypeDisabled(), _
                "UseApplicationFrameworkCheckbox shouldn't be indeterminate")
            Dim NewMyType As String = MyTypeFromApplicationType(AppType, UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked OrElse Not MyApplicationFrameworkSupported() OrElse MyTypeDisabled())
            Debug.Assert(NewMyType IsNot Nothing)
            NewMyType = NothingToEmptyString(NewMyType)
            Dim CurrentMyType As Object = Nothing
            If MyTypeGet(Nothing, Nothing, CurrentMyType) Then
                If CurrentMyType Is Nothing OrElse Not TypeOf CurrentMyType Is String OrElse Not String.Equals(NewMyType, CStr(CurrentMyType), StringComparison.Ordinal) Then
                    'The value has changed - 
                    ' now poke it into our storage thru the same mechanism that the page-hosting
                    '   infrastructure does.
                    '
                    If MyTypeDisabled() Then
                        Trace.WriteLine("MyType has been disabled (""Empty"" or ""Custom"") - not changing the value of MyType")
                        Return
                    End If

                    ' Save the new MyType
                    Dim stValue As String = CType(NewMyType, String)

                    If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then
                        m_MyType = stValue
                    Else
                        m_MyType = Nothing
                    End If

                    SetDirty(VBProjPropId.VBPROJPROPID_MyType, ReadyToApply)
                    If ReadyToApply Then
                        UpdateApplicationTypeUI()
                    End If
                End If
            Else
                Debug.Fail("MyTypeGet failed")
            End If
        End Sub

        ''' <summary>
        ''' Sets the current value of MyType based on the application type
        ''' </summary>
        ''' <param name="AppType"></param>
        ''' <remarks></remarks>
        Private Function MyTypeFromApplicationType(ByVal AppType As ApplicationTypes, ByVal CustomSubMain As Boolean) As String
            Dim MyType As String

            Select Case AppType
                Case ApplicationTypes.WindowsApp
                    If CustomSubMain Then
                        MyType = MyApplication.MyApplicationProperties.Const_MyType_WindowsFormsWithCustomSubMain
                    Else
                        MyType = MyApplication.MyApplicationProperties.Const_MyType_WindowsForms
                    End If

                Case ApplicationTypes.WindowsClassLib
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Windows

                Case ApplicationTypes.CommandLineApp
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Console

                Case ApplicationTypes.WindowsService
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Console

                Case ApplicationTypes.WebControl
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_WebControl

                Case Else
                    Debug.Fail("Unexpected Application Type - setting MyType to empty")
                    MyType = ""
            End Select

            Return MyType
        End Function


        ''' <summary>
        ''' Sets the text on the start-up object label to be either "Startup object" or "Startup form" depending
        '''   on whether a custom sub main is being used or not.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetStartupObjectLabelText()
            If MyApplicationFrameworkEnabled() Then
                Me.StartupObjectLabel.Text = m_StartupFormLabelText
            Else
                Me.StartupObjectLabel.Text = m_StartupObjectLabelText
            End If
        End Sub

        Private Sub AssemblyInfoButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(SR.GetString(SR.PPG_AssemblyInfo_Title), GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComboBoxes_DropDown(ByVal sender As Object, ByVal e As EventArgs) Handles ApplicationTypeComboBox.DropDown
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Retrieve the current application type set in the UI
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetAppTypeFromUI() As ApplicationTypes
            Dim appTypeInfo As ApplicationTypeInfo = TryCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            If appTypeInfo IsNot Nothing Then
                Return appTypeInfo.ApplicationType
            Else
                Return ApplicationTypes.WindowsApp
            End If
        End Function

        ''' <summary>
        ''' Add required references for the current application type set in the UI
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub AddRequiredReferences()
            Dim appTypeInfo As ApplicationTypeInfo = TryCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            Dim requiredReferences As String()
            If appTypeInfo Is Nothing Then
                appTypeInfo = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(ApplicationTypes.WindowsApp))
            End If

            requiredReferences = appTypeInfo.References

            Dim vsProj As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            For Each requiredReference As String In requiredReferences
                vsProj.References.Add(requiredReference)
            Next
        End Sub

        Private Sub ApplicationTypeComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles ApplicationTypeComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            If m_settingApplicationType Then
                Return
            End If

            Try
                m_settingApplicationType = True

                Dim outputType As UInteger

                If m_UsingMyApplicationTypes Then
                    'Disable or enable the controls based on ApplicationType
                    Dim AppType As ApplicationTypes = GetAppTypeFromUI()
                    EnableControlSet(AppType)

                    ' add necessary references...
                    Try
                        AddRequiredReferences()
                    Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex) AndAlso Not Common.Utils.IsCheckoutCanceledException(ex)
                        ShowErrorMessage(ex)
                    End Try

                    'Update MyType property
                    '
                    SetMyType(AppType, False)
                    outputType = MyApplication.MyApplicationProperties.OutputTypeFromApplicationType(AppType)
                Else
                    outputType = CUInt(GetControlValueNative(Const_OutputTypeEx))
                End If

                SetStartupObjectLabelText()

                'Mark all fields dirty that need to update with this change
                '
                SetDirty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, False)
                SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)

                SetDirty(True)
                If ProjectReloadedDuringCheckout Then
                    Return
                End If

                PopulateControlSet(outputType)

            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                ' There are lots of issues with check-out... I leave it to vswhidbey 475879
                Dim appTypeValue As Object = Nothing
                Dim CurrentAppType As ApplicationTypes = CType(appTypeValue, ApplicationTypes)
                ApplicationTypeComboBox.SelectedIndex = CInt(CurrentAppType)
                EnableControlSet(CurrentAppType)
                PopulateControlSet(CurrentAppType)
                ShowErrorMessage(ex)
            Finally
                m_settingApplicationType = False

            End Try

            ' We've got to make sure that we run the custom tool whenever we change
            ' the "application type"
            TryRunCustomToolForMyApplication()

        End Sub

        Private Sub StartupObjectComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles StartupObjectComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If
            SetDirty(sender, True)
        End Sub


        ''' <summary>
        ''' Handle the "View Code" button's click event.  On this, we navigate to the MyEvents.vb file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ViewCodeButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ViewCodeButton.Click
            Static IsInViewCodeButtonClick As Boolean
            If IsInViewCodeButtonClick Then
                'Avoid recursive call (possible because of DoEvents work-around in CreateNewMyEventsFile
                Exit Sub
            End If
            IsInViewCodeButtonClick = True

            ' Navigate to events may add a file to the project, which may in turn cause the
            ' project file to be checked out at a later version. This will cause the project
            ' file to be reloaded, which will dispose me and bad things will happen (unless I
            ' tell myselft that I'm about to potentially check out stuff)
            EnterProjectCheckoutSection()
            Try
                MyApplicationProperties.NavigateToEvents()
            Catch ex As Exception
                Common.RethrowIfUnrecoverable(ex)
                If Not Me.ProjectReloadedDuringCheckout Then
                    ShowErrorMessage(ex)
                End If
            Finally
                LeaveProjectCheckoutSection()
                IsInViewCodeButtonClick = False
            End Try
        End Sub


        ''' <summary>
        ''' Happens when the splash screen combobox box is opened.  Use this to populate it with the 
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub SplashScreenComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles SplashScreenComboBox.DropDown
            PopulateSplashScreenList(True)
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub


        ''' <summary>
        ''' Happens when the start-up object combobox box is opened.  Use this to populate it with the 
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub StartupObjectComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles StartupObjectComboBox.DropDown
            PopulateStartupObject(StartUpObjectSupported(), True)
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Returns true iff the current project supports the default settings file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MySettingsSupported() As Boolean
            Debug.Assert(DTEProject IsNot Nothing)
            If DTEProject IsNot Nothing Then
                Dim SpecialFiles As IVsProjectSpecialFiles = TryCast(ProjectHierarchy, IVsProjectSpecialFiles)
                If SpecialFiles IsNot Nothing Then
                    Dim ItemId As UInteger
                    Dim SpecialFilePath As String = Nothing
                    Dim hr As Integer = SpecialFiles.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
                    If VSErrorHandler.Succeeded(hr) AndAlso SpecialFilePath <> "" Then
                        'Yes, settings files are supported (doesn't necessarily mean the file currently exists)
                        Return True
                    End If
                Else
                    Debug.Fail("Couldn't get IVsProjectSpecialFiles")
                End If
            End If

            Return False
        End Function

#Region "Application icon"

        Private Sub IconCombobox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.DropDown
            MyBase.HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_DropDownClosed(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.DropDownClosed
            MyBase.HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.SelectionChangeCommitted
            MyBase.HandleIconComboboxSelectionChangeCommitted(sender)
        End Sub

#End Region

        Private Sub UseApplicationFrameworkCheckBox_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles UseApplicationFrameworkCheckBox.CheckedChanged
            If m_fInsideInit Then
                Return
            End If

            If UseApplicationFrameworkCheckBox.CheckState = CheckState.Checked Then
                'Having the application framework enabled requires that the start-up object be a form.  If there
                '  is no such form available, the code in StartupObjectGet will not be able to correct the Start-up 
                '  object to be a form, and we'll end up possibly with compiler errors in the generated code which will
                '  be confusing to the user.  So if there is no start-up form available in the project, then disable
                '  the application framework again and tell the user why.
                If GetFormEntryPoints(IncludeSplashScreen:=False).Length = 0 Then
                    ShowErrorMessage(SR.GetString(SR.PPG_Application_InvalidSubMainStartup))
                    Try
                        Debug.Assert(Not m_fInsideInit, "This should have been checked at the beginning of this method")
                        m_fInsideInit = True 'Keep this routine from getting called recursively
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                    Finally
                        m_fInsideInit = False
                    End Try
                    Return
                End If
            End If

            'Checkstate should toggle the enabled state of the application groupbox
            Me.WindowsAppGroupBox.Enabled = MyApplicationFrameworkEnabled()

            'Startupobject must be reset when 'CustomSubMain' is changed
            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)
            SetDirty(MyAppDISPIDs.CustomSubMain, False)
            'MyType may change
            SetMyType(GetAppTypeFromUI, False)
            SetStartupObjectLabelText()

            SetDirty(True)
            If ProjectReloadedDuringCheckout Then
                Return
            End If

            UpdateApplicationTypeUI()
            PopulateStartupObject(StartUpObjectSupported(), False)
        End Sub

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported for this app type
        ''' </summary>
        Private Function StartUpObjectSupportedForApplicationType(ByVal AppType As ApplicationTypes) As Boolean
            Return Not IsClassLibrary(AppType)
        End Function

        ''' <summary>
        ''' Returns True iff the given string is the special value used for "(None)"
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsNoneText(ByVal Value As String) As Boolean
            'We use ordinal because a) we put the value into the combobox, it could not have magically
            '  changed case, and b) we don't want to use culture-aware because if the user changes cultures
            '  while our page is up, our functionality might be affected
            Return Value IsNot Nothing AndAlso Value.Equals(m_NoneText, StringComparison.Ordinal)
        End Function


        ''' <summary>
        ''' Fired when any of the MyApplicationProperty values has been changed
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MyApplicationProperties_PropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs) Handles m_MyApplicationPropertiesNotifyPropertyChanged.PropertyChanged
            Debug.Assert(e.PropertyName <> "")
            Switches.TracePDProperties(TraceLevel.Info, "MyApplicationProperties_PropertyChanged(""" & e.PropertyName & """)")

            Dim Data As PropertyControlData = GetPropertyControlData(e.PropertyName)

            If Data IsNot Nothing Then
                'Let the base class take care of it in the usual way for external property changes...
                MyBase.OnExternalPropertyChanged(Data.DispId, "MyApplicationProperties")
            Else
                Debug.Fail("Couldn't find property control data for property changed in MyApplicationProperties")
            End If
        End Sub

#Region "UAC Settings"

        ''' <summary>
        ''' The View UAC Settings button has been clicked...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ViewUACSettingsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ViewUACSettingsButton.Click
            ViewUACSettings()
        End Sub

#End Region

    End Class

End Namespace


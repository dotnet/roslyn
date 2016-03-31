Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Imports System.ComponentModel
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports EnvDTE
Imports VSLangProj80
Imports VslangProj90
Imports VslangProj100
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    ''' <summary>
    ''' The application property page for VB WPF apps
    ''' - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ApplicationPropPageVBWPF
        Inherits ApplicationPropPageVBBase
        'Inherits UserControl

        'Holds the DocData for the Application.xaml file
        Private WithEvents m_ApplicationXamlDocData As DocData

        Private Shared m_NoneText As String '(None)" in the startup object combobox
        Private Shared m_StartupObjectLabelText As String 'The label text to use for a startup object
        Private Shared m_StartupUriLabelText As String 'The label text to use for a startup Uri
        Private m_errorControl As AppDotXamlErrorControl

        Protected Const STARTUPOBJECT_SubMain As String = "Sub Main"

        Private Const VB_EXTENSION As String = ".vb"

        Const BUILDACTION_PAGE As String = "Page"
        Const BUILDACTION_APPLICATIONDEFINITION As String = "ApplicationDefinition"


#Region "User-defined properties for this page"

        Private Const PROPID_StartupObjectOrUri As Integer = 100
        Private Const PROPNAME_StartupObjectOrUri As String = "StartupObjectOrUri"

        Private Const PROPID_ShutDownMode As Integer = 101
        Private Const PROPNAME_ShutDownMode As String = "ShutdownMode"

        Private Const PROPID_UseApplicationFramework As Integer = 102
        Private Const PROPNAME_UseApplicationFramework As String = "UseApplicationFramework"

        'This property is added by the WPF flavor as an extended property
        Private Const PROPID_HostInBrowser As Integer = 103
        Private Const PROPNAME_HostInBrowser As String = "HostInBrowser"

#End Region

#Region "Dispose"

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            '
            'NOTE:
            '  Most clean-up should be done in the overridden CleanupCOMReferences
            '  function, which is called by the base in its Dispose method and also
            '  when requested by the property page host.
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If

                CleanUpApplicationXamlDocData()
            End If
            MyBase.Dispose(disposing)
        End Sub

#End Region

#Region "Clean-up"

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub CleanupCOMReferences()
            TrySaveDocDataIfLastEditor()
            m_docDataHasChanged = False

            MyBase.CleanupCOMReferences()

        End Sub

        ''' <summary>
        ''' Closes our copy of the Application.xaml doc data.  If we're the last editor on it,
        '''   then we save it first.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CleanUpApplicationXamlDocData()
            TrySaveDocDataIfLastEditor()
            If m_ApplicationXamlDocData IsNot Nothing Then
                Dim docData As DocData = m_ApplicationXamlDocData
                m_ApplicationXamlDocData = Nothing
                docData.Dispose()
            End If
        End Sub

#End Region

#Region "Shared Sub New"

        ''' <summary>
        '''  Set up shared state...
        ''' </summary>
        ''' <remarks></remarks>
        Shared Sub New()
            InitializeApplicationTypes()
            InitializeShutdownModeValues()

            m_NoneText = SR.GetString(SR.PPG_ComboBoxSelect_None)

            'Get text for the Startup Object/Uri label from resources
            m_StartupUriLabelText = My.Resources.Designer.PPG_Application_StartupUriLabelText
            m_StartupObjectLabelText = My.Resources.Designer.PPG_Application_StartupObjectLabelText
        End Sub

#End Region

#Region "Sub New"
        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            SetCommonControls()
            AddChangeHandlers()

            MyBase.PageRequiresScaling = False
        End Sub

#End Region

#Region "PropertyControlData"

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                Dim ControlsThatDependOnStartupObjectOrUriProperty As Control() = { _
                    StartupObjectOrUriLabel, UseApplicationFrameworkCheckBox, WindowsAppGroupBox _
                }

                If m_ControlData Is Nothing Then
                    Dim list As New List(Of PropertyControlData)
                    Dim data As PropertyControlData

                    'StartupObject.  
                    'StartupObjectOrUri must be kept after OutputType because it depends on the initialization of "OutputType" values
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, Const_StartupObject, Nothing, ControlDataFlags.Hidden)
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_StartupObject)
                    list.Add(data)

                    'RootNamespace
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RootNamespace, Const_RootNamespace, Me.RootNamespaceTextBox, New Control() {RootNamespaceLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_RootNamespace)
                    list.Add(data)

                    'OutputType
                    'Use RefreshAllPropertiesWhenChanged because changing the OutputType (application type) affects
                    '  the enabled state of other controls
                    list.Add(New PropertyControlData(VsProjPropId.VBPROJPROPID_OutputType, Const_OutputType, ApplicationTypeComboBox, AddressOf Me.SetOutputTypeIntoUI, AddressOf Me.GetOutputTypeFromUI, ControlDataFlags.RefreshAllPropertiesWhenChanged, New Control() {ApplicationTypeComboBox, ApplicationTypeLabel}))

                    'StartupObjectOrUri (user-defined)
                    'NoOptimisticFileCheckout - this property is stored in either the project file or the
                    '  application definition file, depending on whether we're storing a startup URI or a
                    '  startup object.  So we turn off the automatic file checkout so we don't require
                    '  the user to check out files s/he doesn't need to.  This is okay - the property change
                    '  will still cause a file checkout, it just won't be grouped together if there are
                    '  any other files needing to be checked out at the same time.
                    list.Add(New PropertyControlData( _
                        PROPID_StartupObjectOrUri, PROPNAME_StartupObjectOrUri, _
                        Me.StartupObjectOrUriComboBox, _
                        AddressOf Me.SetStartupObjectOrUriIntoUI, AddressOf Me.GetStartupObjectOrUriFromUI, _
                        ControlDataFlags.UserPersisted Or ControlDataFlags.NoOptimisticFileCheckout, _
                        ControlsThatDependOnStartupObjectOrUriProperty))

                    'AssemblyName
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", Me.AssemblyNameTextBox, New Control() {AssemblyNameLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyName)
                    list.Add(data)

                    'ApplicationIcon
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", Me.IconCombobox, AddressOf MyBase.ApplicationIconSet, AddressOf MyBase.ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {Me.IconLabel, Me.IconPicturebox})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_ApplicationIcon)
                    list.Add(data)

                    'ShutdownMode (user-defined)
                    list.Add(New PropertyControlData( _
                        PROPID_ShutDownMode, PROPNAME_ShutDownMode, _
                        Me.ShutdownModeComboBox, _
                        AddressOf SetShutdownModeIntoUI, AddressOf GetShutdownModeFromUI, _
                        ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInApplicationDefinitionFile, _
                        New Control() {ShutdownModeLabel}))

                    'UseApplicationFramework (user-defined)
                    'Use RefreshAllPropertiesWhenChanged to force other property controls to get re-enabled/disabled when this changes
                    list.Add(New PropertyControlData( _
                        PROPID_UseApplicationFramework, PROPNAME_UseApplicationFramework, UseApplicationFrameworkCheckBox, _
                        AddressOf SetUseApplicationFrameworkIntoUI, AddressOf GetUseApplicationFrameworkFromUI, _
                        ControlDataFlags.UserPersisted Or ControlDataFlags.RefreshAllPropertiesWhenChanged, _
                        New Control() {Me.WindowsAppGroupBox}))

                    'HostInBrowser (Avalon flavor extended property)
                    '  Tells whether the project is an XBAP app
                    list.Add(New PropertyControlData( _
                        PROPID_HostInBrowser, PROPNAME_HostInBrowser, Nothing, _
                        ControlDataFlags.Hidden))

                    ' ApplicationManifest - added simply to enable flavoring visibility of the button
                    list.Add(New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", Nothing, ControlDataFlags.Hidden))

                    m_TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData( _
                        VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker,
                        ApplicationPropPage.Const_TargetFrameworkMoniker,
                        TargetFrameworkComboBox,
                        AddressOf SetTargetFrameworkMoniker,
                        AddressOf GetTargetFrameworkMoniker,
                        ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout,
                        New Control() {Me.TargetFrameworkLabel})

                    list.Add(m_TargetFrameworkPropertyControlData)

                    m_ControlData = list.ToArray()
                End If

                Return m_ControlData
            End Get
        End Property

#End Region

#Region "Common controls (used by base)"

        ''' <summary>
        ''' Let the base class know which control instances correspond to shared controls
        '''   between this inherited class and the base vb application property page class.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetCommonControls()
            m_CommonControls = New CommonControls( _
                Me.IconCombobox, Me.IconLabel, Me.IconPicturebox)
        End Sub

#End Region

#Region "Pre-init and post-init page initialization customization"

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

            PopulateApplicationTypes(ApplicationTypeComboBox, s_applicationTypes)

            Me.ShutdownModeComboBox.Items.Clear()
            Me.ShutdownModeComboBox.Items.AddRange(s_shutdownModes.ToArray())

            DisplayErrorControlIfAppXamlIsInvalid()

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

            PopulateIconList(False)
            UpdateIconImage(False)
            DisableControlsForXBAPProjects()

            ' Enable/disable the "View UAC Settings" button
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(ProjectProperties.OutputType))
        End Sub

#End Region

#Region "F1 help"

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropApplicationWPF
        End Function

#End Region

#Region "Saving the doc data"

        Private Sub TrySaveDocDataIfLastEditor()
            If m_ApplicationXamlDocData IsNot Nothing AndAlso ServiceProvider IsNot Nothing Then
                Try
                    Dim rdt As IVsRunningDocumentTable = TryCast(ServiceProvider.GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
                    Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")
                    If rdt Is Nothing Then Throw New PropertyPageException("No RDT")

                    Dim hier As IVsHierarchy = Nothing
                    Dim flags As UInteger
                    Dim localPunk As IntPtr = IntPtr.Zero
                    Dim localFileName As String = Nothing
                    Dim itemId As UInteger
                    Dim docCookie As UInteger = 0
                    Dim readLocks As UInteger = 0
                    Dim editLocks As UInteger = 0

                    Try
                        VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), m_ApplicationXamlDocData.Name, hier, itemId, localPunk, docCookie))
                    Finally
                        If Not localPunk.Equals(IntPtr.Zero) Then
                            Marshal.Release(localPunk)
                            localPunk = IntPtr.Zero
                        End If
                    End Try

                    Debug.Assert(hier Is ProjectHierarchy, "RunningDocumentTable.FindAndLockDocument returned a different hierarchy than the one I was constructed with?")

                    Try
                        VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hier, itemId, localPunk))
                    Finally
                        If Not localPunk.Equals(IntPtr.Zero) Then
                            Marshal.Release(localPunk)
                            localPunk = IntPtr.Zero
                        End If
                    End Try

                    If editLocks = 1 Then
                        ' we're the only person with it open, save the document
                        VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), hier, itemId, docCookie))
                    End If
                Catch ex As Exception
                    ShowErrorMessage(ex)
                End Try
            End If
        End Sub

#End Region

#Region "Application type"

        ' Shared list of all known application types and their properties...
        Private Shared s_applicationTypes As New Generic.List(Of ApplicationTypeInfo)

        ''' <summary>
        ''' Initialize the application types applicable to this page (logic is in the base class)
        ''' </summary>
        ''' <remarks></remarks>
        Private Shared Sub InitializeApplicationTypes()
            '   Note: WPF application page does not support NT service or Web control application types
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsApp, SR.GetString(SR.PPG_WindowsApp_WPF), True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsClassLib, SR.GetString(SR.PPG_WindowsClassLib_WPF), True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.CommandLineApp, SR.GetString(SR.PPG_CommandLineApp_WPF), True))
        End Sub

#End Region

#Region "Application icon"

        '
        'Delegate to the base class for all functionality related to the icon combobox
        '

        Private Sub IconCombobox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.DropDown
            MyBase.HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_DropDownClosed(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.DropDownClosed
            MyBase.HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles IconCombobox.SelectionChangeCommitted
            MyBase.HandleIconComboboxSelectionChangeCommitted(sender)
        End Sub

        ''' <summary>
        ''' Enables the Icon combobox (if Enable=True), but only if the associated property is supported
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub EnableIconComboBox(ByVal Enable As Boolean)
            'Icon combobox shouldn't be enabled for XBAP projects
            EnableControl(m_CommonControls.IconCombobox, Enable AndAlso Not IsXBAP())
            UpdateIconImage(False)
        End Sub

#End Region

#Region "Assembly Information button"

        ''' <summary>
        ''' Display the assembly information dialog
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub AssemblyInfoButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(SR.GetString(SR.PPG_AssemblyInfo_Title), GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub

#End Region

#Region "OutputType property ('Application Type' combobox)"

        ''' <summary>
        ''' Gets the output type from the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks>OutputType is obtained from the value in the Application Type field</remarks>
        Private Function GetOutputTypeFromUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim AppType As ApplicationTypes

            If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                AppType = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo).ApplicationType
            Else
                Debug.Fail("Why isn't there a selection in the Application Type combobox?")
                AppType = ApplicationTypes.WindowsApp
            End If

            value = OutputTypeFromApplicationType(AppType)
            Return True
        End Function

        ''' <summary>
        ''' Sets the output type into the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SetOutputTypeIntoUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(value) Then
                Dim AppType As MyApplication.ApplicationTypes = ApplicationTypeFromOutputType(CType(value, VSLangProj.prjOutputType))
                Me.ApplicationTypeComboBox.SelectedItem = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(AppType))
                Me.EnableApplicationIconAccordingToApplicationType(AppType)
                EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(AppType))
            Else
                Me.ApplicationTypeComboBox.SelectedIndex = -1
                EnableIconComboBox(False)
                EnableControl(ViewUACSettingsButton, False)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Enables/Disables some controls based on the current application type
        ''' </summary>
        ''' <param name="AppType"></param>
        ''' <remarks></remarks>
        Private Sub EnableApplicationIconAccordingToApplicationType(ByVal AppType As ApplicationTypes)
            Select Case AppType
                Case ApplicationTypes.CommandLineApp
                    EnableIconComboBox(True)

                Case ApplicationTypes.WindowsApp
                    EnableIconComboBox(True)

                Case ApplicationTypes.WindowsClassLib
                    EnableIconComboBox(False)

                Case Else
                    Debug.Fail("Unexpected ApplicationType")
                    EnableIconComboBox(False)
            End Select
        End Sub

        '
        ' Application Type and Output Type are related in the following manner (note that it *is* one-to-one for WPF,
        '  unlikes the more complicated logic for the non-WPF VB application page):
        '
        '  Application Type      -> Output Type
        '  ---------------------    -----------
        '  Windows Application   -> winexe
        '  Windows Class Library -> library
        '  Console Application   -> exe
        '

        ''' <summary>
        ''' Given an OutputType, returns the Application Type for it, differentiating if necessary based on the value of MyType
        ''' </summary>
        ''' <param name="OutputType">Output type</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function ApplicationTypeFromOutputType(ByVal OutputType As VSLangProj.prjOutputType) As ApplicationTypes
            Select Case OutputType

                Case VSLangProj.prjOutputType.prjOutputTypeExe
                    Return ApplicationTypes.CommandLineApp
                Case VSLangProj.prjOutputType.prjOutputTypeWinExe
                    Return ApplicationTypes.WindowsApp
                Case VSLangProj.prjOutputType.prjOutputTypeLibrary
                    Return ApplicationTypes.WindowsClassLib
                Case Else
                    If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                        Debug.Fail(String.Format("Unexpected Output Type {0} - Mapping to ApplicationTypes.WindowsApp", OutputType))
                    End If
                    Return ApplicationTypes.WindowsApp
            End Select
        End Function

        ''' <summary>
        ''' Given an Application Type (a VB-only concept), return the Output Type for it (the project system's concept)
        ''' </summary>
        ''' <param name="AppType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function OutputTypeFromApplicationType(ByVal AppType As ApplicationTypes) As VSLangProj.prjOutputType
            Select Case AppType

                Case ApplicationTypes.WindowsApp
                    Return VSLangProj.prjOutputType.prjOutputTypeWinExe
                Case ApplicationTypes.WindowsClassLib
                    Return VSLangProj.prjOutputType.prjOutputTypeLibrary
                Case ApplicationTypes.CommandLineApp
                    Return VSLangProj.prjOutputType.prjOutputTypeExe
                Case Else
                    Debug.Fail(String.Format("Unexpected ApplicationType {0}", AppType))
                    Return VSLangProj.prjOutputType.prjOutputTypeExe
            End Select
        End Function

#End Region

#Region "Use Application Framework checkbox"

        ''' <summary>
        ''' Enables the "Enable application framework" checkbox (if Enable=True), but only if it is supported in this project with current settings
        ''' </summary>
        ''' <param name="Enable"></param>
        ''' <remarks></remarks>
        Private Sub EnableUseApplicationFrameworkCheckBox(ByVal Enable As Boolean)
            GetPropertyControlData(PROPID_UseApplicationFramework).EnableControls(Enable)
        End Sub

        Private Enum TriState
            [False]
            [Disabled]
            [True]
        End Enum

        Private Function SetUseApplicationFrameworkIntoUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                UseApplicationFrameworkCheckBox.CheckState = CheckState.Indeterminate
                EnableUseApplicationFrameworkCheckBox(False)
            Else
                Select Case CType(value, TriState)
                    Case TriState.Disabled
                        EnableUseApplicationFrameworkCheckBox(False)
                        UseApplicationFrameworkCheckBox.Checked = False
                    Case TriState.True
                        EnableUseApplicationFrameworkCheckBox(True)
                        UseApplicationFrameworkCheckBox.Checked = True
                    Case TriState.False
                        EnableUseApplicationFrameworkCheckBox(True)
                        UseApplicationFrameworkCheckBox.Checked = False
                    Case Else
                        Debug.Fail("Unexpected tristate")
                End Select
            End If

            'Toggle whether the application framework properties are enabled
            EnableControl(WindowsAppGroupBox, UseApplicationFrameworkCheckBox.Enabled AndAlso UseApplicationFrameworkCheckBox.Checked)

            Return True
        End Function

        Private Function GetUseApplicationFrameworkFromUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If Not UseApplicationFrameworkCheckBox.Enabled Then
                Debug.Fail("Get shouldn't be called if disabled")
                value = TriState.Disabled
            Else
                value = Common.IIf(UseApplicationFrameworkCheckBox.Checked, TriState.True, TriState.False)
            End If

            Return True
        End Function

        Private Sub SetUseApplicationFrameworkIntoStorage(ByVal value As TriState)
            Select Case value
                Case TriState.Disabled
                    Debug.Fail("Shouldn't get here")
                    EnableUseApplicationFrameworkCheckBox(False)
                Case TriState.False
                    'Enable using a start-up object instead of a startup URI.  

                    Dim isStartupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
                    Debug.Assert(Not isStartupObjectMissing, "Checkbox should have been disabled")

                    ' Must set the project's start-up object to "Sub Main", unless it's already set to
                    '  something non-empty.
                    If startupObject = "" Then
                        SetStartupObjectIntoStorage(STARTUPOBJECT_SubMain)
                    End If

                    'Set the Application.xaml file's build action to None
                    Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(createAppXamlIfDoesNotExist:=False)
                    If appXamlProjectItem IsNot Nothing Then
                        DTEUtils.SetBuildAction(appXamlProjectItem, VSLangProj.prjBuildAction.prjBuildActionNone)

                        'Close our cached docdata of the file
                        CleanUpApplicationXamlDocData()
                    End If
                Case TriState.True
                    'Enable using a StartupURI instead of a startup object.  Must 
                    '  create an Application.xaml file and set the project's start-up object 
                    '  to blank (if it's not already).

                    '... First create the Application.xaml if it doesn't exist.  We do this first because
                    '  we don't want to change the startup object until we know this has succeeded.
                    Using CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                        'Don't need to do anything with it, just make sure it gets created
                    End Using

                    '... Then change the project's start-up object.
                    Dim isStartupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
                    Debug.Assert(Not isStartupObjectMissing, "Checkbox should have been disabled")

                    If startupObject <> "" Then 'Don't change it if it's already blank
                        SetStartupObjectIntoStorage("")
                    End If
                Case Else
                    Debug.Fail("Unexpected tristate")
            End Select
        End Sub

        Private Function GetUseApplicationFrameworkFromStorage() As TriState
            If Not IsStartUpObjectSupportedInThisProject() Then
                Return TriState.Disabled
            End If

            'The application framework checkbox should only be enabled for WPF Application
            '  projects, not console or class library
            Dim oOutputType As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_OutputType, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) Then
                Dim outputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                If outputType <> VSLangProj.prjOutputType.prjOutputTypeWinExe Then
                    Return TriState.Disabled
                End If
            End If

            Dim isStartupObjectMissing, isSubMain As Boolean
            Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
            Debug.Assert(Not isStartupObjectMissing, "Should've been caught in IsStartupObjectSupportedInThisProject...")
            If startupObject <> "" Then
                'A start-up object (or Sub Main) is specified for this project.  This takes run-time precedence over
                '  the StartupURI.  So set Use Application Framework to false.
                Return TriState.False
            End If

            'Is there an Application.xaml file?
            If Not ApplicationXamlFileExistsInProject() Then
                'No Application.xaml file currently.  Use startup object, not URI.
                Return TriState.False
            End If

            Return TriState.True
        End Function

#End Region

#Region "Application.xaml handling"

        Private Enum __PSFFILEID3
            PSFFILEID_AppXaml = -1008
        End Enum

        ''' <summary>
        ''' Returns true iff the project contains an Application.xaml file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function ApplicationXamlFileExistsInProject() As Boolean
            Return FindApplicationXamlProjectItem(False) IsNot Nothing
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file in the application, if one exists.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Overridable for unit testing.
        ''' </remarks>
        Private Function FindApplicationXamlProjectItem(ByVal createAppXamlIfDoesNotExist As Boolean) As ProjectItem
            Return FindApplicationXamlProjectItem(ProjectHierarchy, createAppXamlIfDoesNotExist)
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file in the application, if one exists.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Overridable for unit testing.
        ''' </remarks>
        Friend Shared Function FindApplicationXamlProjectItem(ByVal hierarchy As IVsHierarchy, ByVal createAppXamlIfDoesNotExist As Boolean) As ProjectItem
            Try
                Dim specialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
                If specialFiles Is Nothing Then
                    Return Nothing
                End If

                Dim flags As UInteger = 0
                Dim bstrFilename As String = Nothing
                Dim itemid As UInteger
                ErrorHandler.ThrowOnFailure(specialFiles.GetFile(__PSFFILEID3.PSFFILEID_AppXaml, flags, itemid, bstrFilename))
                If itemid <> VSITEMID.NIL AndAlso bstrFilename <> "" Then
                    'Get the ProjectItem for it
                    Dim extObject As Object = Nothing
                    ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(itemid, __VSHPROPID.VSHPROPID_ExtObject, extObject))
                    Return CType(extObject, ProjectItem)
                End If

                If createAppXamlIfDoesNotExist Then
                    'There is no current application definition file, and the caller requested us to create it.
                    '  First we need to see if there is an existing Application.xaml file that has its build action
                    '  set to none.  If so, we'll just flip its build action to ApplicationDefinition and try again.
                    Const ApplicationDefinitionExpectedName As String = "Application.xaml"
                    Dim Project As Project = DTEUtils.EnvDTEProject(hierarchy)
                    Dim foundAppDefinition As ProjectItem = DTEUtils.QueryProjectItems(Project.ProjectItems, ApplicationDefinitionExpectedName)
                    If foundAppDefinition IsNot Nothing Then
                        'We only do this if the build action is actually set to None.  We'll assume if it was set to
                        '  anything else that the user intended it that way.
                        If DTEUtils.GetBuildAction(foundAppDefinition) = VSLangProj.prjBuildAction.prjBuildActionNone Then
                            DTEUtils.SetBuildActionAsString(foundAppDefinition, BUILDACTION_APPLICATIONDEFINITION)
                        End If
                    End If

                    'Ask the project system to create the application definition file for us
                    flags = flags Or CUInt(__PSFFLAGS.PSFF_CreateIfNotExist)
                    ErrorHandler.ThrowOnFailure(specialFiles.GetFile(__PSFFILEID3.PSFFILEID_AppXaml, flags, itemid, bstrFilename))
                    If itemid <> VSITEMID.NIL AndAlso bstrFilename <> "" Then
                        'Get the ProjectItem for it
                        Dim extObject As Object = Nothing
                        ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(itemid, __VSHPROPID.VSHPROPID_ExtObject, extObject))
                        Return CType(extObject, ProjectItem)
                    End If

                    'The file should have been created, or it should have failed.  Throw an unexpected
                    '  error, because our contract says we have to succeed or throw if
                    '  createAppXamlIfDoesNotExist is specified.
                    Throw New PropertyPageException(My.Resources.Designer.PPG_Unexpected)
                End If

                Return Nothing
            Catch ex As Exception
                Throw New PropertyPageException( _
                    String.Format(My.Resources.Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg, ex.Message), _
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml, _
                    ex)
            End Try
        End Function

        ''' <summary>
        ''' Lazily creates and returns a DocData representing the application definition file for this
        '''   project (Application.xaml).
        ''' </summary>
        ''' <param name="createAppXamlIfDoesNotExist">If True, will attempt to create the file if it does not exist.  In this case, 
        ''' the function will never return Nothing, but rather will throw an exception if there's a problem.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetApplicationXamlDocData(ByVal createAppXamlIfDoesNotExist As Boolean) As DocData
            If m_ApplicationXamlDocData Is Nothing Then
                Dim applicationXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(createAppXamlIfDoesNotExist)
                If applicationXamlProjectItem IsNot Nothing Then
                    m_ApplicationXamlDocData = New DocData(ServiceProvider, applicationXamlProjectItem.FileNames(1))
                End If
            End If

            If m_ApplicationXamlDocData IsNot Nothing Then
                Return m_ApplicationXamlDocData
            ElseIf createAppXamlIfDoesNotExist Then
                Debug.Fail("This function should not have reached here if createAppDotXamlFileIfNotExist was passed in as True.  It should have thrown an exception by now.")
                Throw New PropertyPageException( _
                    String.Format(My.Resources.Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg, _
                        My.Resources.Designer.PPG_Unexpected), _
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file, if any, in the project, and returns a
        '''   WFPAppDotXamlDocument to read/write to it.
        ''' If there is no Application.xaml, and createAppDotXamlFileIfNotExist=True, 
        '''   an Application.xaml file is created.  If createAppDotXamlFileIfNotExist is specified,
        '''   this function will either succeed or throw an exception, but will not return Nothing.
        ''' </summary>
        ''' <param name="createAppXamlIfDoesNotExist"></param>
        ''' <returns>The AppDotXamlDocument</returns>
        ''' <remarks></remarks>
        Protected Overridable Function CreateAppDotXamlDocumentForApplicationDefinitionFile(ByVal createAppXamlIfDoesNotExist As Boolean) As AppDotXamlDocument
            Dim docData As DocData = GetApplicationXamlDocData(createAppXamlIfDoesNotExist)
            If docData IsNot Nothing Then
                Dim vsTextLines As IVsTextLines = TryCast(docData.Buffer, IVsTextLines)
                If vsTextLines Is Nothing Then
                    Throw New PropertyPageException( _
                        My.Resources.Designer.PPG_WPFApp_AppXamlOpenInUnsupportedEditor, _
                        HelpKeywords.VBProjPropWPFApp_AppXamlOpenInUnsupportedEditor)
                End If
                Dim document As New AppDotXamlDocument(vsTextLines)
                Return document
            End If

            If createAppXamlIfDoesNotExist Then
                Debug.Fail("This function should not have reached here if createAppDotXamlFileIfNotExist was passed in as True.  It should have thrown an exception by now.")
                Throw New PropertyPageException( _
                    String.Format(My.Resources.Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg, _
                        My.Resources.Designer.PPG_Unexpected), _
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml)
            Else
                Return Nothing
            End If
        End Function

        Private Function GetStartupUriFromStorage() As String
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                If document Is Nothing Then
                    Return Nothing
                Else
                    Return document.GetStartupUri()
                End If
            End Using
        End Function

        Private Sub SetStartupUriIntoStorage(ByVal value As String)
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                Debug.Assert(document IsNot Nothing, "This shouldn't ever be returned as Nothing from GetAppDotXamlDocument(True)")
                document.SetStartupUri(value)
            End Using
        End Sub

#End Region

#Region "StartupObject/StartupUri combobox"

#Region "Nested class hierarchy StartupObjectOrUri, StartupObject, StartupUri"

        ''' <summary>
        ''' Represents an entry in the Startup Object/Startup URI combobox
        '''   (depending on the setting of the Enable Application Framework
        '''   checkbox)
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Friend MustInherit Class StartupObjectOrUri
            Private m_value As String
            Private m_description As String

            Public Sub New(ByVal value As String, ByVal description As String)
                If value Is Nothing Then
                    value = ""
                End If
                If description Is Nothing Then
                    description = ""
                End If

                m_value = value
                m_description = description
            End Sub

            ''' <summary>
            ''' The value displayed to the user in the combobox
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function ToString() As String
                Return Description
            End Function

            Public ReadOnly Property Value() As String
                Get
                    Return m_value
                End Get
            End Property

            Public ReadOnly Property Description() As String
                Get
                    Return m_description
                End Get
            End Property

            Public Overrides Function Equals(ByVal obj As Object) As Boolean
                If TypeOf obj Is StartupObjectOrUri Then
                    If obj.GetType() IsNot Me.GetType() Then
                        Return False
                    Else
                        Return Me.Value.Equals(CType(obj, StartupObjectOrUri).Value, StringComparison.OrdinalIgnoreCase)
                    End If
                End If

                Return False
            End Function

            Public Overrides Function GetHashCode() As Integer
                Throw New NotImplementedException()
            End Function

        End Class

        <Serializable()> _
        Friend Class StartupObject
            Inherits StartupObjectOrUri

            Public Sub New(ByVal value As String, ByVal description As String)
                MyBase.New(value, description)
            End Sub

            Protected Overridable ReadOnly Property IsEquivalentToSubMain() As Boolean
                Get
                    Return Value = "" OrElse Value.Equals(STARTUPOBJECT_SubMain, StringComparison.OrdinalIgnoreCase)
                End Get
            End Property

            Public Overrides Function Equals(ByVal obj As Object) As Boolean
                If TypeOf obj Is StartupObject Then
                    If Me.GetType() IsNot obj.GetType() Then
                        Return False
                    ElseIf Me.IsEquivalentToSubMain AndAlso CType(obj, StartupObject).IsEquivalentToSubMain Then
                        Return True
                    Else
                        Return Me.Value.Equals(CType(obj, StartupObject).Value, StringComparison.OrdinalIgnoreCase)
                    End If
                End If

                Return False
            End Function

            Public Overrides Function GetHashCode() As Integer
                Throw New NotImplementedException()
            End Function

        End Class

        <Serializable()> _
        Friend Class StartupObjectNone
            Inherits StartupObject

            Public Sub New()
                MyBase.New("", m_NoneText)
            End Sub

            Protected Overrides ReadOnly Property IsEquivalentToSubMain() As Boolean
                Get
                    Return False
                End Get
            End Property

        End Class

        <Serializable()> _
        Friend Class StartupUri
            Inherits StartupObjectOrUri

            Public Sub New(ByVal value As String)
                MyBase.New(value, value)
            End Sub

        End Class

#End Region

        ''' <summary>
        ''' Happens when the start-up object combobox box is opened.  Use this to populate it with the 
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub StartupObjectOrUriComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles StartupObjectOrUriComboBox.DropDown
            PopulateStartupObjectOrUriComboboxAndKeepCurrentEntry()
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Populates the startup object/URI combobox with the available choices, depending on whether
        '''   it should be showing startup URI or startup object.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PopulateStartupObjectOrUriComboboxAndKeepCurrentEntry()
            Dim populateWithStartupUriInsteadOfStartupObject As Boolean = ShouldStartupUriBeDisplayedInsteadOfStartupObject()
#If DEBUG Then
            If populateWithStartupUriInsteadOfStartupObject Then
                Debug.Assert(StartupObjectOrUriComboBox.SelectedItem Is Nothing OrElse TypeOf StartupObjectOrUriComboBox.SelectedItem Is StartupUri, _
                    "Current entry in the startup object/URI combobox is out of sync with the current state - it was expected to be a startup URI")
            Else
                Debug.Assert(StartupObjectOrUriComboBox.SelectedItem Is Nothing OrElse TypeOf StartupObjectOrUriComboBox.SelectedItem Is StartupObject, _
                    "Current entry in the startup object/URI combobox is out of sync with the current state - it was expected to be a startup Object")
            End If
#End If

            'Remember the current selected item
            Dim currentSelectedItem As StartupObjectOrUri = CType(StartupObjectOrUriComboBox.SelectedItem, StartupObjectOrUri)

            'Populate the dropdowns
            If populateWithStartupUriInsteadOfStartupObject Then
                PopulateStartupUriDropdownValues(StartupObjectOrUriComboBox)
            Else
                PopulateStartupObjectDropdownValues(StartupObjectOrUriComboBox)
            End If

            'Reselect the current selected item
            SetSelectedStartupObjectOrUriIntoCombobox(StartupObjectOrUriComboBox, currentSelectedItem)
        End Sub

        Private Function GetStartupObjectPropertyControlData() As PropertyControlData
            Return GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)
        End Function

        Private Function IsStartupObjectMissing() As Boolean
            Return GetStartupObjectPropertyControlData().IsMissing
        End Function

        Private Function GetCurrentStartupObjectFromStorage(ByRef isMissing As Boolean, ByRef isSubMain As Boolean) As String
            isMissing = False
            Dim oStartupObject As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_StartupObject, oStartupObject) AndAlso oStartupObject IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oStartupObject) Then
                Dim startupObject As String = TryCast(oStartupObject, String)
                If startupObject = "" OrElse startupObject.Equals(STARTUPOBJECT_SubMain, StringComparison.OrdinalIgnoreCase) Then
                    isSubMain = True
                End If

                Return startupObject
            End If

            isMissing = True
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported for the current settings
        ''' </summary>
        Private Function IsStartUpObjectSupportedInThisProject() As Boolean
            If IsStartupObjectMissing() Then
                Return False
            End If

            Dim oOutputType As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_OutputType, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) Then
                Dim outputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                If outputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                    'Not supported for class libraries
                    Return False
                End If
            Else
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Retrieve the current value of the startup object/URI value from the combobox on the page
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetStartupObjectOrUriFromUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = CType(StartupObjectOrUriComboBox.SelectedItem, StartupObjectOrUri)
            Debug.Assert(value IsNot Nothing, "GetStartupObjectOrUriFromUI(): Shouldn't get null value")
            Return True
        End Function

        Private Shared Sub SetSelectedStartupObjectOrUriIntoCombobox(ByVal combobox As ComboBox, ByVal startupObjectOrUri As StartupObjectOrUri)
            'Find the value in the combobox
            Dim foundStartupObjectOrUri As StartupObjectOrUri = Nothing
            If startupObjectOrUri IsNot Nothing Then
                For Each entry As StartupObjectOrUri In combobox.Items
                    If entry.Equals(startupObjectOrUri) Then
                        combobox.SelectedItem = entry
                        foundStartupObjectOrUri = entry
                        Exit For
                    End If
                Next
            End If

            If foundStartupObjectOrUri Is Nothing AndAlso startupObjectOrUri IsNot Nothing Then
                'The value wasn't found in the combobox.  Add it now.
                combobox.Items.Add(startupObjectOrUri)
                combobox.SelectedItem = startupObjectOrUri
            End If
        End Sub

        ''' <summary>
        ''' Setter - Place the startup object/URI value into the combobox on the page
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SetStartupObjectOrUriIntoUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                StartupObjectOrUriComboBox.SelectedIndex = -1
                EnableControl(StartupObjectOrUriComboBox, False)
                Return True
            End If

            If GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject).IsReadOnly Then
                EnableControl(control, False)
            End If

            Debug.Assert(TypeOf value Is StartupObjectOrUri)
            Dim valueAsStartupObjectOrUri As StartupObjectOrUri = CType(value, StartupObjectOrUri)
            SetSelectedStartupObjectOrUriIntoCombobox(StartupObjectOrUriComboBox, valueAsStartupObjectOrUri)

            If value Is Nothing Then
                Debug.Fail("Unexpected null value in SetStartupObjectOrUriIntoUI")
            ElseIf TypeOf value Is StartupObject Then
                StartupObjectOrUriLabel.Text = m_StartupObjectLabelText
            ElseIf TypeOf value Is StartupUri Then
                StartupObjectOrUriLabel.Text = m_StartupUriLabelText
            Else
                Debug.Fail("Unexpected startup/uri type")
            End If
            Return True
        End Function

        ''' <summary>
        ''' True if, according to the current, persisted state, the StartupObject/URI label should be
        '''   "Startup URI".  If it should be "Startup Object", it returns False.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ShouldStartupUriBeDisplayedInsteadOfStartupObject() As Boolean
            Dim tristateUseApplicationFramework As TriState = GetUseApplicationFrameworkFromStorage()
            If tristateUseApplicationFramework = TriState.True Then
                Return True 'Show Startup URI
            Else
                'Show Startup Object
                Return False
            End If
        End Function

        ''' <summary>
        ''' Retrieves the value of the Startup Object or Startup Uri from
        '''   its persisted storage (project file or Application.xaml)
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetStartupObjectOrUriValueFromStorage() As StartupObjectOrUri
            If ShouldStartupUriBeDisplayedInsteadOfStartupObject() Then
                Return New StartupUri(GetStartupUriFromStorage())
            Else
                If IsStartUpObjectSupportedInThisProject() Then
                    Dim startupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(startupObjectMissing, isSubMain)
                    Debug.Assert(Not startupObjectMissing, "IsStartUpObjectSupportedInThisProject should have failed")
                    If isSubMain Then
                        Return New StartupObject(startupObject, STARTUPOBJECT_SubMain)
                    Else
                        Debug.Assert(startupObject <> "", "but isSubMain was supposed to be false")
                        Dim fullyQualifiedStartupObject As String = startupObject
                        Dim relativeStartupObject As String = RemoveCurrentRootNamespace(fullyQualifiedStartupObject)
                        Return New StartupObject(fullyQualifiedStartupObject, relativeStartupObject)
                    End If
                Else
                    Return New StartupObjectNone
                End If
            End If
        End Function

        ''' <summary>
        ''' Stores the value of the Startup Object or Startup Uri into
        '''   its persisted storage (project file or Application.xaml)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetStartupObjectOrUriValueIntoStorage(ByVal value As StartupObjectOrUri)
            If TypeOf value Is StartupObject Then
                SetStartupObjectIntoStorage(value.Value)
            ElseIf TypeOf value Is StartupUri Then
                SetStartupUriIntoStorage(value.Value)
            Else
                Debug.Fail("Unexpected startupobject/uri type")
            End If
        End Sub

        Private Sub SetStartupObjectIntoStorage(ByVal value As String)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject).SetPropertyValue(value)
        End Sub

        Private Sub PopulateStartupObjectDropdownValues(ByVal startupObjectComboBox As ComboBox)
            startupObjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            startupObjectComboBox.Items.Clear()

            If Not IsStartUpObjectSupportedInThisProject() Then
                startupObjectComboBox.Items.Add(New StartupObjectNone())
                startupObjectComboBox.SelectedIndex = 0
            Else
                startupObjectComboBox.Items.AddRange(GetAvailableStartupObjects().ToArray())
            End If
        End Sub

        Private Function GetAvailableStartupObjects() As List(Of StartupObject)
            Dim startupObjects As New List(Of StartupObject)

            Dim startupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)
            Dim startupObjectPropertyDescriptor As PropertyDescriptor = startupObjectPropertyControlData.PropDesc

            If Not startupObjectPropertyControlData.IsMissing Then
                Using New WaitCursor
                    Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                    Dim rawStartupObjects As ICollection = Nothing

                    'Force us to see any new start-up objects in the project
                    RefreshPropertyStandardValues()

                    'Certain project types may not support standard values
                    If startupObjectPropertyDescriptor.Converter.GetStandardValuesSupported() Then
                        rawStartupObjects = startupObjectPropertyDescriptor.Converter.GetStandardValues()
                    End If

                    If rawStartupObjects IsNot Nothing Then
                        For Each o As Object In rawStartupObjects
                            Dim fullyQualifiedStartupObject As String = TryCast(o, String)
                            Dim relativeStartupObject As String = RemoveCurrentRootNamespace(fullyQualifiedStartupObject)
                            startupObjects.Add(New StartupObject(fullyQualifiedStartupObject, relativeStartupObject))
                        Next
                    End If
                End Using
            End If

            Return startupObjects
        End Function

        Private Sub PopulateStartupUriDropdownValues(ByVal startupObjectComboBox As ComboBox)
            startupObjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            startupObjectComboBox.Items.Clear()

            If Not IsStartUpObjectSupportedInThisProject() Then
                Debug.Fail("Shouldn't reach here, because we should be showing a Startup Object instead of a Startup URI if StartupObject is not supported")
                startupObjectComboBox.Items.Add(New StartupObjectNone())
                startupObjectComboBox.SelectedIndex = 0
            Else
                startupObjectComboBox.Items.AddRange(GetAvailableStartupUris().ToArray())
            End If
        End Sub

        ''' <summary>
        ''' Returns true if the given file path is relative to the project directory
        ''' </summary>
        ''' <param name="fullPath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsFileRelativeToProjectPath(ByVal fullPath As String) As Boolean
            Dim relativePath As String = GetProjectRelativeFilePath(fullPath)
            Return Not System.IO.Path.IsPathRooted(relativePath)
        End Function

        ''' <summary>
        ''' Finds all .xaml files in the project which can be used as the start-up URI.
        ''' </summary>
        ''' <param name="projectItems"></param>
        ''' <param name="list"></param>
        ''' <remarks></remarks>
        Private Sub FindXamlPageFiles(ByVal projectItems As ProjectItems, ByVal list As List(Of ProjectItem))
            For Each projectitem As ProjectItem In projectItems
                If IO.Path.GetExtension(projectitem.FileNames(1)).Equals(".xaml", StringComparison.OrdinalIgnoreCase) Then
                    'We only want .xaml files with BuildAction="Page"
                    Dim CurrentBuildAction As String = DTEUtils.GetBuildActionAsString(projectitem)
                    If CurrentBuildAction IsNot Nothing AndAlso BUILDACTION_PAGE.Equals(CurrentBuildAction, StringComparison.OrdinalIgnoreCase) Then
                        'Build action is correct.

                        'Is the item inside the project folders (instead of, say, a link to an external file)?
                        If IsFileRelativeToProjectPath(projectitem.FileNames(1)) Then
                            'Okay, we want this one
                            list.Add(projectitem)
                        End If
                    End If
                End If

                If projectitem.ProjectItems IsNot Nothing Then
                    FindXamlPageFiles(projectitem.ProjectItems, list)
                End If
            Next
        End Sub


        ''' <summary>
        ''' Gets all the files (as a list of StartupUri objects) in the project which are appropriate for the 
        '''   StartupUri property.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Note: it's currently returning a List of Object only because I'm having trouble getting the VSTS
        '''   code accessors to work properly with List(Of StartupUri).
        ''' </remarks>
        Private Function GetAvailableStartupUris() As List(Of Object)
            Dim startupObjects As New List(Of Object)

            Dim startupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)
            Dim startupObjectPropertyDescriptor As PropertyDescriptor = startupObjectPropertyControlData.PropDesc

            If Not startupObjectPropertyControlData.IsMissing Then
                Using New WaitCursor
                    Switches.TracePDPerf("*** Populating start-up URI list from the project [may be slow for a large project]")
                    Dim xamlFiles As New List(Of ProjectItem)
                    FindXamlPageFiles(DTEProject.ProjectItems, xamlFiles)

                    For Each projectItem As ProjectItem In xamlFiles
                        startupObjects.Add(New StartupUri(GetProjectRelativeFilePath(projectItem.FileNames(1))))
                    Next
                End Using
            End If

            Return startupObjects
        End Function

#End Region

#Region "ShutdownMode"

#Region "Nested class ShutdownMode"

        ''' <summary>
        ''' Nested class that represents a shutdown mode value, and can be placed
        '''   directly into a combobox as an entry.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class ShutdownMode
            Private m_Value As String
            Private m_Description As String

            Public Sub New(ByVal value As String, ByVal description As String)
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If
                If description Is Nothing Then
                    Throw New ArgumentNullException("description")
                End If

                Me.m_Value = value
                Me.m_Description = description
            End Sub

            Public ReadOnly Property Value() As String
                Get
                    Return m_Value
                End Get
            End Property

            Public ReadOnly Property Description() As String
                Get
                    Return m_Description
                End Get
            End Property

            Public Overrides Function ToString() As String
                Return m_Description
            End Function

        End Class

#End Region

        Private Shared s_shutdownModes As New List(Of ShutdownMode)
        Private Shared s_defaultShutdownMode As ShutdownMode

        Private Shared Sub InitializeShutdownModeValues()
            'This order affects the order in the combobox
            s_defaultShutdownMode = New ShutdownMode("OnLastWindowClose", SR.GetString(SR.PPG_WPFApp_ShutdownMode_OnLastWindowClose))
            s_shutdownModes.Add(s_defaultShutdownMode)
            s_shutdownModes.Add(New ShutdownMode("OnMainWindowClose", SR.GetString(SR.PPG_WPFApp_ShutdownMode_OnMainWindowClose)))
            s_shutdownModes.Add(New ShutdownMode("OnExplicitShutdown", SR.GetString(SR.PPG_WPFApp_ShutdownMode_OnExplicitShutdown)))
        End Sub

        Public Function GetShutdownModeFromStorage() As String
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                If document Is Nothing Then
                    Return Nothing
                Else
                    Return document.GetShutdownMode()
                End If
            End Using
        End Function

        Public Sub SetShutdownModeIntoStorage(ByVal value As String)
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                document.SetShutdownMode(value)
            End Using
        End Sub

        ''' <summary>
        ''' Getter for the "ShutdownMode" property.  Retrieves the current value
        '''   of the property from the combobox.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetShutdownModeFromUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim currentShutdownMode As ShutdownMode = CType(ShutdownModeComboBox.SelectedItem, ShutdownMode)
            If currentShutdownMode Is Nothing Then
                value = ""
            Else
                value = currentShutdownMode.Value
            End If

            Return True
        End Function

        ''' <summary>
        ''' Getter for the "ShutdownMode" property.  Takes the given value for the
        '''   property, and converts it into the display value, then puts it into the
        '''   combobox.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function SetShutdownModeIntoUI(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                ShutdownModeComboBox.SelectedIndex = -1
            Else
                Dim shutdownModeStringValue As String = CType(value, String)

                'Display empty string as the default value used by the runtime
                If shutdownModeStringValue = "" Then
                    shutdownModeStringValue = s_defaultShutdownMode.Value
                End If

                'Find the value in the combobox
                Dim foundShutdownMode As ShutdownMode = Nothing
                For Each entry As ShutdownMode In ShutdownModeComboBox.Items
                    If entry.Value.Equals(shutdownModeStringValue, StringComparison.OrdinalIgnoreCase) Then
                        foundShutdownMode = entry
                        ShutdownModeComboBox.SelectedItem = entry
                        Exit For
                    End If
                Next

                If foundShutdownMode Is Nothing Then
                    'The value wasn't found in the combobox.  Add it, but show it as an unsupported value.
                    foundShutdownMode = New ShutdownMode(shutdownModeStringValue, String.Format(My.Resources.Designer.PPG_WPFApp_InvalidShutdownMode, shutdownModeStringValue))
                    ShutdownModeComboBox.Items.Add(foundShutdownMode)
                    ShutdownModeComboBox.SelectedItem = foundShutdownMode
                End If

            End If

            Return True
        End Function

#End Region

#Region "User-defined property persistence"

        ''' <summary>
        ''' Override this method to return a property descriptor for user-defined properties in a page.
        ''' </summary>
        ''' <param name="PropertyName">The property to return a property descriptor for.</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This method must be overridden to handle all user-defined properties defined in a page.  The easiest way to implement
        '''   this is to return a new instance of the UserPropertyDescriptor class, which was created for that purpose.
        ''' </remarks>
        Public Overrides Function GetUserDefinedPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    Return New UserPropertyDescriptor(PropertyName, GetType(StartupObjectOrUri))

                Case PROPNAME_ShutDownMode
                    Return New UserPropertyDescriptor(PropertyName, GetType(String))

                Case PROPNAME_UseApplicationFramework
                    'Note: Need to specify Int32 instead of TriState enum because undo/redo code doesn't
                    '  handle the enum properly.
                    Return New UserPropertyDescriptor(PropertyName, GetType(Integer))

                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Takes a value from the property store, and converts it into the UI-displayable form
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function ReadUserDefinedProperty(ByVal PropertyName As String, ByRef Value As Object) As Boolean
            '
            'NOTE: We do not want to throw any exceptions from this method for our properties, because if this happens 
            '  during initialization, it will cause the property's controls to get disabled, and simply
            '  doing a refresh will not re-enable them.  Instead, we show an error value inside the control to the user.

            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    Try
                        If IsStartupObjectMissing() Then
                            Value = PropertyControlData.MissingProperty
                        Else
                            Value = GetStartupObjectOrUriValueFromStorage()
                        End If
                    Catch ex As Exception
                        If ShouldStartupUriBeDisplayedInsteadOfStartupObject() Then
                            Value = New StartupUri("")
                        Else
                            Value = New StartupObject("", "")
                        End If
                    End Try

                Case PROPNAME_ShutDownMode
                    Try
                        Value = GetShutdownModeFromStorage()
                    Catch ex As Exception
                        Value = ""
                    End Try

                Case PROPNAME_UseApplicationFramework
                    Try
                        Value = GetUseApplicationFrameworkFromStorage()
                    Catch ex As Exception
                        Value = TriState.Disabled
                    End Try

                Case Else
                    Return False
            End Select

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
            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    SetStartupObjectOrUriValueIntoStorage(CType(Value, StartupObjectOrUri))

                Case PROPNAME_ShutDownMode
                    SetShutdownModeIntoStorage(CType(Value, String))

                Case PROPNAME_UseApplicationFramework
                    SetUseApplicationFrameworkIntoStorage(CType(Value, TriState))

                Case Else
                    Debug.Fail("Unexpected property name")
                    Return False
            End Select

            Return True
        End Function



#End Region

#Region "Edit XAML button"

        ''' <summary>
        ''' The user has clicked the "Edit XAML" button.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub EditXamlButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles EditXamlButton.Click
            TryShowXamlEditor(True)
        End Sub

        ''' <summary>
        ''' Attempts to show the editor for the Application.xaml file.  Shows an error message if it
        '''   fails.
        ''' </summary>
        ''' <param name="createAppDotXamlIfItDoesntExist"></param>
        ''' <remarks></remarks>
        Friend Sub TryShowXamlEditor(ByVal createAppDotXamlIfItDoesntExist As Boolean)
            EnterProjectCheckoutSection()
            Try
                Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(ProjectHierarchy, createAppDotXamlIfItDoesntExist)
                If appXamlProjectItem Is Nothing Then
                    ShowErrorMessage(My.Resources.Designer.PPG_WPFApp_CantFindAppXaml)
                    Return
                End If

                appXamlProjectItem.Open(LogicalViewID.TextView)
                If appXamlProjectItem.Document IsNot Nothing Then
                    appXamlProjectItem.Document.Activate()
                End If
            Catch ex As Exception
                ShowErrorMessage(ex)
            Finally
                LeaveProjectCheckoutSection()
            End Try
        End Sub

#End Region

#Region "View Application Events button"

        Private Sub ViewCodeButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ViewCodeButton.Click
            TryShowApplicationEventsCode()
        End Sub

        ''' <summary>
        ''' Given a project item, finds the first dependent project item with the given extension
        ''' </summary>
        ''' <param name="projectItem"></param>
        ''' <param name="extension"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindDependentFile(ByVal projectItem As ProjectItem, ByVal extension As String) As ProjectItem
            For Each dependentItem As ProjectItem In projectItem.ProjectItems
                If dependentItem.FileNames(1) IsNot Nothing _
                        AndAlso IO.Path.GetExtension(dependentItem.Name).Equals(extension, StringComparison.OrdinalIgnoreCase) Then
                    Return dependentItem
                End If
            Next

            Return Nothing
        End Function

        Private Function GetExpectedApplicationEventsFileName(ByVal appDotXamlFilename As String) As String
            Return appDotXamlFilename & VB_EXTENSION
        End Function

        Private Function CreateApplicationEventsFile(ByVal parent As ProjectItem) As ProjectItem
            'First, determine the new name by appending ".vb"
            Dim newFileName As String = GetExpectedApplicationEventsFileName(parent.Name)

            'Find the path to the template
            Dim templateFileName As String = CType(DTE.Solution, EnvDTE80.Solution2).GetProjectItemTemplate( _
                "InternalWPFApplicationDefinitionUserCode.zip", "VisualBasic")

            'Add it as a dependent file
            parent.ProjectItems.AddFromTemplate(templateFileName, newFileName)

            'Now find the item that was added (for some reason, AddFromTemplate won't return this
            '  to us).
            Dim newProjectItem As ProjectItem = FindDependentFile(parent, VB_EXTENSION)
            If newProjectItem Is Nothing Then
                Throw New PropertyPageException(My.Resources.Designer.PPG_Unexpected)
            End If

            Return newProjectItem
        End Function

        ''' <summary>
        ''' Open the XAML editor on the application.xaml file.  If it doesn't exist, create one.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub TryShowApplicationEventsCode()
            EnterProjectCheckoutSection()
            Try
                'This will throw if it fails, won't return Nothing
                Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(True)

                'Look for a dependent .vb file, this should be the normal case
                Dim dependentVBItem As ProjectItem = FindDependentFile(appXamlProjectItem, VB_EXTENSION)

                If dependentVBItem Is Nothing Then
                    'If none, then also look for a file with the same name as the Application.xaml file (+ .vb) in either the
                    '  root folder or the same folder as the Application.xaml.

                    '... First, check same folder
                    Dim expectedFileName As String = GetExpectedApplicationEventsFileName(appXamlProjectItem.Name)
                    Try
                        'Will throw if not found
                        dependentVBItem = appXamlProjectItem.Collection.Item(expectedFileName)
                    Catch ex As Exception
                    End Try

                    '... Next, check root
                    If dependentVBItem Is Nothing Then
                        Try
                            'Will throw if not found
                            dependentVBItem = appXamlProjectItem.ContainingProject.ProjectItems.Item(expectedFileName)
                        Catch ex As Exception
                        End Try
                    End If
                End If

                If dependentVBItem Is Nothing Then
                    'Still not found - try to create it.
                    Try
                        dependentVBItem = CreateApplicationEventsFile(appXamlProjectItem)
                    Catch ex As Exception
                        Throw New PropertyPageException( _
                            String.Format(My.Resources.Designer.PPG_WPFApp_CouldntCreateApplicationEventsFile_1Arg, ex.Message), _
                            HelpKeywords.VBProjPropWPFApp_CouldntCreateApplicationEventsFile, _
                            ex)
                    End Try
                End If

                dependentVBItem.Open(LogicalViewID.TextView)
                If dependentVBItem.Document IsNot Nothing Then
                    dependentVBItem.Document.Activate()
                End If
            Catch ex As Exception
                ShowErrorMessage(ex)
            Finally
                LeaveProjectCheckoutSection()
            End Try
        End Sub

#End Region

#Region "Error control"

        'If this is non-null, then the error control is visible
        Private WithEvents m_pageErrorControl As AppDotXamlErrorControl = Nothing

        Private Sub DisplayErrorControl(ByVal message As String)
            RemoveErrorControl()

            Me.SuspendLayout()
            Me.overarchingTableLayoutPanel.Visible = False
            m_pageErrorControl = New AppDotXamlErrorControl(message)
            m_pageErrorControl.Dock = DockStyle.Fill
            Me.Controls.Add(m_pageErrorControl)
            m_pageErrorControl.BringToFront()
            m_pageErrorControl.Visible = True
            Me.ResumeLayout()
            Me.PerformLayout()
        End Sub

        Private Sub RemoveErrorControl()
            If m_pageErrorControl IsNot Nothing Then
                Me.Controls.Remove(m_pageErrorControl)
                m_pageErrorControl.Dispose()
                m_pageErrorControl = Nothing
            End If

            Me.overarchingTableLayoutPanel.Visible = True
        End Sub

        Private Sub PageErrorControl_EditXamlClick() Handles m_pageErrorControl.EditXamlClicked
            TryShowXamlEditor(False)
        End Sub

        Private Function TryGetAppDotXamlFilename() As String
            Try
                Dim appXaml As ProjectItem = FindApplicationXamlProjectItem(False)
                If appXaml IsNot Nothing Then
                    Return appXaml.FileNames(1)
                End If
            Catch ex As Exception
            End Try

            Return ""
        End Function

        Private Sub DisplayErrorControlIfAppXamlIsInvalid()
            Dim document As AppDotXamlDocument = Nothing
            Try
                Try
                    document = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                Catch ex As Exception
                    'Errors here would involve problems creating the file, or perhaps it's loaded already in an incompatible
                    '  editor, etc.
                    DisplayErrorControl(ex.Message)
                    Return
                End Try

                Try
                    If document IsNot Nothing Then
                        document.VerifyAppXamlIsValidAndThrowIfNot()
                    End If
                Catch ex As Exception
                    'Problems here should be parsing errors.
                    Dim message As String = _
                        String.Format(My.Resources.Designer.PPG_WPFApp_ErrorControlMessage_1Arg, TryGetAppDotXamlFilename()) _
                        & vbCrLf & vbCrLf _
                        & ex.Message
                    DisplayErrorControl(message)
                End Try
            Finally
                If document IsNot Nothing Then
                    document.Dispose()
                End If
            End Try
        End Sub

#End Region

#Region "DocData changes"

        Private m_docDataHasChanged As Boolean

        Private Sub ApplicationXamlDocData_DataChanged(ByVal sender As Object, ByVal e As EventArgs) Handles m_ApplicationXamlDocData.DataChanged
            m_docDataHasChanged = True
        End Sub

        Private Sub RetryPageLoad()
            If m_docDataHasChanged Then
                Try
                    m_docDataHasChanged = False
                    RemoveErrorControl()
                    RefreshPropertyValues()
                    DisplayErrorControlIfAppXamlIsInvalid()
                Catch ex As Exception
                    Debug.Fail("Unexpected exception in RetryPageLoad(): " & ex.ToString())
                End Try
            End If
        End Sub

        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            MyBase.WndProc(m)

            If m.Msg = Interop.win.WM_SETFOCUS Then
                If m_docDataHasChanged Then
                    Me.BeginInvoke(New MethodInvoker(AddressOf RetryPageLoad))
                End If
            End If
        End Sub

#End Region

#Region "XBAP projects"

        Private Function IsXBAP() As Boolean
            Dim pcd As PropertyControlData = GetPropertyControlData(PROPID_HostInBrowser)
            If pcd.IsSpecialValue Then
                'HostInBrowser property not available.  This shouldn't happen except in
                '  unit tests.
                Return False
            End If

            Return CBool(pcd.InitialValue)
        End Function

        ''' <summary>
        ''' If this is an XBAP project, some properties need to be disabled that currently can't
        '''   be disabled by the flavor mechanism (due to architectural limitations for user-defined
        '''   properties - we should change this in the future).
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisableControlsForXBAPProjects()
            'Note: Once a project is an XBAP, it's always an XBAP (can't change it except
            '  by editing the project file)
            If IsXBAP() Then
                EnableControl(Me.ShutdownModeComboBox, False)
                EnableIconComboBox(False)
                EnableControl(Me.ApplicationTypeComboBox, False)
            End If
        End Sub

#End Region

#Region "Set the drop-down width of comboboxes with user-handled events so they'll fit their contents"

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComboBoxes_DropDown(ByVal sender As Object, ByVal e As EventArgs) Handles IconCombobox.DropDown
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

#End Region

#Region "View UAC Settings button"

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

        '***************************************************************************

#Region " Windows Form Designer generated code "


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
        Friend WithEvents StartupObjectOrUriLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectOrUriComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents UseApplicationFrameworkCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents IconLabel As System.Windows.Forms.Label
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents IconCombobox As System.Windows.Forms.ComboBox
        Friend WithEvents IconPicturebox As System.Windows.Forms.PictureBox
        Friend WithEvents WindowsAppGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents BottomHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents EditXamlButton As System.Windows.Forms.Button
        Friend WithEvents ViewCodeButton As System.Windows.Forms.Button
        Friend WithEvents ShutdownModeLabel As System.Windows.Forms.Label
        Friend WithEvents ShutdownModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ButtonsLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ViewUACSettingsButton As System.Windows.Forms.Button

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPageVBWPF))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.ButtonsLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyInfoButton = New System.Windows.Forms.Button
            Me.ViewUACSettingsButton = New System.Windows.Forms.Button
            Me.AssemblyNameLabel = New System.Windows.Forms.Label
            Me.AssemblyNameTextBox = New System.Windows.Forms.TextBox
            Me.RootNamespaceLabel = New System.Windows.Forms.Label
            Me.RootNamespaceTextBox = New System.Windows.Forms.TextBox
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label
            Me.TargetFrameworkComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectOrUriComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectOrUriLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeComboBox = New System.Windows.Forms.ComboBox
            Me.UseApplicationFrameworkCheckBox = New System.Windows.Forms.CheckBox
            Me.IconLabel = New System.Windows.Forms.Label
            Me.IconPicturebox = New System.Windows.Forms.PictureBox
            Me.IconCombobox = New System.Windows.Forms.ComboBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.WindowsAppGroupBox = New System.Windows.Forms.GroupBox
            Me.BottomHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.EditXamlButton = New System.Windows.Forms.Button
            Me.ViewCodeButton = New System.Windows.Forms.Button
            Me.ShutdownModeLabel = New System.Windows.Forms.Label
            Me.ShutdownModeComboBox = New System.Windows.Forms.ComboBox
            Me.TopHalfLayoutPanel.SuspendLayout()
            Me.ButtonsLayoutPanel.SuspendLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.WindowsAppGroupBox.SuspendLayout()
            Me.BottomHalfLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.ButtonsLayoutPanel, 0, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameTextBox, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceTextBox, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkComboBox, 0, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeComboBox, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectOrUriComboBox, 0, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectOrUriLabel, 0, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.UseApplicationFrameworkCheckBox, 0, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconLabel, 1, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconPicturebox, 2, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconCombobox, 1, 5)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
            '
            'ButtonsLayoutPanel
            '
            resources.ApplyResources(Me.ButtonsLayoutPanel, "ButtonsLayoutPanel")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.ButtonsLayoutPanel, 2)
            Me.ButtonsLayoutPanel.Controls.Add(Me.AssemblyInfoButton, 0, 0)
            Me.ButtonsLayoutPanel.Controls.Add(Me.ViewUACSettingsButton, 1, 0)
            Me.ButtonsLayoutPanel.Name = "ButtonsLayoutPanel"
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
            Me.ViewUACSettingsButton.UseVisualStyleBackColor = True
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
            'StartupObjectOrUriComboBox
            '
            resources.ApplyResources(Me.StartupObjectOrUriComboBox, "StartupObjectOrUriComboBox")
            Me.StartupObjectOrUriComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObjectOrUriComboBox.FormattingEnabled = True
            Me.StartupObjectOrUriComboBox.Name = "StartupObjectOrUriComboBox"
            '
            'StartupObjectOrUriLabel
            '
            resources.ApplyResources(Me.StartupObjectOrUriLabel, "StartupObjectOrUriLabel")
            Me.StartupObjectOrUriLabel.Name = "StartupObjectOrUriLabel"
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
            'UseApplicationFrameworkCheckBox
            '
            resources.ApplyResources(Me.UseApplicationFrameworkCheckBox, "UseApplicationFrameworkCheckBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.UseApplicationFrameworkCheckBox, 2)
            Me.UseApplicationFrameworkCheckBox.Name = "UseApplicationFrameworkCheckBox"
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
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.WindowsAppGroupBox, 0, 1)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
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
            Me.BottomHalfLayoutPanel.Controls.Add(Me.EditXamlButton, 0, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ViewCodeButton, 1, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeLabel, 0, 0)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeComboBox, 0, 1)
            Me.BottomHalfLayoutPanel.Name = "BottomHalfLayoutPanel"
            '
            'EditXamlButton
            '
            resources.ApplyResources(Me.EditXamlButton, "EditXamlButton")
            Me.EditXamlButton.Name = "EditXamlButton"
            '
            'ViewCodeButton
            '
            resources.ApplyResources(Me.ViewCodeButton, "ViewCodeButton")
            Me.ViewCodeButton.Name = "ViewCodeButton"
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
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.ShutdownModeComboBox, 2)
            Me.ShutdownModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ShutdownModeComboBox.FormattingEnabled = True
            Me.ShutdownModeComboBox.Name = "ShutdownModeComboBox"
            '
            'ApplicationPropPageVBWPF
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "ApplicationPropPageVBWPF"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            Me.ButtonsLayoutPanel.ResumeLayout(False)
            Me.ButtonsLayoutPanel.PerformLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).EndInit()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.WindowsAppGroupBox.ResumeLayout(False)
            Me.WindowsAppGroupBox.PerformLayout()
            Me.BottomHalfLayoutPanel.ResumeLayout(False)
            Me.BottomHalfLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

    End Class

End Namespace


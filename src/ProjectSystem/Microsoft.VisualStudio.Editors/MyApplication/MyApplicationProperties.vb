Imports System
Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports System.Collections
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports System.IO
Imports System.ComponentModel
Imports Microsoft.VisualStudio.Editors.Common.CodeModelUtils
Imports Microsoft.VisualStudio.Editors.Common.DTEUtils
Imports Microsoft.VisualStudio.Editors.Interop
Imports EnvDTE

Namespace Microsoft.VisualStudio.Editors.MyApplication


    Public Enum ApplicationTypes
        'Unknown = -1 'Not shown in UI
        'Custom = -2 'Not shown in UI
        WindowsApp = 0
        WindowsClassLib = 1
        CommandLineApp = 2
        WindowsService = 3
        WebControl = 4
    End Enum


    '****************************************************************************************
    'Interface IVsMyAppManager
    '****************************************************************************************


    ''' <summary>
    ''' This is a class that the project system uses to get the MyApplicationProperties object that it exposes publicly
    '''   through the DTE's Properties.Item("MyApplication") property.  This allows code to programatically change the
    '''   MyApplication settings through extensibility.  
    ''' 
    ''' Example macro code for setting these properties:
    '''    Imports EnvDTE
    '''    Imports EnvDTE80
    '''    Imports System.Diagnostics
    '''    
    '''    Public Module Module1
    '''    
    '''        Public Sub ChangeSingleInstance()
    '''            Dim p As Project
    '''            p = DTE.Solution.Projects.Item(1)
    '''            Dim MyApp As Properties = p.Properties.Item("MyApplication").Value
    '''            Dim SingleInstance As [Property] = MyApp.Item("SingleInstance")
    '''            MsgBox("SingleInstance currently: " + SingleInstance.Value)
    '''            MyApp.Item("SingleInstance").Value = True
    '''            MsgBox("SingleInstance currently: " + SingleInstance.Value)
    '''        End Sub
    '''    End Module
    '''    
    ''' </summary>
    ''' <remarks>
    ''' This interface is defined in vseditors.idl
    ''' </remarks>
    <ComImport(), Guid("365cb21a-0f0f-47bc-9653-3c81e0e3f9d6"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    Friend Interface IVsMyAppManager
        <PreserveSig()> _
        Function Init(<[In]()> ByVal ProjectHierarchy As IVsHierarchy) As Integer 'Initialize the MyApplicationProperties object, etc.

        <PreserveSig()> _
        Function GetProperties(<Out(), MarshalAs(UnmanagedType.IDispatch)> ByRef MyAppProps As Object) As Integer 'Get MyAppliationProperties object

        <PreserveSig()> _
        Function Save() As Integer 'Save any MyApplicationProperties changes to disk (in MyApplication.myapp), if dirty

        <PreserveSig()> _
        Function Close() As Integer 'Called by the project system upon closing a project.  Any unpersisted data at this point is discarded
    End Interface



    '****************************************************************************************
    ' Interface IVsMyApplicationProperties
    '****************************************************************************************


    Friend Enum MyAppDISPIDs
        CustomSubMain = 1
        MainForm = 2
        SingleInstance = 3
        ShutdownMode = 4
        EnableVisualStyles = 5
        AuthenticationMode = 7
        SplashScreen = 8
        ' ApplicationType = 9 ' OBSOLETE
        SaveMySettingsOnExit = 10
    End Enum

    ''' <summary>
    ''' The shape of the MyApplicationProperties object.
    ''' </summary>
    ''' <remarks>
    ''' This interface is not currently exposed to the public, except via dispatch.
    ''' </remarks>
    <ComVisible(True), Guid("6fec8bad-4bec-4447-a4ce-b48543a31165"), InterfaceType(ComInterfaceType.InterfaceIsDual)> _
    Public Interface IVsMyApplicationProperties
        <DispId(MyAppDISPIDs.CustomSubMain)> Property CustomSubMain() As Boolean
        <DispId(MyAppDISPIDs.MainForm)> Property MainForm() As String
        <DispId(MyAppDISPIDs.SingleInstance)> Property SingleInstance() As Boolean
        <DispId(MyAppDISPIDs.ShutdownMode)> Property ShutdownMode() As Integer
        <DispId(MyAppDISPIDs.EnableVisualStyles)> Property EnableVisualStyles() As Boolean
        <DispId(MyAppDISPIDs.AuthenticationMode)> Property AuthenticationMode() As Integer
        <DispId(MyAppDISPIDs.SplashScreen)> Property SplashScreen() As String
        ' <DispId(MyAppDISPIDs.ApplicationType)> Property ApplicationType() As Integer ' OBSOLETE
        <DispId(MyAppDISPIDs.SaveMySettingsOnExit)> Property SaveMySettingsOnExit() As Boolean
    End Interface



    Friend Interface IMyApplicationPropertiesInternal 'Not publicly exposed - for internal use only
        Inherits IVsMyApplicationProperties

        Sub RunCustomTool()
        ReadOnly Property CustomSubMainRaw() As Boolean
        Property SplashScreenNoRootNS() As String
        Property MainFormNoRootNamespace() As String
        Sub NavigateToEvents()
    End Interface

    '****************************************************************************************


    ''' <summary>
    ''' Our implementation of IVsMyAppManager
    ''' </summary>
    ''' <remarks></remarks>
    <ComVisible(True), Guid("29255174-ccb9-434d-8489-dae5b912b1d3"), CLSCompliantAttribute(False)> _
    Public NotInheritable Class MyApplicationManager
        Implements IVsMyAppManager

        Private m_MyProps As MyApplicationProperties

        'WeakReference list of MyApplicationProperties objects, one for each
        '  IVsHierarchy that we've been initialized against
        Private Shared m_MyPropertyInstances As New Hashtable


        ''' <summary>
        ''' Initialize the MyApplicationProperties object, etc.
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function Init(ByVal ProjectHierarchy As Shell.Interop.IVsHierarchy) As Integer Implements IVsMyAppManager.Init
            If ProjectHierarchy Is Nothing Then
                Throw New ArgumentNullException("ProjectHierarchy")
            End If

            SyncLock m_MyPropertyInstances
                Dim weakref As WeakReference

                If m_MyPropertyInstances.Contains(ProjectHierarchy) Then
                    weakref = TryCast(m_MyPropertyInstances.Item(ProjectHierarchy), WeakReference)
                    If weakref.IsAlive Then
                        m_MyProps = TryCast(weakref.Target, MyApplicationProperties)
                        Return VSConstants.S_OK
                    End If
                    'Reference is no longer alive, remove it from the list
                    m_MyPropertyInstances.Remove(ProjectHierarchy)
                End If

                'Create a new instance
                m_MyProps = New MyApplicationProperties()
                m_MyProps.Init(ProjectHierarchy)
                'Cache for later use
                m_MyPropertyInstances.Add(ProjectHierarchy, New WeakReference(m_MyProps))
            End SyncLock

            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Returns a dispatch object containing the properties for the MyApplication component
        ''' </summary>
        ''' <param name="MyAppProps"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProperties(ByRef MyAppProps As Object) As Integer Implements IVsMyAppManager.GetProperties
            MyAppProps = m_MyProps
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called by the project system upon closing a project.  Any unpersisted data at this point is discarded
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function Close() As Integer Implements IVsMyAppManager.Close
            If m_MyProps IsNot Nothing Then
                m_MyProps.Close()
                m_MyProps = Nothing
            End If
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called by the project system when we need to save all the MyApplication files.  Saves directly to disk.  Does not save
        '''   if not dirty.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function Save() As Integer Implements IVsMyAppManager.Save
            If m_MyProps IsNot Nothing Then
                m_MyProps.Save()
            End If
            Return NativeMethods.S_OK
        End Function

    End Class




    '****************************************************************************************
    ' Class MyApplicationProperties
    '****************************************************************************************



    ''' <summary>
    ''' This class provides access to the MyApplication properties
    ''' Data is stored locally until instructed to persist
    ''' </summary>
    ''' <remarks>Class must be Public to marshal the IDispatch</remarks>
    <ClassInterface(ClassInterfaceType.None)> _
    Public NotInheritable Class MyApplicationProperties
        Inherits MyApplicationPropertiesBase
        Implements IVsMyApplicationProperties
        Implements IMyApplicationPropertiesInternal
        Implements IDisposable
        Implements INotifyPropertyChanged

#Region "Public Events"
        ''' <summary>
        ''' This event is fired after any property on MyApplicationProperties is changed
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Public Event PropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
#End Region


        Friend Const Const_MyType_WindowsForms As String = "WindowsForms"
        'WindowsFormsWithCustomSubMain is used for applicationtype="Windows Application" when Custom Sub Main
        '  is used.  It keeps my.vb from defining their own Shared Sub Main, which would be ambiguous
        '  if the user chose "Sub Main" as the start-up object.
        Friend Const Const_MyType_WindowsFormsWithCustomSubMain As String = "WindowsFormsWithCustomSubMain"
        Friend Const Const_MyType_Windows As String = "Windows"
        Friend Const Const_MyType_Console As String = "Console"
        Friend Const Const_MyType_Empty As String = "Empty"
        Friend Const Const_MyType_WebControl As String = "WebControl"
        Friend Const Const_MyType_Custom As String = "Custom"

        'Constants for property change notifications
        Private Const PROPNAME_CustomSubMain As String = "CustomSubMain"
        Private Const PROPNAME_MainForm As String = "MainForm"
        Private Const PROPNAME_SingleInstance As String = "SingleInstance"
        Private Const PROPNAME_ShutdownMode As String = "ShutdownMode"
        Private Const PROPNAME_EnableVisualStyles As String = "EnableVisualStyles"
        Private Const PROPNAME_SaveMySettingsOnExit As String = "SaveMySettingsOnExit"
        Private Const PROPNAME_AuthenticationMode As String = "AuthenticationMode"
        Private Const PROPNAME_SplashScreen As String = "SplashScreen"

        Private m_ProjectHierarchy As Shell.Interop.IVsHierarchy
        Private WithEvents m_MyAppDocData As DocData 'The DocData which backs the MyApplication.myapp file
        Private m_ProjectDesignerProjectItem As EnvDTE.ProjectItem
        Private m_ServiceProvider As Microsoft.VisualStudio.Shell.ServiceProvider
        Private m_DocDataService As DesignerDocDataService
        Private m_MyAppData As MyApplication.MyApplicationData

        'The filename for the XML file where we store the MyApplication properties
        Private Const Const_MyApplicationFileName As String = "Application.myapp"
        Private Const Const_MyApplicationFileName_B1Compat As String = "MyApplication.myapp" 'Old (Beta 1) name for this file to remain backwards compatible

        Private m_MyAppFileName As String 'The actual file for the XML file that we found and are using (*not* including the path)

        Private Const Const_MyApplicationEntryPoint As String = "My.MyApplication"

        'The name of the file where users hook up My events
        Private Const Const_MyEventsFileName As String = "ApplicationEvents.vb"
        Private Const Const_MyEventsFileName_B2Compat As String = "Application.vb"
        Private Const Const_MyEventsFileName_B1Compat As String = "MyEvents.vb" 'Old (Beta 1) name for this file to remain backwards compatible

        'The relevant project property names
        Private Const PROJECTPROPERTY_CUSTOMTOOL As String = "CustomTool"
        Private Const PROJECTPROPERTY_CUSTOMTOOLNAMESPACE As String = "CustomToolNamespace"

        'The custom tool name to use for the default resx file in VB projects
        Private Const MYAPPCUSTOMTOOL As String = "MyApplicationCodeGenerator"

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub New()

        End Sub

        ''' <summary>
        ''' Initialization.  Called by the project system before requesting the MyApplicationProperties object.
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        ''' <remarks></remarks>
        Friend Sub Init(ByVal ProjectHierarchy As IVsHierarchy)
            Dim hr As Integer
            Dim obj As Object = Nothing

            If ProjectHierarchy Is Nothing Then
                Throw New ArgumentNullException("ProjectHierarchy")
            End If

            m_ProjectHierarchy = ProjectHierarchy

            hr = m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, obj)
            If NativeMethods.Succeeded(hr) Then
                Dim DTEProject As EnvDTE.Project = TryCast(obj, EnvDTE.Project)
                Debug.Assert(DTEProject IsNot Nothing)
                If DTEProject IsNot Nothing Then
                    m_ServiceProvider = New Microsoft.VisualStudio.Shell.ServiceProvider(TryCast(DTEProject.DTE, Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
                End If
            End If

            'Get the Project Designer node
            m_ProjectDesignerProjectItem = GetProjectItemForProjectDesigner(ProjectHierarchy)

            'Determine the filename for the .myapp file
            m_MyAppFileName = Const_MyApplicationFileName

            'BEGIN Beta 1 Backwards compatibility
            If Not File.Exists(MyAppFileNameWithPath) Then
                Dim FileNameCompat As String = IO.Path.Combine(ProjectDesignerProjectItem.FileNames(1), Const_MyApplicationFileName_B1Compat)
                If File.Exists(FileNameCompat) Then
                    'The new version of the filename does not exist, but the old one does - use it instead
                    m_MyAppFileName = Const_MyApplicationFileName_B1Compat
                    Debug.Assert(File.Exists(MyAppFileNameWithPath), "Huh?")
                End If
            End If
            'END Beta 1 Backwards compatibility

            If File.Exists(MyAppFileNameWithPath) Then
                'The .myapp file exists - try to read it in
                PrepareMyAppDocData()
                Using Reader As TextReader = GetMyAppTextReader()
                    m_MyAppData = MyApplication.MyApplicationSerializer.Deserialize(Reader)
                    Reader.Close()
                End Using
                m_MyAppData.IsDirty = False
            Else
                'The .myapp file doesn't exist.  Just use default properties.  Don't force create the .myapp file until
                '  a property is changed that forces us to write to it.
                m_MyAppData = New MyApplication.MyApplicationData()
                m_MyAppData.IsDirty = False
            End If
        End Sub


        ''' <summary>
        ''' Flushes all values from m_MyAppData to the doc data.  This should be done after any property change.
        ''' </summary>
        ''' <remarks>
        ''' IMPORTANT: CONSIDER: The current implementation does not use a designer loader, but just a DocData/Service.  Therefore,
        '''   it does not get hooked up into the flush mechanism, and we receive no warnings to flush before our doc data is saved.
        '''   Therefore, it is critical to call this function after any property value change.  Should consider using BaseDesignerLoader
        '''   like regular designers.
        ''' </remarks>
        Private Sub FlushToDocData()
            Debug.Assert(m_MyAppDocData IsNot Nothing, "m_MyAppDocData is nothing")
            If m_MyAppDocData IsNot Nothing Then
                Using Writer As TextWriter = GetMyAppTextWriter()
                    MyApplication.MyApplicationSerializer.Serialize(m_MyAppData, Writer)
                    Writer.Close()
                End Using

                'The IsDirty flag indicates that we have changes in memory which have not been committed to the doc data yet.  Thus,
                '  we're no longer dirty.
                m_MyAppData.IsDirty = False
            End If
        End Sub


        ''' <summary>
        ''' Attempts to check out the doc data, if it is not already checked out.  This should be done prior to any property change.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CheckOutDocData()
            Debug.Assert(m_ServiceProvider IsNot Nothing, "m_ServiceProvider is nothing")

            PrepareMyAppDocData()

            Debug.Assert(m_MyAppDocData IsNot Nothing, "m_MyAppDocData is nothing")
            If m_MyAppDocData IsNot Nothing AndAlso m_ServiceProvider IsNot Nothing Then
                m_MyAppDocData.CheckoutFile(m_ServiceProvider)
            End If
        End Sub

        ''' <summary>
        ''' Request the custom tool for MyApplication.myapp to be run 
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub RunCustomTool() Implements IMyApplicationPropertiesInternal.RunCustomTool
            Dim item As EnvDTE.ProjectItem = MyAppProjectItem
            If item IsNot Nothing Then
                Dim VsProjectItem As VSLangProj.VSProjectItem = TryCast(item.Object, VSLangProj.VSProjectItem)
                If VsProjectItem IsNot Nothing Then
                    VsProjectItem.RunCustomTool()
                End If
            End If
        End Sub

        Protected Overloads Function AddFileToProject(ByVal ProjectItems As EnvDTE.ProjectItems, ByVal FileName As String, ByVal CopyFile As Boolean) As EnvDTE.ProjectItem
            Dim ProjectItem As EnvDTE.ProjectItem = MyAppProjectItem

            'First see if it is already in the project
            If ProjectItem IsNot Nothing Then
                Return ProjectItem
            End If

            If CopyFile Then
                ProjectItem = ProjectItems.AddFromFileCopy(FileName)
            Else
                ProjectItem = ProjectItems.AddFromFile(FileName)
            End If
            Return ProjectItem
        End Function

        Private ReadOnly Property ServiceProvider() As Microsoft.VisualStudio.Shell.ServiceProvider
            Get
                Return m_ServiceProvider
            End Get
        End Property

        Private ReadOnly Property ProjectDesignerProjectItem() As EnvDTE.ProjectItem
            Get
                Return m_ProjectDesignerProjectItem
            End Get
        End Property

#Region "Public Properties"

        '******************************
        '
        ' IMPORTANT: If you add more properties here, you must add corresponding code in FireChangeNotificationsForNewValues
        '
        '******************************

        ''' <summary>
        ''' Returns the "raw" value of CustomSubMain.  I.e., it will return True if the internal
        '''   state says that CustomSubMain is on, even if custom sub main is not valid in the
        '''   project's current state.  This can be important to access if the face of undo/redo
        '''   and other issues, but it's not what we public expose via DTE.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property CustomSubMainRaw() As Boolean Implements IMyApplicationPropertiesInternal.CustomSubMainRaw
            Get
                Return Not m_MyAppData.MySubMain
            End Get
        End Property


        Public Property CustomSubMain() As Boolean Implements IVsMyApplicationProperties.CustomSubMain
            Get
                Return Not m_MyAppData.MySubMain AndAlso IsMySubMainSupported(m_ProjectHierarchy)
            End Get
            Set(ByVal value As Boolean)
                If m_MyAppData.MySubMain <> (Not value) Then
                    CheckOutDocData()
                    m_MyAppData.MySubMain = Not value
                    FlushToDocData()

                    'Changing this property in the generated .vb file can cause or remove build errors, so we
                    '  want to force the custom tool to run now.
                    RunCustomTool()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_CustomSubMain)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Retrieves the MainForm property, including the root namespace
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' If this property is not currently set to a meaningful value, we return empty string.
        ''' </remarks>
        Public Property MainForm() As String Implements IVsMyApplicationProperties.MainForm
            Get
                If m_MyAppData.MainFormNoRootNS = "" Then
                    Return ""
                Else
                    Return AddNamespace(GetRootNamespace(), m_MyAppData.MainFormNoRootNS)
                End If
            End Get
            Set(ByVal value As String)
                Me.MainFormNoRootNamespace = RemoveRootNamespace(value, GetRootNamespace())
            End Set
        End Property

        ''' <summary>
        ''' Retrieves the MainForm property, without the root namespace (this is how the property is
        '''   serialized and stored, so this specifically does not do the step of adding/removing the
        '''   root namespace of the project)
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' If this property is not currently set to a meaningful value, we return empty string.
        ''' </remarks>
        Friend Property MainFormNoRootNamespace() As String Implements IMyApplicationPropertiesInternal.MainFormNoRootNamespace
            Get
                Return NothingToEmptyString(m_MyAppData.MainFormNoRootNS)
            End Get
            Set(ByVal value As String)
                If System.String.CompareOrdinal(NothingToEmptyString(m_MyAppData.MainFormNoRootNS), NothingToEmptyString(value)) <> 0 Then
                    CheckOutDocData()
                    m_MyAppData.MainFormNoRootNS = EmptyStringToNothing(value)
                    FlushToDocData()

                    'Changing this property in the generated .vb file can cause or remove build errors, so we
                    '  want to force the custom tool to run now.
                    RunCustomTool()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_MainForm)
                End If
            End Set
        End Property

        Public Property SingleInstance() As Boolean Implements IVsMyApplicationProperties.SingleInstance
            Get
                Return m_MyAppData.SingleInstance
            End Get
            Set(ByVal value As Boolean)
                If m_MyAppData.SingleInstance <> value Then
                    CheckOutDocData()
                    m_MyAppData.SingleInstance = value
                    FlushToDocData()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_SingleInstance)
                End If
            End Set
        End Property

        Public Property ShutdownMode() As Integer Implements IVsMyApplicationProperties.ShutdownMode
            Get
                Return m_MyAppData.ShutdownMode
            End Get
            Set(ByVal value As Integer)
                Select Case value
                    Case _
                    Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterMainFormCloses, _
                    Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterAllFormsClose
                        'Valid - continue
                    Case Else
                        Throw New ArgumentOutOfRangeException("value")
                End Select

                If m_MyAppData.ShutdownMode <> value Then
                    CheckOutDocData()
                    m_MyAppData.ShutdownMode = value
                    FlushToDocData()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_ShutdownMode)
                End If
            End Set
        End Property

        Public Property EnableVisualStyles() As Boolean Implements IVsMyApplicationProperties.EnableVisualStyles
            Get
                Return m_MyAppData.EnableVisualStyles
            End Get
            Set(ByVal value As Boolean)
                If m_MyAppData.EnableVisualStyles <> value Then
                    CheckOutDocData()
                    m_MyAppData.EnableVisualStyles = value
                    FlushToDocData()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_EnableVisualStyles)
                End If
            End Set
        End Property

        Public Property SaveMySettingsOnExit() As Boolean Implements IVsMyApplicationProperties.SaveMySettingsOnExit
            Get
                Return m_MyAppData.SaveMySettingsOnExit
            End Get
            Set(ByVal value As Boolean)
                If m_MyAppData.SaveMySettingsOnExit <> value Then
                    CheckOutDocData()
                    m_MyAppData.SaveMySettingsOnExit = value
                    FlushToDocData()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_SaveMySettingsOnExit)
                End If
            End Set
        End Property

        Public Property AuthenticationMode() As Integer Implements IVsMyApplicationProperties.AuthenticationMode
            Get
                Return m_MyAppData.AuthenticationMode
            End Get
            Set(ByVal value As Integer)
                Select Case value
                    Case _
                    Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows, _
                    Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.ApplicationDefined
                        'Valid - continue
                    Case Else
                        Throw New ArgumentOutOfRangeException("value")
                End Select

                If m_MyAppData.AuthenticationMode <> value Then
                    CheckOutDocData()
                    m_MyAppData.AuthenticationMode = value
                    FlushToDocData()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_AuthenticationMode)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Retrieves the MainForm property, including the root namespace
        ''' </summary>
        ''' <value></value>
        ''' If this property is not currently set to a meaningful value, we return empty string.
        Public Property SplashScreen() As String Implements IVsMyApplicationProperties.SplashScreen
            Get
                If m_MyAppData.SplashScreenNoRootNS = "" Then
                    Return ""
                Else
                    Return AddNamespace(GetRootNamespace(), m_MyAppData.SplashScreenNoRootNS)
                End If
            End Get
            Set(ByVal value As String)
                SplashScreenNoRootNS = RemoveRootNamespace(value, GetRootNamespace())
            End Set
        End Property

        ''' <summary>
        ''' Retrieves the MainForm property, without the root namespace (this is how the property is
        '''   serialized and stored, so this specifically does not do the step of adding/removing the
        '''   root namespace of the project)
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' If this property is not currently set to a meaningful value, we return empty string.
        ''' </remarks>
        Friend Property SplashScreenNoRootNS() As String Implements IMyApplicationPropertiesInternal.SplashScreenNoRootNS
            Get
                Return NothingToEmptyString(m_MyAppData.SplashScreenNoRootNS)
            End Get
            Set(ByVal value As String)
                If System.String.CompareOrdinal(NothingToEmptyString(m_MyAppData.SplashScreenNoRootNS), NothingToEmptyString(value)) <> 0 Then
                    CheckOutDocData()
                    m_MyAppData.SplashScreenNoRootNS = EmptyStringToNothing(value)
                    FlushToDocData()

                    'Changing this property in the generated .vb file can cause or remove build errors, so we
                    '  want to force the custom tool to run now.
                    RunCustomTool()

                    'Notify users of property change
                    OnPropertyChanged(PROPNAME_SplashScreen)
                End If
            End Set
        End Property


#End Region


        ''' <summary>
        ''' Makes sure the .MyApp file exists, creates a doc data for it, etc.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PrepareMyAppDocData()
            If Not File.Exists(MyAppFileNameWithPath) Then
                Dim FullPath As String = MyAppFileNameWithPath()

                'Make sure the directory exists
                Dim Path As String = IO.Path.GetDirectoryName(FullPath)
                If Not Directory.Exists(Path) Then
                    Directory.CreateDirectory(Path)
                End If

                'Create the file
                Dim stream As Stream = Nothing
                Dim writer As StreamWriter = Nothing

                Try
                    stream = File.Create(FullPath)
                    writer = New StreamWriter(stream)
                    'Initialize the file with a default MyApplicationData
                    MyApplication.MyApplicationSerializer.Serialize(New MyApplication.MyApplicationData(), writer)
                    stream = Nothing 'Writer will now close

                    m_MyAppDocData = Nothing
                    If m_DocDataService IsNot Nothing Then
                        m_DocDataService.Dispose()
                    End If
                    m_DocDataService = Nothing
                Finally
                    If writer IsNot Nothing Then
                        writer.Close()
                    ElseIf stream IsNot Nothing Then
                        stream.Close()
                    End If
                End Try
            End If

            'Add the file to the project
            Dim Item As EnvDTE.ProjectItem = MyAppProjectItem
            If Item Is Nothing Then
                Item = AddFileToProject(ProjectDesignerProjectItem.ProjectItems, MyAppFileNameWithPath, False)

                'Make sure the custom tool for the MyApplication data file is set correctly
                SetCustomTool(Item, MYAPPCUSTOMTOOL)

                'BuildAction should be None so the file doesn't get published
                Common.DTEUtils.SetBuildAction(Item, VSLangProj.prjBuildAction.prjBuildActionNone)
            End If

            'Create the DocData for the file
            If m_MyAppDocData Is Nothing Then
                Debug.Assert(m_DocDataService Is Nothing)
                m_MyAppDocData = New DocData(ServiceProvider, Item.FileNames(1))

                Dim ItemId As UInteger = DTEUtils.ItemIdOfProjectItem(m_ProjectHierarchy, Item)
                m_DocDataService = New DesignerDocDataService(ServiceProvider, m_ProjectHierarchy, ItemId, MyAppDocData)
            End If
        End Sub


        ''' <summary>
        ''' The filename (*not*) including path) of the .myapp filename in use or to be created
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property MyAppFileName() As String
            Get
                Return m_MyAppFileName
            End Get
        End Property


        ''' <summary>
        ''' Returns the full path/filename of the .myapp file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MyAppFileNameWithPath() As String
            Return IO.Path.Combine(ProjectDesignerProjectItem.FileNames(1), m_MyAppFileName)
        End Function


        ''' <summary>
        ''' Returns the DTE ProjectItem for the .myapp file
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property MyAppProjectItem() As EnvDTE.ProjectItem
            Get
                'First see if it is already in the project
                For Each ProjectItem As EnvDTE.ProjectItem In ProjectDesignerProjectItem.ProjectItems
                    If ProjectItem.FileNames(1).Equals(MyAppFileNameWithPath, StringComparison.OrdinalIgnoreCase) Then
                        Return ProjectItem
                    End If
                Next

                ' Nope - clear out whatever docdata we had...
                m_MyAppDocData = Nothing
                If m_DocDataService IsNot Nothing Then
                    m_DocDataService.Dispose()
                End If
                m_DocDataService = Nothing

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns the docdata after initializing for the .myapp file
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property MyAppDocData() As DocData
            Get
                Return m_MyAppDocData
            End Get
        End Property

        ''' <summary>
        ''' Returns the data storage values for the .myapp portion of the page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property MyAppData() As MyApplication.MyApplicationData
            Get
                Return Me.m_MyAppData
            End Get
        End Property

        ''' <summary>
        ''' DocDataService provides SCC checkin/out interaction with VS
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property DocDataService() As DesignerDocDataService
            Get
                Return m_DocDataService
            End Get
        End Property

        ''' <summary>
        ''' Provides the TextReader for reading the .myapp file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetMyAppTextReader() As TextReader
            Return New DocDataTextReader(DocDataService.PrimaryDocData, False)
        End Function

        ''' <summary>
        ''' Provides the TextWriter for writing the .myapp file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetMyAppTextWriter() As TextWriter
            Return New MyAppTextWriter(DocDataService.PrimaryDocData, False)
        End Function

        ''' <summary>
        ''' Retrieves the given project item's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProjectItemProperty(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal PropertyName As String) As EnvDTE.Property
            If ProjectItem.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As EnvDTE.Property In ProjectItem.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Sets the custom tool and namespace for this resx file appropriately so that strongly typed resource
        '''   generation is hooked up.
        ''' </summary>
        ''' <remarks>Caller is responsible for catching exceptions</remarks>
        Private Sub SetCustomTool(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal Value As String)
            Dim ToolProperty As EnvDTE.Property = GetProjectItemProperty(ProjectItem, PROJECTPROPERTY_CUSTOMTOOL)
            Dim NamespaceProperty As EnvDTE.Property = GetProjectItemProperty(ProjectItem, PROJECTPROPERTY_CUSTOMTOOLNAMESPACE)

            Try
                If ToolProperty Is Nothing Then
                    'No custom tool property in this project, so nothing to do
                    Exit Sub
                End If

                Dim CurrentCustomTool As String = TryCast(ToolProperty.Value, String)

                If CurrentCustomTool <> Value Then
                    ToolProperty.Value = Value
                End If

            Catch ex As COMException
                'The COM exception don't give us much to go on.  In the SCC case, for instance, if 
                '  project check-out fails, the message is simply "Exception occurred".  We'd rather
                '  simply throw a general exception of our own text than propagate this to the user.
                Throw New Exception(SR.GetString(SR.RSE_Task_CantChangeCustomToolOrNamespace), ex)

            Catch ex As Exception
                Common.Utils.RethrowIfUnrecoverable(ex)

                'For anything else, combine our error messages.
                Throw New Exception(SR.GetString(SR.RSE_Task_CantChangeCustomToolOrNamespace & Microsoft.VisualBasic.vbCrLf & ex.Message))
            End Try
        End Sub

        ''' <summary>
        ''' Creates a new MyEvents.vb file from scratch.
        ''' </summary>
        ''' <param name="DestinationProjectItems">The ProjectItems in which to place the new file</param>
        ''' <param name="MyEventsFileName">The name of the file to create</param>
        ''' <param name="MyEventsNamespaceName">The name of the namespace to use</param>
        ''' <param name="MyEventsClassName">The name of the partial class to use</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CreateNewMyEventsFile(ByVal DestinationProjectItems As EnvDTE.ProjectItems, ByVal MyEventsFileName As String, ByVal MyEventsNamespaceName As String, ByVal MyEventsClassName As String) As EnvDTE.ProjectItem
            Debug.Assert(IO.Path.GetExtension(MyEventsFileName) = ".vb", "Extension of MyEvents.vb file doesn't end in .vb?")

            'Create the new file
            Dim NewFilePath As String = IO.Path.Combine(Common.DTEUtils.GetFolderNameFromProjectItems(DestinationProjectItems), MyEventsFileName)
            Dim filestream As IO.FileStream = IO.File.Create(NewFilePath)

            'Write out the UTF-8 BOM so that the code model will treat it as a UTF-8 file.
            Dim BOM() As Byte = System.Text.Encoding.UTF8.GetPreamble()
            filestream.Write(BOM, 0, BOM.Length)

            'Close the file's handle before trying to add it to the project, or the code model
            '  will not be able to start using it until garbage collection closes the handle.
            filestream.Close()

            'Add it to the project
            Dim MyEventsProjectItem As EnvDTE.ProjectItem = ProjectDesignerProjectItem.ProjectItems.AddFromFile(NewFilePath)

            'Now generate the class guts, it should look simply like this:
            '
            '  Namespace My
            '    Partial Friend Class MyApplication
            '    End Class
            '  End Namespace
            '

            Dim CodeModel As FileCodeModel = MyEventsProjectItem.FileCodeModel
            If CodeModel Is Nothing Then
                Debug.Fail("Couldn't get file code model for new '" & NewFilePath & "' file")
            Else
                Dim MyEventsNamespace As CodeNamespace = Nothing

                'There is a timing issue, in that the VB compiler sometimes needs a little time after the new file has been added,
                '  before we can start changing stuff in it.  We may need to sleep and retry a time or two.
                'NOTE: with the filestream.Close() call above, this may no longer be necessary...
                Const MaxSleepIterations As Integer = 20
                For iIteration As Integer = 1 To MaxSleepIterations
                    Try
                        'DoEvents for 500 milliseconds
                        Dim Start As Double = Microsoft.VisualBasic.Timer
                        While Microsoft.VisualBasic.Timer < Start + 0.5
                            System.Windows.Forms.Application.DoEvents()
                        End While

                        MyEventsNamespace = CodeModel.AddNamespace(MyEventsNamespaceName)
                        Exit For
                    Catch ex As COMException
                    End Try
                Next


                If MyEventsNamespace Is Nothing Then
                    Debug.Fail("Unable to add Namespace to new file")
                Else
                    Dim MyEventsClass As CodeClass = MyEventsNamespace.AddClass(MyEventsClassName, Access:=vsCMAccess.vsCMAccessProject)
                    Dim MyEventsClass2 As EnvDTE80.CodeClass2 = TryCast(MyEventsClass, EnvDTE80.CodeClass2)
                    If MyEventsClass2 IsNot Nothing Then
                        MyEventsClass2.DataTypeKind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial
                    Else
                        Debug.Fail("Couldn't get CodeClass2 to set the new class to a partial class - may be ignorable")
                    End If

                    'Add comments
                    Dim Comments As String = _
                        SR.GetString(SR.PPG_Application_AppEventsCommentLine1) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine2) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine3) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine4) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine5) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine6) _
                        & vbCrLf & SR.GetString(SR.PPG_Application_AppEventsCommentLine7)
                    MyEventsClass.Comment = Comments
                End If
            End If

            Return MyEventsProjectItem
        End Function

        ''' <summary>
        ''' Navigates to the default event in the MyEvents.vb file, if it exists.  Creates the file if necessary.
        ''' </summary>
        ''' <remarks>
        ''' This action *may* cause the project file to be reloaded, so the caller should guard against that by
        ''' putting this call in a EnterProjectCheckoutSection/LeaveProjectCheckoutSection block...
        ''' </remarks>
        Friend Sub NavigateToEvents() Implements IMyApplicationPropertiesInternal.NavigateToEvents
            Const Const_MyEventsNamespace As String = "My"
            Const Const_MyEventsClassName As String = "MyApplication"
            Const Const_MyEventsDefaultEventHandlerName As String = "MyApplication_StartUp"
            Const Const_MyEventsDefaultEventName As String = "Me.StartUp"
            Dim Const_MyEventsDefaultEventEventArgsType As Type = GetType(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs)

            'Search for the Application.vb (used to be MyEvents.vb) file
            Dim FileIsNew As Boolean
            Dim MyEventsProjectItem As EnvDTE.ProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ProjectItems, Const_MyEventsFileName)
            If MyEventsProjectItem Is Nothing Then
                'The file doesn't exist in the My Application folder.  Let's also look in the root folder, in case the user
                '  moved it there.
                MyEventsProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ContainingProject.ProjectItems, Const_MyEventsFileName)

                'BEGIN Beta 2 Backwards compatibility
                If MyEventsProjectItem Is Nothing Then
                    'Could not find the file with the new, expected name.  Also search for the old name in both places
                    MyEventsProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ProjectItems, Const_MyEventsFileName_B2Compat)
                    If MyEventsProjectItem Is Nothing Then
                        MyEventsProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ContainingProject.ProjectItems, Const_MyEventsFileName_B2Compat)
                    End If
                End If
                'END Beta 2 Backwards compatibility

                'BEGIN Beta 1 Backwards compatibility
                If MyEventsProjectItem Is Nothing Then
                    'Could not find the file with the new, expected name.  Also search for the old name in both places
                    MyEventsProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ProjectItems, Const_MyEventsFileName_B1Compat)
                    If MyEventsProjectItem Is Nothing Then
                        MyEventsProjectItem = QueryProjectItems(ProjectDesignerProjectItem.ContainingProject.ProjectItems, Const_MyEventsFileName_B1Compat)
                    End If
                End If
                'END Beta 1 Backwards compatibility

                If MyEventsProjectItem Is Nothing Then
                    'Still not found.  Need to create a new one.
                    '  We create it in the root of the project (that's now the preferred location
                    '  for this file).

                    ' First, make sure that the project file is checked out...
                    Dim filesToCheckOut As New Generic.List(Of String)
                    Dim fileReloaded As Boolean
                    Dim projectFileName As String = ProjectDesignerProjectItem.ContainingProject.FullName
                    filesToCheckOut.Add(projectFileName)
                    DesignerFramework.SourceCodeControlManager.QueryEditableFiles(ServiceProvider, filesToCheckOut, True, False, fileReloaded)
                    If fileReloaded Then
                        ' The project was reloaded - we've got to bail ASAP!
                        Return
                    End If

                    MyEventsProjectItem = CreateNewMyEventsFile(ProjectDesignerProjectItem.ContainingProject.ProjectItems, Const_MyEventsFileName, Const_MyEventsNamespace, Const_MyEventsClassName)
                    FileIsNew = True
                End If
            End If

            'Add or navigate to the default event handler
            Dim DefaultEventHandler As CodeFunction
            If MyEventsProjectItem.IsOpen AndAlso MyEventsProjectItem.Document IsNot Nothing Then
                'Document is already open.  Don't change anything and don't do any navigation.  Just activate it.
                Debug.Assert(Not FileIsNew)
                MyEventsProjectItem.Document.Activate()
            Else
                'Open and activate it.
                MyEventsProjectItem.Open()
                If MyEventsProjectItem.Document IsNot Nothing Then
                    MyEventsProjectItem.Document.Activate()

                    Dim MyApplicationClass As CodeClass = FindCodeClass(MyEventsProjectItem.FileCodeModel.CodeElements, Const_MyEventsNamespace, Const_MyEventsClassName)
                    If MyApplicationClass IsNot Nothing Then
                        If FileIsNew Then
#If 0 Then 'Looks like we don't want to create a default event handler, because it will fail to compile if the application type is changed to not be Windows application

                        'If we just created the file, then go ahead and add an event handler.  Otherwise we don't want to, because the user might have
                        '  purposely removed it.
                        DefaultEventHandler = TryAddEventHandler(MyApplicationClass, Const_MyEventsDefaultEventName, Const_MyEventsDefaultEventHandlerName, Const_MyEventsDefaultEventEventArgsType, vsCMAccess.vsCMAccessPrivate)
                        If DefaultEventHandler IsNot Nothing Then
                            'Navigate to the new event handler
                            NavigateToFunction(DefaultEventHandler)
                        End If
#End If
                        Else
                            'Try to find the default event in the code, if it's already there.
                            DefaultEventHandler = FindEventHandler(MyApplicationClass.Members, Const_MyEventsDefaultEventName, Const_MyEventsDefaultEventHandlerName, True)
                            If DefaultEventHandler IsNot Nothing Then
                                'It's there.  Is there anything else in the class?  We only want to navigate to the default event handler if there's
                                '  nothing else in the class, otherwise it might be confusing.
                                If MyApplicationClass.Members.Count = 1 Then
                                    NavigateToFunction(DefaultEventHandler)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Returns the project item for the My Project node
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function GetProjectItemForProjectDesigner(ByVal ProjectHierarchy As IVsHierarchy) As EnvDTE.ProjectItem
            Dim SpecialFiles As IVsProjectSpecialFiles = CType(ProjectHierarchy, IVsProjectSpecialFiles)
            Dim ProjectDesignerItemId As UInteger
            Dim ProjectDesignerDirName As String = Nothing
            Dim hr As Integer
            Dim obj As Object = Nothing

            Debug.Assert(SpecialFiles IsNot Nothing, "Failed to get IVsProjectSpecialFiles for Hierarchy")
            'Make sure 'My Application' node exists
            If SpecialFiles IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(SpecialFiles.GetFile(__PSFFILEID2.PSFFILEID_AppDesigner, CUInt(__PSFFLAGS.PSFF_CreateIfNotExist), ProjectDesignerItemId, ProjectDesignerDirName))

                hr = ProjectHierarchy.GetProperty(ProjectDesignerItemId, __VSHPROPID.VSHPROPID_ExtObject, obj)
                If NativeMethods.Succeeded(hr) Then
                    Return TryCast(obj, EnvDTE.ProjectItem)
                End If
            End If
            Return Nothing
        End Function


        ''' <summary>
        ''' Called by the project system upon closing a project.  Any unpersisted data at this point is discarded
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub Close()
            If m_MyAppDocData IsNot Nothing Then
                m_MyAppDocData = Nothing
                m_DocDataService.Dispose()
                m_DocDataService = Nothing
            End If
        End Sub


        ''' <summary>
        ''' Called by the project system when we need to save all the MyApplication files.  Saves directly to disk.  Does not save
        '''   if not dirty.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub Save()
            If m_MyAppDocData IsNot Nothing Then
                'Make sure the doc data is up to date (in reality in our current model, we shouldn't ever be in a dirty state
                '  like this).
                If Me.m_MyAppData.IsDirty Then
                    FlushToDocData()
                End If

                If m_DocDataService IsNot Nothing AndAlso m_DocDataService.PrimaryDocData.Modified Then
                    'The doc data is dirty.  We need to flush it immediately to disk.  This routine
                    '  gets called from the project system when it is saving the project file.  All other
                    '  files in the project (e.g. the .myapp) will have already been saved by now if we're
                    '  doing a build.  If the previous FlushToDocData caused the doc data to be dirty, but
                    '  we don't force a flush to the disk, the .myapp file will still be dirty after the
                    '  build.  Also, we want to enforce that whenever the project file is saved, the .myapp
                    '  file should be saved (for the case of saving via extensibility, etc.).
                    '
                    'Also note that theoretically we should participate in the decision of whether the project
                    '  file is dirty (in the project code), but since we always flush to our doc data after any
                    '  property change, this isn't currently necessary.  If we've made changes, either our doc
                    '  data will be dirty or the file on disk will have been changed.

                    Dim Item As EnvDTE.ProjectItem = MyAppProjectItem
                    Debug.Assert(Item IsNot Nothing)
                    If Item IsNot Nothing Then
                        'We could go through the running doc table, or through ProjectItem.Save.  We choose to use
                        '  ProjectItem.Save() because a) it's convenient, b) it does an immediate run of the custom
                        '  tool.  Besides it also goes through the RDT.
                        Item.Save()
                    End If
                End If

                m_MyAppData.IsDirty = False
            End If
        End Sub

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                Close()
            End If
        End Sub

        Public Overloads Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
        End Sub


        ''' <summary>
        ''' Gets the root namespace for a given IVsHierarchy
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetRootNamespace() As String
            If m_ProjectHierarchy IsNot Nothing Then
                Dim ObjNamespace As Object = Nothing
                VSErrorHandler.ThrowOnFailure(m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_DefaultNamespace, ObjNamespace))
                Return DirectCast(ObjNamespace, String)
            End If

            Debug.Fail("Unable to get project's root namespace inside MyApplicationProperties")
            Return ""
        End Function


        ''' <summary>
        ''' Returns the set of files that need to be checked out to change the given property
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function FilesToCheckOut(ByVal CreateIfNotExist As Boolean) As String()
            If CreateIfNotExist Then
                PrepareMyAppDocData()
            End If

            If MyAppProjectItem Is Nothing Then
                Debug.Fail("MyAppProjectItem is Nothing")
                Return New String() {}
            End If

            Dim MyAppFile As String = MyAppFileNameWithPath()
            Dim MyAppDesignerFile As String = Path.ChangeExtension(MyAppFile, "Designer.vb")

            Return New String() {MyAppFile, MyAppDesignerFile}
        End Function

        ''' <summary>
        ''' Whenver the backing file changes, we better update ourselves...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MyAppDocDataChanged(ByVal sender As Object, ByVal e As EventArgs) Handles m_MyAppDocData.DataChanged
            'Now read the data 
            Using Reader As TextReader = GetMyAppTextReader()
                Dim NewValues As MyApplicationData = MyApplication.MyApplicationSerializer.Deserialize(Reader)
                Reader.Close()

                'Remember the old values
                Dim OldValues As MyApplicationData = m_MyAppData

                '... and store the new ones
                m_MyAppData = NewValues

                'Some of the properties may have just changed.  Go through and fire notifications for the ones that actually did.
                FireChangeNotificationsForNewValues(OldValues, NewValues)
            End Using
        End Sub


        ''' <summary>
        ''' Given a set of old and new property values, fires a property changed notification for each property that has changed values.
        ''' </summary>
        ''' <param name="OldValues"></param>
        ''' <param name="NewValues"></param>
        ''' <remarks></remarks>
        Private Sub FireChangeNotificationsForNewValues(ByVal OldValues As MyApplication.MyApplicationData, ByVal NewValues As MyApplication.MyApplicationData)
            'AuthenticationMode
            If OldValues.AuthenticationMode <> NewValues.AuthenticationMode Then
                OnPropertyChanged(PROPNAME_AuthenticationMode)
            End If

            'CustomSubMain
            If OldValues.MySubMain <> NewValues.MySubMain Then
                OnPropertyChanged(PROPNAME_CustomSubMain)
            End If

            'EnableVisualStyles
            If OldValues.EnableVisualStyles <> NewValues.EnableVisualStyles Then
                OnPropertyChanged(PROPNAME_EnableVisualStyles)
            End If

            'MainForm
            If Not StringPropertyValuesEqual(OldValues.MainFormNoRootNS, NewValues.MainFormNoRootNS) Then
                OnPropertyChanged(PROPNAME_MainForm)
            End If

            'SaveMySettingsOnExit
            If OldValues.SaveMySettingsOnExit <> NewValues.SaveMySettingsOnExit Then
                OnPropertyChanged(PROPNAME_SaveMySettingsOnExit)
            End If

            'ShutdownMode
            If OldValues.ShutdownMode <> NewValues.ShutdownMode Then
                OnPropertyChanged(PROPNAME_ShutdownMode)
            End If

            'SingleInstance
            If OldValues.SingleInstance <> NewValues.SingleInstance Then
                OnPropertyChanged(PROPNAME_SingleInstance)
            End If

            'SplashScreen
            If Not StringPropertyValuesEqual(OldValues.SplashScreenNoRootNS, NewValues.SplashScreenNoRootNS) Then
                OnPropertyChanged(PROPNAME_SplashScreen)
            End If

        End Sub

        ''' <summary>
        ''' Compares two string values, and returns true iff they are equal (using a ordinal compare)
        ''' </summary>
        ''' <param name="String1"></param>
        ''' <param name="String2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function StringPropertyValuesEqual(ByVal String1 As String, ByVal String2 As String) As Boolean
            Return Utils.NothingToEmptyString(String1).Equals(Utils.NothingToEmptyString(String2), StringComparison.Ordinal)
        End Function


        ''' <summary>
        ''' Fires the PropertyChanged event
        ''' </summary>
        ''' <param name="PropertyName">The name of the property whose value has changed</param>
        ''' <remarks></remarks>
        Private Sub OnPropertyChanged(ByVal PropertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
        End Sub


        '************************************************


        ''' <summary>
        ''' A text writer for the DocData behind the .myapp file
        ''' </summary>
        ''' <remarks></remarks>
        Private Class MyAppTextWriter
            Inherits DocDataTextWriter

            Friend Sub New(ByVal docData As DocData)
                MyBase.New(docData)
            End Sub

            Friend Sub New(ByVal docData As DocData, ByVal disposeDocData As Boolean)
                MyBase.New(docData, disposeDocData)

            End Sub

            Public Overrides ReadOnly Property Encoding() As System.Text.Encoding
                Get
                    Return System.Text.Encoding.UTF8
                End Get
            End Property
        End Class


#Region "MyType, Application Type, Output Type, My Application-related stuff"
        '
        ' Application Type and Output Type are related in the following manner (note that it is not one-to-one):
        '
        '  Application Type      -> Output Type
        '  ---------------------    -----------
        '  Windows Application   -> winexe
        '  Windows Class Library -> library
        '  Console Application   -> exe
        '  Windows Service       -> winexe
        '  Web Control Library   -> library
        '

        '
        'The MyType constant depends on the Application Type according to this formula:
        '
        '  Application Type      ->  MyType
        '  -------------------       ------
        '  Windows Application   ->  “WindowsForms” or "WindowsFormsWithCustomSubMain"
        '  Windows Class Library ->  “Windows”
        '  Console Application   ->  “Console”
        '  Windows Service       ->  “Console”
        '  Web Control Library   ->  "WebControl"
        '


        ''' <summary>
        ''' Given an OutputType, returns the Application Type for it, differentiating if necessary based on the value of MyType
        ''' </summary>
        ''' <param name="OutputType">Output type</param>
        ''' <param name="MyType">Current value of MyType in the project</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function ApplicationTypeFromOutputType(ByVal OutputType As UInteger, ByVal MyType As String) As ApplicationTypes
            Select Case OutputType

                Case CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Exe)
                    If Const_MyType_Console.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        Return ApplicationTypes.CommandLineApp
                    ElseIf Const_MyType_Windows.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        'Backwards compat with earlier projects
                        'Disable until templates are changed: Debug.Fail("MyType value for a Console application has changed - please update MyType in the project file to be ""Console""")
                        Return ApplicationTypes.CommandLineApp
                        'End Backwards compat
                    Else
                        'Default if anything else to CommandLineApp
                        If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                            Debug.Assert(MyType = "" OrElse MyType.Equals(Const_MyType_Empty, StringComparison.OrdinalIgnoreCase) OrElse MyType.Equals(Const_MyType_Custom, StringComparison.OrdinalIgnoreCase), "Unrecognized MyType value in use with the Application property page")
                        End If
                        Return ApplicationTypes.CommandLineApp
                    End If

                Case CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_WinExe)
                    'We must use MyType to differentiate between these two

                    If Const_MyType_WindowsForms.Equals(MyType, StringComparison.OrdinalIgnoreCase) _
                    OrElse Const_MyType_WindowsFormsWithCustomSubMain.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        Return ApplicationTypes.WindowsApp
                    ElseIf Const_MyType_Console.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        Return ApplicationTypes.WindowsService
                    ElseIf Const_MyType_Windows.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        'Backwards compat with earlier projects
                        'Disable until templates are changed: Debug.Fail("MyType value for a Windows Service application has changed - please update MyType in the project file to be ""Console""")
                        Return ApplicationTypes.WindowsService
                        'End Backwards compat
                    Else
                        'Default if anything else to WindowsApp
                        If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                            Debug.Assert(MyType = "" OrElse MyType.Equals(Const_MyType_Empty, StringComparison.OrdinalIgnoreCase) OrElse MyType.Equals(Const_MyType_Custom, StringComparison.OrdinalIgnoreCase), "Unrecognized MyType value in use with the Application property page")
                        End If
                        Return ApplicationTypes.WindowsApp
                    End If

                Case CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Library)
                    If Const_MyType_Windows.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        Return ApplicationTypes.WindowsClassLib
                    ElseIf Const_MyType_WebControl.Equals(MyType, StringComparison.OrdinalIgnoreCase) Then
                        Return ApplicationTypes.WebControl
                    Else
                        'Default if anything else to WindowsClassLib
                        If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                            Debug.Assert(MyType = "" OrElse MyType.Equals(Const_MyType_Empty, StringComparison.OrdinalIgnoreCase) OrElse MyType.Equals(Const_MyType_Custom, StringComparison.OrdinalIgnoreCase), "Unrecognized MyType value in use with the Application property page")
                        End If
                        Return ApplicationTypes.WindowsClassLib
                    End If

                Case Else
                    If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                        Debug.Fail(String.Format("Unexpected Output Type {0}, MyType {1} - Mapping to ApplicationTypes.WindowsApp", OutputType, MyType))
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
        Friend Shared Function OutputTypeFromApplicationType(ByVal AppType As ApplicationTypes) As UInteger
            Select Case AppType

                Case ApplicationTypes.WindowsApp
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_WinExe)
                Case ApplicationTypes.WindowsClassLib
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Library)
                Case ApplicationTypes.CommandLineApp
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Exe)
                Case ApplicationTypes.WindowsService
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_WinExe)
                Case ApplicationTypes.WebControl
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Library)
                Case Else
                    Debug.Fail(String.Format("Unexpected ApplicationType {0}", AppType))
                    Return CUInt(VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Exe)
            End Select
        End Function

        ''' <summary>
        ''' Do we support the My SubMain?
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsMySubMainSupported(ByVal Hierarchy As IVsHierarchy) As Boolean
            Try
                Dim obj As Object = Nothing
                If Hierarchy IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(Hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_BrowseObject, obj))
                End If
                Dim props3 As VSLangProj80.VBProjectProperties3 = TryCast(obj, VSLangProj80.VBProjectProperties3)
                If props3 IsNot Nothing Then
                    Return MyApplicationProperties.ApplicationTypeFromOutputType( _
                                CUInt(props3.OutputType), _
                                props3.MyType) = ApplicationTypes.WindowsApp
                End If
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
            End Try
            Return False
        End Function

#End Region
    End Class ' Class MyApplicationProperties

End Namespace

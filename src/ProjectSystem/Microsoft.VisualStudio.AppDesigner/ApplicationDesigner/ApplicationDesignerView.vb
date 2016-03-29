'******************************************************************************
'* ApplicationDesignerView.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports EnvDTE
Imports Microsoft.VisualStudio.Editors
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.ComponentModel.Design
Imports Microsoft.Internal.Performance
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports OleInterop = Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports VSITEMID=Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' Main UI for Application Designer
    ''' </summary>
    ''' <remarks>
    '''   This class contains the actual top-level UI surface for the resource
    '''   editor.  It is created by ApplicationDesignerRootDesigner.
    '''</remarks>
    Public NotInheritable Class ApplicationDesignerView
        'Inherits UserControl
        Inherits ProjectDesignerTabControl
        Implements IServiceProvider
        Implements IVsSelectionEvents
        Implements IVsRunningDocTableEvents
        Implements IVsRunningDocTableEvents4
        Implements IPropertyPageSiteOwner




#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            '
            'ApplicationDesignerView
            '
            Me.SuspendLayout()
            Me.AutoScroll = False
            Me.BackColor = System.Drawing.SystemColors.ControlLight
            Me.Name = "ApplicationDesignerView"
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

#End Region

#If DEBUG Then
        private Shared ApplicationDesignerViewCount As Integer = 0
        private Shared InstanceCount As Integer = 0
        Private MyInstanceCount As Integer
#End If

        ' explicitly hard-coding these strings since that's what QA's
        '   automation will look for in order to find our various tabs
        '
        Const PROP_PAGE_TAB_PREFIX As String = "PropPage_"
        Const RESOURCES_AUTOMATION_TAB_NAME As String = "Resources"
        Const SETTINGS_AUTOMATION_TAB_NAME As String = "Settings"

        'The designer panels hold the property pages and other designers
        Private m_DesignerPanels As ApplicationDesignerPanel()
        Private m_ActivePanelIndex As Integer = -1

        'App Designer data
        Private m_serviceProvider As IServiceProvider
        Private m_ProjectHierarchy As IVsHierarchy
        Private m_ProjectFilePath As String 'Full path to the project filename

        '*** Project Property related data
        Private m_ProjectObject As Object 'Project's browse object
        Private m_DTEProject As EnvDTE.Project 'Project's DTE object
        Private m_SpecialFiles As IVsProjectSpecialFiles

        'Set to true when the application designer window pane has completely initialized the application designer view
        Private m_InitializationComplete As Boolean

        '*** Monitor Selection
        Private m_monitorSelection As IVsMonitorSelection
        Private m_selectionEventsCookie As UInteger

        'Data shared by all pages hooked up to this project designer (available through GetService)
        Private m_ConfigurationState As PropPageDesigner.ConfigurationState

        'True if we have queued a delayed request to refresh the dirty indicators of any tab
        '  or the project designer.
        Private m_RefreshDirtyIndicatorsQueued As Boolean

        'The state of the project designer dirty indicator last time it was updated
        Private m_LastProjectDesignerDirtyState As Boolean

        'True if SetFrameDirtyIndicator has already been called at least once
        Private m_ProjectDesignerDirtyStateInitialized As Boolean

        'Cookie for IVsRunningDocumentTableEvents
        Private m_rdtEventsCookie As UInteger

        ' Instance of the editors package
        Private m_editorsPackage As IVBPackage

        'True if we're in the process of showing a tab
        Private m_InShowTab As Boolean

        'True if we're already in the middle of showing a panel's WindowFrame
        Private m_isInPanelWindowFrameShow As Boolean

        'True if it's okay for us to activate child panels on WM_SETFOCUS
        Private m_OkayToActivatePanelsOnFocus As Boolean

        ' Helper class to handle font change notifications...
        Private m_FontChangeWatcher As Common.ShellUtil.FontChangeMonitor

        ''' <summary>
        ''' Constructor for the ApplicationDesigner view
        ''' </summary>
        ''' <param name="serviceProvider">The service provider from the root designer.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal serviceProvider As IServiceProvider)
            MyBase.New()
            Me.SuspendLayout()
            MyBase.HostingPanel.SuspendLayout()

            SetSite(serviceProvider)

            'PERF: Set font before InitializeComponent so we don't cause unnecessary layouts (needs the site first)
            m_FontChangeWatcher = New Common.ShellUtil.FontChangeMonitor(Me, Me, True)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

#If DEBUG Then
            AddHandler HostingPanel.Layout, AddressOf HostingPanel_Layout
            AddHandler HostingPanel.SizeChanged, AddressOf HostingPanel_SizeChanged

            ApplicationDesignerViewCount += 1
            InstanceCount += 1
            MyInstanceCount = InstanceCount
            'Need to allow for multiple VB projects in this assert
            'Debug.Assert(ApplicationDesignerViewCount = 1, "Multiple ApplicationDesigners created!")
#End If

            MyBase.HostingPanel.ResumeLayout(False)
            Me.ResumeLayout(False) 'Don't need to lay out yet - we'll do that at the end of AddTabs
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub InitView()
            Dim WindowFrame As IVsWindowFrame
            Dim Value As Object = Nothing
            Dim hr As Integer
            Dim AppDesignerFileName As String = Nothing

            Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerView.InitView()")
            Common.Switches.TracePDPerfBegin("ApplicationDesignerView.InitView")

            ' Whenever we open the project designer, we ping SQM...
            AddSqmItemToStream(VsSqmDataPoint.DATAID_STRM_VB_EDITOR_PROJPROPSHOW, AppDesCommon.SQMData.DEFAULT_PAGE)

            ' Store the vbpackage instance in utils to share within the assembly
            Common.Utils.VBPackageInstance = Package
            WindowFrame = Me.WindowFrame
            Debug.Assert(WindowFrame IsNot Nothing, "WindowFrame is nothing")
            If WindowFrame IsNot Nothing Then

                'Determine the hierarchy for the project that we need to show properties for.

                hr = WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_Hierarchy, Value)
                If NativeMethods.Succeeded(hr) Then
                    Dim Hierarchy As IVsHierarchy = CType(Value, IVsHierarchy)
                    Dim ItemId As UInteger
                    hr = WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_ItemID, Value)
                    ItemId = AppDesCommon.NoOverflowCUInt(Value)

                    'We now have the Hierarchy/ItemId that were stored in the windowframe.
                    '  But this hierarchy is not necessarily that of the project - in fact
                    '  it's generally the hierarchy of the solution (or outer project, in the
                    '  case of nested projects), with the itemid specifying which itemid in 
                    '  the solution corresponds to the project we need to view properties for.

                    'Use GetNestedHierarchy to get the hierarchy of the project within the
                    '  solution or outer project.

                    Dim NestedHierarchy As IntPtr
                    Dim NestedItemId As UInteger
                    If VSErrorHandler.Succeeded(Hierarchy.GetNestedHierarchy(ItemId, GetType(IVsHierarchy).GUID, NestedHierarchy, NestedItemId)) Then
                        'This is the project we want
                        Hierarchy = TryCast(Marshal.GetObjectForIUnknown(NestedHierarchy), IVsHierarchy)
                        Marshal.Release(NestedHierarchy)
                    Else
                        'If GetNestedHierarchy failed, we must already have the hierarchy for the
                        '  project.

                        '(Note: we don't expect this code path anymore, as I believe the project designer 
                        '  is always set up using the solution hierarchy because of the fact that it's
                        '  an editor on the project file.)
                        Debug.Fail("GetNestedHierarchy failed")
                    End If

                    Debug.Assert(TypeOf Hierarchy Is IVsProject, "We didn't get a hierarchy to a project?")

                    m_ProjectHierarchy = Hierarchy
                    m_SpecialFiles = TryCast(m_ProjectHierarchy, IVsProjectSpecialFiles)
                End If

                If m_ProjectHierarchy Is Nothing Then
                    Debug.Fail("Failed to get project hierarchy")
                    Throw New Package.InternalException()
                End If
                Debug.Assert(m_SpecialFiles IsNot Nothing, "Failed to get IVsProjectSpecialFiles for Hierarchy")

                Dim ExtObject As Object = Nothing
                hr = m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ExtObject)
                If NativeMethods.Succeeded(hr) Then
                    Dim DTE As EnvDTE.DTE

                    If TypeOf ExtObject Is EnvDTE.Project Then
                        m_DTEProject = CType(ExtObject, EnvDTE.Project)
                        DTE = DTEProject.DTE
                    End If

                    'Set View title to allow finding designer in test suites
                    'Title should never be seen
                    Me.Text = "AppDesigner+" & DTEProject.Name

                    m_ProjectFilePath = DTEProject.FullName
                End If

                If m_DTEProject Is Nothing Then
                    'Currently we require the DTE Project object.  In the future, if we are allowed in 
                    '  other project types, we'll need to ease this restriction.
                    Debug.Fail("Unable to retrieve DTE Project object")
                    Throw New Package.InternalException
                End If

                m_monitorSelection = CType(GetService(GetType(IVsMonitorSelection)), IVsMonitorSelection)
                If m_monitorSelection IsNot Nothing Then
                    m_monitorSelection.AdviseSelectionEvents(Me, m_selectionEventsCookie)
                End If

                'PERF: Before adding any page panels, we need to activate the main windowframe, so that it 
                '  can get its size/layout set up correctly.
                WindowFrame.Show()

                'Now add the tabs (but don't load them)
                AddTabs(GetPropertyPages())

                'We'll actually show the initial tab later (in OnInitializationComplete), don't need to do
                '  it here.

                Common.Switches.TracePDPerfEnd("ApplicationDesignerView.InitView")
            End If
        End Sub

        Public ReadOnly Property WindowFrame() As IVsWindowFrame
            Get
                Return CType(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the DTE project object associated with this project designer instance.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property DTEProject() As EnvDTE.Project
            Get
                Return m_DTEProject
            End Get
        End Property

        Public ReadOnly Property SpecialFiles() As IVsProjectSpecialFiles
            Get
                Return m_SpecialFiles
            End Get
        End Property

        ''' <summary>
        ''' Instance of the loaded IVBPackage
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Used to persist user data</remarks>
        Private ReadOnly Property Package() As IVBPackage
            Get
                If m_editorsPackage Is Nothing Then
                    Dim shell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                    Dim pPackage As IVsPackage = Nothing
                    If shell IsNot Nothing Then
                        Dim hr As Integer = shell.IsPackageLoaded(New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.VBPackage_GUID), pPackage)
                        Debug.Assert(NativeMethods.Succeeded(hr) AndAlso pPackage IsNot Nothing, "VB editors package not loaded?!?")
                    End If

                    m_editorsPackage = TryCast(pPackage, IVBPackage)
                End If
                Return m_editorsPackage
            End Get
        End Property

        ''' <summary>
        ''' Get/set the last viewed tab of for this application page...
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property LastShownTab() As Integer
            Get
                Dim editorsPackage As IVBPackage = Package
                If editorsPackage IsNot Nothing Then
                    Dim result As Integer = editorsPackage.GetLastShownApplicationDesignerTab(m_ProjectHierarchy)
                    If result >= 0 AndAlso result < m_DesignerPanels.Length Then
                        Return result
                    End If
                End If
                Return 0
            End Get
            Set(ByVal value As Integer)
                Dim editorsPackage As IVBPackage = Package
                If editorsPackage IsNot Nothing Then
                    editorsPackage.SetLastShownApplicationDesignerTab(m_ProjectHierarchy, value)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Should be called to let the project designer know it's shutting down and should no longer try
        '''   to activate child pages
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub NotifyShuttingDown()
            Common.Switches.TracePDFocus(TraceLevel.Info, "NotifyShuttingDown")
            m_OkayToActivatePanelsOnFocus = False
        End Sub


        ''' <summary>
        ''' Helper to determine if the docdata (designated by DocCookie) is dirty
        ''' </summary>
        ''' <param name="DocCookie"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <returns></returns>
        ''' <remarks>Used by view to prompt for saving changes</remarks>
        Private Function IsDocDataDirty(ByVal DocCookie As UInteger, ByRef Hierarchy As IVsHierarchy, ByRef ItemId As UInteger) As Boolean
            Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
            Dim hr As Integer
            Dim flags, readLocks, editLocks As UInteger
            Dim fileName As String = Nothing
            Dim localPunk As IntPtr = IntPtr.Zero

            If rdt IsNot Nothing Then
                Try
                    hr = rdt.GetDocumentInfo(DocCookie, flags, readLocks, editLocks, fileName, Hierarchy, ItemId, localPunk)
                    If NativeMethods.Succeeded(hr) Then
                        Dim obj As Object
                        Debug.Assert(localPunk <> IntPtr.Zero, "IUnknown for document is NULL")
                        obj = Marshal.GetObjectForIUnknown(localPunk)
                        If TypeOf obj Is IVsPersistDocData Then
                            Dim dirty As Integer
                            If VSErrorHandler.Succeeded(TryCast(obj, IVsPersistDocData).IsDocDataDirty(dirty)) Then
                                Return (dirty <> 0)
                            End If
                        ElseIf TypeOf obj Is IPersistFileFormat Then
                            Dim dirty As Integer
                            If VSErrorHandler.Succeeded(TryCast(obj, IPersistFileFormat).IsDirty(dirty)) Then
                                Return dirty <> 0
                            End If
                        Else
                            Debug.Fail("Unable to determine if DocData is dirty - doesn't support an interface we recognize")
                        End If
                    End If
                Finally
                    If localPunk <> IntPtr.Zero Then
                        Marshal.Release(localPunk)
                    End If
                End Try
            End If
            Return False
        End Function

        ''' <summary>
        ''' Populates the list of documents based on flags argument
        ''' </summary>
        ''' <param name="flags"></param>
        ''' <value></value>
        ''' <remarks>Used to build table of documents to save</remarks>
        Public ReadOnly Property GetSaveTreeItems(ByVal flags As __VSRDTSAVEOPTIONS) As VSSAVETREEITEM()
            Get
                Dim items As VSSAVETREEITEM() = New VSSAVETREEITEM(m_DesignerPanels.Length - 1) {}
                Dim Count As Integer
                Dim DocCookie As UInteger
                Dim Hierarchy As IVsHierarchy = Nothing
                Dim ItemId As UInteger

                If m_DesignerPanels IsNot Nothing Then
                    For Index As Integer = 0 To m_DesignerPanels.Length - 1
                        'If the designer was opened, then add it to the list for saving
                        If m_DesignerPanels(Index) IsNot Nothing AndAlso _
                            m_DesignerPanels(Index).VsWindowFrame IsNot Nothing Then
                            DocCookie = m_DesignerPanels(Index).DocCookie
                            If IsDocDataDirty(DocCookie, Hierarchy, ItemId) Then
                                If Count >= items.Length Then
                                    ReDim Preserve items(Count)
                                End If
                                items(Count).docCookie = DocCookie
                                items(Count).grfSave = CUInt(flags)
                                items(Count).itemid = ItemId
                                items(Count).pHier = Hierarchy
                                Count += 1
                            End If
                        End If

#If False Then 'This interface is currently disabled, no clients using it, see PropPage.vb
                        'Property pages may have DocDatas that should be included in the list
                        If TypeOf m_DesignerPanels(Index).DocView Is PropPageDesigner.PropPageDesignerView Then
                            Dim PropPage As OleInterop.IPropertyPage = TryCast(m_DesignerPanels(Index).DocView, PropPageDesigner.PropPageDesignerView).PropPage
                            If TypeOf PropPage Is PropertyPages.IVsDocDataContainer Then
                                Dim DocCookies As UInteger()
                                DocCookies = TryCast(PropPage, PropertyPages.IVsDocDataContainer).GetDocDataCookies()
                                If DocCookies IsNot Nothing AndAlso DocCookies.Length > 0 Then
                                    For Each DocCookie In DocCookies
                                        If IsDocDataDirty(DocCookie, Hierarchy, ItemId) Then
                                            If Count >= items.Length Then
                                                ReDim Preserve items(Count)
                                            End If
                                            items(Count).docCookie = DocCookie
                                            items(Count).grfSave = CUInt(flags)
                                            items(Count).itemid = ItemId
                                            items(Count).pHier = Hierarchy
                                            Count += 1
                                        End If
                                    Next
                                End If
                            End If
                        End If
#End If
                    Next
                End If

                ReDim Preserve items(Count - 1)
                Return items
            End Get
        End Property

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
#If DEBUG Then
                RemoveHandler HostingPanel.Layout, AddressOf HostingPanel_Layout
#End If
                If m_monitorSelection IsNot Nothing AndAlso m_selectionEventsCookie <> 0 Then
                    m_monitorSelection.UnadviseSelectionEvents(m_selectionEventsCookie)
                    m_monitorSelection = Nothing
                    m_selectionEventsCookie = 0
                End If
                UnadviseRunningDocTableEvents()

                If m_FontChangeWatcher IsNot Nothing Then
                    m_FontChangeWatcher.Dispose()
                    m_FontChangeWatcher = Nothing
                End If

                If components IsNot Nothing Then
                    components.Dispose()
                End If

                If m_DesignerPanels IsNot Nothing Then
                    For Index As Integer = 0 To m_DesignerPanels.Length - 1
                        If m_DesignerPanels(Index) IsNot Nothing Then
                            Try
                                Dim Panel As ApplicationDesignerPanel = m_DesignerPanels(Index)
                                m_DesignerPanels(Index) = Nothing
                                Panel.Dispose()
                            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                                Trace.WriteLine("Exception trying to dispose ApplicationDesignerPanel: " & vbCrLf & ex.ToString())
                                Debug.Fail("Exception trying to dispose ApplicationDesignerPanel: " & ex.ToString())
                            End Try
                        End If
                    Next
                End If

                If m_ConfigurationState IsNot Nothing Then
                    m_ConfigurationState.Dispose()
                    m_ConfigurationState = Nothing
                End If

#If DEBUG Then
                ApplicationDesignerViewCount -= 1
#End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        ''' <summary>
        ''' Creates a set of all property pages that the project wants us to display.
        ''' Does *not* load them now, but waits to load them on demand.
        ''' </summary>
        ''' <returns>An array of PropertyPageInfo with the loaded property page information.</returns>
        ''' <remarks></remarks>
        Public Function GetPropertyPages() As PropertyPageInfo()
            Dim LocalRegistry As ILocalRegistry
            LocalRegistry = CType(GetService(GetType(ILocalRegistry)), ILocalRegistry)

            Debug.Assert(LocalRegistry IsNot Nothing, "Unabled to obtain ILocalRegistry")

            Dim ConfigPageGuids As Guid() = GetPageGuids(GetActiveConfigBrowseObject())
            Dim CommonPageGuids As Guid() = GetPageGuids(GetProjectBrowseObject())

#If DEBUG Then
            'Add the VB WPF Application property page to all projects, even non-WPF projects.  This allows for debugging
            '  this page without the new WPF flavor
            If Common.Switches.PDAddVBWPFApplicationPageToAllProjects.Enabled Then
                Dim commonList As New List(Of Guid)
                commonList.AddRange(CommonPageGuids)
                Dim wpfPage As Guid = New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.WPFApplicationWithMyPropPageComClass_GUID)
                commonList.Add(wpfPage)
                CommonPageGuids = commonList.ToArray()
            End If
#End If

            'Create a combined array list of the property page guids
            Dim PropertyPages(CommonPageGuids.Length + ConfigPageGuids.Length - 1) As PropertyPageInfo

            For Index As Integer = 0 To PropertyPages.Length - 1
                Dim PageGuid As Guid
                Dim IsConfigPage As Boolean
                With PropertyPages(Index)
                    If Index < CommonPageGuids.Length Then
                        PageGuid = CommonPageGuids(Index)
                        IsConfigPage = False
                    Else
                        PageGuid = ConfigPageGuids(Index - CommonPageGuids.Length)
                        IsConfigPage = True
                    End If
                End With
                PropertyPages(Index) = New PropertyPageInfo(Me, PageGuid, IsConfigPage)
            Next

            Return PropertyPages
        End Function


#If 0 Then
        ''' <summary>
        ''' Get the max property page size based on the reported page infos
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property GetMaxPropPageSize() As Drawing.Size
            Get
                Dim MaxSize As Drawing.Size
                Dim OleSize As OleInterop.SIZE

                If m_DesignerPanels IsNot Nothing Then
                    For Index As Integer = 0 To m_DesignerPanels.Length - 1
                        If m_DesignerPanels(Index) Is Nothing AndAlso m_DesignerPanels(Index).IsPropertyPage Then
                            OleSize = m_DesignerPanels(Index).PropertyPageInfo.Info.SIZE
                            If OleSize.cx > MaxSize.Width Then
                                MaxSize.Width = OleSize.cx
                            End If
                            If OleSize.cy > MaxSize.Height Then
                                MaxSize.Height = OleSize.cy
                            End If
                        End If
                    Next
                End If
                Return MaxSize
            End Get
        End Property
#End If



        Private Function GetProjectBrowseObject() As Object
            If m_ProjectObject Is Nothing Then
                Dim BrowseObject As Object = Nothing
                VSErrorHandler.ThrowOnFailure(m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_BrowseObject, BrowseObject))
                m_ProjectObject = BrowseObject
            End If
            Return m_ProjectObject
        End Function

        Private m_VsCfgProvider As IVsCfgProvider2
        Private ReadOnly Property VsCfgProvider() As IVsCfgProvider2
            Get
                If m_VsCfgProvider Is Nothing Then
                    Dim Value As Object = Nothing

                    VSErrorHandler.ThrowOnFailure(m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, Value))

                    m_VsCfgProvider = CType(Value, IVsCfgProvider2)
                End If
                Return m_VsCfgProvider
            End Get
        End Property

        ''' <summary>
        ''' Obtain the current Config browse object from the project hierarchy
        ''' </summary>
        ''' <returns>The browse object for the currently selected configuration.</returns>
        ''' <remarks></remarks>
        Private Function GetActiveConfigBrowseObject() As Object
            Return AppDesCommon.DTEUtils.GetActiveConfiguration(DTEProject, VsCfgProvider)
        End Function


        Public Property ActiveView() As Guid
            Get
                If m_ActivePanelIndex < 0 OrElse m_ActivePanelIndex >= m_DesignerPanels.Length OrElse m_DesignerPanels(m_ActivePanelIndex) Is Nothing Then
                    Return Guid.Empty
                End If
                Dim Panel As ApplicationDesignerPanel = m_DesignerPanels(m_ActivePanelIndex)
                'Use ActualGuid so that for property pages we return the property page's guid 
                '  instead of the PropPageDesigner's guid
                Return Panel.ActualGuid
            End Get

            Set(ByVal Value As Guid)
                Common.Switches.TracePDFocus(TraceLevel.Info, "ApplicationDesignerView: set_ActiveView")
                'Find the guid and switch to that tab
                'Keep the current tab if guid not found
                For Index As Integer = 0 To m_DesignerPanels.Length - 1
                    'If this is a property page, check the property page guid (thus using ActualGuid)
                    If Value.Equals(m_DesignerPanels(Index).ActualGuid) Then
                        ShowTab(Index)
                        Return
                    End If
                Next

                'Guid not found - keep current tab
                ShowTab(m_ActivePanelIndex)
            End Set
        End Property


        ''' <summary>
        ''' Determines if the given tab should be added to the designer, and if so, returns
        '''   the ProjectItem corresponding to it.
        ''' </summary>
        ''' <param name="FileId">The FileId to use as a parameter to IVsProjectSpecialFiles</param>
        ''' <param name="TabSupported">[Out] True if the given tab is supported by the project</param>
        ''' <param name="FileExists">[Out] True if the given tab's file actually exists currently.  Always false if Not TabSupported.</param>
        ''' <param name="FullPathToProjectItem">[Out] The full path to the given tab's file.  If TabSupported is True but FileExists is False, this value indicates the preferred file and location for the project for this special file.</param>
        ''' <remarks></remarks>
        Private Sub CheckIfTabSupported(ByVal FileId As Integer, ByRef TabSupported As Boolean, ByRef FileExists As Boolean, ByRef FullPathToProjectItem As String)
            TabSupported = False
            FileExists = False
            FullPathToProjectItem = Nothing

            If m_SpecialFiles Is Nothing Then
                Debug.Fail("IVsProjectSpecialFiles is Nothing - can't look for the given tab's file - tab will be hidden")
                Return
            End If

            Dim ItemId As UInteger
            Dim SpecialFilePath As String = Nothing
            Dim hr As Integer = m_SpecialFiles.GetFile(FileId, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
            If VSErrorHandler.Succeeded(hr) Then
                'Yes, the tab is supported
                TabSupported = True
                FullPathToProjectItem = SpecialFilePath

                'Does the file actually exist (both in the project and on disk)?
                If ItemId <> VSITEMID.NIL AndAlso SpecialFilePath <> "" AndAlso IO.File.Exists(SpecialFilePath) Then
                    'Yes, the file exists
                    FileExists = True
                End If
            End If
        End Sub


        ''' <summary>
        ''' Adds the tab buttons for the App Designer
        ''' </summary>
        ''' <param name="PropertyPages">The list of property pages to display</param>
        ''' <remarks></remarks>
        Private Sub AddTabs(ByVal PropertyPages() As PropertyPageInfo)
            SuspendLayout()
            HostingPanel.SuspendLayout()

            HostingPanel.Controls.Clear()
            ClearTabs()

            'Categories are Common property pages + Config Property pages + Resources + Settings
            Dim TabCount As Integer
            Dim AppDesignerItems As New ArrayList '(Of String [path + filename]) 'Resources, Settings, etc (not property pages)

            'Add the resources tab
            Dim ResourcesTabSupported, DefaultResourcesExist As Boolean
            Dim DefaultResourcesPath As String = Nothing
            CheckIfTabSupported(__PSFFILEID2.PSFFILEID_AssemblyResource, ResourcesTabSupported, DefaultResourcesExist, DefaultResourcesPath)
            If ResourcesTabSupported Then
                AppDesignerItems.Add(DefaultResourcesPath)
            End If

            'Add the settings tab
            Dim DefaultSettingsSupported, DefaultSettingsExist As Boolean
            Dim DefaultSettingsPath As String = Nothing
            CheckIfTabSupported(__PSFFILEID2.PSFFILEID_AppSettings, DefaultSettingsSupported, DefaultSettingsExist, DefaultSettingsPath)
            If DefaultSettingsSupported Then
                AppDesignerItems.Add(DefaultSettingsPath)
            End If

            'Total tab count
            TabCount = PropertyPages.Length + AppDesignerItems.Count 'Resource Designer + Settings Designer + property pages

            m_DesignerPanels = New ApplicationDesignerPanel(TabCount - 1) {}

            'Create the designer panels
            For Index As Integer = 0 To TabCount - 1

                Dim DesignerPanel As ApplicationDesignerPanel
                If Index < PropertyPages.Length Then
                    'This is a property page
                    Debug.Assert(PropertyPages(Index) IsNot Nothing)
                    DesignerPanel = New ApplicationDesignerPanel(Me, m_ProjectHierarchy, CUInt(Index), PropertyPages(Index))
                Else
                    DesignerPanel = New ApplicationDesignerPanel(Me, m_ProjectHierarchy, CUInt(Index))
                End If

                With DesignerPanel
                    .SuspendLayout()
                    .Dock = System.Windows.Forms.DockStyle.Fill
                    .Location = New System.Drawing.Point(0, 0)
                    .Name = "DesignerPanel" & Index
                    .Size = New System.Drawing.Size(555, 392)
                    .TabIndex = 1
                    .Dock = DockStyle.Fill
                    .Font = MyBase.HostingPanel.Font 'PERF: Prepopulating with the font means you reduce the number of OnFontChanged that occur when child panels are added/removed from the parent
                    .Visible = False 'Don't make visible until that particular tab is selected

                    'Note: 
                    ' tab-titles are display names the user sees, TabAutomationNames are
                    '   for QA automation (they should not be localized)
                    '

                    If .PropertyPageInfo IsNot Nothing Then
                        'It must be a property page tab
                        .MkDocument = m_ProjectFilePath & ";" & PropertyPages(Index).Guid.ToString()
                        .PhysicalView = PropertyPages(Index).Guid.ToString()
                        .EditFlags = CUInt(_VSRDTFLAGS.RDT_VirtualDocument Or _VSRDTFLAGS.RDT_DontAddToMRU)

                        'PERF: This property call will attempt to retrieve a cached version of the title 
                        '  to avoid having to instantiate the COM object for the property page until
                        '  the user actually browses to that page.
                        .EditorCaption = PropertyPages(Index).Title

                        .TabTitle = .EditorCaption
                        .TabAutomationName = PROP_PAGE_TAB_PREFIX & PropertyPages(Index).Guid.ToString("N")

                    Else
                        Dim FileName As String = DirectCast(AppDesignerItems(Index - PropertyPages.Length), String)

                        .EditFlags = CUInt(_VSRDTFLAGS.RDT_DontAddToMRU)
                        If System.String.Compare(Microsoft.VisualBasic.Right(FileName, 5), ".resx", StringComparison.OrdinalIgnoreCase) = 0 Then
                            'Add .resx file with a known editor so user config cannot change
                            .EditorGuid = New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.ResourceEditorFactory_GUID)
                            .EditorCaption = SR.GetString(SR.APPDES_ResourceTabTitle)
                            .TabAutomationName = RESOURCES_AUTOMATION_TAB_NAME

                            'If the resx file doesn't actually exist yet, we have to display the "Click here
                            '  to create it" message instead of the actual editor.
                            If DefaultResourcesExist Then
                                'We can't set .MkDocument directly from FileName, because the FileName returned by 
                                '  IVsProjectSpecialFile might change before we try to open it (e.g., when a ZIP
                                '  project is saved).  Instead, delay fetching of the filename via 
                                '  SpecialFileCustomDocumentMonikerProvider).
                                .CustomMkDocumentProvider = New SpecialFileCustomDocumentMonikerProvider(Me, __PSFFILEID2.PSFFILEID_AssemblyResource)
                            Else
                                .CustomViewProvider = New SpecialFileCustomViewProvider(Me, DesignerPanel, __PSFFILEID2.PSFFILEID_AssemblyResource, SR.GetString(SR.APPDES_ClickHereCreateResx))
                            End If
                        ElseIf System.String.Compare(Microsoft.VisualBasic.Right(FileName, 9), ".settings", StringComparison.OrdinalIgnoreCase) = 0 Then
                            'Add .settings file with a known editor so user config cannot change
                            .EditorGuid = New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.SettingsDesignerEditorFactory_GUID)
                            .EditorCaption = SR.GetString(SR.APPDES_SettingsTabTitle)
                            .TabAutomationName = SETTINGS_AUTOMATION_TAB_NAME

                            'If the settings file doesn't actually exist yet, we have to display the "Click here
                            '  to create it" message instead of the actual editor.
                            If DefaultSettingsExist Then
                                'We can't set .MkDocument directly from FileName, because the FileName returned by 
                                '  IVsProjectSpecialFile might change before we try to open it (e.g., when a ZIP
                                '  project is saved).  Instead, delay fetching of the filename via 
                                '  SpecialFileCustomDocumentMonikerProvider).
                                .CustomMkDocumentProvider = New SpecialFileCustomDocumentMonikerProvider(Me, __PSFFILEID2.PSFFILEID_AppSettings)
                            Else
                                .CustomViewProvider = New SpecialFileCustomViewProvider(Me, DesignerPanel, __PSFFILEID2.PSFFILEID_AppSettings, SR.GetString(SR.APPDES_ClickHereCreateSettings))
                            End If
                        Else
                            Debug.Fail("Unexpected file in list of intended tabs")
                        End If

                        .TabTitle = .EditorCaption
                    End If

                    Debug.Assert(.TabTitle <> "" OrElse (.PropertyPageInfo IsNot Nothing AndAlso .PropertyPageInfo.LoadException IsNot Nothing), "Why is the tab title text empty?")
                    Debug.Assert(.TabAutomationName <> "" OrElse (.PropertyPageInfo IsNot Nothing AndAlso .PropertyPageInfo.LoadException IsNot Nothing), "Why is the tab automation name text empty?")

                    .ResumeLayout(False) 'Controls.Add below will call PerformLayout, so no need to do it here.

                    'Don't actually add the panel to the HostingPanel yet...
                    m_DesignerPanels(Index) = DesignerPanel
                End With
            Next

            'Order the tabs
            OrderTabs(m_DesignerPanels)

            'PERF: Tell the tab control how many panels there are and what their titles are before
            '  adding the AppicationDesignerPanels, so that the final size of the HostingPanel is
            '  known.
            For i As Integer = 0 To m_DesignerPanels.GetUpperBound(0)
                Dim iTab As Integer = MyBase.AddTab(m_DesignerPanels(i).TabTitle, m_DesignerPanels(i).TabAutomationName)
                MyBase.GetTabButton(iTab).TabStop = False 'Keep from setting focus to the tabs when they're clicked so we don't fire OnItemGotFocus
            Next

            'Now that all the tab titles have been figured out, we can go ahead and add all the 
            '  panels to the HostingPanel's control array.  This will cause PerformLayout on all
            '  the panels.  We couldn't do it before adding the tab titles because they affect the 
            '  size of the HostingPanel.  Now we should have a stable size for the hosting panel.
            For Index As Integer = 0 To TabCount - 1
                Dim DesignerPanel As ApplicationDesignerPanel = m_DesignerPanels(Index)
                MyBase.HostingPanel.Controls.Add(DesignerPanel)
            Next

            HostingPanel.ResumeLayout(False)
            ResumeLayout(True)
        End Sub

        ''' <summary>
        ''' Re-orders the given set of designer panels according to what we want.
        ''' </summary>
        ''' <param name="DesignerPanels">List of designer panels to be re-ordered in-place.</param>
        ''' <remarks>
        ''' Recognized tabs will be placed in a specific order.  All others will be placed at the end,
        '''   in the order passed in to this method.
        ''' </remarks>
        Private Sub OrderTabs(ByVal DesignerPanels() As ApplicationDesignerPanel)

            'A default list of known editor guids and the order we want when they appear.  We only
            '  use this list if we can't get the order from the IVsHierarchy for some reason.
            Dim DefaultDesiredOrder() As Guid = { _
                AppDesCommon.KnownPropertyPageGuids.GuidApplicationPage_VB, _
                AppDesCommon.KnownPropertyPageGuids.GuidApplicationPage_CS, _
                AppDesCommon.KnownPropertyPageGuids.GuidApplicationPage_JS, _
                AppDesCommon.KnownPropertyPageGuids.GuidCompilePage_VB, _
                AppDesCommon.KnownPropertyPageGuids.GuidBuildPage_CS, _
                AppDesCommon.KnownPropertyPageGuids.GuidBuildPage_JS, _
                AppDesCommon.KnownPropertyPageGuids.GuidBuildEventsPage, _
                AppDesCommon.KnownPropertyPageGuids.GuidDebugPage, _
                AppDesCommon.KnownPropertyPageGuids.GuidDebugPage_VSD, _
                AppDesCommon.KnownPropertyPageGuids.GuidReferencesPage_VB, _
                New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.SettingsDesignerEditorFactory_GUID), _
                AppDesCommon.KnownPropertyPageGuids.GuidServicesPropPage, _
                New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.ResourceEditorFactory_GUID), _
                AppDesCommon.KnownPropertyPageGuids.GuidReferencePathsPage, _
                AppDesCommon.KnownPropertyPageGuids.GuidSigningPage, _
                AppDesCommon.KnownPropertyPageGuids.GuidSecurityPage, _
                AppDesCommon.KnownPropertyPageGuids.GuidPublishPage _
            }
            Dim DesiredOrder() As Guid = DefaultDesiredOrder

            'Get the requested ordering of project designer pages from the IVsHierarchy.
            Dim CLSIDListObject As Object = Nothing
            Dim hr As Integer = m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID2.VSHPROPID_PriorityPropertyPagesCLSIDList, CLSIDListObject)
            If VSErrorHandler.Succeeded(hr) AndAlso TypeOf CLSIDListObject Is String Then
                Dim CLSIDListString As String = DirectCast(CLSIDListObject, String)
                Dim CLSIDList As New List(Of Guid)
                For Each CLSID As String In CLSIDListString.Split(";"c)
                    If CLSID <> "" Then
                        CLSID = CLSID.Trim()
                        Try
                            Dim Guid As New Guid(CLSID)
                            CLSIDList.Add(Guid)
                        Catch ex As System.FormatException
                            Debug.Fail("VSHPROPID_PriorityPropertyPagesCLSIDList returned a string in a bad format")
                        End Try
                    End If
                Next

                DesiredOrder = CLSIDList.ToArray()
                Debug.Assert(DesiredOrder.Length > 0, "Got an empty list from VSHPROPID_PriorityPropertyPagesCLSIDList")
            Else
                Debug.Fail("Unable to get VSHPROPID_PriorityPropertyPagesCLSIDList from hierarchy")
            End If

            Dim OldOrder As New ArrayList(DesignerPanels.Length) '(Of ApplicationDesignerPanel)
            Dim NewOrder As New ArrayList(DesignerPanels.Length) '(Of ApplicationDesignerPanel)

            'Initialize OldOrder
            OldOrder.AddRange(DesignerPanels)

            'First in the new order come the pages found in DesiredOrder, in exactly that order
            For Each Guid As Guid In DesiredOrder
                For PanelIndex As Integer = 0 To OldOrder.Count - 1
                    Dim Panel As ApplicationDesignerPanel = DirectCast(OldOrder(PanelIndex), ApplicationDesignerPanel)
                    Debug.Assert(Panel IsNot Nothing)
                    If Panel IsNot Nothing AndAlso Panel.ActualGuid.Equals(Guid) Then
                        'Found one in the preferred order.
                        NewOrder.Add(Panel)
                        OldOrder.RemoveAt(PanelIndex)
                        Exit For
                    End If
                Next
            Next

            'At the end of the list, add all other panels, in the order in which they were passed in
            '  to this function
            For Each Panel As ApplicationDesignerPanel In OldOrder
                NewOrder.Add(Panel)
            Next

            'Re-order the original list
            Debug.Assert(NewOrder.Count = DesignerPanels.Length, "Ordering didn't work")
            For i As Integer = 0 To NewOrder.Count - 1
                DesignerPanels(i) = DirectCast(NewOrder(i), ApplicationDesignerPanel)
            Next
        End Sub

        ''' <summary>
        ''' Gets the guid list from the specified object
        ''' </summary>
        ''' <param name="BrowseObject"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetPageGuids(ByVal BrowseObject As Object) As Guid()
            If TypeOf BrowseObject Is IVsSpecifyProjectDesignerPages Then
                Dim CauuidPages() As OleInterop.CAUUID = New OleInterop.CAUUID(1) {}
                Try
                    CType(BrowseObject, IVsSpecifyProjectDesignerPages).GetProjectDesignerPages(CauuidPages)
                    Return CAUUIDMarshaler.GetData(CauuidPages(0))
                Finally
                    If Not CauuidPages(0).pElems.Equals(IntPtr.Zero) Then
                        Marshal.FreeCoTaskMem(CauuidPages(0).pElems)
                    End If
                End Try
            End If
            Return New Guid() {}
        End Function

        Private Sub SetSite(ByVal serviceProvider As IServiceProvider)
            m_serviceProvider = serviceProvider

            'Set the provider into the base tab control so it can get access to fonts and colors
            MyBase.ServiceProvider = m_serviceProvider
        End Sub

        Public Shadows Function GetService(ByVal ServiceType As Type) As Object Implements System.IServiceProvider.GetService, IPropertyPageSiteOwner.GetService
            Dim Service As Object

            If ServiceType Is GetType(PropPageDesigner.ConfigurationState) Then
                If m_ConfigurationState Is Nothing Then
                    m_ConfigurationState = New PropPageDesigner.ConfigurationState(m_DTEProject, m_ProjectHierarchy, Me)
                End If
                Return m_ConfigurationState
            End If

            If ServiceType Is GetType(ApplicationDesignerView) Then
                'Allows the PropPageDesignerView to access the ApplicationDesignerView
                Return Me
            End If

            Service = m_serviceProvider.GetService(ServiceType)
            Return Service
        End Function

        ''' <summary>
        ''' Called by designer when changes need to be persisted
        ''' </summary>
        ''' <returns>Return true if success </returns>
        ''' <remarks></remarks>
        Public Function CommitAnyPendingChanges() As Boolean
            If m_ActivePanelIndex >= 0 Then
                Dim currentPanel As ApplicationDesignerPanel = m_DesignerPanels(m_ActivePanelIndex)
                If currentPanel IsNot Nothing Then
                    Return currentPanel.CommitPendingEdit()
                End If
            End If
            Return True
        End Function


        ''' <summary>
        ''' Show the requested tab
        ''' </summary>
        ''' <param name="Index">Index of Designer panel to show</param>
        ''' <param name="ForceShow">Forces the Show code to go through, even if the current panel is the same as the one requested.</param>
        ''' <remarks></remarks>
        Private Sub ShowTab(ByVal Index As Integer, Optional ByVal ForceShow As Boolean = False)

            Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerView.ShowTab(" & Index & ")")
            If m_InShowTab Then
                Common.Switches.TracePDFocus(TraceLevel.Warning, " ...Already in ShowTab")
                Exit Sub
            End If

            m_InShowTab = True
            Try
                If m_ActivePanelIndex = Index AndAlso Not ForceShow Then
                    'PERF: PERFORMANCE SENSITIVE CODE: No need to go through the designer creation again if we're already on the
                    '  correct page.
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... Ignoring because Index is already " & Index & " and ForceShow=False")
                    Return
                End If

                ' If current Page can not commit pending changes, we shouldn't go away (but only if we're actually changing tabs)
                If (Index <> m_ActivePanelIndex) AndAlso Not CommitAnyPendingChanges() Then
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... Ignoring because CommitAnyPendingChanges returned False")
                    Return
                End If

                Common.Switches.TracePDPerfBegin("ApplicationDesignerView.ShowTab")
                Common.Switches.TracePDFocus(TraceLevel.Error, "CodeMarker: perfMSVSEditorsShowTabBegin")
                Common.Switches.TracePDPerf("CodeMarker: perfMSVSEditorsShowTabBegin")
                Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(CodeMarkerEvent.perfMSVSEditorsShowTabBegin)

                Dim NewCurrentPanel As ApplicationDesignerPanel = m_DesignerPanels(Index)
                Dim ErrorMessage As String = Nothing
                Dim DesignerAlreadyShownOnCreation As Boolean = False

#If DEBUG Then
                NewCurrentPanel.m_Debug_cWindowFrameShow = 0
                NewCurrentPanel.m_Debug_cWindowFrameBoundsUpdated = 0
#End If
                Try
                    If m_ActivePanelIndex <> Index Then
                        LastShownTab = Index
                        Dim PageId As Byte = AppDesCommon.Utils.SQMData.PageGuidToId(NewCurrentPanel.ActualGuid)
#If DEBUG Then
                        If PageId = AppDesCommon.SQMData.UNKNOWN_PAGE Then
#If 0 Then
                            Debug.Fail("We've encountered a property page GUID we don't recognize.  This is fine, but it means that this page " _
                            & "will show up as UNKNOWN_PAGE in SQM data.  Please enter a pri 3 bug with the page name, context and GUID against " _
                            & "the path ""\VBPU\Designers\App Designer""." _
                            & vbCrLf & "Property page caption: " & NewCurrentPanel.EditorCaption _
                            & vbCrLf & "Property page guid: " & NewCurrentPanel.ActualGuid.ToString())
#End If
                        End If
#End If
                        AddSqmItemToStream(VsSqmDataPoint.DATAID_STRM_VB_EDITOR_PROJPROPSHOW, PageId)
                    End If

                    m_ActivePanelIndex = Index

                    'Hide any visible panel that is not the currently selected panel
                    For Each Panel As ApplicationDesignerPanel In m_DesignerPanels
                        If Panel IsNot NewCurrentPanel Then
                            Panel.ShowDesigner(False)
                        End If
                    Next

                    'Designer not yet created, do special handling for property pages
                    If NewCurrentPanel.DocData Is Nothing Then
                        Common.Switches.TracePDFocus(TraceLevel.Info, "  ... Designer not yet created")
                        With NewCurrentPanel
                            If .IsPropertyPage Then
                                'This is a property page.  Need to do some special handling
                                Common.Switches.TracePDFocus(TraceLevel.Info, "  ... Special property page handling")

                                If .PropertyPageInfo.LoadException IsNot Nothing Then
                                    Common.Switches.TracePDFocus(TraceLevel.Error, "  ... LoadException: " & .PropertyPageInfo.LoadException.Message)
                                    ErrorMessage = SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & .PropertyPageInfo.LoadException.Message
                                ElseIf .PropertyPageInfo.ComPropPageInstance Is Nothing OrElse .PropertyPageInfo.Site Is Nothing Then
                                    Common.Switches.TracePDFocus(TraceLevel.Info, "  ... ComPropPageInstance or the site is Nothing")
                                    ErrorMessage = SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & .PropertyPageInfo.Guid.ToString()
                                Else
                                    Common.Switches.TracePDFocus(TraceLevel.Info, "  ... Calling CreateDesigner")
                                    HostingPanel.SuspendLayout()
                                    Debug.Assert(HostingPanel.Size.Width <> 0 AndAlso HostingPanel.Size.Height <> 0)
                                    Try
                                        .CreateDesigner()
                                    Finally
                                        HostingPanel.ResumeLayout(True)
                                    End Try

                                    'PERF: No need to call ShowDesigner 'cause the window frame's already been shown through the creation
                                    DesignerAlreadyShownOnCreation = True

                                    'ActivatePage is required because a IPropertyPage.Show will
                                    'fail if IPropertyPage.Activate has not been done first
                                    Dim PropPageView As PropPageDesigner.PropPageDesignerView
                                    PropPageView = TryCast(.DocView, PropPageDesigner.PropPageDesignerView)
                                    If PropPageView IsNot Nothing Then
                                        PropPageView.Init(DTEProject, .PropertyPageInfo.ComPropPageInstance, .PropertyPageInfo.Site, m_ProjectHierarchy, .PropertyPageInfo.IsConfigPage)
                                    Else
                                        'Must have had error loading
                                    End If

                                End If

                                'Because we may have previously retrieved the tab title from a cache, it 
                                '  is possible (though it shouldn't generally happen) that the title
                                '  is different from the cache.  Now that the property page may have been
                                '  loaded, set the text again to its official non-cached value.
                                .TabTitle = .PropertyPageInfo.Title
                                GetTabButton(Index).Text = .PropertyPageInfo.Title
                            Else
                                'No special handling for non-property pages
                            End If

                        End With
                    End If
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    ErrorMessage = AppDesCommon.DebugMessageFromException(ex)
                End Try

                'Now make the selected design panel visible
                Try
                    If TypeOf NewCurrentPanel.CustomViewProvider Is ErrorControlCustomViewProvider Then
                        'The page is showing an error control.  Let's try again to load the real docview into it.
                        NewCurrentPanel.CustomViewProvider.CloseView()
                        NewCurrentPanel.CustomViewProvider = Nothing
                    End If
                    If Not DesignerAlreadyShownOnCreation Then
                        NewCurrentPanel.ShowDesigner(True)
                    End If

#If DEBUG Then
                    If NewCurrentPanel.CustomViewProvider IsNot Nothing Then
                        'New panel has a custom view provider, so IVsWindowFrame.Show won’t have been called.
                    Else
                        If NewCurrentPanel.PropertyPageInfo IsNot Nothing AndAlso NewCurrentPanel.PropertyPageInfo.LoadException IsNot Nothing Then
                            'There was an error loading the page, so IVsWindowFrame.Show() would not have been called
                        Else
                            'IVsWindowFrame.Show() should have been called
                            Debug.Assert(NewCurrentPanel.m_Debug_cWindowFrameShow > 0, "New page panel didn't get activated?")
                        End If
                    End If

                    Debug.Assert(NewCurrentPanel.m_Debug_cWindowFrameShow <= 1, "PERFORMANCE/FLICKER WARNING: More than one IVsWindowFrame.Activate() occurred")
                    Debug.Assert(NewCurrentPanel.m_Debug_cWindowFrameBoundsUpdated <= 1, "PERFORMANCE/FLICKER WARNING: Window frame bounds were updated more than once")
#End If

                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    If ErrorMessage = "" Then
                        ErrorMessage = SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & Common.DebugMessageFromException(ex)
                    End If
                End Try

                Me.SelectedIndex = Index

                If ErrorMessage <> "" Then
                    Try
                        'Display the error control if there was a problem
                        NewCurrentPanel.CloseFrame()
                        NewCurrentPanel.CustomViewProvider = New ErrorControlCustomViewProvider(ErrorMessage)
                        NewCurrentPanel.ShowDesigner()
                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                        'If there's an error showing the error control, it's time to give up
                        Debug.Fail("Error showing error control: " & ex.ToString())
                    End Try
                End If

                'We may have opened a new page, need to verify all dirty states
                DelayRefreshDirtyIndicators()

                Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(CodeMarkerEvent.perfMSVSEditorsShowTabEnd)
                Common.Switches.TracePDFocus(TraceLevel.Error, "CodeMarker: perfMSVSEditorsShowTabEnd")
                Common.Switches.TracePDPerf("CodeMarker: perfMSVSEditorsShowTabEnd")
                Common.Switches.TracePDPerfEnd("ApplicationDesignerView.ShowTab")
            Finally
                m_InShowTab = False
            End Try
        End Sub

        'Standard title for messageboxes, etc.
        Private ReadOnly MessageBoxCaption As String = SR.GetString(SR.APPDES_Title)

        ''' <summary>
        ''' Displays a message box using the Visual Studio-approved manner.
        ''' </summary>
        ''' <param name="Message">The message text.</param>
        ''' <param name="Buttons">Which buttons to show</param>
        ''' <param name="Icon">the icon to show</param>
        ''' <param name="DefaultButton">Which button should be default?</param>
        ''' <param name="HelpLink">The help link</param>
        ''' <returns>One of the DialogResult values</returns>
        ''' <remarks></remarks>
        Public Function DsMsgBox(ByVal Message As String, _
                ByVal Buttons As MessageBoxButtons, _
                ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing) As DialogResult

            Debug.Assert(m_serviceProvider IsNot Nothing)
            Return AppDesDesignerFramework.DesignerMessageBox.Show(m_serviceProvider, Message, Me.MessageBoxCaption, _
                Buttons, Icon, DefaultButton, HelpLink)
        End Function


        ''' <summary>
        ''' Displays a message box using the Visual Studio-approved manner.
        ''' </summary>
        ''' <param name="ex">The exception whose text should be displayed.</param>
        ''' <param name="HelpLink">The help link</param>
        ''' <remarks></remarks>
        Public Sub DsMsgBox(ByVal ex As Exception, _
                Optional ByVal HelpLink As String = Nothing) Implements IPropertyPageSiteOwner.DsMsgBox

            Debug.Assert(m_serviceProvider IsNot Nothing)
            AppDesDesignerFramework.DesignerMessageBox.Show(m_serviceProvider, ex, Me.MessageBoxCaption, HelpLink:=HelpLink)
        End Sub


        ''' <summary>
        ''' Moves to the next or previous tab in the project designer
        ''' </summary>
        ''' <param name="forward">If true, moves forward a tab.  If false, moves back a tab.</param>
        ''' <remarks></remarks>
        Public Sub SwitchTab(ByVal forward As Boolean)
            Dim Index As Integer = m_ActivePanelIndex
            If forward Then
                Index += 1
            Else
                Index -= 1
            End If
            If Index < 0 Then
                Index = m_DesignerPanels.Length - 1
            ElseIf Index >= m_DesignerPanels.Length Then
                Index = 0
            End If
            ShowTab(Index)
        End Sub

        ''' <summary>
        ''' Occurs when the user clicks on one of the tab buttons.  Switch to that tab.
        ''' </summary>
        ''' <param name="item"></param>
        ''' <remarks></remarks>
        Public Overrides Sub OnItemClick(ByVal item As ProjectDesignerTabButton)
            Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerView.OnItemClick")
            MyBase.OnItemClick(item)
            ShowTab(SelectedIndex, ForceShow:=True)

            ' we need set back the tab, if we failed to switch...
            If SelectedIndex <> m_ActivePanelIndex Then
                SelectedIndex = m_ActivePanelIndex
            End If
        End Sub


        ''' <summary>
        ''' WndProc for the project designer.
        ''' </summary>
        ''' <param name="m"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            If m.Msg = AppDesInterop.win.WM_SETFOCUS AndAlso Not m_isInPanelWindowFrameShow Then 'in MDI mode this can get hit recursively
                'We need to intercept WM_SETFOCUS on the project designer to keep WinForms from setting focus to the
                '  current control (one of the tab buttons).  Instead, we want to keep the tab buttons from getting
                '  focus (unless they're clicked on directly), and instead activate the current page directly.
                '  This circumvents lots of back-and-forth swapping between the project designer and the current page
                '  as the active designer, and helps us handle the on-click case correctly.
                'Note: Handling OnGotFocus would not be good enough - we need to keep WinForms from doing their default
                '  processing on WM_SETFOCUS, and we can't do that by handling OnGotFocus.
                Common.Switches.TracePDFocus(TraceLevel.Warning, "Preprocess: Stealing ApplicationDesignerView.WM_SETFOCUS handling")
                Common.Switches.TracePDFocus(TraceLevel.Verbose, New Diagnostics.StackTrace().ToString)

                If Not m_InShowTab AndAlso m_OkayToActivatePanelsOnFocus Then
                    If m_ActivePanelIndex >= 0 AndAlso m_ActivePanelIndex < m_DesignerPanels.Length Then
                        Dim Panel As ApplicationDesignerPanel = m_DesignerPanels(m_ActivePanelIndex)
                        If Panel IsNot Nothing Then
                            If Panel.VsWindowFrame IsNot Nothing Then
                                'Activate the currently-active panel's window frame, give it focus, and ensure that 
                                '  the active document is updated.
                                Common.Switches.TracePDFocus(TraceLevel.Warning, "... VsWindowFrame.Show()")
                                Try
                                    m_isInPanelWindowFrameShow = True
                                    Panel.VsWindowFrame.Show()
                                Finally
                                    m_isInPanelWindowFrameShow = False
                                End Try
                            ElseIf Panel.CustomViewProvider IsNot Nothing Then
                                MyBase.WndProc(m)
                            End If
                        End If
                    End If
                Else
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... Ignoring")
                End If

                'Return without calling in to base functionality.  This keeps the application designer's WinForms code
                '  from automatically setting focus to the currently-active tab.
                Return
            End If

            MyBase.WndProc(m)
        End Sub


        ''' <summary>
        ''' Calls when the application designer window pane has completely initialized the application designer view (the
        '''   ApplicationDesignerWindowPane controls initialization and population of the view).
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub OnInitializationComplete()
            Common.Switches.TracePDFocus(TraceLevel.Warning, "OnInitializationComplete")
            m_InitializationComplete = True

            'UI initialization is complete.  Now we need to now show the first page.
            ShowTab(LastShownTab, True)

            'Queue a request to update the dirty indicators
            DelayRefreshDirtyIndicators()

            '... and start listening to when the dirty state might change
            AdviseRunningDocTableEvents()

            m_OkayToActivatePanelsOnFocus = True
        End Sub


        ''' <summary>
        ''' Returns true if initialization is complete for the project designer.  This is used
        '''   by ApplicationDesignerPanel to delay any window frame activations until after
        '''   initialization.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property InitializationComplete() As Boolean
            Get
                Return m_InitializationComplete
            End Get
        End Property


#Region "Dirty indicators"

        ''' <summary>
        ''' Queues up a request (via PostMessage) to refresh all of our dirty indicators.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub DelayRefreshDirtyIndicators() Implements IPropertyPageSiteOwner.DelayRefreshDirtyIndicators
            If Not m_InitializationComplete Then
                Exit Sub
            End If

            If Not m_RefreshDirtyIndicatorsQueued AndAlso Me.IsHandleCreated Then
                BeginInvoke(New MethodInvoker(AddressOf RefreshDirtyIndicatorsHelper))
                m_RefreshDirtyIndicatorsQueued = True
            End If
        End Sub


        ''' <summary>
        ''' Used by DelayRefreshDirtyIndicators, do not call directly.  Updates the dirty
        '''   indicators for the project designer and all tabs.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RefreshDirtyIndicatorsHelper()
            Try
                'First, update all tab dirty indicators
                If m_DesignerPanels IsNot Nothing Then
                    For i As Integer = 0 To m_DesignerPanels.Length - 1
                        GetTabButton(i).DirtyIndicator = m_DesignerPanels(i) IsNot Nothing AndAlso m_DesignerPanels(i).IsDirty()
                    Next
                End If

                'Should the project designer as a whole look dirty or not?
                'We show the dirty state for the project designer if:
                '  a) the project file is dirty
                '    or
                '  b) any of the tabs is dirty
                Dim ProjectDesignerIsDirty As Boolean = False

                Dim AnyTabIsDirty As Boolean = False
                For i As Integer = 0 To m_DesignerPanels.Length - 1
                    AnyTabIsDirty = AnyTabIsDirty OrElse GetTabButton(i).DirtyIndicator
                Next
                ProjectDesignerIsDirty = AnyTabIsDirty OrElse IsProjectFileDirty(DTEProject)

                'Update the project designer's dirty status
                SetFrameDirtyIndicator(ProjectDesignerIsDirty)

            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                ' VsVhidbey 446720 - if we have messed up the UNDO stack, the m_designerPanels.IsDirty call may 
                ' throw an exception (when trying to enumerate the UNDO units)
                Debug.Fail(String.Format("Exception {0} caught when trying to update dirty indicators...", ex))
            Finally
                'Allow us to queue refresh requests again
                m_RefreshDirtyIndicatorsQueued = False
            End Try
        End Sub


        ''' <summary>
        ''' Returns true if the project file is dirty
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsProjectFileDirty(ByVal Project As Project) As Boolean
            Debug.Assert(Project IsNot Nothing)

            Dim hr As Integer = NativeMethods.E_FAIL
            If Project IsNot Nothing Then
                Dim ProjectFullName As String = Project.FullName
                Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
                If rdt IsNot Nothing Then
                    Dim punkDocData As New IntPtr(0)
                    Try
                        Dim Hierarchy As IVsHierarchy = Nothing
                        Dim ItemId As UInteger
                        Dim dwCookie As UInteger
                        hr = rdt.FindAndLockDocument(CUInt(_VSRDTFLAGS.RDT_NoLock), ProjectFullName, Hierarchy, ItemId, punkDocData, dwCookie)
                        If VSErrorHandler.Succeeded(hr) Then
                            Return IsDocDataDirty(dwCookie, Hierarchy, ItemId)
                        End If
                    Finally
                        If punkDocData <> IntPtr.Zero Then
                            Marshal.Release(punkDocData)
                        End If
                    End Try
                End If
            End If

            Return False
        End Function


        ''' <summary>
        ''' Gets the cookie for the project file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProjectFileCookie(ByVal Project As Project) As UInteger
            Debug.Assert(Project IsNot Nothing)

            Dim hr As Integer = NativeMethods.E_FAIL
            If Project IsNot Nothing Then
                Dim ProjectFullName As String = Project.FullName
                Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
                If rdt IsNot Nothing Then
                    Dim punkDocData As New IntPtr(0)
                    Try
                        Dim Hierarchy As IVsHierarchy = Nothing
                        Dim ItemId As UInteger
                        Dim dwCookie As UInteger
                        hr = rdt.FindAndLockDocument(CUInt(_VSRDTFLAGS.RDT_NoLock), ProjectFullName, Hierarchy, ItemId, punkDocData, dwCookie)
                        If VSErrorHandler.Succeeded(hr) Then
                            Return dwCookie
                        End If
                    Finally
                        If punkDocData <> IntPtr.Zero Then
                            Marshal.Release(punkDocData)
                        End If
                    End Try
                End If
            End If

            Return 0
        End Function


        ''' <summary>
        ''' Sets the frame's dirty indicator.  This causes an asterisk to appear or disappear
        '''   from the project designer's MDI tab title (i.e., represents the project designer's dirty 
        '''   state as a whole).
        ''' </summary>
        ''' <param name="Dirty">If true, the asterisk is added, if false, it is removed.</param>
        ''' <remarks></remarks>
        Private Sub SetFrameDirtyIndicator(ByVal Dirty As Boolean)
            If Not m_ProjectDesignerDirtyStateInitialized OrElse m_LastProjectDesignerDirtyState <> Dirty Then
                Dim Frame As IVsWindowFrame = Me.WindowFrame
                If Frame IsNot Nothing Then
                    'VSFPROPID_OverrideDirtyState - this is a tri-state property.  If Empty, we get default behavior.  True/False
                    '  overrides the state.
                    Dim newState As Object = Dirty
                    Frame.SetProperty(__VSFPROPID2.VSFPROPID_OverrideDirtyState, newState)
                    m_LastProjectDesignerDirtyState = Dirty
                    m_ProjectDesignerDirtyStateInitialized = True
                End If
            End If
        End Sub

#End Region


#Region "IVsSelectionEvents"

        ''' <summary>
        '''     Called by the shell when the UI context changes.  We don't care about this.
        '''
        ''' </summary>
        ''' <param name='dwCmdUICookie'>
        '''     A cookie representing the area of UI that has changed.
        ''' </param>
        ''' <param name='fActive'>
        '''     Nonzero if the context is now active.
        '''
        ''' </param>
        ''' <seealso cref='IVsSelectionEvents'/>
        Public Function OnCmdUIContextChanged(ByVal dwCmdUICookie As UInteger, ByVal fActive As Integer) As Integer Implements Shell.Interop.IVsSelectionEvents.OnCmdUIContextChanged
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        '''     Called by the shell when the the document or other part of the active UI changes.
        '''
        ''' </summary>
        ''' <param name='elementid'>
        '''     A tag indicating the type of element that has changed.
        ''' </param>
        ''' <param name='varValueOld'>
        '''     The old value of the element.
        ''' </param>
        ''' <param name='varValueNew'>
        '''     The new value of the element.
        '''
        ''' </param>
        ''' <seealso cref='IVsSelectionEvents'/>
        Public Function OnElementValueChanged(ByVal elementid As UInteger, ByVal varValueOld As Object, ByVal varValueNew As Object) As Integer Implements Shell.Interop.IVsSelectionEvents.OnElementValueChanged
            If elementid = 1 AndAlso m_DesignerPanels IsNot Nothing AndAlso varValueOld IsNot varValueNew Then ' WindowFrame changed
                For Each panel As ApplicationDesignerPanel In m_DesignerPanels
                    If panel.VsWindowFrame Is varValueOld Then
                        panel.OnWindowActivated(False)
                        Exit For
                    End If
                Next
                For Each panel As ApplicationDesignerPanel In m_DesignerPanels
                    If panel.VsWindowFrame Is varValueNew Then
                        panel.OnWindowActivated(True)
                        Exit For
                    End If
                Next
            End If
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        '''     Called by the shell when a new selection container is available.  We broadcast this to
        '''     anyone listening.
        '''
        ''' </summary>
        ''' <param name='pHierOld'>
        '''     The previous IVsHierarchy.  We ignore this.
        ''' </param>
        ''' <param name='itemidOld'>
        '''     The previous hierarchies ITEMID.  We ignore this.
        ''' </param>
        ''' <param name='pMISOld'>
        '''     A MultiItemSelection pointer, which we ignore.
        ''' </param>
        ''' <param name='pSCOld'>
        '''     The old selection container.
        ''' </param>
        ''' <param name='pHierNew'>
        '''     The new IVsHierarchy. We ignore this.
        ''' </param>
        ''' <param name='itemidNew'>
        '''     The new hieararchies ITEMID.  We ignore this.
        ''' </param>
        ''' <param name='pMISNew'>
        '''     The new MultiItemSelection pointer, which we ignore.
        ''' </param>
        ''' <param name='pSCNew'>
        '''     The new selection container.  We do use this.
        '''
        ''' </param>
        ''' <seealso cref='IVsSelectionEvents'/>
        Public Function OnSelectionChanged(ByVal pHierOld As Shell.Interop.IVsHierarchy, ByVal itemidOld As UInteger, ByVal pMISOld As Shell.Interop.IVsMultiItemSelect, ByVal pSCOld As Shell.Interop.ISelectionContainer, ByVal pHierNew As Shell.Interop.IVsHierarchy, ByVal itemidNew As UInteger, ByVal pMISNew As Shell.Interop.IVsMultiItemSelect, ByVal pSCNew As Shell.Interop.ISelectionContainer) As Integer Implements Shell.Interop.IVsSelectionEvents.OnSelectionChanged
            Return NativeMethods.S_OK
        End Function

#End Region


#Region "IVsRunningDocTableEvents"


        'We sync these events to be notified of when our DocData might have been dirtied/undirtied


        ''' <summary>
        ''' Start listening to IVsRunningDocTableEvents events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub AdviseRunningDocTableEvents()
            Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
            Debug.Assert(rdt IsNot Nothing, "Couldn't get running document table")
            If rdt IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(rdt.AdviseRunningDocTableEvents(Me, m_rdtEventsCookie))
            End If
        End Sub


        ''' <summary>
        ''' Stop listening to IVsRunningDocTableEvents events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnadviseRunningDocTableEvents()
            If m_rdtEventsCookie <> 0 Then
                Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
                Debug.Assert(rdt IsNot Nothing, "Couldn't get running document table")
                If rdt IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(rdt.UnadviseRunningDocTableEvents(m_rdtEventsCookie))
                End If
                m_rdtEventsCookie = 0
            End If
        End Sub


        ''' <summary>
        ''' Fires after an attribute of a document in the RDT is changed.
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <param name="grfAttribs"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterAttributeChange(ByVal docCookie As UInteger, ByVal grfAttribs As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterAttributeChange
            Const InterestingFlags As Long = __VSRDTATTRIB.RDTA_DocDataIsDirty Or __VSRDTATTRIB.RDTA_DocDataIsNotDirty Or __VSRDTATTRIB.RDTA_NOTIFYDOCCHANGEDMASK
            If (grfAttribs And InterestingFlags) <> 0 Then
                'CONSIDER: better would be to check it against all of our DocData's.  But we don't have a simple, static list
                '  lying around), we'll just queue up a request to refresh all of our states.  This shouldn't be a performance
                '  problem since we do this via PostMessage.
                DelayRefreshDirtyIndicators()
            End If

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Fires after a document window is placed in the Hide state.
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <param name="pFrame"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterDocumentWindowHide(ByVal docCookie As UInteger, ByVal pFrame As Shell.Interop.IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnAfterDocumentWindowHide
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Fires after the first document in the RDT is locked.
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <param name="dwRDTLockType"></param>
        ''' <param name="dwReadLocksRemaining"></param>
        ''' <param name="dwEditLocksRemaining"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterFirstDocumentLock(ByVal docCookie As UInteger, ByVal dwRDTLockType As UInteger, ByVal dwReadLocksRemaining As UInteger, ByVal dwEditLocksRemaining As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterFirstDocumentLock
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Fires after a document in the RDT is saved.
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterSave(ByVal docCookie As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterSave
            Debug.Assert(m_DesignerPanels IsNot Nothing, "m_DesignerPanels should not be Nothing")
            If m_DesignerPanels IsNot Nothing Then
                'Was the project file saved?
                If docCookie = GetProjectFileCookie(DTEProject) Then
                    'Yes.  Need to reset the undo/redo clean state of all property pages
                    SetUndoRedoCleanStateOnAllPropertyPages()
                End If
                DelayRefreshDirtyIndicators()
            End If

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Fires before a document window is placed in the Show state.
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <param name="fFirstShow"></param>
        ''' <param name="pFrame"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnBeforeDocumentWindowShow(ByVal docCookie As UInteger, ByVal fFirstShow As Integer, ByVal pFrame As Shell.Interop.IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnBeforeDocumentWindowShow
            Debug.Assert(m_DesignerPanels IsNot Nothing, "m_DesignerPanels should not be Nothing")
            If m_DesignerPanels IsNot Nothing Then
                If Not m_InShowTab Then
                    ' If the window frame passed to us belongs to any of our panels,
                    ' we better set that as the active tab...
                    For Index As Integer = 0 To m_DesignerPanels.Length - 1
                        Dim panel As ApplicationDesignerPanel
                        panel = Me.m_DesignerPanels(Index)
                        Debug.Assert(panel IsNot Nothing, "m_DesignerPanels(Index) should not be Nothing")
                        If Object.ReferenceEquals(panel.VsWindowFrame, pFrame) Then
                            ShowTab(Index)
                            Exit For
                        End If
                    Next
                End If
            End If

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Fires before the last document in the RDT is unlocked
        ''' </summary>
        ''' <param name="docCookie"></param>
        ''' <param name="dwRDTLockType"></param>
        ''' <param name="dwReadLocksRemaining"></param>
        ''' <param name="dwEditLocksRemaining"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnBeforeLastDocumentUnlock(ByVal docCookie As UInteger, ByVal dwRDTLockType As UInteger, ByVal dwReadLocksRemaining As UInteger, ByVal dwEditLocksRemaining As UInteger) As Integer Implements IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock
            Return NativeMethods.S_OK
        End Function
        ''' <summary>
        ''' Fires after a document is added to the running document table but before it is locked for the first time.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="MkDocument"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnBeforeFirstDocumentLock(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal MkDocument As String) As Integer Implements IVsRunningDocTableEvents4.OnBeforeFirstDocumentLock
            Return NativeMethods.S_OK
        End Function
        ''' <summary>
        ''' Fires after all documents are saved (some of the documents saved may not be in the running document table).
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterSaveAll() As Integer Implements IVsRunningDocTableEvents4.OnAfterSaveAll
            'A Save All operation just occurred.  Need to reset the undo/redo clean state of all property pages
            SetUndoRedoCleanStateOnAllPropertyPages()
        End Function

        ''' <summary>
        ''' Calls SetUndoRedoCleanState() on each property page
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetUndoRedoCleanStateOnAllPropertyPages()
            For i As Integer = 0 To m_DesignerPanels.Length - 1
                Debug.Assert(m_DesignerPanels(i) IsNot Nothing, "m_DesignerPanels(Index) should not be Nothing")
                If m_DesignerPanels(i) IsNot Nothing AndAlso m_DesignerPanels(i).IsPropertyPage Then
                    Dim PropPageView As PropPageDesigner.PropPageDesignerView = TryCast(m_DesignerPanels(i).DocView, PropPageDesigner.PropPageDesignerView)
                    If PropPageView IsNot Nothing Then
                        PropPageView.SetUndoRedoCleanState()
                    End If
                End If
            Next

            DelayRefreshDirtyIndicators()
        End Sub

        ''' <summary>
        ''' Fires after a document is unlocked and removed from the running document table.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="MkDocument"></param>
        ''' <param name="ClosedWithoutSaving"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnAfterLastDocumentUnlock(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal MkDocument As String, ByVal ClosedWithoutSaving As Integer) As Integer Implements IVsRunningDocTableEvents4.OnAfterLastDocumentUnlock
            Return NativeMethods.S_OK
        End Function

#End Region


        ''' <summary>
        ''' Gets the locale ID from the shell
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetLocaleID() As UInteger Implements IPropertyPageSiteOwner.GetLocaleID
            Dim LocaleId As UInteger
            Dim UIHostLocale As Shell.Interop.IUIHostLocale = DirectCast(GetService(GetType(Shell.Interop.IUIHostLocale)), Shell.Interop.IUIHostLocale)
            If UIHostLocale IsNot Nothing Then
                UIHostLocale.GetUILocale(LocaleId)
                Return LocaleId
            End If

            'Fallback
            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetUserDefaultLCID()
        End Function


#Region "Debug tracing for OnLayout/Size events..."

        Protected Overrides Sub OnLayout(ByVal levent As System.Windows.Forms.LayoutEventArgs)
            Common.Switches.TracePDPerfBegin(levent, "ApplicationDesignerView.OnLayout()")
            MyBase.OnLayout(levent)
            Common.Switches.TracePDPerfEnd("ApplicationDesignerView.OnLayout()")
        End Sub

        Private Sub HostingPanel_Layout(ByVal sender As Object, ByVal levent As LayoutEventArgs)
            Common.Switches.TracePDPerf(levent, "ApplicationDesignerView.HostingPanel_Layout()")
        End Sub

        Private Sub HostingPanel_SizeChanged(ByVal sender As Object, ByVal e As EventArgs)
            Common.Switches.TracePDPerf("ApplicationDesignerView.HostingPanel_SizeChanged: " & HostingPanel.Size.ToString())
        End Sub

        Private Sub ApplicationDesignerView_SizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.SizeChanged
            Common.Switches.TracePDPerf("ApplicationDesignerView.SizeChanged: " & Me.Size.ToString())
        End Sub

#End Region

#Region "SQM helpers"
        ''' <summary>
        ''' In case you want to get hold of a IVsSqm service, but you don't have a service provider around
        ''' this is a nice helper method...
        ''' </summary>
        ''' <value></value>
        ''' <remarks>May return nothing if the package can't find the service</remarks>
        Friend ReadOnly Property VsLog() As Microsoft.VisualStudio.Shell.Interop.IVsSqm
            Get
                Try
                    Return DirectCast(Package.GetService(GetType(Microsoft.VisualStudio.Shell.Interop.SVsLog)), Microsoft.VisualStudio.Shell.Interop.IVsSqm)
                Catch Ex As InvalidCastException
                    Debug.Fail("Failed to cast returned Service to an IVsSqm - SQM logging will be disabled")
                End Try
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Log a SQM datapoint. Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Friend Sub LogSqmDatapoint(ByVal dataPointId As UInteger, ByVal value As UInteger)
            If VsLog IsNot Nothing Then
                VsLog.SetDatapoint(dataPointId, value)
            End If
        End Sub

        ''' <summary>
        ''' Increment a SQM datapoint.Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Friend Sub IncrementSqmDatapoint(ByVal dataPointId As UInteger, Optional ByVal value As UInteger = 1)
            If VsLog IsNot Nothing Then
                VsLog.IncrementDatapoint(dataPointId, value)
            End If
        End Sub

        ''' <summary>
        ''' Add an item to a SQM stream. Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Friend Sub AddSqmItemToStream(ByVal dataPointId As UInteger, Optional ByVal value As UInteger = 1)
            If VsLog IsNot Nothing Then
                VsLog.AddItemToStream(dataPointId, value)
            End If
        End Sub
#End Region

    End Class

End Namespace

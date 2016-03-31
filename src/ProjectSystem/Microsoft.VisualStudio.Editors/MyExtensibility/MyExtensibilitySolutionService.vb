Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensibilitySolutionService
    ''' <summary>
    ''' Main entry to My Extensibility service.
    ''' This class manages the My Extensibility services for each VB project in a Solution.
    ''' One instance of this class per VS instance (solution).
    ''' </summary>
    ''' <remarks>
    ''' [OBSOLETE] (Keep for reference only)
    ''' - Once the manager is created, it will listen to EnvDTE.Events2.SolutionEvents
    '''   to update / clear the project services and to handle Zero-Impact-Project (ZIP).
    ''' - Special handling for ZIP: When a Save All occurs to DIFFERENT disk drive than the temporary ZIP,
    '''   (the project system will call the compiler to Remove,Add,Remove,Add each new references).
    '''   VB compiler does not know much about this event so each project service will listen to
    '''   _dispReferencesEvents themselves. MyExtensibilitySolutionService will not inform 
    '''   project service of reference changes once a project service exists.
    ''' [/OBSOLETE]
    ''' The solution above leads to DevDiv Bugs 51380. If multiple assemblies are being added,
    ''' the ProjectService will only know about the first assemblies.
    ''' The issue with ZIP does not exist with later Orcas build.
    ''' </remarks>
    Friend Class MyExtensibilitySolutionService

#Region "Shared methods"
        ''' ;Instance
        ''' <summary>
        ''' Shared property to obtain the instance of MyExtensibilityManager associated with
        ''' the current VS environment.
        ''' </summary>
        Public Shared ReadOnly Property Instance() As MyExtensibilitySolutionService
            Get
                If m_SharedInstance Is Nothing Then
                    m_SharedInstance = New MyExtensibilitySolutionService(VBPackage.Instance)
                End If

                Debug.Assert(m_SharedInstance IsNot Nothing)
                Return m_SharedInstance
            End Get
        End Property

        ''' ;IdeStatusBar
        ''' <summary>
        ''' Shared property to obtain the current VS status bar.
        ''' </summary>
        Public Shared ReadOnly Property IdeStatusBar() As VsStatusBarWrapper
            Get
                If m_IdeStatusBar Is Nothing Then
                    Dim vsStatusBar As IVsStatusbar = TryCast( _
                        VBPackage.Instance.GetService(GetType(IVsStatusbar)), IVsStatusbar)
                    If vsStatusBar IsNot Nothing Then
                        m_IdeStatusBar = New VsStatusBarWrapper(vsStatusBar)
                    End If

                    Debug.Assert(m_IdeStatusBar IsNot Nothing, "Could not get IVsStatusBar!")
                End If

                Return m_IdeStatusBar
            End Get
        End Property

        Private Shared m_SharedInstance As MyExtensibilitySolutionService ' shared instance for the current VS environment.
        Private Shared m_IdeStatusBar As VsStatusBarWrapper ' shared instance of the current VS status bar.
#End Region

        ''' ;GetService
        ''' <summary>
        ''' Obtain the specified service.
        ''' </summary>
        Public Function GetService(ByVal serviceType As Type) As Object
            Return m_VBPackage.GetService(serviceType)
        End Function

        ''' ;ReferenceAdded
        ''' <summary>
        ''' Notify the project's My Extensibility service that a reference has been added.
        ''' </summary>
        ''' <remarks>VB Compiler will call this method through VBReferenceChangedService.</remarks>
        Public Sub ReferenceAdded(ByVal projectHierarchy As IVsHierarchy, ByVal assemblyInfo As String)
            Me.HandleReferenceChange(projectHierarchy, assemblyInfo, AddRemoveAction.Add)
        End Sub

        ''' ;ReferenceRemoved
        ''' <summary>
        ''' Notify the project's My Extensibility service that a reference has been removed.
        ''' </summary>
        ''' <remarks>VB Compiler will call this method through VBReferenceChangedService.</remarks>
        Public Sub ReferenceRemoved(ByVal projectHierarchy As IVsHierarchy, ByVal assemblyInfo As String)
            Me.HandleReferenceChange(projectHierarchy, assemblyInfo, AddRemoveAction.Remove)
        End Sub

        ''' ;GetProjectService
        ''' <summary>
        ''' Get the MyExtensibilityProjectService for the given IVsHierarchy.
        ''' </summary>
        ''' <remarks>This can be invoked by VB Compiler (through ReferenceAdded, ReferenceRemoved) or
        ''' My Extensibility Property Page.</remarks>
        Public Function GetProjectService(ByVal projectHierarchy As IVsHierarchy) _
                As MyExtensibilityProjectService

            ' Expect an IVsHierarchy but if none is provided, attempt to get it from IVsMonitorSelection.
            If projectHierarchy Is Nothing Then
                Try
                    Dim vsMonitorSelection As IVsMonitorSelection = TryCast( _
                        m_VBPackage.GetService(GetType(IVsMonitorSelection)), IVsMonitorSelection)

                    If vsMonitorSelection IsNot Nothing Then
                        Dim vsHierarchyPointer As IntPtr = IntPtr.Zero
                        Dim itemID As UInteger = VSITEMID.NIL
                        Dim vsMultiItemSelect As IVsMultiItemSelect = Nothing
                        Dim selectionContainerPointer As IntPtr = IntPtr.Zero

                        Try
                            vsMonitorSelection.GetCurrentSelection( _
                                vsHierarchyPointer, itemID, vsMultiItemSelect, selectionContainerPointer)
                        Finally
                            If selectionContainerPointer <> IntPtr.Zero Then
                                Marshal.Release(selectionContainerPointer)
                            End If

                            If vsHierarchyPointer <> IntPtr.Zero Then
                                projectHierarchy = TryCast( _
                                    Marshal.GetObjectForIUnknown(vsHierarchyPointer), IVsHierarchy)
                                Marshal.Release(vsHierarchyPointer)
                                vsHierarchyPointer = IntPtr.Zero
                            End If
                        End Try
                    End If ' If vsMonitorSelection IsNot Nothing
                Catch ex As Exception
                    ' Ignore exceptions.
                End Try
            End If

            ' Get the EnvDTE.Project from the project hierarchy.
            Dim project As EnvDTE.Project = Nothing
            If projectHierarchy IsNot Nothing Then
                Dim projectObject As Object = Nothing
                Dim hr As Integer = projectHierarchy.GetProperty( _
                    VSITEMID.ROOT, CInt(__VSHPROPID.VSHPROPID_ExtObject), projectObject)

                If VSErrorHandler.Succeeded(hr) AndAlso projectObject IsNot Nothing Then
                    project = TryCast(projectObject, EnvDTE.Project)
                End If
            End If

            ' Create a MyExtensibilityProjectService for the current project if need to
            If project IsNot Nothing Then
                If Not m_ProjectServices.ContainsKey(project) Then
                    m_ProjectServices.Add(project, _
                        MyExtensibilityProjectService.CreateNew(m_VBPackage, project, projectHierarchy, Me.ExtensibilitySettings))
                End If
                Return m_ProjectServices(project)
            End If

            Return Nothing

        End Function

        Public ReadOnly Property TrackProjectDocumentsEvents() As TrackProjectDocumentsEventsHelper
            Get
                If m_TrackProjectDocumentsEvents Is Nothing Then
                    m_TrackProjectDocumentsEvents = TrackProjectDocumentsEventsHelper.GetInstance(m_VBPackage)
                End If
                Return m_TrackProjectDocumentsEvents
            End Get
        End Property

        ''' ;New
        ''' <summary>
        ''' Private constructor since MyExtensibilityManager can be accessed through
        ''' shared property Instance.
        ''' </summary>
        Private Sub New(ByVal vbPackage As VBPackage)
            Debug.Assert(vbPackage IsNot Nothing, "vbPackage Is Nothing")
            m_VBPackage = vbPackage
            Me.AddEnvDTEEvents()
        End Sub

        ''' ;ExtensibilitySettings
        ''' <summary>
        ''' Lazy-initialized My Extensibility settings containing information about extension templates.
        ''' </summary>
        Private ReadOnly Property ExtensibilitySettings() As MyExtensibilitySettings
            Get
                If m_ExtensibilitySettings Is Nothing Then

                    Dim vsAppDataDir As String = String.Empty
                    Dim vsShell As IVsShell = TryCast(m_VBPackage.GetService(GetType(IVsShell)), IVsShell)
                    If vsShell IsNot Nothing Then
                        Dim appDataDir As Object = Nothing
                        Dim hr As Integer = vsShell.GetProperty(__VSSPROPID.VSSPROPID_AppDataDir, appDataDir)
                        If VSErrorHandler.Succeeded(hr) Then
                            vsAppDataDir = CStr(appDataDir)
                        End If
                    End If
                    m_ExtensibilitySettings = New MyExtensibilitySettings(vsAppDataDir)
                End If
                Return m_ExtensibilitySettings
            End Get
        End Property

        ''' ;HandleReferenceChange
        ''' <summary>
        ''' If needed, notify the given project's My Extensibility service that a reference has been added or removed.
        ''' </summary>
        ''' <remarks>
        ''' The compiler will initialize a My Extensibility project service when a reference is added or removed.
        ''' After that, the project service will listen to reference added or removed event itself (to avoid ZIP problem).
        ''' Therefore, if a project service already exists, do not notify it.
        ''' </remarks>
        Private Sub HandleReferenceChange(ByVal projectHierarchy As IVsHierarchy, ByVal assemblyInfo As String, _
                ByVal action As AddRemoveAction)
            ' assemblyInfo can be NULL in case of unmanaged assembly.
            If StringIsNullEmptyOrBlank(assemblyInfo) Then
                Exit Sub
            End If

            Switches.TraceMyExtensibility(TraceLevel.Verbose, String.Format("MyExtensibilitySolutionService.HandleReferenceChange: Entry. assemblyInfo='{0}'.", assemblyInfo))

            Dim projectService As MyExtensibilityProjectService = Me.GetProjectService(projectHierarchy)
            If projectService IsNot Nothing Then
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.HandleReferenceChange: ProjectService exists, notifying.")
                If action = AddRemoveAction.Add Then
                    projectService.ReferenceAdded(assemblyInfo)
                Else
                    projectService.ReferenceRemoved(assemblyInfo)
                End If
            End If

            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.HandleReferenceChange: Exit.")
        End Sub

#Region "SolutionEvents and DTEEvents"

        ''' ;AddEnvDTEEvents
        ''' <summary>
        ''' Hook ourselves up to listen to DTE and solution events.
        ''' </summary>
        Private Sub AddEnvDTEEvents()
            Dim dte As EnvDTE80.DTE2 = TryCast(m_VBPackage.GetService(GetType(_DTE)), EnvDTE80.DTE2)
            If dte IsNot Nothing Then
                Dim events As EnvDTE80.Events2 = TryCast(dte.Events, EnvDTE80.Events2)
                If events IsNot Nothing Then
                    m_SolutionEvents = events.SolutionEvents
                    If m_SolutionEvents IsNot Nothing Then
                        AddHandler m_SolutionEvents.AfterClosing, _
                            New EnvDTE._dispSolutionEvents_AfterClosingEventHandler( _
                            AddressOf Me.SolutionEvents_AfterClosing)
                        AddHandler m_SolutionEvents.ProjectRemoved, _
                            New EnvDTE._dispSolutionEvents_ProjectRemovedEventHandler( _
                            AddressOf Me.SolutionEvents_ProjectRemoved)
                    End If
                    m_DTEEvents = events.DTEEvents
                    If m_DTEEvents IsNot Nothing Then
                        AddHandler m_DTEEvents.OnBeginShutdown, _
                            New EnvDTE._dispDTEEvents_OnBeginShutdownEventHandler( _
                            AddressOf Me.DTEEvents_OnBeginShutDown)
                    End If
                End If
            End If
        End Sub

        ''' ;RemoveSolutionEvents
        ''' <summary>
        ''' Remove ourselves as listener of DTE and solution events.
        ''' </summary>
        Private Sub RemoveEnvDTEEvents()
            If m_SolutionEvents IsNot Nothing Then
                RemoveHandler m_SolutionEvents.AfterClosing, _
                    New EnvDTE._dispSolutionEvents_AfterClosingEventHandler( _
                    AddressOf Me.SolutionEvents_AfterClosing)
                RemoveHandler m_SolutionEvents.ProjectRemoved, _
                    New EnvDTE._dispSolutionEvents_ProjectRemovedEventHandler( _
                    AddressOf Me.SolutionEvents_ProjectRemoved)
                m_SolutionEvents = Nothing
            End If
            If m_DTEEvents IsNot Nothing Then
                RemoveHandler m_DTEEvents.OnBeginShutdown, _
                    New EnvDTE._dispDTEEvents_OnBeginShutdownEventHandler( _
                    AddressOf Me.DTEEvents_OnBeginShutDown)
                m_DTEEvents = Nothing
            End If
        End Sub

        ''' ;SolutionEvents_AfterClosing
        ''' <summary>
        ''' Handle solution's AfterClosing events and clear our collection of project services.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SolutionEvents_AfterClosing()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Entry. Clear project services dictionary.")
            m_ProjectServices.Clear()

            If m_TrackProjectDocumentsEvents IsNot Nothing Then
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: UnAdviseTrackProjectDocumentsEvents.")
                m_TrackProjectDocumentsEvents.UnAdviseTrackProjectDocumentsEvents()
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Clear m_TrackProjectDocumentsEvents.")
                m_TrackProjectDocumentsEvents = Nothing
            End If
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Exit.")
        End Sub


        ''' ;SolutionEvents_ProjectRemoved
        ''' <summary>
        ''' Handle ProjectRemoved event and remove the associate project service from our collection.
        ''' </summary>
        Private Sub SolutionEvents_ProjectRemoved(ByVal project As Project)
            If project Is Nothing Then
                Exit Sub
            End If

            If m_ProjectServices.ContainsKey(project) Then
                Dim removedProjectService As MyExtensibilityProjectService = m_ProjectServices(project)
                m_ProjectServices.Remove(project)
                If removedProjectService IsNot Nothing Then
                    removedProjectService.Dispose()
                    removedProjectService = Nothing
                End If
            End If
        End Sub

        ''' ;DTEEvents_OnBeginShutDown
        ''' <summary>
        ''' Handle DTE OnBeginShutDown event to remove our event handlers.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DTEEvents_OnBeginShutDown()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: Entry. Call AfterClosing.")
            Me.SolutionEvents_AfterClosing() ' Dispose all project services.
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: RemoveEnvDTEEvents.")
            Me.RemoveEnvDTEEvents()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: Exit.")
        End Sub
#End Region

        Private m_VBPackage As VBPackage
        ' Collection of MyExtensibilityProjectServices for each known project.
        Private m_ProjectServices As New Dictionary(Of Project, MyExtensibilityProjectService)()
        ' My Extensibility settings of the current VS. Lazy init.
        Private m_ExtensibilitySettings As MyExtensibilitySettings
        ' Handle solution closing and project removal events.
        Private m_SolutionEvents As EnvDTE.SolutionEvents
        ' Handle DTE closing events
        Private m_DTEEvents As EnvDTE.DTEEvents
        ' lazy-init instance of TrackProjectDocumentsEventsHelper
        Private m_TrackProjectDocumentsEvents As TrackProjectDocumentsEventsHelper

    End Class

End Namespace
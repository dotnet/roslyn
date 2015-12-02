'------------------------------------------------------------------------------
' <copyright file="SettingsGlobalObjectProvider.vb" company="Microsoft">
'     Copyright (c) Microsoft Corporation.  All rights reserved.
' </copyright>                                                                
'------------------------------------------------------------------------------
Imports EnvDTE
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Editors.SettingsDesigner
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.CodeDom
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Configuration
Imports System.Diagnostics
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.SettingsGlobalObjects
    ''' <summary>
    '''    The Settings global object provider will provide global objects for each project-level 
    '''    .setings file.  The Settings GlobalObjectProvider will provide a single global object for 
    '''    each project-level .settings file, and it will also provide a global object for each 
    '''    property the code generator will produce for the project-level .settings files.  When 
    '''    asked to provide global objects for a project, the Settings global object provider 
    '''    performs the following tasks:
    ''' 
    '''    1.	It searches the project for Settings files.
    '''    2.	For each file it finds, it loads up a SettingsSerializer for the 
    '''          file and enumerates all the settings in the file.
    '''    3.	It creats a global object for the Settings file as a whole.  This global object 
    '''          consists of properties that are named like the Settings enumerated
    '''          properties.
    '''
    '''    The Settings global object provider is also responsible for updating the global object service whenever 
    '''     the Settings file changes.  For this, it loads up each Settings file in a text buffer and monitors text change 
    '''     notifications on the buffer.  If it sees a change it runs through each global object and calls PerformChange.   
    '''     When asked for an instance of a global object, the Settings provider will re-parse the .settings file and update 
    '''     values for each global object.  If an item is no longer present, PerformRemove will be called for that item.
    ''' </summary>
    <System.Runtime.InteropServices.Guid("13dc9681-b779-3d9a-9208-c346fe982b63")> _
    <System.Runtime.InteropServices.ComVisible(true)> _
    Friend NotInheritable Class SettingsGlobalObjectProvider
        Inherits GlobalObjectProvider
        Implements IServiceProvider, IVsRunningDocTableEvents, IVsTrackProjectDocumentsEvents2


        Private _globalObjects As Dictionary(Of Project, GlobalObjectCollection)
        Private _typedGlobalObjects As Dictionary(Of Project, Dictionary(Of Type, GlobalObjectCollection))
        Private _oldGlobalObjects As Dictionary(Of Project, GlobalObjectCollection)

        Private vsTrackProjectDocuments As IVsTrackProjectDocuments2
        Private vsTrackProjectDocumentsEventsCookie As UInt32

        Private _solutionEvents As SolutionEvents
        Private _rdt As IVsRunningDocumentTable
        Private _rdtEventsCookie As UInteger

        Private _ignoreAppConfigChanges As Boolean

        Private Shared _globalSettings As TraceSwitch

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Shared ReadOnly Property GlobalSettings() As TraceSwitch
            Get
                If (_globalSettings Is Nothing) Then
                    _globalSettings = New TraceSwitch("GlobalSettings", "Enable tracing for the Typed Settings GlobalObjectProvider.")
                End If

                Return _globalSettings
            End Get
        End Property

        ''' <summary>
        ''' gets the running-doc-table for the instance of VS
        ''' </summary>
        ''' <value>running-doc-table</value>
        ''' <remarks></remarks>
        Friend ReadOnly Property RunningDocTable() As IVsRunningDocumentTable
            Get
                If (_rdt Is Nothing) Then
                    Dim rdt As IVsRunningDocumentTable = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)

                    Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")
                    VSErrorHandler.ThrowOnFailure(rdt.AdviseRunningDocTableEvents(Me, _rdtEventsCookie))
                    _rdt = rdt
                End If

                Return _rdt
            End Get
        End Property


        ''' <summary>
        ''' Performs the actual global object creation.
        ''' </summary>
        ''' <param name="project">the project we're supposed to grope for all .settings files</param>
        ''' <returns>a collection of SettingsFileGlobalObject's</returns>
        Private Function CreateGlobalObjects(ByVal project As Project) As GlobalObjectCollection

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.CreateGlobalObjects(" & DebugGetId(project) & ")...")
#End If

            Dim objects As New GlobalObjectCollection()

            ' There's a bucket of things all global objects will need, so let's get them now.
            '
            Dim dts As DynamicTypeService = TryCast(GetService(GetType(DynamicTypeService)), DynamicTypeService)
            If (dts Is Nothing) Then
                Throw New NotSupportedException(SR.GetString(SR.General_MissingService, GetType(DynamicTypeService).Name))
            End If

            Dim hierarchy As IVsHierarchy = ProjectUtilities.GetVsHierarchy(Me, project)
            Debug.Assert((hierarchy IsNot Nothing), "Unable to get hierarchy for project")

            Dim oldObjects As GlobalObjectCollection = Nothing

            If (_oldGlobalObjects IsNot Nothing) Then
                _oldGlobalObjects.TryGetValue(project, oldObjects)
            End If

            ' get the main .settings file using the IVsProjectSpecialFiles interface
            '
            Dim specialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
            If (specialFiles IsNot Nothing) Then

                Dim itemid As UInteger
                Dim filePath As String = Nothing

                Dim hrGetFile As Integer = specialFiles.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, 0, itemid, filePath)
                If NativeMethods.Succeeded(hrGetFile) Then
                    ' odd, but sometimes we get back a file-name and VSITEMID_NIL, so we don't assert that
                    '   filePath is Nothing or filePath.Length = 0...
                    '
                    Debug.Assert((itemid = VSConstants.VSITEMID_NIL) OrElse (filePath IsNot Nothing AndAlso itemid <> VSConstants.VSITEMID_NIL), "specialFiles.GetFile should fill in both or neither params")

                    If (itemid <> VSConstants.VSITEMID_NIL) Then
                        ' now we need to map this hierarchy/itemid to a ProjectItem
                        '
                        Dim o As Object = Nothing
                        Dim hr As Integer = hierarchy.GetProperty(itemid, Microsoft.VisualStudio.Shell.Interop.__VSHPROPID.VSHPROPID_ExtObject, o)

                        Debug.Assert(NativeMethods.Succeeded(hr), "GetProperty(ExtObject) failed?")
                        Debug.Assert(TypeOf o Is ProjectItem, "returned object is not a ProjectItem?")

                        If (NativeMethods.Succeeded(hr) AndAlso o IsNot Nothing) Then
                            Dim projItem As ProjectItem = TryCast(o, ProjectItem)

                            If (projItem IsNot Nothing) Then

                                ' we only want to enable global objects if the .settings file has our SingleFileGenerator
                                '   associated with it.
                                '
                                Dim name As String = SettingsDesigner.SettingsDesigner.GeneratedClassName(hierarchy, itemid, Nothing, Common.DTEUtils.FileNameFromProjectItem(projItem))
                                Dim typeResolver As ITypeResolutionService = dts.GetTypeResolutionService(hierarchy, itemid)
                                objects.Add(New SettingsFileGlobalObject(Me, hierarchy, projItem, name, typeResolver))

                            End If
                        End If
                    End If
                End If
            End If

            ' loop through each project-item in the given project and look for .settings files
            '
            For Each item As ProjectItem In project.ProjectItems

                If item.Name.EndsWith(Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesigner.SETTINGS_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase) Then

                    ' See if we already have an existing global object with this value.
                    ' If so, we will use it (because the global object itself tracks changes to its
                    ' project item).  If not, we will create a new one.
                    '
                    Dim gob As SettingsFileGlobalObject = Nothing

                    If (oldObjects IsNot Nothing) Then

                        For Each g As GlobalObject In oldObjects

                            Dim gsetting As SettingsFileGlobalObject = TryCast(g, SettingsFileGlobalObject)
                            If (gsetting IsNot Nothing) AndAlso (gsetting.ProjectItem Is item) Then

                                'Consider: we may need to remove this from the collection if
                                ' the CustomTool property is not our generator anymore
                                '
                                gob = gsetting
                                Exit For
                            End If
                        Next g
                    End If

                    If (gob Is Nothing) Then

                        ' we only want to enable global objects if the .settings file has our SingleFileGenerator
                        '   associated with it.
                        '
                        Dim name As String = SettingsDesigner.SettingsDesigner.GeneratedClassName(hierarchy, VSITEMID.NIL, Nothing, Common.DTEUtils.FileNameFromProjectItem(item))
                        Dim typeResolver As ITypeResolutionService = dts.GetTypeResolutionService(hierarchy, ProjectUtils.ItemId(hierarchy, item))
                        gob = New SettingsFileGlobalObject(Me, hierarchy, item, name, typeResolver)
                    End If

                    If (gob IsNot Nothing) Then

                        ' add the settings-file global object
                        '
                        objects.Add(gob)
                    End If
                End If
            Next item

            If (_oldGlobalObjects IsNot Nothing) AndAlso (oldObjects IsNot Nothing) Then
                _oldGlobalObjects.Remove(project)
            End If

            Return New GlobalObjectCollection(objects, True)

        End Function 'CreateGlobalObjects

        ''' <summary>
        ''' Overrides the standard dispose to clean up COM events we're listening to.
        ''' </summary>
        ''' <param name="disposing">true if called from MyBase.Dispose(), false if called from finalizer</param>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.Dispose(" & disposing & ")...")
#End If

            If (disposing) Then
                If (_solutionEvents IsNot Nothing) Then
                    RemoveHandler _solutionEvents.ProjectRemoved, AddressOf OnProjectRemoved
                    RemoveHandler _solutionEvents.BeforeClosing, AddressOf OnBeforeSolutionClosed
                    _solutionEvents = Nothing

                    ' Remove for item added/removed events...
                    If vsTrackProjectDocuments IsNot Nothing Then
                        If vsTrackProjectDocuments IsNot Nothing AndAlso vsTrackProjectDocumentsEventsCookie <> 0 Then
                            Dim hr As Integer = vsTrackProjectDocuments.UnadviseTrackProjectDocumentsEvents(vsTrackProjectDocumentsEventsCookie)
                            Debug.Assert(NativeMethods.Succeeded(hr), String.Format("GlobalSettings failed to unadvice VsTrackDocumentsEvents {0}", hr))
                        End If
                        vsTrackProjectDocuments = Nothing
                        vsTrackProjectDocumentsEventsCookie = 0
                    End If

                End If

                If (_rdt IsNot Nothing) Then
                    VSErrorHandler.ThrowOnFailure(_rdt.UnadviseRunningDocTableEvents(_rdtEventsCookie))
                    _rdt = Nothing
                End If

                DisposeAllGlobalObjects()
                GC.SuppressFinalize(Me)
            End If

            MyBase.Dispose(disposing)

        End Sub 'Dispose

        ''' <summary>
        ''' Walks our global object tables and detaches each project, and disposes each object.
        ''' </summary>
        Private Sub DisposeAllGlobalObjects()

            If (_globalObjects IsNot Nothing) Then

                For Each de As KeyValuePair(Of Project, GlobalObjectCollection) In _globalObjects
                    DisposeGlobalObjects(de.Value)
                Next

                _globalObjects = Nothing
                _typedGlobalObjects = Nothing
            End If

            _oldGlobalObjects = Nothing

        End Sub 'DisposeAllGlobalObjects

        ''' <summary>
        ''' Disposes all objects in the given global object collection.
        ''' </summary>
        ''' <param name="col">collection of global-objects to dispose</param>
        Private Sub DisposeGlobalObjects(ByVal col As GlobalObjectCollection)

            Debug.Assert(col IsNot Nothing, "don't call this with a null collection")

            For Each gob As GlobalObject In col

                Dim settingGob As SettingsFileGlobalObject = TryCast(gob, SettingsFileGlobalObject)
                If (settingGob IsNot Nothing) Then
                    settingGob.Dispose()
                End If
            Next gob
        End Sub 'DisposeGlobalObjects

        ''' <summary>
        ''' Helper to return the file name for a project item.
        ''' </summary>
        ''' <param name="item">project-item DTE object</param>
        ''' <returns>the file-name for the project-item</returns>
        Friend Shared Function GetFileNameForProjectItem(ByVal item As ProjectItem) As String

            Dim fileName As String

            If item.FileCount > 0 Then
                fileName = item.FileNames(1) 'Index is 1-based
            Else
                fileName = item.Name
            End If

            Return fileName

        End Function 'GetFileNameForProjectItem


        ''' <summary>
        '''  Returns a collection of global objects.  If a base type is specified, the collection 
        '''  should only consist of global objects derived from the given base type.  
        '''  This method should never return a null collection object.  This will be
        '''  validated in the GetGlobalObjects methods that call this method.
        ''' </summary>
        ''' <param name="project">Project that we're searching</param>
        ''' <param name="baseType">type to which we should limit our search</param>
        ''' <returns>collection of global objects</returns>
        ''' <remarks></remarks>
        Protected Overrides Function GetGlobalObjectsCore(ByVal project As Project, ByVal baseType As Type) As GlobalObjectCollection

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.GetGlobalObjectsCore(" & DebugGetId(project) & ", " & DebugGetStr(baseType) & ")...")
#End If

            If (_globalObjects Is Nothing) Then
                _globalObjects = New Dictionary(Of EnvDTE.Project, GlobalObjectCollection)

                ' Start tracking solution events so we can be notified when a project is removed
                _solutionEvents = project.DTE.Events.SolutionEvents
                AddHandler _solutionEvents.ProjectRemoved, AddressOf OnProjectRemoved
                AddHandler _solutionEvents.BeforeClosing, AddressOf OnBeforeSolutionClosed

                ' Listen for item added/removed events...
                If vsTrackProjectDocuments Is Nothing Then
                    vsTrackProjectDocuments = DirectCast(GetService(GetType(SVsTrackProjectDocuments)), IVsTrackProjectDocuments2)
                    If (vsTrackProjectDocuments IsNot Nothing) Then
                        VSErrorHandler.ThrowOnFailure(vsTrackProjectDocuments.AdviseTrackProjectDocumentsEvents(Me, vsTrackProjectDocumentsEventsCookie))
                        Debug.Assert(vsTrackProjectDocumentsEventsCookie <> 0, "AdviseTrackProjectDocumentsEvents gave us a 0 cookie!")
                    End If
                End If

            End If

            Dim result As GlobalObjectCollection

            If _globalObjects.ContainsKey(project) Then
                result = _globalObjects(project)
            Else
                Dim objects As GlobalObjectCollection = CreateGlobalObjects(project)

                _globalObjects.Item(project) = objects
                result = objects
            End If

            If (baseType IsNot Nothing) Then
                ' If we were given a base type, limit our result by that type.
                '
                If (_typedGlobalObjects Is Nothing) Then
                    _typedGlobalObjects = New Dictionary(Of EnvDTE.Project, Dictionary(Of Type, GlobalObjectCollection))
                End If '

                Dim gobs As Dictionary(Of Type, GlobalObjectCollection)

                If _typedGlobalObjects.ContainsKey(project) Then
                    gobs = _typedGlobalObjects(project)
                Else
                    gobs = New Dictionary(Of Type, GlobalObjectCollection)
                    _typedGlobalObjects.Item(project) = gobs
                End If

                If gobs.ContainsKey(baseType) Then
                    result = gobs(baseType)
                Else
                    Dim typedResult As New GlobalObjectCollection()

                    ' If the type being requested is ApplicationSettingsBase or any type that derives from it, 
                    '   then the caller is asking for a description of properties to which they can bind or 
                    '   retrieve values, and they won't be able to bind unless we're generating code, so we 
                    '   need to make sure we enable generation for any of these instances.
                    '
                    Dim needToEnsureGenerator As Boolean = (GetType(SettingsBase).IsAssignableFrom(baseType))

                    Dim gob As GlobalObject
                    For Each gob In result
                        If baseType.IsAssignableFrom(gob.ObjectType) Then

                            Dim addToResults As Boolean = True

                            If (needToEnsureGenerator) Then

                                Dim settingGob As SettingsFileGlobalObject = TryCast(gob, SettingsFileGlobalObject)
                                Debug.Assert(settingGob IsNot Nothing, "how did we put a global-object other than a SettingsFileGlobalObject in our list?")

                                If (settingGob IsNot Nothing) Then
                                    Try
                                        settingGob.EnsureGeneratingSettingClass()
                                    Catch ex As CheckoutException
                                        addToResults = False
                                    End Try
                                End If
                            End If

                            If (addToResults) Then
                                typedResult.Add(gob)
                            End If
                        End If
                    Next gob

                    result = New GlobalObjectCollection(typedResult, True)
                    gobs.Item(baseType) = result
                End If
            End If


            If result.Count = 0 AndAlso GetType(SettingsBase).IsAssignableFrom(baseType) Then
                ' BEGIN HACK: If we didn't find anything, we'll make a last try using IVsProjectSpecialFiles 
                ' to see if someone just added the file to the project. It seems that Venus fires of the 
                ' IVsTrackProjectDocumentEvents2 asynchronously, so we may not have received the project change 
                ' notification yet....
                '
                ' The project *will* get the notification eventually, so we should be OK if we just return a 
                ' a temporary object representing the global settings object... All other clients will be 
                ' notified as soon as the item is added!
                Dim hierarchy As IVsHierarchy = ProjectUtilities.GetVsHierarchy(Me, project)
                Dim projSpecialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
                If projSpecialFiles IsNot Nothing Then
                    Dim itemid As UInteger
                    Dim filePath As String = Nothing
                    Dim hr As Integer = projSpecialFiles.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), itemid, filePath)
                    If NativeMethods.Succeeded(hr) Then
                        If itemid <> VSITEMID.NIL Then
                            Dim dts As DynamicTypeService = TryCast(GetService(GetType(DynamicTypeService)), DynamicTypeService)
                            If (dts Is Nothing) Then
                                Throw New NotSupportedException(SR.GetString(SR.General_MissingService, GetType(DynamicTypeService).Name))
                            End If

                            Dim typeResolver As ITypeResolutionService = dts.GetTypeResolutionService(hierarchy, itemid)

                            Dim name As String = SettingsDesigner.SettingsDesigner.GeneratedClassName(hierarchy, itemid, Nothing, filePath)
                            Dim tempSettingsGob As New SettingsFileGlobalObject(Me, hierarchy, Common.DTEUtils.ProjectItemFromItemId(hierarchy, itemid), name, typeResolver)
                            result = New GlobalObjectCollection(New SettingsFileGlobalObject() {tempSettingsGob}, True)
                        End If
                    End If
                End If
                ' END HACK
            End If
            Return result
        End Function 'GetGlobalObjectsCore

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="o"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function DebugGetId(ByVal o As Object) As String
#If DEBUG Then
            If (o Is Nothing) Then
                Return "<nada>"
            Else
                Return CStr(o.GetHashCode().ToString("x8"))
            End If
#Else
            Return ""
#End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="o"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function DebugGetStr(ByVal o As Object) As String
#If DEBUG Then
            If (o Is Nothing) Then
                Return "<nada>"
            Else
                Return o.ToString()
            End If
#Else
            Return ""
#End If
        End Function

        ''' <summary>
        ''' This method takes the given doc cookie and tries to locate a matching
        ''' SettingsFileGlobalObject for it.  If it finds one, it will return it, along with an
        ''' AddRef'd punk pointing to the doc data.  If it returns null, punk is
        ''' indeterminate (but definitely not addref'd).
        ''' </summary>
        ''' <param name="docCookie">running-doc-table cookie representing a doc</param>
        ''' <param name="punk">punk pointing to the doc-data</param>
        ''' <returns>global setting object</returns>
        Private Function GetObjectForCookie(ByVal docCookie As UInteger, ByRef punk As IntPtr) As SettingsFileGlobalObject

            punk = IntPtr.Zero
            If (_globalObjects Is Nothing) OrElse (_globalObjects.Count = 0) Then
                Return Nothing
            End If

            Dim result As SettingsFileGlobalObject = Nothing
            Dim flags, readLocks, editLocks, itemid As UInteger
            Dim fileName As String = Nothing
            Dim hierarchy As IVsHierarchy = Nothing
            Dim localPunk As IntPtr = IntPtr.Zero
            Dim rdt As IVsRunningDocumentTable = RunningDocTable

            Debug.Assert((rdt IsNot Nothing), "Only call with a valid RDT")

            If (rdt IsNot Nothing) Then
                VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, fileName, hierarchy, itemid, localPunk))
                Try
                    Dim obj As Object = Nothing
                    Dim hr As Integer = hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, obj)
                    Dim proj As Project = TryCast(obj, Project)

                    If NativeMethods.Succeeded(hr) AndAlso (proj IsNot Nothing) Then

                        Dim gobs As GlobalObjectCollection = Nothing
                        If (_globalObjects.TryGetValue(proj, gobs)) Then

                            For Each gob As GlobalObject In gobs
                                Dim settingGob As SettingsFileGlobalObject = TryCast(gob, SettingsFileGlobalObject)

                                If (settingGob IsNot Nothing) AndAlso (String.Equals(GetFileNameForProjectItem(settingGob.ProjectItem), fileName, StringComparison.OrdinalIgnoreCase)) Then
                                    result = settingGob
                                    punk = localPunk
                                    localPunk = IntPtr.Zero ' xfer ref
                                    Exit For
                                End If
                            Next gob
                        End If
                    End If
                Finally
                    If (localPunk <> IntPtr.Zero) Then
                        Marshal.Release(localPunk)
                    End If
                End Try
            End If

            Return result

        End Function 'GetObjectForCookie

        ''' <summary>
        ''' IServiceProvider implementation we pass to DocData.
        ''' </summary>
        ''' <param name="serviceType">service to fetch</param>
        ''' <returns>object implementing requested service or Nothing</returns>
        Private Overloads Function GetService(ByVal serviceType As Type) As Object _
            Implements IServiceProvider.GetService

            Return MyBase.GetService(serviceType)
        End Function

        ''' <summary>
        ''' Called when the solution is closed.
        ''' </summary>
        Private Sub OnBeforeSolutionClosed()

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.OnBeforeSolutionClosed...")
#End If

            If ((_globalObjects IsNot Nothing) AndAlso (_globalObjects.Count > 0)) Then
                DisposeAllGlobalObjects()
                OnCollectionChanged(EventArgs.Empty)
            End If

            If (_typedGlobalObjects IsNot Nothing) Then
                _typedGlobalObjects.Clear()
            End If

            If (_oldGlobalObjects IsNot Nothing) Then
                _oldGlobalObjects.Clear()
            End If

        End Sub 'OnBeforeSolutionClosed

        ''' <summary>
        ''' Called when a new project item is added to a project we're tracking.  When
        ''' a new item is added, we clear off our prior collection and announce
        ''' that the collection has changed.  We want to return the same global
        ''' object for the same project all the time, so whenever something
        ''' changes for our collection, we simply save off the old list and re-integrate
        ''' it for each .settings file.
        ''' </summary>
        ''' <param name="projectItem">item being added to the project</param>
        Private Sub OnProjectItemAdded(ByVal projectItem As ProjectItem)

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.OnProjectItemAdded(" & DebugGetId(projectItem) & ")...")
#End If

            If (projectItem.Name.EndsWith(Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesigner.SETTINGS_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase)) Then

                If (_globalObjects IsNot Nothing) Then

                    Dim project As Project = projectItem.ContainingProject
                    Dim existing As GlobalObjectCollection = Nothing

                    If _globalObjects.TryGetValue(project, existing) Then

                        If (_oldGlobalObjects Is Nothing) Then
                            _oldGlobalObjects = New Dictionary(Of EnvDTE.Project, GlobalObjectCollection)
                        End If

                        _oldGlobalObjects(project) = existing
                        'DetachEvents(project)
                        _globalObjects.Remove(project)
                        _typedGlobalObjects = Nothing
                        OnCollectionChanged(EventArgs.Empty)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Called to notify us when a project is removed.
        ''' </summary>
        ''' <param name="target">project that was removed</param>
        Private Sub OnProjectRemoved(ByVal target As Project)

#If DEBUG Then
            Debug.WriteLineIf(GlobalSettings.TraceVerbose, "SettingsGlobalObjectProvider.OnProjectRemoved(" & DebugGetId(target) & ")...")
#End If

            If (_globalObjects IsNot Nothing) AndAlso (_globalObjects.ContainsKey(target)) Then
                DisposeGlobalObjects(_globalObjects(target))
                _globalObjects.Remove(target)
                OnCollectionChanged(EventArgs.Empty)
            End If

            If ((_typedGlobalObjects IsNot Nothing) AndAlso (_typedGlobalObjects.ContainsKey(target))) Then
                _typedGlobalObjects.Remove(target)
            End If

            If ((_oldGlobalObjects IsNot Nothing) AndAlso (_oldGlobalObjects.ContainsKey(target))) Then
                _oldGlobalObjects.Remove(target)
            End If

        End Sub 'OnProjectRemoved

        ''' <summary>
        ''' Given a docCookie, determine if the corresponding file is the default app.config file in a 
        ''' project, and if it is, which project it is
        ''' </summary>
        ''' <param name="docCookie">Cookie corresponding to the file we are looking for</param>
        ''' <param name="hier">Nothing or the IVsHierarchy in which the document is the default app.config file</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsDefaultAppConfigFile(ByVal docCookie As UInteger, ByRef hier As IVsHierarchy) As Boolean
            Dim itemid As UInteger = VSITEMID.NIL

            ' Now, look in the RDT to see if this doc data already has an edit lock on it.
            ' if it does, we keep it and we begin tracking changes.  Otherwise, we
            ' let it get disposed.
            '
            Debug.Assert((RunningDocTable IsNot Nothing), "What?  No RDT?")

            Dim localPunk As IntPtr = IntPtr.Zero
            Try
                ' we don't want the punk, but since this is Marshal'ed ByRef on our behalf by VB, the native side will 
                '   get a pointer and fill it in and we'll leak it. So we need to take care of it by giving them a punk 
                '   and then releasing it once we get it back on the managed side.
                '
                VSErrorHandler.ThrowOnFailure(Me.RunningDocTable.GetDocumentInfo(docCookie, Nothing, Nothing, Nothing, Nothing, hier, itemid, localPunk))
            Finally
                If (localPunk <> IntPtr.Zero) Then
                    Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            If hier IsNot Nothing AndAlso itemid <> VSITEMID.NIL Then
                Dim specFiles As IVsProjectSpecialFiles = TryCast(hier, IVsProjectSpecialFiles)
                If specFiles IsNot Nothing Then
                    Dim appConfigItemId As UInteger = 0
                    Dim AppConfigFileName As String = Nothing
                    Dim hr As Integer = specFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, 0, appConfigItemId, AppConfigFileName)
                    If hr = NativeMethods.S_OK Then
                        Return appConfigItemId = itemid
                    End If
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' We don't want to reload all settings objects if the change was caused by a flush of one of
        ''' the settings objects... That will only affect that particular object, and it should raise
        ''' it's own change notifications!
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Property IgnoreAppConfigChanges() As Boolean
            Get
                Return _ignoreAppConfigChanges
            End Get
            Set(ByVal value As Boolean)
#If DEBUG Then
                Debug.WriteLineIf(GlobalSettings.TraceVerbose, String.Format("SettingsGlobalObject:IgnoreAppConfigChanges set to {0}", value))
#End If
                _ignoreAppConfigChanges = value
            End Set
        End Property

#Region "IVsRunningDocTableEvents Implementation"
        ''' <summary>
        ''' IVsRunningDocTable events.  Here we check for the reload attribute, which
        ''' means that the document has been changed outside of VS.
        ''' </summary>
        ''' <param name="docCookie">id for the document who's attribute is changing</param>
        ''' <param name="attributes">the attribute chaning</param>
        Private Function OnAfterAttributeChange(ByVal docCookie As UInteger, ByVal attributes As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterAttributeChange

            If _globalObjects Is Nothing Then
                ' No global objects => Nothing to do...
                Return NativeMethods.S_OK
            End If


            Dim hier As IVsHierarchy = Nothing
            If Not IgnoreAppConfigChanges AndAlso _
               ((attributes And (__VSRDTATTRIB.RDTA_DocDataReloaded Or __VSRDTATTRIB.RDTA_DocDataIsNotDirty)) <> 0) AndAlso _
               IsDefaultAppConfigFile(docCookie, hier) _
            Then
#If DEBUG Then
                Debug.WriteLineIf(GlobalSettings.TraceVerbose, "AppConfigValue changed - raising change notifications for all global settings objects")
#End If
                Debug.Assert(hier IsNot Nothing, "Why didn't we find a IVsHierarchy for the default app.config file?")
                Dim goc As GlobalObjectCollection = Nothing
                If Me._globalObjects.TryGetValue(Editors.Common.DTEUtils.EnvDTEProject(hier), _
                                                 goc) _
                Then
                    For Each go As GlobalObject In goc
                        Dim sgo As SettingsFileGlobalObject = TryCast(go, SettingsFileGlobalObject)
                        If sgo IsNot Nothing Then
                            sgo.RaiseChange()
                        End If
                    Next
                End If
            ElseIf ((attributes And __VSRDTATTRIB.RDTA_DocDataReloaded) <> 0) Then
                Dim punk As IntPtr
                Dim settingGob As SettingsFileGlobalObject = GetObjectForCookie(docCookie, punk)

                If (settingGob IsNot Nothing) Then

#If DEBUG Then
                    If (GlobalSettings.TraceVerbose) Then
                        Try
                            Dim attrs As __VSRDTATTRIB = CType(System.Enum.ToObject(GetType(__VSRDTATTRIB), attributes), __VSRDTATTRIB)
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnAfterAttributeChange(" & attrs.ToString("G") & ")...")
                        Catch ex As Exception
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnAfterAttributeChange(0x" & attributes.ToString("X8") & ")...")
                        End Try
                    End If
#End If

                    Debug.Assert(punk <> IntPtr.Zero, "global object but no punk?")
                    Marshal.Release(punk)
                    settingGob.RaiseChange()
                End If
            End If
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' IVsRunningDocTable events.  On first lock of a settings document we ask the 
        ''' global object to start listening to buffer changes.
        ''' </summary>
        ''' <param name="docCookie">id for the document just locked</param>
        ''' <param name="lockType">type of lock taken</param>
        ''' <param name="readLocksRemaining">number or read-locks remaining</param>
        ''' <param name="editLocksRemaining">number of edit-locks remaining</param>
        Private Function OnAfterFirstDocumentLock(ByVal docCookie As UInteger, ByVal lockType As UInteger, ByVal readLocksRemaining As UInteger, ByVal editLocksRemaining As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterFirstDocumentLock

            If ((lockType And _VSRDTFLAGS.RDT_EditLock) = _VSRDTFLAGS.RDT_EditLock) Then
                Dim punk As IntPtr
                Dim settingGob As SettingsFileGlobalObject = GetObjectForCookie(docCookie, punk)

                If (settingGob IsNot Nothing) Then
#If DEBUG Then
                    If (GlobalSettings.TraceVerbose) Then
                        Try
                            Dim flags As _VSRDTFLAGS = CType(System.Enum.ToObject(GetType(_VSRDTFLAGS), lockType), _VSRDTFLAGS)
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnAfterFirstDocumentLock(" & flags.ToString("G") & ")...")
                        Catch ex As Exception
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnAfterFirstDocumentLock(0x" & lockType.ToString("X8") & ")...")
                        End Try
                    End If
#End If

                    Try
                        Debug.Assert(punk <> IntPtr.Zero, "global object but no punk?")
                        settingGob.OnFirstLock(punk)
                    Finally
                        Marshal.Release(punk)
                    End Try
                End If
            End If
        End Function

        ''' <summary>
        ''' IVsRunningDocTable events.  On last unlock of a settings document we
        ''' ask the global object to stop listening to buffer changes.
        ''' </summary>
        ''' <param name="docCookie">id for the document just released</param>
        ''' <param name="lockType">type of lock taken</param>
        ''' <param name="readLocksRemaining">number or read-locks remaining</param>
        ''' <param name="editLocksRemaining">number of edit-locks remaining</param>
        Private Function OnBeforeLastDocumentUnlock(ByVal docCookie As UInteger, ByVal lockType As UInteger, ByVal readLocksRemaining As UInteger, ByVal editLocksRemaining As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock

            If ((lockType And _VSRDTFLAGS.RDT_EditLock) = _VSRDTFLAGS.RDT_EditLock) Then

                Dim punk As IntPtr
                Dim settingGob As SettingsFileGlobalObject = GetObjectForCookie(docCookie, punk)

                If (settingGob IsNot Nothing) Then

#If DEBUG Then
                    If (GlobalSettings.TraceVerbose) Then
                        Try
                            Dim flags As _VSRDTFLAGS = CType(System.Enum.ToObject(GetType(_VSRDTFLAGS), lockType), _VSRDTFLAGS)
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnBeforeLastDocumentUnlock(" & flags.ToString("G") & ")...")
                        Catch ex As Exception
                            Debug.WriteLine("SettingsGlobalObjectProvider.OnBeforeLastDocumentUnlock(0x" & lockType.ToString("x8") & ")...")
                        End Try
                    End If
#End If

                    Debug.Assert(punk <> IntPtr.Zero, "global object but no punk?")
                    Marshal.Release(punk)
                    settingGob.OnLastUnlock()
                End If
            End If
        End Function


        ''' <summary>
        ''' IVsRunningDocTable events we don't care about.
        ''' </summary>
        Private Function OnAfterDocumentWindowHide(ByVal docCookie As UInteger, ByVal frame As IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnAfterDocumentWindowHide
        End Function

        ''' <summary>
        ''' IVsRunningDocTable events we don't care about.
        ''' </summary>
        Private Function OnAfterSave(ByVal docCookie As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterSave
        End Function

        ''' <summary>
        ''' IVsRunningDocTable events we don't care about.
        ''' </summary>
        Private Function OnBeforeDocumentWindowShow(ByVal docCookie As UInteger, ByVal firstShow As Integer, ByVal frame As IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnBeforeDocumentWindowShow
        End Function
#End Region

        Public Function OnAfterAddDirectoriesEx(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSADDDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterAddFilesEx(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSADDFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx
            ' New files added - let's go through 'em and see if they may be .settings files!
            '

            ' Validate arguments....
            Debug.Assert(rgpProjects IsNot Nothing AndAlso rgpProjects.Length = cProjects, "null rgpProjects or bad-length array")
            If (rgpProjects Is Nothing) Then Throw New ArgumentNullException("rgpProjects")
            If (rgpProjects.Length <> cProjects) Then Throw Common.CreateArgumentException("rgpProjects")

            Debug.Assert(rgFirstIndices IsNot Nothing AndAlso rgFirstIndices.Length = cProjects, "null rgFirstIndices or bad-length array")
            If (rgFirstIndices Is Nothing) Then Throw New ArgumentNullException("rgFirstIndices")
            If (rgFirstIndices.Length <> cProjects) Then Throw Common.CreateArgumentException("rgFirstIndices")

            Debug.Assert(rgpszMkDocuments IsNot Nothing AndAlso rgpszMkDocuments.Length = cFiles, "null rgpszMkDocuments or bad-length array")
            If (rgpszMkDocuments Is Nothing) Then Throw New ArgumentNullException("rgpszMkDocuments")
            If (rgpszMkDocuments.Length <> cFiles) Then Throw Common.CreateArgumentException("rgpszMkDocuments")

            Debug.Assert(rgFlags IsNot Nothing AndAlso rgFlags.Length = cFiles, "null rgFlags or bad-length array")
            If (rgFlags Is Nothing) Then Throw New ArgumentNullException("rgFlags")
            If (rgFlags.Length <> cFiles) Then Throw Common.CreateArgumentException("rgFlags")

            ' CONSIDER: Check/pass the flags to the MapToSettingsFileProjectItems to exclude special/dependent/nested files from being added
            Dim expandedHierarchies() As IVsHierarchy = GetCorrespondingProjects(rgpProjects, rgFirstIndices, cFiles)
            For i As Integer = 0 To cFiles - 1
                If expandedHierarchies(i) IsNot Nothing Then
                    AddItem(expandedHierarchies(i), rgpszMkDocuments(i))
                End If
            Next
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterRemoveDirectories(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSREMOVEDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories
            ' CONSIDER: It seems we don't get a item removed for the files in the removed directories, we should really traverse the
            ' directory to find all items to remove...
            ' VsWhidbey 318791
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterRemoveFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSREMOVEFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles
            ' Validate arguments....
            Debug.Assert(rgpProjects IsNot Nothing AndAlso rgpProjects.Length = cProjects, "null rgpProjects or bad-length array")
            If (rgpProjects Is Nothing) Then Throw New ArgumentNullException("rgpProjects")
            If (rgpProjects.Length <> cProjects) Then Throw Common.CreateArgumentException("rgpProjects")

            Debug.Assert(rgFirstIndices IsNot Nothing AndAlso rgFirstIndices.Length = cProjects, "null rgFirstIndices or bad-length array")
            If (rgFirstIndices Is Nothing) Then Throw New ArgumentNullException("rgFirstIndices")
            If (rgFirstIndices.Length <> cProjects) Then Throw Common.CreateArgumentException("rgFirstIndices")

            Debug.Assert(rgpszMkDocuments IsNot Nothing AndAlso rgpszMkDocuments.Length = cFiles, "null rgpszMkDocuments or bad-length array")
            If (rgpszMkDocuments Is Nothing) Then Throw New ArgumentNullException("rgpszMkDocuments")
            If (rgpszMkDocuments.Length <> cFiles) Then Throw Common.CreateArgumentException("rgpszMkDocuments")

            Debug.Assert(rgFlags IsNot Nothing AndAlso rgFlags.Length = cFiles, "null rgFlags or bad-length array")
            If (rgFlags Is Nothing) Then Throw New ArgumentNullException("rgFlags")
            If (rgFlags.Length <> cFiles) Then Throw Common.CreateArgumentException("rgFlags")

            Dim expandedHierarchies() As IVsHierarchy = GetCorrespondingProjects(rgpProjects, rgFirstIndices, cFiles)
            For i As Integer = 0 To cFiles - 1
                If expandedHierarchies(i) IsNot Nothing Then
                    RemoveItem(expandedHierarchies(i), rgpszMkDocuments(i))
                End If
            Next
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterRenameDirectories(ByVal cProjects As Integer, ByVal cDirs As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSRENAMEDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterRenameFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSRENAMEFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles
            ' Validate arguments....
            Debug.Assert(rgpProjects IsNot Nothing AndAlso rgpProjects.Length = cProjects, "null rgpProjects or bad-length array")
            If (rgpProjects Is Nothing) Then Throw New ArgumentNullException("rgpProjects")
            If (rgpProjects.Length <> cProjects) Then Throw Common.CreateArgumentException("rgpProjects")

            Debug.Assert(rgFirstIndices IsNot Nothing AndAlso rgFirstIndices.Length = cProjects, "null rgFirstIndices or bad-length array")
            If (rgFirstIndices Is Nothing) Then Throw New ArgumentNullException("rgFirstIndices")
            If (rgFirstIndices.Length <> cProjects) Then Throw Common.CreateArgumentException("rgFirstIndices")

            Debug.Assert(rgszMkOldNames IsNot Nothing AndAlso rgszMkOldNames.Length = cFiles, "null rgszMkOldNames or bad-length array")
            If (rgszMkOldNames Is Nothing) Then Throw New ArgumentNullException("rgszMkOldNames")
            If (rgszMkOldNames.Length <> cFiles) Then Throw Common.CreateArgumentException("rgszMkOldNames")

            Debug.Assert(rgszMkNewNames IsNot Nothing AndAlso rgszMkNewNames.Length = cFiles, "null rgszMkNewNames or bad-length array")
            If (rgszMkNewNames Is Nothing) Then Throw New ArgumentNullException("rgszMkNewNames")
            If (rgszMkNewNames.Length <> cFiles) Then Throw Common.CreateArgumentException("rgszMkNewNames")

            Debug.Assert(rgFlags IsNot Nothing AndAlso rgFlags.Length = cFiles, "null rgFlags or bad-length array")
            If (rgFlags Is Nothing) Then Throw New ArgumentNullException("rgFlags")
            If (rgFlags.Length <> cFiles) Then Throw Common.CreateArgumentException("rgFlags")


            Dim expandedHierarchies() As IVsHierarchy = GetCorrespondingProjects(rgpProjects, rgFirstIndices, cFiles)
            For i As Integer = 0 To cFiles - 1
                If expandedHierarchies(i) IsNot Nothing Then
                    RemoveItem(expandedHierarchies(i), rgszMkOldNames(i))
                    AddItem(expandedHierarchies(i), rgszMkNewNames(i))
                End If
            Next
            Return NativeMethods.S_OK
        End Function

        Public Function OnAfterSccStatusChanged(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgdwSccStatus() As UInteger) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryAddDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirectories As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYADDDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYADDDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYADDDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryAddFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYADDFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYADDFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYADDFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryAddFiles
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryRemoveDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirectories As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYREMOVEDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYREMOVEDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYREMOVEDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryRemoveFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYREMOVEFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYREMOVEFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYREMOVEFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryRenameDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirs As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSQUERYRENAMEDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYRENAMEDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYRENAMEDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories
            Return NativeMethods.S_OK
        End Function

        Public Function OnQueryRenameFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSQUERYRENAMEFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYRENAMEFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYRENAMEFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Map the given file start indexes into their corresponding projects. The project on position N in the returned
        ''' array should be the project associated with the filename on position N in the array of files into which the
        ''' rgFirstIndices parameter points. This way we can simply loop through all the items and use the SAME index into
        ''' both the projects and filenames arrays.... 
        ''' This is a *little* extra work up-front everytime a file is added to the project, but it is a small loop, and 
        ''' there is not much info that is being stored. Even adding 10000 files at the same time would only require 10000
        ''' iterations and use about ~40k memory.
        ''' </summary>
        ''' <param name="rgpProjects"></param>
        ''' <param name="rgFirstIndices"></param>
        ''' <param name="cFiles"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetCorrespondingProjects(ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal cFiles As Integer) As Shell.Interop.IVsHierarchy()
            ' We trust that someone has already checked these parameters, so we only ASSERT if something looks
            ' bogus....
            Debug.Assert(rgpProjects IsNot Nothing, "NULL rgpProjects passed in - this is a bug the SettingsGlobalObject!")
            Debug.Assert(rgFirstIndices IsNot Nothing, "NULL rgFirstIndices passed in - this is a bug the SettingsGlobalObject!")
            Debug.Assert(cFiles > 0, "Negative or zero count of files to map!?")

            ' Allocate somewhere to put our results
            Dim result(cFiles) As Shell.Interop.IVsHierarchy
            Dim fileIndex As Integer = rgFirstIndices(0)

            ' Walk all projects passed in
            For projectIndex As Integer = 0 To rgpProjects.Length - 1
                Dim hierarchy As Shell.Interop.IVsHierarchy = TryCast(rgpProjects(projectIndex), IVsHierarchy)
                Dim endIndex As Integer
                If projectIndex >= rgpProjects.Length - 1 Then
                    ' If this is the last project, then the end index corresponds to 
                    ' the last index of files (that is, the cFiles parameters passed in - 1)
                    endIndex = cFiles - 1
                Else
                    ' This is not the last project - the end index is the start index for the next
                    ' project - 1
                    endIndex = rgFirstIndices(projectIndex + 1) - 1
                End If
                ' Put the correct hierarchy in the results
                While fileIndex <= endIndex
                    result(fileIndex) = hierarchy
                    fileIndex += 1
                End While
            Next

            Return result
        End Function


        ''' <summary>
        ''' Called when a new project item is removed from a project we're tracking.  In this
        ''' case we must find the individaul global object, raise its removing event, and
        ''' then dispose it.
        ''' </summary>
        ''' <param name="hierarchy">IVsHierarchy (project) that we are going to remove an item from</param>
        ''' <param name="fileName">Name of the file in the project that we are removing</param>
        Private Sub RemoveItem(ByVal hierarchy As IVsHierarchy, ByVal fileName As String)
            ' Search for this project item
            If (_globalObjects IsNot Nothing) Then
                If (fileName.EndsWith(Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesigner.SETTINGS_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase)) Then
                    Dim targetObject As SettingsFileGlobalObject = Nothing
                    Dim ProjectObj As Object = Nothing
                    VSErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ProjectObj))
                    Dim project As Project = TryCast(ProjectObj, EnvDTE.Project)

                    Dim newObjects As New GlobalObjectCollection()

                    Dim objects As GlobalObjectCollection = Nothing
                    _globalObjects.TryGetValue(project, objects)
                    If (objects IsNot Nothing) Then

                        Dim gob As GlobalObject
                        For Each gob In objects
                            Dim settingGob As SettingsFileGlobalObject = TryCast(gob, SettingsFileGlobalObject)

                            If (settingGob IsNot Nothing) Then
                                If (settingGob.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) Then
                                    Debug.Assert(targetObject Is Nothing, "We have multiple global objects pointing to the same project item!")
                                    targetObject = settingGob
                                Else
                                    newObjects.Add(settingGob)
                                End If
                            End If
                        Next gob
                    End If

                    If (targetObject IsNot Nothing) Then

                        targetObject.RaiseRemove()
                        _globalObjects.Item(project) = New GlobalObjectCollection(newObjects, True)
                        _typedGlobalObjects = Nothing
                        OnCollectionChanged(EventArgs.Empty)

                    End If
                End If
            End If

        End Sub

        Private Sub AddItem(ByVal project As IVsHierarchy, ByVal fileName As String)
            ' Only care about files with names that end with .settings
            If fileName.EndsWith(Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesigner.SETTINGS_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase) Then
                Try
                    Try
                        If Not System.IO.File.Exists(fileName) Then
                            Return
                        End If
                    Catch ex As ArgumentException
                        ' Argument exception = filename was invalid = file doesn't exist!
                        Return
                    End Try
                    Dim itemId As UInteger
                    VSErrorHandler.ThrowOnFailure(project.ParseCanonicalName(fileName, itemId))

                    Dim o As Object = Nothing
                    VSErrorHandler.ThrowOnFailure(project.GetProperty(itemId, Microsoft.VisualStudio.Shell.Interop.__VSHPROPID.VSHPROPID_ExtObject, o))

                    Debug.Assert(TypeOf o Is ProjectItem, "returned object is not a ProjectItem?")

                    Dim projItem As ProjectItem = TryCast(o, ProjectItem)
                    If (projItem IsNot Nothing) Then
                        OnProjectItemAdded(projItem)
                    End If
                Catch Ex As Exception When Not Common.Utils.IsUnrecoverable(Ex)
                    ' Dunno what kind of exceptions ParseCanonicalName or GetProperty may throw....
                    Debug.Fail("Caught exception while trying to map added/removed files to project items")
                End Try
            End If
        End Sub
    End Class



























    ''' <summary>
    ''' Our global object for .settings files.  This monitors changes to the individual settings file.
    ''' </summary>
    Friend NotInheritable Class SettingsFileGlobalObject
        Inherits GlobalObject
        Implements Microsoft.VSDesigner.VSDesignerPackage.IRefreshSettingsObject

        Private _provider As SettingsGlobalObjectProvider
        Private _item As ProjectItem
        Private _hierarchy As IVsHierarchy
        Private _itemid As UInteger
        Private _typeResolver As ITypeResolutionService
        Private _docData As DocData
        Private _virtualType As Type
        Private _dtSettings As DesignTimeSettings
        Private _namespace As String
        Private _className As String
        Private _ignoreDocLock As Boolean
        Private _fileName As String
        Private _valueCache As SettingsValueCache
        Private _typeCache As SettingsTypeCache

        ''' <summary>
        ''' By deserializing types, we may cause re-entrancy into the load settings code 
        ''' (the XML serializer may use the DynamicTypeService to resolve the type to
        ''' deserialize, which means that we may be called again - See VsWhidbey 444946)
        ''' </summary>
        ''' <remarks></remarks>
        Private _loadingSettings As Boolean = False

        ''' <summary>
        ''' Create a new settings global object.  We defer building the type until later.
        ''' </summary>
        ''' <param name="provider">parent provider class</param>
        ''' <param name="item">EnvDTE project-item for the file this setting represents</param>
        ''' <param name="name">the name of this setting class</param>
        ''' <param name="typeResolver">type-resolution service</param>
        Friend Sub New(ByVal provider As SettingsGlobalObjectProvider, ByVal hierarchy As IVsHierarchy, ByVal item As ProjectItem, ByVal name As String, ByVal typeResolver As ITypeResolutionService)
            MyBase.New(GetType(Object), name)

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.ctor(" & name & ")...")
#End If

            _provider = provider
            _hierarchy = hierarchy
            _item = item
            _className = name
            _typeResolver = typeResolver
            _fileName = SettingsGlobalObjectProvider.GetFileNameForProjectItem(item)
            _valueCache = New SettingsValueCache(System.Globalization.CultureInfo.InvariantCulture)
            _typeCache = New SettingsTypeCache(hierarchy, Common.DTEUtils.ItemIdOfProjectItem(hierarchy, item), typeResolver, True)

        End Sub 'New

        ''' <summary>
        ''' access to the DesignTimeSettings class this global-object represents
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property Settings() As DesignTimeSettings
            Get
                Debug.Assert(_dtSettings IsNot Nothing, "missing the design-time settings collection?")
                Return _dtSettings
            End Get
        End Property


        Friend Function ResolveType(ByVal typeName As String) As System.Type
            Return _typeCache.GetSettingType(typeName)
        End Function

        Friend Function DeserializeValue(ByVal type As System.Type, ByVal serializedValue As String) As Object
            Return _valueCache.GetValue(type, serializedValue)
        End Function

        ''' <summary>
        ''' gets/sets the DocData associated with this global object, appropriately adding
        ''' or removing event listeners
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property DocData() As DocData
            Get
                Return _docData
            End Get
            Set(ByVal Value As DocData)

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.DocData-setter(" & Name & "), _docData is " _
                                & SettingsGlobalObjectProvider.DebugGetStr(_docData) & ", value is " & SettingsGlobalObjectProvider.DebugGetStr(Value) & "...")
#End If

                If (_docData IsNot Nothing) Then
                    RemoveHandler _docData.DataChanged, AddressOf OnDocDataChanged
                    Dim d As DocData = _docData
                    _docData = Nothing
                    ' Dispose can fire RDT events that would cause us to recurse, so it's safer to
                    '   disable our member variable before calling Dispose...
                    '
                    d.Dispose()
                End If

                If (Value IsNot Nothing) Then
                    AddHandler Value.DataChanged, AddressOf OnDocDataChanged
                    _docData = Value
                End If
            End Set
        End Property


        ''' <summary>
        ''' Interesting to know what project item we're related to.
        ''' </summary>
        ''' <value>the project we are contained in</value>
        Friend ReadOnly Property ProjectItem() As ProjectItem
            Get
                Return _item
            End Get
        End Property

        ''' <summary>
        ''' Interesting to know what file name our project item has
        ''' </summary>
        ''' <value>the project we are contained in</value>
        Friend ReadOnly Property FileName() As String
            Get
                Return _fileName
            End Get
        End Property

        ''' <summary>
        ''' Builds a virtual type based on the data stored in this global object.  Also populates
        ''' all of the child global objects.
        ''' </summary>
        ''' <returns></returns>
        Private Function BuildType() As Type

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.BuildType(" & CStr(_className) & ")...")
#End If

            If (_dtSettings IsNot Nothing) Then
                _dtSettings.Dispose()
                _dtSettings = Nothing
            End If

            Dim fileName As String = SettingsGlobalObjectProvider.GetFileNameForProjectItem(_item)

            ' load up our designer object-model
            '
            _dtSettings = LoadSettings(fileName)

            ' We need to check for unresolved types. The virtual type builder will not be happy if it can't resolve the 
            ' types that we pass in, so we've got to clean up the settings class first. We still need to keep the original 
            ' instance around, though, since that is needed when we serialize the data later...
            Dim unresolvedTypesExist As Boolean = False
            For Each setting As DesignTimeSettingInstance In _dtSettings
                Dim TypeNameResolutionComponent As New SettingTypeNameResolutionService("") ' We don't care about the code model
                Dim settingType As System.Type = _typeCache.GetSettingType(TypeNameResolutionComponent.PersistedSettingTypeNameToFxTypeName(setting.SettingTypeName))
                If settingType Is Nothing Then
                    ' No setting type = failed to resolve type!
                    unresolvedTypesExist = True
                ElseIf _typeResolver.GetType(settingType.FullName, False) Is Nothing Then
                    ' We managed to resolve this type, but the type resolution service we are going to use to 
                    ' build the virtual type failed to do so. Since we know what assembly this type belongs to,
                    ' we can add a reference to the type...
                    _typeResolver.ReferenceAssembly(settingType.Assembly.GetName())
                End If
            Next

            Dim designTimeSettingsToPresent As DesignTimeSettings
            If unresolvedTypesExist Then
                ' Yep, we failed to resolve one or more setting types. Load another copy of the settings file and
                ' modify that... This should be fairly uncommon since there isn't an easy way to add settings without 
                ' referencing the defining assembly. 
                designTimeSettingsToPresent = LoadSettings(fileName)
                Dim itemsToRemove As New List(Of DesignTimeSettingInstance)
                For Each instance As DesignTimeSettingInstance In designTimeSettingsToPresent
                    If _typeCache.GetSettingType(instance.SettingTypeName) Is Nothing Then
                        itemsToRemove.Add(instance)
                    End If
                Next
                For Each instance As DesignTimeSettingInstance In itemsToRemove
                    designTimeSettingsToPresent.Remove(instance)
                Next
            Else
                ' All types resolved just fine. Use the originally loaded object to generate the CodeDom tree...
                designTimeSettingsToPresent = _dtSettings
            End If

            Debug.Assert(_hierarchy IsNot Nothing, "LoadSettings should have retrieved the hierarchy")
            Debug.Assert(_itemid <> 0, "LoadSettings should have retrieved the itemid")

            Try
                _namespace = SettingsDesigner.ProjectUtils.GeneratedSettingsClassNamespace(_hierarchy, _itemid, True)

            Catch ex As COMException
                Debug.Fail("GetProperty(DefaultNamespace) failed?")
            End Try

            Dim isDesignTime As Boolean = True

            Dim ccu As CodeCompileUnit = SettingsSingleFileGenerator.Create(_hierarchy, designTimeSettingsToPresent, _namespace, fileName, isDesignTime, Nothing, Reflection.TypeAttributes.Public Or Reflection.TypeAttributes.Sealed)
            Debug.Assert(ccu.Namespaces.Count = 1, "Expected a single namespace from SettingsSingleFileGenerator")

            ' Remove structure from the compile unit that virtual types can't handle.  
            ' Then, create a virtual type based on the code dom we got back.
            '
            ScrubCompileUnit(ccu)

            Dim builder As New VirtualTypeBuilder()

            builder.BaseType = GetType(System.Configuration.ApplicationSettingsBase)
            builder.Implementor = New SettingsFileTypeImplementor(Me)

            ' We have to make sure that the type resolver is referencing the assembly where the applicationsettingsbase
            ' is defined, otherwise bad things will happen when you try to build the virtual type.
            _typeResolver.ReferenceAssembly(builder.BaseType.Assembly().GetName())

            builder.InitializeFromType(_typeResolver, ccu.Namespaces(0))

            ' next add the special shared property that gets the single instance -- note that this property
            '   was originally in the CodeCompileUnit we got back, but we removed it while scrubbing
            '   b/c a VirtualType can't have a property of its own type. So we add a hacked property
            '   by the same name with type Object instead.
            '
            builder.Properties.Add(SettingsSingleFileGenerator.DefaultInstancePropertyName, _
                                GetType(Object), _
                                True, _
                                New Attribute() {}, _
                                System.Reflection.MethodAttributes.Public Or System.Reflection.MethodAttributes.Static)

            Return builder.CreateType()

        End Function 'BuildType

        ''' <summary>
        ''' Creates an instance of this global type
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function CreateInstance() As Object

            Dim value As Object = TypeDescriptor.CreateInstance(DirectCast(_provider, IServiceProvider), GetObjectType(), New Type() {}, New Object() {})

            TypeDescriptor.AddAttributes(value, New SettingsGlobalObjectValueAttribute(Me, Nothing))

            Return value

        End Function 'CreateInstance

        '/ <devdoc>
        '/    Internal dispose. We don't implement IDisposable because we don't want the outside world
        '/    to call this.  Here we detach our events from the RDT.
        '/ </devdoc>
        Friend Sub Dispose()

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.Dispose(" & CStr(_className) & ")...")
#End If

            OnLastUnlock()

            If _typeCache IsNot Nothing Then
                CType(_typeCache, IDisposable).Dispose()
                _typeCache = Nothing
            End If

        End Sub 'Dispose

        ''' <summary>
        ''' ensures that this file is generating a typed-settings class
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub EnsureGeneratingSettingClass()

            Dim customTool As String
            Try
                customTool = TryCast(ProjectItem.Properties.Item("CustomTool").Value, String)

                If ((customTool IsNot Nothing) AndAlso (customTool.Length = 0)) Then
                    ' non-Nothing + 0-length means we're not generating a class for
                    '   this .settings file, so turn on our generator
                    '
                    ProjectItem.Properties.Item("CustomTool").Value = SettingsSingleFileGenerator.SingleFileGeneratorName
                End If
            Catch argEx As System.ArgumentException
                ' venus throws this since they don't support the CustomTool property, and all we
                '   can do is catch it and ignore it
            Catch ex As Exception
                ' we don't expect to fail randomly, but we also don't really want to propagate
                '   failures from project systems we don't know about out to the user through
                '   the global-object-service since users won't really know that we're setting
                '   the CustomTool property on a .settings file while they're editing a form file
                '
                Debug.Fail(ex.ToString())
            End Try

        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="fileName">IN: name of the file to get the document info from</param>
        ''' <param name="readLocks">OUT: Number of read locks for the document</param>
        ''' <param name="editLocks">OUT: Number of edit locks on the document</param>
        ''' <param name="docCookie">OUT: A cookie for the doc, 0 if the doc isn't found in the RDT</param>
        ''' <remarks></remarks>
        Private Sub GetDocumentInfo(ByVal fileName As String, ByRef readLocks As UInteger, ByRef editLocks As UInteger, ByRef itemid As UInteger, ByRef docCookie As UInteger)
            Dim rdt As IVsRunningDocumentTable = _provider.RunningDocTable
            Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")

            Dim foundHierarchy As IVsHierarchy = Nothing
            Common.ShellUtil.GetDocumentInfo(fileName, rdt, foundHierarchy, readLocks, editLocks, itemid, docCookie)

            Debug.Assert(Me._hierarchy Is foundHierarchy, "Different hierarchies!?")
        End Sub

        '/ <devdoc>
        '/    Called by the ObjectType property to retrieve the type of this global type.  Once 
        '/    retrieved the value will be cached until PerformChange is called.  The default 
        '/    implementation of this method returns the type that was passed into the 
        '/    GlobalType constructor.
        '/ </devdoc>
        Protected Overrides Function GetObjectType() As Type

            If _virtualType Is Nothing Then
                _virtualType = BuildType()
            End If

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.GetObjectType(" & CStr(_className) & ")...")
#End If
            Return _virtualType

        End Function 'GetObjectType
        ''' <summary>
        ''' Whenever the schema of the object changes, we have to clear our cached type
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub PerformChange()
            MyBase.PerformChange()
            _virtualType = Nothing
        End Sub

        '/ <devdoc>
        '/    We need to return the serializer for our class.
        '/ </devdoc>
        Public Overrides Function GetSerializerCore(ByVal serializerType As Type) As Object

            If (serializerType Is GetType(CodeDomSerializer)) Then
#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.GetSerializerCore(" & CStr(_className) & ") -- returning GetType(CodeDomSerializer)...")
#End If
                Return SettingsFileCodeDomSerializer.Default
            End If

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.GetSerializerCore(" & CStr(_className) & ") -- returning Nothing...")
#End If
            Return Nothing
        End Function 'GetSerializerCore

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="service"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetService(ByVal service As Type) As Object
            Return DirectCast(_provider, IServiceProvider).GetService(service)
        End Function

        ''' <summary>
        ''' Loads a DesignTimeSettings object from the given fileName
        ''' </summary>
        ''' <param name="fileName">path to a .settings file</param>
        ''' <returns>DesignTimeSettings that represents the given file</returns>
        ''' <remarks></remarks>
        Private Function LoadSettings(ByVal fileName As String) As DesignTimeSettings

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.LoadSettings(" & CStr(_className) & ") -- file '" & fileName & "'...")
#End If

            Debug.Assert(_hierarchy IsNot Nothing, "Loading setting without a hierarchy? Type resolution will probably fail...")

            If _loadingSettings Then
                Return New DesignTimeSettings()
            End If

            Try
                _loadingSettings = True
                Dim data As DocData = DocData

                If (data Is Nothing) Then
                    Dim savedIgnoreDocLock As Boolean = _ignoreDocLock
                    Try
                        _ignoreDocLock = True
                        data = New DocData(_provider, fileName)
                    Finally
                        _ignoreDocLock = savedIgnoreDocLock
                    End Try
                End If

                Dim readLocks, editLocks, docCookie As UInteger
                Try
                    GetDocumentInfo(fileName, readLocks, editLocks, _itemid, docCookie)

#If DEBUG Then
                    Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.LoadSettings(" & CStr(_className) & ") -- editLocks=" & editLocks & ", readLocks=" & readLocks & "...")
#End If
                Catch Ex As Exception
                    Debug.Fail(String.Format("Failed to get document info for document {0}", fileName))
                    Throw
                End Try

                Dim dtSettings As New DesignTimeSettings()
                Try
                    ' We need to check to make sure this doc data is sitting on top
                    ' of a text buffer.  If it isn't, then we can't read it.  For this
                    ' case we will create an empty setting set until the file becomes
                    ' available again.
                    '
                    If (data.Buffer IsNot Nothing) Then

                        Dim bufferSize As Integer
                        VSErrorHandler.ThrowOnFailure(data.Buffer.GetSize(bufferSize))

                        ' if the buffer-size is 0, then this is an empty file, and we should
                        '   proceed as if it's empty (which is where dtSettings is now)
                        '
                        If (bufferSize > 0) Then
                            Dim textReader As New DocDataTextReader(data)
                            Using (textReader)
                                SettingsSerializer.Deserialize(dtSettings, textReader, False)
                            End Using
                        End If

                        ' We've got one edit lock - do anyone else have it too? If so, 
                        ' we shouldn't try to save the document from under them....
                        If ((editLocks > 1) AndAlso (DocData Is Nothing)) Then
                            DocData = data
                            data = Nothing
                        End If
                    End If

                    ' Override values in .settings file with values from app.config (if any)
                    Dim AppConfigDocData As DocData = Nothing
                    Try
                        AppConfigDocData = AppConfigSerializer.GetAppConfigDocData(DirectCast(_provider, IServiceProvider), _hierarchy, False, False)

                        If AppConfigDocData IsNot Nothing Then
                            Dim cfgHelper As New ConfigurationHelperService
                            Dim FullyQualifedClassName As String = ProjectUtils.FullyQualifiedClassName(ProjectUtils.GeneratedSettingsClassNamespace(_hierarchy, _itemid, True), _className)
                            Try
                                AppConfigSerializer.Deserialize(dtSettings, _
                                                                    _typeCache, _
                                                                    _valueCache, _
                                                                    cfgHelper.GetSectionName(FullyQualifedClassName, String.Empty), _
                                                                    AppConfigDocData, _
                                                                    AppConfigSerializer.MergeValueMode.UseAppConfigFileValue)
                            Catch ex As System.Configuration.ConfigurationErrorsException
                                ' App config is broken - not much we can do...
                            End Try
                        End If
                    Finally
                        If AppConfigDocData IsNot Nothing Then
                            AppConfigDocData.Dispose()
                        End If
                    End Try
                Finally
                    ' if DocData is not null, then data should be a ref to DocData (and we don't want to dispose that)
                    '
                    If ((DocData Is Nothing) AndAlso (data IsNot Nothing)) Then
                        data.Dispose()
                    End If
                End Try
                Return dtSettings
            Finally
                _loadingSettings = False
            End Try
        End Function

        ''' <summary>
        ''' Allow clients to force us to update our contents..
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Refresh() Implements VSDesigner.VSDesignerPackage.IRefreshSettingsObject.Refresh
            RaiseChange()
        End Sub

        '/ <devdoc>
        '/    Raised while we are listening to doc data events to signal that
        '/    the global object has changed.
        '/ </devdoc>
        Private Sub OnDocDataChanged(ByVal sender As Object, ByVal e As EventArgs)

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.OnDocDataChanged(" & CStr(_className) & ")...")
#End If

            ' This may happen a LOT.  
            '
            ' CONSIDER: Investigate how to improve performance...
            RaiseChange()
        End Sub 'OnDocDataChanged

        '/ <devdoc>
        '/    Called when the first lock is taken out on this document.  punkDocData
        '/    is an add-ref'd pointer (we do not have to release it, it will be 
        '/    released for us).
        '/ </devdoc>
        Friend Sub OnFirstLock(ByVal punkDocData As IntPtr)

            If (Not _ignoreDocLock) Then
#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.OnFirstLock(" & CStr(_className) & ")...")
#End If
                Debug.Assert(DocData Is Nothing, "Why do we have a DocData if this is the first lock?")

                Dim buffer As Object = Marshal.GetObjectForIUnknown(punkDocData)
                DocData = New DocData(buffer)

            End If

        End Sub 'OnFirstLock

        ''' <summary>
        ''' Called when the last lock is removed from this document.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnLastUnlock()

            If (Not _ignoreDocLock) Then
#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.OnLastUnlock(" & CStr(_className) & ")...")
#End If
                If (_docData IsNot Nothing) Then
                    DocData = Nothing
                End If
            End If
        End Sub 'OnLastUnlock

        '/ <devdoc>
        '/    Called by our RDT code when an item changes.
        '/ </devdoc>
        Friend Sub RaiseChange()

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.RaiseChange(" & CStr(_className) & ")...")
#End If
            PerformChange()

        End Sub 'RaiseChange

        '/ <devdoc>
        '/    Called by project item monitoring code when a project item
        '/    is removed.
        '/ </devdoc>
        Friend Sub RaiseRemove()

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.RaiseRemove(" & CStr(_className) & ")...")
#End If
            PerformRemove()

        End Sub 'RaiseRemove

        ''' <summary>
        ''' If the root namespace changes, we've gotta remove all old stuff from the 
        ''' app/web.config and re-serialize the .settings file
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnRootNamespaceChanged(ByVal OldRootNamespace As String)
            ' If we don't have a persisted namespace, that means that we have a newly created
            ' instance, which doesn't exist in the app.config (yet)
            If _dtSettings.PersistedNamespace IsNot Nothing Then
                Dim namespaceAsInAppConfig As String = ""
                ' We've gotta check if we have the complete namespace persisted in the .settings file,
                ' or if we had the root namespace part changed....
                If ProjectUtils.PersistedNamespaceIncludesRootNamespace(_hierarchy, _itemid) Then
                    ' Everything was there...
                    namespaceAsInAppConfig = _dtSettings.PersistedNamespace
                Else
                    ' Nope, the root namespace was stripped out. Let's prepend the old root namespace
                    ' to the namespace as stored in the .settings file and remove this from the app.config
                    namespaceAsInAppConfig = String.Join(".", New String() {OldRootNamespace, _dtSettings.PersistedNamespace}).TrimStart("."c).TrimEnd("."c)
                End If

                Dim emptySettings As New DesignTimeSettings()
                SaveToAppConfig(emptySettings, namespaceAsInAppConfig, _className)

                Save()
            End If
            _namespace = SettingsDesigner.ProjectUtils.GeneratedSettingsClassNamespace(_hierarchy, _itemid, True)

            ' Since we have changed the root namespace, we have to rebuild the type!
            PerformChange()
        End Sub

        ''' <summary>
        ''' Makes this object write out the values that it has stored in its
        ''' DesignTimeSettings class -- intended to be called after the forms
        ''' designer pokes in new settings
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub Save()

#If DEBUG Then
            Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileGlobalObject.Save(" & CStr(_className) & ")...")
#End If

            Dim docDataTemp As DocData = DocData
            Dim docCookie, itemid As UInteger

            ' Make sure everything is checked out fine...
            Dim filesToCheckOut As List(Of String) = Common.ShellUtil.FileNameAndGeneratedFileName(ProjectItem)
            Dim appConfigOrProjectFile As String = ProjectUtils.AppConfigOrProjectFileNameForCheckout(ProjectItem, _hierarchy)
            If appConfigOrProjectFile <> "" Then
                filesToCheckOut.Add(appConfigOrProjectFile)
            End If
            Microsoft.VisualStudio.Editors.DesignerFramework.SourceCodeControlManager.QueryEditableFiles(Me._provider, filesToCheckOut, True, False)

            Try
                _ignoreDocLock = True

                ' if we haven't stashed off the docdata (because the .settings file is not opened in another editor),
                '   then we need to open the file & note that we should close/dispose it once we're done saving the file
                '
                If (docDataTemp Is Nothing) Then
                    Dim fileName As String = SettingsGlobalObjectProvider.GetFileNameForProjectItem(_item)

                    ' create a temporary DocData for this file
                    '
                    docDataTemp = New DocData(_provider, fileName)

                    ' we need to get the doc-cookie & itemid for this document now so we can save
                    '   it after we're done modifying it
                    '
                    Dim readLocks, editLocks As UInteger
                    GetDocumentInfo(fileName, readLocks, editLocks, itemid, docCookie)
                End If


                Try


                    Dim disposable As IDisposable = docDataTemp.CreateChangeMarker()
                    Try
                        Dim settingsWriter As New DocDataTextWriter(docDataTemp)
                        Try
                            ' We make a copy of the settings we are about to save, since someone may 
                            ' reload us while we are saving, and we don't want to get the app.config and .settings files
                            ' out of sync...
                            Dim settingsToSave As DesignTimeSettings = Settings
                            SettingsSerializer.Serialize(settingsToSave, _
                                SettingsDesigner.ProjectUtils.GeneratedSettingsClassNamespace(_hierarchy, _itemid), _
                                Me._className, _
                                settingsWriter, _
                                DesignerFramework.DesignUtil.GetEncoding(docDataTemp))
                            SaveToAppConfig(settingsToSave, SettingsDesigner.ProjectUtils.GeneratedSettingsClassNamespace(_hierarchy, _itemid, True), _
                                Me._className)
                        Finally
                            settingsWriter.Close()
                        End Try
                    Finally
                        disposable.Dispose()
                    End Try
                Finally
                    ' and then clean up the DocData if we only opened it for this save operation
                    '
                    If (docCookie <> 0) Then

                        ' if we opened the document just for this save operation, then we need to persist
                        '   the change before disposing the doc
                        '
                        Dim rdt As IVsRunningDocumentTable = _provider.RunningDocTable
                        Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")

                        VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), _hierarchy, itemid, docCookie))

                        Debug.Assert(docDataTemp IsNot Nothing, "we must have a docDataTemp to get here...")
                        docDataTemp.Dispose()
                    Else
                        ' If we didn't save the document, we still have to make sure that the single file generator is run 
                        ' (Clients may depend on the new property in TempPE:s - VsWhidbey 449609)
                        If ProjectItem IsNot Nothing AndAlso ProjectItem.Object IsNot Nothing Then
                            Dim vsProjItem As VSLangProj.VSProjectItem = TryCast(Me.ProjectItem.Object, VSLangProj.VSProjectItem)
                            If vsProjItem IsNot Nothing Then
                                Try
                                    vsProjItem.RunCustomTool()
                                Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                                    Debug.Fail(String.Format("Failed to run custom tool: {0}", ex))
                                End Try
                            End If
                        End If
                    End If
                End Try

            Finally
                _ignoreDocLock = False
            End Try

            ' lastly signal that something has changed
            '

            '12/7-10:25PM, this causes a recursive set when prop-browser is setting a value
            '
            'Me.RaiseChange()
            '
            ' so does this
            '
            'If Not _changeQueued Then
            '    AddHandler Application.Idle, AddressOf OnIdleQueuedChange
            '    _changeQueued = True
            'End If
        End Sub

        ''' <summary>
        ''' Serialize contents of contained design time settings object to app/web.config 
        ''' </summary>
        ''' <param name="Settings">The settings which we should serialize</param>
        ''' <param name="GeneratedNamespace">The namespace into which the class was generated</param>
        ''' <param name="GeneratedClassName">The name of the class into which the settings should be serialized</param>
        ''' <remarks>
        ''' Serializing an empty settings file will remove all old references to it from
        ''' the app/web.config file
        ''' </remarks>
        Private Sub SaveToAppConfig(ByVal Settings As DesignTimeSettings, ByVal GeneratedNamespace As String, ByVal GeneratedClassName As String)
            ' now just write the stuff out to the file
            '
            Dim appConfigItemid As UInteger = VSITEMID.NIL
            Dim shouldSaveAppConfig As Boolean = False
            Dim appConfigCookie As UInteger
            Dim rdtLockType As UInteger = CUInt(_VSRDTFLAGS.RDT_NoLock)
            Dim appConfigHier As IVsHierarchy = Nothing
            Dim pAppConfigUnkDocData As IntPtr
            Dim AppConfigFileName As String = Nothing


            Dim oldIgnoreAppConfigChanges As Boolean = _provider.IgnoreAppConfigChanges
            Try
                _provider.IgnoreAppConfigChanges = True
                Try
                    Dim projSpecialFiles As IVsProjectSpecialFiles = TryCast(_hierarchy, IVsProjectSpecialFiles)
                    Debug.Assert(projSpecialFiles IsNot Nothing, "Failed to get IVsProjectSpecialFiles from hierarchy!")
                    VSErrorHandler.ThrowOnFailure( _
                        projSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, CUInt(__PSFFLAGS.PSFF_FullPath), appConfigItemid, AppConfigFileName))
                    If appConfigItemid = VSITEMID.NIL Then
                        ' If the file didn't exist in the project, we should save it
                        shouldSaveAppConfig = True
                    Else
                        ' The the file *is* in the project, we only save it if we can't find the associated
                        ' docdata in the RDT...
                        VSErrorHandler.ThrowOnFailure( _
                            _provider.RunningDocTable.FindAndLockDocument(rdtLockType, AppConfigFileName, appConfigHier, appConfigItemid, pAppConfigUnkDocData, appConfigCookie) _
                            )
                        '... and the way we decide that is by checking the returned native docdata
                        shouldSaveAppConfig = (pAppConfigUnkDocData = IntPtr.Zero)
                    End If
                Finally
                    If pAppConfigUnkDocData <> IntPtr.Zero Then
                        Marshal.Release(pAppConfigUnkDocData)
                    End If
                End Try

                Dim appConfigDocData As DocData = AppConfigSerializer.GetAppConfigDocData(_provider, _hierarchy, True, True)

                Try
                    If appConfigDocData IsNot Nothing Then
                        appConfigDocData.CheckoutFile(_provider)

                        Using appConfigDocData
                            Using appConfigChangeMarker As IDisposable = appConfigDocData.CreateChangeMarker()
                                ' let's serialize the contents of this guy...
                                AppConfigSerializer.Serialize(Settings, _
                                                            _typeCache, _
                                                            _valueCache, _
                                                            GeneratedClassName, _
                                                            GeneratedNamespace, _
                                                            appConfigDocData, _
                                                            _hierarchy, _
                                                            True)
                                If shouldSaveAppConfig Then
                                    If appConfigItemid = VSITEMID.NIL OrElse appConfigCookie = 0 Then
                                        ' Let's make sure we have all the nescessary info for this guy...
                                        Dim appConfigReadLocks As UInteger
                                        Dim appConfigEditLocks As UInteger
                                        GetDocumentInfo(AppConfigFileName, appConfigReadLocks, appConfigEditLocks, appConfigItemid, appConfigCookie)
                                    End If
                                    Dim rdt As IVsRunningDocumentTable = _provider.RunningDocTable
                                    Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")
                                    VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), _hierarchy, appConfigItemid, appConfigCookie))
                                End If
                            End Using
                        End Using
                    End If
                Catch ex As System.Configuration.ConfigurationErrorsException
                    ' Can't do much about this...
                End Try
            Finally
                _provider.IgnoreAppConfigChanges = oldIgnoreAppConfigChanges
            End Try
        End Sub

        '/ <devdoc>
        '/     Scrubs the compile unit of structures that a virtual type
        '/     can't support.  For example, virtual types can't support
        '/     private members, so we remove them here.
        '/ </devdoc>
        Private Sub ScrubCompileUnit(ByVal ccu As CodeCompileUnit)

            Dim allowed As MemberAttributes = SettingsSingleFileGenerator.SettingsPropertyVisibility

            For Each ns As System.CodeDom.CodeNamespace In ccu.Namespaces

                For Each t As CodeTypeDeclaration In ns.Types

                    Dim cnt As Integer = t.Members.Count
                    Dim idx As Integer = 0

                    While (idx < cnt)

                        Dim attrs As MemberAttributes = t.Members(idx).Attributes

                        ' we only allow properties that match the SFG.Visibility 
                        '   (Public|Friend instance properties, that is)
                        '
                        If ((attrs And allowed) <> attrs) Then
                            t.Members.RemoveAt(idx)
                            idx -= 1
                            cnt -= 1
                        End If

                        idx += 1
                    End While
                Next t
            Next ns
        End Sub 'ScrubCompileUnit










        ''' <summary>
        ''' This class is the code serializer for SettingsFile global object.
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Private NotInheritable Class SettingsFileCodeDomSerializer
            Inherits CodeDomSerializer

            Private Shared _default As SettingsFileCodeDomSerializer

            ''' <summary>
            ''' Provides a stock serializer instance.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Friend Shared ReadOnly Property [Default]() As SettingsFileCodeDomSerializer
                Get
                    If (_default Is Nothing) Then
                        _default = New SettingsFileCodeDomSerializer()
                    End If
                    Return _default
                End Get
            End Property

            ''' <summary>
            ''' Settings serializers shouldn't actually be invoked for anything.
            ''' </summary>
            ''' <param name="manager"></param>
            ''' <param name="codeObject"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function Deserialize(ByVal manager As IDesignerSerializationManager, ByVal codeObject As Object) As Object

                Debug.Fail("Should never be called")
                Return Nothing
            End Function 'Deserialize

            ''' <summary>
            ''' Gives this class a chance to serialize the given SettingsGlobalObjectValueAttribute class' reference to a
            '''  global object as the right-hand-side of a property set statement.
            ''' </summary>
            ''' <param name="manager"></param>
            ''' <param name="value">a SettingsGlobalObjectValueAttribute</param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function Serialize(ByVal manager As IDesignerSerializationManager, ByVal value As Object) As Object

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileCodeDomSerializer.Serialize...")
#End If

                Dim attr As SettingsGlobalObjectValueAttribute = TryCast(TypeDescriptor.GetAttributes(value)(GetType(SettingsGlobalObjectValueAttribute)), SettingsGlobalObjectValueAttribute)
                Debug.Assert(attr IsNot Nothing, "we should not be serializing something unless it has our attribute")

                Dim fullyQualifiedName As String
                If attr.GlobalObject._namespace <> "" Then
                    fullyQualifiedName = attr.GlobalObject._namespace & "." & attr.GlobalObject._className
                Else
                    fullyQualifiedName = attr.GlobalObject._className
                End If
                Dim globalObjectTypeRef As New CodeTypeReference(fullyQualifiedName, CodeTypeReferenceOptions.GlobalReference)
                Dim globalObjectClassRef As New CodeTypeReferenceExpression(globalObjectTypeRef)
                Dim defaultInstancePropertyRef As New CodePropertyReferenceExpression(globalObjectClassRef, SettingsSingleFileGenerator.DefaultInstancePropertyName)
                Dim propertyExpression As CodePropertyReferenceExpression

                If attr.PropertyName Is Nothing Then
                    propertyExpression = defaultInstancePropertyRef
                Else
                    propertyExpression = New CodePropertyReferenceExpression(defaultInstancePropertyRef, attr.PropertyName)
                End If

                If manager IsNot Nothing Then
                    SetExpression(manager, value, propertyExpression)
                End If

                Return propertyExpression

            End Function 'Serialize

        End Class 'SettingsFileCodeDomSerializer











        ''' <summary>
        ''' The virtual type implementor for our global object.  This maps properties to setting names.
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class SettingsFileTypeImplementor
            Inherits VirtualTypeImplementor

            Private _globalObject As SettingsFileGlobalObject

            ''' <summary>
            ''' constructor for this implementor
            ''' </summary>
            ''' <param name="globalObject">object we implement in place of</param>
            ''' <remarks></remarks>
            Friend Sub New(ByVal globalObject As SettingsFileGlobalObject)

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileTypeImplementor.ctor(" & CStr(globalObject._className) & ")...")
#End If

                _globalObject = globalObject
            End Sub 'New

            ''' <summary>
            ''' Returns the value for the given property.
            ''' </summary>
            ''' <param name="prop"></param>
            ''' <param name="instance"></param>
            ''' <param name="args"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function GetPropertyValue(ByVal prop As System.Reflection.PropertyInfo, ByVal instance As Object, ByVal args() As Object) As Object

                Debug.Assert(prop IsNot Nothing, "bad property passed to GetPropertyValue")
                If (prop Is Nothing) Then
                    Throw New ArgumentNullException("prop")
                End If

                ' make sure this .settings file is generating code, otherwise it's not really
                '   worth it to attempt to get property values...
                '
                _globalObject.EnsureGeneratingSettingClass()

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileTypeImplementor.GetPropertyValue(" & CStr(_globalObject._className) & " -- " & CStr(prop.Name) & ")...")
#End If

                ' DefaultInstance should return the GlobalObject that this represents since DefaultInstance
                '   returns an instance of itself
                '
                If (prop.Name.Equals(SettingsSingleFileGenerator.DefaultInstancePropertyName, StringComparison.Ordinal)) Then
                    Return (_globalObject.Instance)
                End If

#If DEBUG Then
                ' included here just to make sure that this compiles since we count on the property
                '   name being "Properties" when we string-compare the requested property name
                ' note that after Whidbey ships, we could really remove this code since changing the
                '   name after we ship would be a breaking change
                '
                If (True) Then
                    Dim propertyCheckerInstance As New ConcreteApplicationSettings(_globalObject)
                    Dim propColl As SettingsPropertyCollection = propertyCheckerInstance.Properties
                End If
#End If

                ' the Forms designer will ask for this collection in order to add or remove properties
                '   to/from our collection.
                '
                If (prop.Name.Equals("Properties", StringComparison.Ordinal)) Then
                    Return New GlobalSettingsPropertyCollection(_globalObject)
                End If

                For Each setting As DesignTimeSettingInstance In _globalObject.Settings

                    ' comparing code-names should use OrdinalComparison
                    '
                    If (setting.Name.Equals(prop.Name, StringComparison.Ordinal)) Then
                        ' Debug.Assert(prop.PropertyType.FullName = setting.SettingTypeName, "Type mismatch!") ' Can't assert this since unfortunately we return serializableconnectionstrings instead of strings
                        Dim settingType As System.Type = _globalObject.ResolveType(setting.SettingTypeName)
                        If settingType IsNot Nothing Then
                            Dim value As Object = _globalObject.DeserializeValue(settingType, setting.SerializedValue)

                            ' if this instance doesn't have a DesignerSerializerAttribute on it, then we need to
                            '   put one on it now so serialization works
                            '
                            If Not value Is Nothing Then
                                Dim attributes As AttributeCollection = TypeDescriptor.GetAttributes(value)
                                If (attributes IsNot Nothing AndAlso attributes(GetType(DesignerSerializerAttribute)) Is Nothing) Then
                                    Dim serializer As Object = _globalObject.GetSerializer(GetType(CodeDomSerializer))
                                    Debug.Assert(serializer IsNot Nothing, "we provide this -- why can't we get it?")
                                    If (serializer IsNot Nothing) Then
                                        ' Dev10 Bug 838702 -- VS crashed after repeat open-close Sync Designer due to stack overflow.  
                                        ' Need to check to see if DesignTimeVisibleAttribute or SettingsGlobalObjectValueAttribute 
                                        ' does not exist before adding them.  
                                        ' See also VSWhidby bug 417560, consider to remove the comment and the check on 
                                        ' DesignerSerializerAttribute  
                                        '
                                        If (attributes(GetType(SettingsGlobalObjectValueAttribute)) Is Nothing OrElse attributes(GetType(DesignTimeVisibleAttribute)) Is Nothing) Then
                                            TypeDescriptor.AddAttributes(value, _
                                                                        New DesignTimeVisibleAttribute(False), _
                                                                        New SettingsGlobalObjectValueAttribute(_globalObject, setting.Name))
                                        End If
                                    End If
                                End If
                                Return value
                            End If
                        End If
                    End If
                Next

                Debug.Fail(("Property " + prop.Name + " could not be located in our global object collection."))
                Return Nothing
            End Function 'GetPropertyValue

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="ctor"></param>
            ''' <param name="args"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function InvokeConstructor(ByVal ctor As System.Reflection.ConstructorInfo, ByVal args As System.Object()) As Object

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileTypeImplementor.InvokeConstructor(" & CStr(_globalObject._className) & ")...")
#End If

                Debug.Assert(args Is Nothing OrElse args.Length = 0, "We should be creating a type with a default constructor only")

                Dim constructedObject As New ConcreteApplicationSettings(_globalObject)
                Return constructedObject

            End Function 'InvokeConstructor

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="prop"></param>
            ''' <param name="instance"></param>
            ''' <param name="value"></param>
            ''' <param name="args"></param>
            ''' <remarks></remarks>
            Public Overrides Sub SetPropertyValue(ByVal prop As System.Reflection.PropertyInfo, ByVal instance As Object, ByVal value As Object, ByVal args As System.Object())

                Debug.Assert(prop IsNot Nothing, "bad property passed to SetPropertyValue")
                If (prop Is Nothing) Then
                    Throw New ArgumentNullException("prop")
                End If

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "SettingsFileTypeImplementor.SetPropertyValue(" & CStr(_globalObject._className) & " -- " & CStr(prop.Name) & ")...")
#End If

                Debug.Assert(Not prop.Name.Equals(SettingsSingleFileGenerator.DefaultInstancePropertyName, StringComparison.Ordinal), "DefaultInstance is read-only, we can't set it")
                Debug.Assert(Not prop.Name.Equals("Properties", StringComparison.Ordinal), "Properties collection is read-only")

                ' a DesignTimeSettings class implements IList but does not support fetching settings
                '   by their name, so we need to loop through the settings collection looking for
                '   a name-match. Note that this shouldn't be a big deal since we're not architecting
                '   config to have hundreds (or more) settings.
                '
                For Each setting As DesignTimeSettingInstance In _globalObject.Settings

                    If (prop.Name.Equals(setting.Name, StringComparison.Ordinal)) Then

                        Dim settingType As System.Type = _globalObject.ResolveType(setting.SettingTypeName)
                        Dim settingValue As Object = Nothing
                        If settingType IsNot Nothing Then
                            settingValue = _globalObject.DeserializeValue(settingType, setting.SerializedValue)
                        End If

                        ' only poke in the new value if the two values are different
                        '
                        If ((settingValue Is Nothing AndAlso value IsNot Nothing) _
                            OrElse (settingValue IsNot Nothing AndAlso Not settingValue.Equals(value))) Then
                            Try
                                ' poke in the new value (which should set it as the value in
                                '   the currently selected profile)
                                '
                                Dim serializer As New SettingsValueSerializer
                                setting.SetSerializedValue(serializer.Serialize(value, System.Globalization.CultureInfo.InvariantCulture))

                                ' now ask the file to persist the change
                                '
                                _globalObject.Save()
                            Catch ex As CheckoutException
                                ' If we fail to check out, we better tell everyone that this failed...
                                _globalObject.RaiseChange()
                                Throw
                            End Try
                        End If

                        ' and stop looking for other settings
                        '
                        Return

                    End If
                Next

                Debug.Fail(("Property " + prop.Name + " could not be located in our global object collection, so we could not set the value."))

            End Sub

        End Class 'SettingsFileTypeImplementor










        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Private Class ConcreteApplicationSettings
            Inherits System.Configuration.ApplicationSettingsBase

            Dim _globalObject As SettingsFileGlobalObject
            Dim _properties As SettingsPropertyCollection

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="globalObject"></param>
            ''' <remarks></remarks>
            Friend Sub New(ByVal globalObject As SettingsFileGlobalObject)

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "ConcreteApplicationSettings.ctor(" & CStr(globalObject._className) & ")...")
#End If

                Debug.Assert(globalObject IsNot Nothing, "")
                _globalObject = globalObject
            End Sub

            ''' <summary>
            ''' Return the properties collection for this type. The ApplicationSettingsBase class uses reflection to
            ''' enumrate all the correctly attributed properties in the current instance and put 'em in it's settings property
            ''' collection. Since reflecting over this specific instance wouldn't return any settings unless we wrote a custom
            ''' type provider (which will be hard to get to work with all the magic custom type descriptor goo used by global
            ''' objects in general) we use our derived SettingsPropertyCollection that will happily pick up the settings from
            ''' the DesignTimeSettings object associated with this instance.
            ''' </summary>
            ''' <value></value>
            ''' <remarks>
            ''' The properties collection is cached in this instance, so it will only reflect the settings that were
            ''' present at the time when the client called this property getter. Whenever someone adds/removes settings
            ''' from the corresponding settings file, this collection is out of date (but on the other hand, the type will
            ''' be changed as well, so any client that cached a reference to this instance/collection should have released
            ''' it when it got a type changed event from the dynamic type service)
            ''' </remarks>
            Public Overrides ReadOnly Property Properties() As SettingsPropertyCollection
                Get
#If DEBUG Then
                    Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "ConcreteApplicationSettings.Properties-getter(" & CStr(_globalObject._className) & ")...")
#End If

                    If (_properties Is Nothing) Then
                        _properties = New GlobalSettingsPropertyCollection(_globalObject)
                    End If
                    Return _properties
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Collection of settings. We need to override this to handle add and removal of settings to the collection
        ''' so that we can update the underlying DesignTimeSettings object.
        ''' 
        ''' The collection will be populated with the values from the associate globalObject's DesignTimeSetting property.
        ''' 
        ''' This collection should only be used to add/remove settings - you should not rely on the actual attributes that
        ''' are put on each property as they may not be correct...
        ''' </summary>
        ''' <remarks></remarks>
        Private Class GlobalSettingsPropertyCollection
            Inherits SettingsPropertyCollection

            Dim _globalObject As SettingsFileGlobalObject

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="globalObject"></param>
            ''' <remarks></remarks>
            Friend Sub New(ByVal globalObject As SettingsFileGlobalObject)

#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "GlobalSettingsPropertyCollection.ctor(" & CStr(globalObject._className) & ")...")
#End If
                For Each instance As SettingsDesigner.DesignTimeSettingInstance In globalObject.Settings
                    Dim prop As SettingsProperty
                    prop = New SettingsProperty(instance.Name)
                    prop.PropertyType = globalObject.ResolveType(instance.SettingTypeName)
                    Me.Add(prop)
                Next
                Debug.Assert(globalObject IsNot Nothing, "")
                _globalObject = globalObject
            End Sub

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="prop"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnAddComplete(ByVal prop As System.Configuration.SettingsProperty)
                If _globalObject Is Nothing Then Return
#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "GlobalSettingsPropertyCollection.OnAddComplete(" & CStr(_globalObject._className) & ")...")
#End If
                Debug.IndentLevel += 1
                Try
                    ' first call our base-class (even though nothing should really happen
                    '   since we only run at design-time...)
                    '
                    MyBase.OnAddComplete(prop)

                    If (prop Is Nothing) Then
                        Throw New ArgumentNullException("prop")
                    End If

                    ' we need the collection of settings objects to which we can add the new setting
                    '
                    Dim dts As DesignTimeSettings = _globalObject.Settings
                    Debug.Assert(dts IsNot Nothing, "globalObject did not return a valid DesignTimeSettings object?")

                    ' and we need a new setting object
                    '
                    Dim setting As New DesignTimeSettingInstance
                    ' First look for and handle "special" settings
                    Dim attr As System.Configuration.SpecialSettingAttribute = TryCast(prop.Attributes(GetType(System.Configuration.SpecialSettingAttribute)), SpecialSettingAttribute)
                    Dim isConnString As Boolean = attr IsNot Nothing AndAlso attr.SpecialSetting = SpecialSetting.ConnectionString
                    Dim isWebReference As Boolean = attr IsNot Nothing AndAlso attr.SpecialSetting = SpecialSetting.WebServiceUrl
                    If isConnString Then
                        setting.SetSettingTypeName(SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString)
                    ElseIf isWebReference Then
                        setting.SetSettingTypeName(SettingsSerializer.CultureInvariantVirtualTypeNameWebReference)
                    Else
                        setting.SetSettingTypeName(prop.PropertyType.FullName)
                    End If

                    ' before we add to the collection, we must set the name of this new setting object
                    '
                    setting.SetName(prop.Name)

                    ' now we need to add that setting to the file that contains all the settings for this class
                    '
                    dts.Add(setting)

                    ' now follow up & set the rest of the properties on this new settings object
                    '
                    ' set the scope to be user-scoped or app-scoped, depending on whether or not the property
                    '   just added has the app-scoped attribute. default will be user-scoped if neither attribue
                    '   is applied.
                    '
                    If (prop.Attributes.Contains(GetType(ApplicationScopedSettingAttribute))) Then
                        setting.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                    Else
                        setting.SetScope(DesignTimeSettingInstance.SettingScope.User)
                    End If

                    ' Note that we explicitly ignore the following properties:
                    '
                    '  SettingsProperty.ReadOnly - for Whidbey, we will not expose this since App-scoped settings are
                    '                               not possible, and read-only user settings aren't interesting
                    '  SettingsProperty.Provider - adding from the forms designer gives you the default provider associated
                    '                               with the class to which we're adding (by design)
                    '  SettingsProperty.SerializeAs - the runtime is responsible for choosing the correct serialize-as
                    '                                   value, and this is opaque to our design-time

                    ' lastly, set the serialized value into the default profile (since this is a new setting,
                    '   that implies that all profiles should pick up this value). The last param to the ConfigHelper
                    '   call means that the SerializeAs property in the SettingsProperty is not necessarily valid and
                    '   the ConfigHelper should choose the best method for serializing this value.
                    Dim serializer As New SettingsValueSerializer
                    setting.SetSerializedValue(serializer.Serialize(prop.DefaultValue, Globalization.CultureInfo.InvariantCulture))

                    ' now ask the file to save itself
                    '
                    _globalObject.Save()
                Finally
                    _globalObject.PerformChange()
                    Debug.IndentLevel -= 1
                End Try

            End Sub

            ''' <summary>
            ''' Handle the remove of a setting
            ''' </summary>
            ''' <param name="property"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnRemoveComplete(ByVal [property] As System.Configuration.SettingsProperty)
                If _globalObject Is Nothing Then Return
#If DEBUG Then
                Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "GlobalSettingsPropertyCollection.OnRemoveComplete(" & CStr(_globalObject._className) & ")...")
#End If
                MyBase.OnRemoveComplete([property])

                If ([property] Is Nothing) Then
                    Throw New ArgumentNullException("property")
                End If

                ' we need the collection of settings objects from which we can remove the setting
                '
                Dim dts As DesignTimeSettings = _globalObject.Settings
                Debug.Assert(dts IsNot Nothing, "globalObject did not return a valid DesignTimeSettings object?")

                Dim isDirty As Boolean

                ' Find the instance to remove by looping through our collection and comparing the names
                '
                For Each instance As DesignTimeSettingInstance In dts
                    If DesignTimeSettings.EqualIdentifiers([property].Name, instance.Name) Then
                        ' Found the instance to delete 
#If DEBUG Then
                        Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "Removing instance " & instance.Name)
#End If
                        dts.Remove(instance)
                        isDirty = True
                        ' Sice the name has to be unique, and we already found a match,
                        ' we can safely exit the loop here
                        Exit For
                    End If
                Next

                If isDirty Then
                    ' Save & rebuild the virtual type (we have removed a property from the class)
                    Try
                        _globalObject.Save()
                    Finally
                        _globalObject.PerformChange()
                    End Try
                Else
#If DEBUG Then
                    Debug.WriteLineIf(SettingsGlobalObjectProvider.GlobalSettings.TraceVerbose, "Failed to find instance to remove...")
#End If
                    Debug.Fail("Why did we get a OnRemoveComplete for a setting that we couldn't find in our DesignTimeSettings object?")
                    Throw New ArgumentOutOfRangeException()
                End If
            End Sub
        End Class






        ''' <summary>
        ''' Attribute we tack on to values that we return from GetPropertyValue to identify which property
        ''' the value hails from.
        ''' </summary>
        ''' <remarks></remarks>
        Private Class SettingsGlobalObjectValueAttribute
            Inherits Attribute

            Private _globalObject As SettingsFileGlobalObject
            Private _propertyName As String

            ''' <summary>
            ''' constructor that takes the params to store
            ''' </summary>
            ''' <param name="globalObject"></param>
            ''' <param name="propertyName"></param>
            ''' <remarks></remarks>
            Friend Sub New(ByVal globalObject As SettingsFileGlobalObject, ByVal propertyName As String)
                _globalObject = globalObject
                _propertyName = propertyName
            End Sub

            ''' <summary>
            ''' gets the GlobalObject associated with this value
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Friend ReadOnly Property GlobalObject() As SettingsFileGlobalObject
                Get
                    Return _globalObject
                End Get
            End Property

            ''' <summary>
            ''' Gets the property-name associated with this value
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Friend ReadOnly Property PropertyName() As String
                Get
                    Return _propertyName
                End Get
            End Property
        End Class 'SettingsGlobalObjectValueAttribute


    End Class 'SettingsFileGlobalObject


    ''' <summary>
    ''' Class that helps get an IVsHierarchy given a DTE project.
    ''' </summary>
    ''' <remarks></remarks>
    Class ProjectUtilities

        Public Shared Function GetVsHierarchy(ByVal provider As IServiceProvider, ByVal project As Project) As IVsHierarchy
            ' YUCK.  We need to use DTE to get references because VSIP doesn't define
            ' that concept.  We need to use VSIP to get deploy dependencies because
            ' DTE doesn't define THAT.  There is no way to go between a DTE project
            ' and a VSIP hierarchy other than the slow nasty junk we do below:
            Dim solution As IVsSolution = CType(provider.GetService(GetType(IVsSolution)), IVsSolution)
            If (solution Is Nothing) Then
                Debug.Fail("No solution.")
                Return Nothing
            End If

            Dim hier As IVsHierarchy = Nothing

            VSErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, hier))

            If (hier Is Nothing) Then
                Debug.Fail(("No project for name " + project.UniqueName))
                Return Nothing
            Else
                Return hier
            End If
        End Function 'GetVsHierarchy
    End Class

End Namespace 'Microsoft.VisualStudio.Design.Serialization

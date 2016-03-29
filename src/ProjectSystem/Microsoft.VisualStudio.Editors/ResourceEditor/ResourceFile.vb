'******************************************************************************
'* ResourceFile.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports EnvDTE
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Editors.Package
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner
Imports System
Imports System.CodeDom.Compiler
Imports System.Collections
Imports System.Collections.Specialized
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Globalization
Imports System.Resources
Imports System.IO
Imports System.Diagnostics
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Windows.Forms
Imports VB = Microsoft.VisualBasic


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A representation of a resx file (essentially a ResourceCollection).  Wraps the
    '''   reading and writing of the file, plus the management of the resources
    '''   (instances of the Resource class) within it.
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Friend Class ResourceFile
        Implements IDisposable
        Implements ResourceTypeEditor.IResourceContentFile

#Region "Fields"

        'A pointer to the host's IComponentChangeService.  We use this to get notified
        '  when components (Resource instances) are added/removed from the collection
        '  (both when we do it manually and when Undo/Redo does it for us), etc.
        Private WithEvents m_ComponentChangeService As IComponentChangeService

        'Our set of resources (keyed by Name normalized to uppercase so our look-up is case-insensitive)
        Private m_ResourcesHash As Hashtable = Nothing

        'an arrayList to keep all meta data, we shouldn't lose them when we save the resource file back...
        Private m_MetadataList As ArrayList

        'The root component for the resource editor.  Cannot be Nothing.
        Private m_RootComponent As ResourceEditorRootComponent

        'A pointer to the task provider service.  Gives us access to the VS task list.
        Private m_ErrorListProvider As ErrorListProvider

        'The main thread we're running on.  Used just to verify that idle time processing
        '  is always on the main thread.
        Private m_MainThread As Threading.Thread

        'Holds a set of tasks for each Resource that has any task list entries.
        'Hashes key=Resource to ResourceTaskSet.
        Private m_ResourceErrorsHash As New Hashtable 'Of Resource To ResourceTaskSet

        'A list of resources that need to be checked for errors during idle-time
        '  processing.
        Private m_ResourcesToDelayCheckForErrors As New ArrayList

        ' Indicate whether we should suspend delay checking temporary...
        Private m_DelayCheckSuspended As Boolean

        'True iff we're in the middle of adding or removing a Resource through AddResource or RemoveResource.  If not,
        '  and we get notified of an add by component changing service, it means an external source has added/removed
        '  the resource (i.e., Undo/Redo).
        Private m_AddingRemovingResourcesInternally As Boolean

        'The base path to use for resolving relative paths in the resx file.  This should be the
        '  directory where the resx file lives.
        Private m_BasePath As String

        ' We get ResourceWrite from this environment service
        '  the reason is some projects (device project) need write the resource file in v1.x format, but other projects write in 2.0 format.
        Private m_resxService As IResXResourceService

        'True if the original file bases on alphabetized order, we will keep this style...
        Private m_alphabetizedOrder As Boolean = True

        'The service provider provided by the designer host
        Private m_ServiceProvider As IServiceProvider

        ' Asynchronous flush & run custom tool already posted?
        Private m_delayFlushAndRunCustomToolQueued As Boolean

        ' It is true, when we are loading a new file.
        '  CONSIDER: Some behaviors in the designer are different when we are loading the file. For example, we don't dirty the file, adding undo/redo...
        '  We should consider to make it to be a part of the global state of the designer, but not within one object.
        Private m_IsLoadingResourceFile As Boolean

        ' If it is true, we are adding a collection of resources to the file
        Private m_InBatchAdding As Boolean

        Private m_multiTargetService As MultiTargetService
#End Region


#Region "Constructors/Destructors"

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="RootComponent">The root component for this ResourceFile</param>
        ''' <param name="ServiceProvider">The service provider provided by the designer host</param>
        ''' <param name="BasePath">The base path to use for resolving relative paths in the resx file.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal mtsrv As MultiTargetService, ByVal RootComponent As ResourceEditorRootComponent, ByVal ServiceProvider As IServiceProvider, ByVal BasePath As String)
            Debug.Assert(Not RootComponent Is Nothing)
            Debug.Assert(ServiceProvider IsNot Nothing)

            m_ResourcesHash = New Hashtable
            m_MetadataList = New ArrayList

            m_RootComponent = RootComponent
            m_ServiceProvider = ServiceProvider

            m_BasePath = BasePath

            m_ComponentChangeService = DirectCast(ServiceProvider.GetService(GetType(IComponentChangeService)), IComponentChangeService)
            If ComponentChangeService Is Nothing Then
                Throw New Package.InternalException
            End If

            m_multiTargetService = mtsrv

            Dim hierarchy As IVsHierarchy = DirectCast(ServiceProvider.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            If hierarchy IsNot Nothing Then
                Dim project As IVsProject = DirectCast(hierarchy, IVsProject)
                Dim sp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = Nothing

                Dim hr As Integer = project.GetItemContext(VSITEMID.ROOT, sp) '0xFFFFFFFE VSITEMID_ROOT
                If Interop.NativeMethods.Succeeded(hr) Then
                    Dim pUnk As System.IntPtr
                    Dim g As System.Guid = GetType(IResXResourceService).GUID
                    Dim g2 As System.Guid = New System.Guid("00000000-0000-0000-C000-000000000046") 'IUnKnown
                    hr = sp.QueryService(g, g2, pUnk)
                    If Interop.NativeMethods.Succeeded(hr) AndAlso Not pUnk = System.IntPtr.Zero Then
                        m_resxService = DirectCast(System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pUnk), IResXResourceService)
                        System.Runtime.InteropServices.Marshal.Release(pUnk)
                    End If
                End If
            End If

            m_MainThread = Threading.Thread.CurrentThread
        End Sub


        ''' <summary>
        ''' IDisposable.Dispose()
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub


        ''' <summary>
        ''' Dispose.
        ''' </summary>
        ''' <param name="Disposing">If True, we're disposing.  If false, we're finalizing.</param>
        ''' <remarks></remarks>
        Protected Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                'Stop listening to component removing events - we want to just tear down in peace.
                ComponentChangeService = Nothing

                'Stop delay-checking resources and remove ourselves from idle-time processing (very important)
                StopDelayingCheckingForErrors()

                'Remove all task list entries
                If m_ErrorListProvider IsNot Nothing Then
                    m_ErrorListProvider.Tasks.Clear()
                End If

                If m_ResourcesHash IsNot Nothing Then
                    'Note: The designer host disposing any Resources of ours that have been
                    '  added as components.  However, we do it now anyway, in case there are some
                    '  that didn't make into into the host container, or in case we later do the
                    '  optimization of delay-adding Resources as components.  The second dispose
                    '  won't hurt the Resource.
                    For Each Entry As DictionaryEntry In m_ResourcesHash
                        Dim Resource As IDisposable = DirectCast(Entry.Value, IDisposable)
                        Resource.Dispose()
                    Next

                    m_ResourcesHash.Clear()
                End If

                If m_MetadataList IsNot Nothing Then
                    m_MetadataList.Clear()
                End If
            End If
        End Sub

#End Region




#Region "Properties"


        ''' <summary>
        ''' The service provider provided by the designer host
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ServiceProvider() As IServiceProvider
            Get
                Return m_ServiceProvider
            End Get
        End Property


        ''' <summary>
        ''' Returns/gets the ComponentChangeService used by this ResourceFile.  To have this class stop listening to
        '''   change events, set this property to Nothing.  It does not need to be set up initially - it gets it
        '''   automatically from the service provider passed in.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ComponentChangeService() As IComponentChangeService
            Get
                Return m_ComponentChangeService
            End Get
            Set(ByVal Value As IComponentChangeService)
                m_ComponentChangeService = Value
            End Set
        End Property


        ''' <summary>
        ''' Gets the ResourceEditorView associated with this ResourceFile.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Overridable for unit testing.</remarks>
        Public Overridable ReadOnly Property View() As ResourceEditorView
            Get
                Return RootComponent.RootDesigner.GetView()
            End Get
        End Property


        ''' <summary>
        ''' Gets the root component associated with this resource file.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property RootComponent() As ResourceEditorRootComponent
            Get
                Return m_RootComponent
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the desiger host for the resource editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property DesignerHost() As IDesignerHost
            Get
                If RootComponent.RootDesigner Is Nothing Then
                    Debug.Fail("No root designer")
                    Throw New Package.InternalException
                End If

                Dim Host As IDesignerHost = RootComponent.RootDesigner.DesignerHost
                Debug.Assert(Host IsNot Nothing)
                Return Host
            End Get
        End Property


        ''' <summary>
        ''' Returns the hashtable of resources from this resource file
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property ResourcesHashTable() As Hashtable
            Get
                Return m_ResourcesHash
            End Get
        End Property


        ''' <summary>
        ''' The base path to use for resolving relative paths in the resx file.  This should be the
        '''   directory where the resx file lives.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property BasePath() As String
            Get
                Return m_BasePath
            End Get
        End Property


        ''' <summary>
        '''  Get the taskProvider
        '''   directory where the resx file lives.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property ErrorListProvider() As ErrorListProvider
            Get
                If m_ErrorListProvider Is Nothing Then
                    If RootComponent.RootDesigner IsNot Nothing Then
                        m_ErrorListProvider = RootComponent.RootDesigner.GetErrorListProvider()
                    End If
                    Debug.Assert(m_ErrorListProvider IsNot Nothing, "ErrorListProvider can not be found")
                End If
                Return m_ErrorListProvider
            End Get
        End Property

        ''' <summary>
        '''  Whether the resource item belongs to a device project
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsInsideDeviceProject() As Boolean Implements ResourceTypeEditor.IResourceContentFile.IsInsideDeviceProject
            Get
                Return RootComponent IsNot Nothing AndAlso RootComponent.IsInsideDeviceProject()
            End Get
        End Property

        ''' <summary>
        ''' Returns whether the provided type is supported in the project containing this resource file
        ''' </summary>
        Public Function IsSupportedType(Type As Type) As Boolean Implements ResourceTypeEditor.IResourceContentFile.IsSupportedType

            ' The type is considered supported unless the MultiTargetService says otherwise (MultiTargetService checks
            ' in the project's target framework).

            If m_multiTargetService IsNot Nothing Then
                Return m_multiTargetService.IsSupportedType(Type)
            Else
                Return True
            End If
        End Function

#End Region





#Region "Resource Naming and look-up"


        ''' <summary>
        ''' Gets a suggested name for a new Resource which is not used by any resource currently in this ResourceFile.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetUniqueName(ByVal TypeEditor As ResourceTypeEditor) As String
            Dim UniqueNamePrefix As String

            Try
                UniqueNamePrefix = TypeEditor.GetSuggestedNamePrefix().Trim()
                If UniqueNamePrefix = "" OrElse UniqueNamePrefix.IndexOf(" "c) >= 0 Then
                    Debug.Fail("Bad unique name prefix - localization bug?")
                    UniqueNamePrefix = ""
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Exception calling ResourceTypeEditor.GetSuggestedNamePrefix(): " & ex.Message)
                UniqueNamePrefix = ""
            End Try

            If UniqueNamePrefix = "" Then
                'Use a default prefix if there's trouble
                UniqueNamePrefix = "id"
            End If

            Dim UniqueNameFormat As String = UniqueNamePrefix & "{0:0}"
            Return GetUniqueName(UniqueNameFormat)
        End Function


        ''' <summary>
        ''' Gets a suggested name for a new Resource which is not used by any resource currently in this ResourceFile.
        ''' </summary>
        ''' <param name="NameFormat">A format to use for String.Format which indicates how to format the integer portion of the name.  Must contain a single {0} parameter.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetUniqueName(ByVal NameFormat As String) As String
            Debug.Assert(NameFormat.IndexOf("{") >= 0 AndAlso NameFormat.IndexOf("}") >= 2, _
                "NameFormat must contain a replacement arg")

            Dim SuffixInteger As Integer = 1
            Do
                Dim NewName As String = String.Format(NameFormat, SuffixInteger)
                If Not Contains(NewName) Then
                    Return NewName
                End If

                SuffixInteger += 1
            Loop
        End Function


        ''' <summary>
        ''' Determines if a resource with a given name (case-insensitive) exists in this ResourceFile.
        ''' </summary>
        ''' <param name="Name">The resource name to look for (case insensitive)</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Contains(ByVal Name As String) As Boolean
            Return Not FindResource(Name) Is Nothing
        End Function


        ''' <summary>
        ''' Determines if a particular resource is in this ResourceFile (by reference)
        ''' </summary>
        ''' <param name="Resource"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Contains(ByVal Resource As Resource) As Boolean
            Return m_ResourcesHash.ContainsValue(Resource)
        End Function


        ''' <summary>
        ''' Searches for a resource with a given name (case-insensitive) in this ResourceFile.
        ''' </summary>
        ''' <param name="Name">The resource name to look for (case insensitive)</param>
        ''' <returns>The found Resource, or Nothing if not found.</returns>
        ''' <remarks></remarks>
        Public Function FindResource(ByVal Name As String) As Resource
            If Name = "" Then
                Return Nothing
            End If

            'Keys are upper-cased
            Dim Found As Resource = DirectCast(m_ResourcesHash(Name.ToUpperInvariant()), Resource)
            Debug.Assert(Found Is Nothing OrElse Found.ParentResourceFile Is Me)
            Return Found
        End Function

        ''' <summary>
        ''' Searches for a resource with a given file link (case-insensitive) in this ResourceFile.
        ''' </summary>
        ''' <param name="FileFullPath">The full path name of the linked file to look for (case insensitive)</param>
        ''' <returns>The found Resource, or Nothing if not found.</returns>
        ''' <remarks> We should be careful there, because there could be different path pointing to a same file</remarks>
        Public Function FindLinkResource(ByVal FileFullPath As String) As Resource
            Dim fileInfo As New FileInfo(FileFullPath)
            FileFullPath = fileInfo.FullName
            For Each Resource As Resource In m_ResourcesHash.Values
                If Resource.IsLink Then
                    Dim linkFileInfo As New FileInfo(Resource.AbsoluteLinkPathAndFileName)
                    If String.Compare(FileFullPath, linkFileInfo.FullName, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return Resource
                    End If
                End If
            Next
            Return Nothing
        End Function

#End Region


#Region "Adding/removing/renaming resources"

        ''' <summary>
        '''  Add a collection of resources to the ResourceFile
        ''' </summary>
        ''' <param name="NewResources">A collection of resource items to add</param>
        Public Sub AddResources(ByVal NewResources As ICollection)
            Debug.Assert(NewResources IsNot Nothing, "Invalid Resources collection")

            m_InBatchAdding = True
            Try
                For Each Resource As Resource In NewResources
                    AddResource(Resource)
                Next
                If Not m_IsLoadingResourceFile Then
                    AddNecessaryReferenceToProject(NewResources)
                End If
            Finally
                m_InBatchAdding = False
            End Try
        End Sub

        ''' <summary>
        ''' Adds a new Resource to the ResourceFile.
        ''' </summary>
        ''' <param name="NewResource">The Resource to add.  Must not be blank.</param>
        ''' <remarks>Exception throw if the name is not unique.</remarks>
        Public Sub AddResource(ByVal NewResource As Resource)
            If NewResource.Name = "" Then
                Debug.Fail("Resource Name is blank - we shouldn't reach here with that condition")
                Throw NewException(SR.GetString(SR.RSE_Err_NameBlank), HelpIDs.Err_NameBlank)
            End If
            If Contains(NewResource.Name) Then
                Throw NewException(SR.GetString(SR.RSE_Err_DuplicateName_1Arg, NewResource.Name), HelpIDs.Err_DuplicateName)
            End If

            'Set up a type resolution context for the resource in case this hasn't
            '  already been done (won't be done if the Resource was deserialized
            '  during an Undo/Redo or Drop/Paste operation, for example)
            NewResource.SetTypeResolutionContext(View)

#If DEBUG Then
            Dim ResourcesCountOld As Integer = m_ResourcesHash.Count
#End If

            Dim AddingRemovingResourcesInternallySave As Boolean = m_AddingRemovingResourcesInternally
            Try
                m_AddingRemovingResourcesInternally = True

                'Add the component to our designer's container.
                'This will cause us to get notified via ComponentChangeService.ComponentAdded, which is where we
                '  will actually add the Resource to our internal list.
                DesignerHost.Container.Add(NewResource, NewResource.Name)
            Finally
                m_AddingRemovingResourcesInternally = AddingRemovingResourcesInternallySave
            End Try

#If DEBUG Then
            Debug.Assert(m_ResourcesHash.Count = ResourcesCountOld + 1)
#End If
        End Sub


        ''' <summary>
        ''' Removes the specified Resource from this ResourceFile.
        ''' </summary>
        ''' <param name="Resource">The Resource to remove.  Must exist in the ResourceFile</param>
        ''' <param name="DisposeResource">If True, the Resource is also disposed.</param>
        ''' <remarks></remarks>
        Public Sub RemoveResource(ByVal Resource As Resource, ByVal DisposeResource As Boolean)
            Debug.Assert(Not Resource Is Nothing)
            Debug.Assert(FindResource(Resource.Name) Is Resource, "RemoveResource: not found by Name")
            Debug.Assert(m_ResourcesHash.ContainsValue(Resource), "RemoveResource: not found")

            Dim ResourcesCountOld As Integer = m_ResourcesHash.Count
            Dim AddingRemovingResourcesInternallySave As Boolean = m_AddingRemovingResourcesInternally

            Try
                m_AddingRemovingResourcesInternally = True

                'Remove the component from our designer's container.
                'This will cause us to get notified via ComponentChangeService.ComponentRemoved, which is where we
                '  will actually remove the Resource from our internal list
                DesignerHost.Container.Remove(Resource)
            Finally
                m_AddingRemovingResourcesInternally = AddingRemovingResourcesInternallySave
            End Try

            Debug.Assert(m_ResourcesHash.Count = ResourcesCountOld - 1)

            'Remove any task list entries for this resource
            ClearResourceTasks(Resource)

            If DisposeResource Then
                Resource.Dispose()
            End If
        End Sub


        ''' <summary>
        ''' Called by the component change service when a new component is added to the designer host's container.
        ''' We get notified of this for both our own internal adding/removing and also for those done on our behalf
        '''   by Undo/Redo.
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        ''' <remarks>
        ''' Here we do the actual adding of the resource to our list.
        ''' </remarks>
        Private Sub ComponentChangeService_ComponentAdded(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Handles m_ComponentChangeService.ComponentAdded
            Dim ResourceObject As Object = e.Component
            If Not TypeOf ResourceObject Is Resource Then
                Debug.Fail("How could we be adding a component that's not a Resource?")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be Nothing")
                Exit Sub
            End If

            'First thing, set the type resolution context (might not have been done yet if this component add was
            '  through a Undo/Redo operation)
            Resource.SetTypeResolutionContext(View)

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Adding " & Resource.ToString())

            Debug.Assert(Not FindResource(Resource.Name) Is Resource, "already a resource by that name")
            Debug.Assert(Not m_ResourcesHash.ContainsValue(Resource), "already exists in our list")

            'Add it to our list (upper-case the key to normalize for in-case-sensitive look-ups)
            m_ResourcesHash.Add(Resource.Name.ToUpperInvariant(), Resource)

            'Set the parent
            Resource.ParentResourceFile = Me

            'Notify the Find feature
            RootComponent.RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=True)

            'Update the number of resources in this resource's category
            Dim Category As Category = Resource.GetCategory(View.Categories)
            If Category IsNot Nothing Then
                Category.ResourceCount += 1
            Else
                Debug.Fail("Couldn't find category for resource")
            End If

            'Add to our list of resources to check for errors in idle time
            DelayCheckResourceForErrors(Resource)

            'Notify the view that resources have been added (if they were added by someone besides us, think "Undo/Redo")
            If Not m_AddingRemovingResourcesInternally Then
                Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: (Resource was added externally)")
                View.OnResourceAddedExternally(Resource)
            End If

            ' Add Reference to the project system if necessary.
            ' Note: we need do this what ever it is added by an editing or undoing/redoing, but never when we are loading the file.
            If Not m_IsLoadingResourceFile AndAlso Not m_InBatchAdding Then
                AddNecessaryReferenceToProject(New Resource() {Resource})
            End If

            'Set up a file watcher for this resource if it's a link
            If View IsNot Nothing Then
                Resource.AddFileWatcherEntry(View.FileWatcher)
            End If
        End Sub


        ''' <summary>
        ''' Called by the component change service when a Resource is removed, either by us or by an external
        '''   party (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Private Sub ComponentChangeService_ComponentRemoved(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Handles m_ComponentChangeService.ComponentRemoved
            Dim ResourceObject As Object = e.Component
            If Not TypeOf ResourceObject Is Resource Then
                Debug.Assert(TypeOf ResourceObject Is ResourceEditorRootComponent, "How could we be removing a component that's not a Resource?")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be Nothing")
                Exit Sub
            End If

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Removing " & Resource.ToString())

            Debug.Assert(FindResource(Resource.Name) Is Resource, "not found by Name")
            Debug.Assert(m_ResourcesHash.ContainsValue(Resource), "not found")

            'Go ahead and remove from our list (keys are normalized as upper-case)
            m_ResourcesHash.Remove(Resource.Name.ToUpperInvariant())

            'Remove the parent pointer
            Resource.ParentResourceFile = Nothing

            'Notify Find
            RootComponent.RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=True)

            'Update the number of resources in this resource's category
            Dim Category As Category = Resource.GetCategory(View.Categories)
            If Category IsNot Nothing Then
                Category.ResourceCount -= 1
            Else
                Debug.Fail("Couldn't find category for resource")
            End If

            'Clear any task list entries
            ClearResourceTasks(Resource)

            'If this Resource is slated to be checked for errors at idle time, that's no longer necessary.
            RemoveResourceToDelayCheckForErrors(Resource)

            'Notify the view that resources have been removed (if they were removed by someone besides us, think "Undo/Redo")
            If Not m_AddingRemovingResourcesInternally Then
                Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: (Resource was removed externally)")
                View.OnResourceRemovedExternally(Resource)
            End If

            'Remove the file watcher for this resource if it's a link (it won't be able to when the Undo/Redo engine disposes it, because
            '  it won't have a parent resource file then and can't get to the file watcher.
            If View IsNot Nothing Then
                Resource.RemoveFileWatcherEntry(View.FileWatcher)
            End If

            '
            ' Whenever we delete a resource, we have to make sure that we run our custom tool. Not doing so may cause problems if:
            ' step 1) Delete resource "A"
            ' step 2) Rename resource "B" to A
            ' 
            ' Now the CodeModel gets angry because we haven't flushed the contents of the designer between step 1 & 2, which means that
            ' both the properties are still in the generated code, and we'd end up with code resource "A" if the operation were to succeed.
            ' 
            ' Flushing & running the SFG after deletes will take care of this scenario... We also take care and post the message so that if
            ' we deleted multiple settings, we only do this once (perf)
            '
            DelayFlushAndRunCustomTool()
        End Sub


        ''' <summary>
        ''' Rename a resource in the ResourceFile.  This operation must come through here and not simply
        '''   be done directly on the Resource, because we also have to change the Resource's
        '''   ISite's name.
        ''' </summary>
        ''' <param name="Resource">Resource to rename</param>
        ''' <param name="NewName">New name.  If it's not unique, an exception is thrown.</param>
        ''' <remarks>Caller is responsible for showing error message boxes</remarks>
        Public Sub RenameResource(ByVal Resource As Resource, ByVal NewName As String)
            If Contains(Resource) Then
                Debug.Assert(DesignerHost.Container.Components(Resource.Name) IsNot Nothing)

                If Resource.Name.Equals(NewName, StringComparison.Ordinal) Then
                    'Name didn't change - nothing to do
                    Exit Sub
                End If

                'Verify that the new name is unique.  Note that it's okay to rename in such a
                '  way that only the case of the name changes (thus ExistingResource will be
                '  the same as Resource, since we find case-insensitively).
                Dim ExistingResource As Resource = FindResource(NewName)
                If ExistingResource IsNot Nothing AndAlso ExistingResource IsNot Resource Then
                    Throw NewException(SR.GetString(SR.RSE_Err_DuplicateName_1Arg, NewName), HelpIDs.Err_DuplicateName)
                End If

                'Make sure the resx file is checked out if it isn't yet.  Otherwise this failure might
                '  happen after we've already changed our internal name but before the site's name gets
                '  changed (because we change our internal name in response to listening in on 
                '  ComponentChangeService).
                View.RootDesigner.DesignerLoader.ManualCheckOut()

                'Rename the component's site's name.  This will cause a ComponentChangeService.ComponentRename event,
                '  which we listen to, and from which we will change the Resource's name.  (We need to do it
                '  this way, because Undo/Redo on a name will change the component's site's name only, and we
                '  need to pick up on those changes in order to reflect the change in the Resource itself.
                Resource.IComponent_Site.Name = NewName
            Else
                Debug.Fail("Trying to rename component that's not in the resource file")
            End If
        End Sub


        ''' <summary>
        ''' Called by the component change service when a resource has been renamed (rather, its component
        '''   ISite has been renamed).  We need to keep these in sync.
        ''' This is called both when we rename the Resource ourselves and when something external does it
        '''   (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Private Sub ComponentChangeService_ComponentRename(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentRenameEventArgs) Handles m_ComponentChangeService.ComponentRename
            If Not TypeOf e.Component Is Resource Then
                Debug.Fail("Got component rename event for a component that isn't a resource")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            Debug.Assert(e.OldName.Equals(Resource.Name, StringComparison.Ordinal))

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Renaming " & Resource.ToString() & " to """ & e.NewName & """")

            If Not Contains(Resource) Then
                Debug.Fail("Trying to rename component that's not in the resource file")
                Exit Sub
            End If

            If Resource.Name.Equals(e.NewName, StringComparison.OrdinalIgnoreCase) Then
                'The name hasn't changed (or differs only by case) - okay to rename
            ElseIf Not Contains(e.NewName) Then
                'The new name is not in use by any current resource - okay to rename
            Else
                'Whoops.  Something's wrong.
                Debug.Fail("Got a RenameComponent event to a name that's already in use - shouldn't have happened")
                Throw Common.CreateArgumentException("NewName")
            End If

            'Go ahead and make the change
            Debug.Assert(Resource.ValidateName(e.NewName, Resource.Name), "Component's Site's name was changed to an invalid name.  That shouldn't have happened.")

            Dim OldName As String = Resource.Name

            'Since resources in the ResourceFile are placed in the hashtable using the Name as key, if
            '  we change the Name, the location of the resource in the hashtable will not longer be correct
            '  (because we just changed the key, which it uses to search the hashtable).  So we have to
            '  remove ourself first and then re-insert ourself into the hashtable.
            m_ResourcesHash.Remove(Resource.Name.ToUpperInvariant())
            Try
                Resource.NameRawWithoutUndo = e.NewName
            Catch ex As Exception
                Common.RethrowIfUnrecoverable(ex)
                Debug.Fail("Unexpected error changing the name of the resource")
            End Try
            m_ResourcesHash.Add(Resource.Name.ToUpperInvariant(), Resource)

            'Notify the view that resources have been removed (if they were removed by someone besides us, think "Undo/Redo")
            View.OnResourceTouched(Resource)

            'Fix up the project to use the new name, if we're creating strongly typed resource classes            
            Try
                View.CallGlobalRename(OldName, e.NewName)
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                RootComponent.RootDesigner.GetView().DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Called by the component change service when a resource has been changed 
        ''' This is called both when we changed the Resource ourselves and when something external does it
        '''   (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Private Sub ComponentChangeService_ComponentChanged(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangedEventArgs) Handles m_ComponentChangeService.ComponentChanged
            If Not TypeOf e.Component Is Resource Then
                Debug.Fail("Got component rename event for a component that isn't a resource")
                Exit Sub
            End If

            View.OnResourceTouched(DirectCast(e.Component, Resource))

        End Sub

#End Region



#Region "Reading/Writing/Enumerating"


        ''' <summary>
        ''' Gets an enumerator.  Allows ResourceFile to be used in For Each statements directly.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetEnumerator() As Collections.IDictionaryEnumerator
            Return m_ResourcesHash.GetEnumerator
        End Function


        ''' <summary>
        ''' Reads resources from a TextReader on a resx file.
        ''' </summary>
        ''' <param name="TextReader">The TextReader to read from</param>
        ''' <remarks></remarks>
        Public Sub ReadResources(ByVal TextReader As TextReader)
            Dim ResXReader As ResXResourceReader
            Dim TypeResolutionService As ITypeResolutionService = View.GetTypeResolutionService()
            If TypeResolutionService IsNot Nothing Then
                ResXReader = New ResXResourceReader(TextReader, TypeResolutionService)
            Else
                ResXReader = New ResXResourceReader(TextReader, ResourceEditorView.GetDefaultAssemblyReferences())
            End If
            ResXReader.BasePath = m_BasePath

            ReadResources(ResXReader)
            ResXReader.Close()
        End Sub

        Public Function TypeNameConverter(ByVal runtimeType As System.Type) As String
            Debug.Assert(runtimeType IsNot Nothing, "runtimeType cannot be Nothing!")
            If m_multiTargetService Is Nothing Then
                Return runtimeType.AssemblyQualifiedName
            Else
                Return m_multiTargetService.TypeNameConverter(runtimeType)
            End If
        End Function


        ''' <summary>
        ''' Writes all resources into a TextWriter in resx format.
        ''' </summary>
        ''' <param name="TextWriter">TextWriter to write to</param>
        ''' <remarks></remarks>
        Public Sub WriteResources(ByVal TextWriter As TextWriter)
            Dim ResXWriter As IResourceWriter

            If m_resxService IsNot Nothing Then
                ResXWriter = m_resxService.GetResXResourceWriter(TextWriter, m_BasePath)
            Else
                Dim r As New ResXResourceWriter(TextWriter, AddressOf TypeNameConverter)
                r.BasePath = m_BasePath

                ResXWriter = r
            End If

            'This call will generate the resources.  We don't want to close the ResXWriter because it
            '  will also close the TextWriter, which closes its stream, which may not be expected by
            '  the caller.
            WriteResources(ResXWriter)
        End Sub


        ''' <summary>
        ''' Reads all resources into this ResourceFile from a ResXReader
        ''' </summary>
        ''' <param name="ResXReader">The ResXReader to read from</param>
        ''' <remarks></remarks>
        Private Sub ReadResources(ByVal ResXReader As ResXResourceReader)
            Debug.Assert(ResXReader IsNot Nothing, "ResXReader must exist!")

            m_IsLoadingResourceFile = True
            Try
                Dim orderID As Integer = 0
                Dim lastName As String = String.Empty

                m_ResourcesHash.Clear()
                m_MetadataList.Clear()

                ResXReader.UseResXDataNodes = True
                Using New WaitCursor
                    For Each DictEntry As DictionaryEntry In ResXReader
                        Dim Node As ResXDataNode = DirectCast(DictEntry.Value, ResXDataNode)
                        Dim Resource As Resource = Nothing

                        Try
                            Resource = New Resource(Me, Node, orderID, View)
                            orderID = orderID + 1

                            'If duplicate Names are found, this function will throw an exception (which is what we want - it will keep the
                            '  file from loading)
                            AddResource(Resource)

                            ' we check whether the resource item in the original file was alphabetized, we keep the style when we save it...
                            If m_alphabetizedOrder Then
                                If String.Compare(lastName, Resource.Name, StringComparison.Ordinal) > 0 Then
                                    m_alphabetizedOrder = False
                                Else
                                    lastName = Resource.Name
                                End If
                            End If
                        Catch ex As Exception
                            If Resource IsNot Nothing Then
                                Resource.Dispose()
                            End If
                            Throw
                        End Try
                    Next
                End Using

                ' Read and save meta data
                Dim enumerator As IDictionaryEnumerator = ResXReader.GetMetadataEnumerator()
                If enumerator IsNot Nothing Then
                    While enumerator.MoveNext()
                        m_MetadataList.Add(enumerator.Entry)
                    End While
                End If
            Finally
                m_IsLoadingResourceFile = False
            End Try
        End Sub


        ''' <summary>
        ''' Writes all resources into a ResXResourceWriter
        ''' </summary>
        ''' <param name="ResXWriter">The ResXResourceWriter instance to use</param>
        ''' <remarks></remarks>
        Private Sub WriteResources(ByVal ResXWriter As IResourceWriter)
            Debug.Assert(Not ResXWriter Is Nothing, "ResXWriter must exist.")

            If Not m_ResourcesHash Is Nothing Then
                ' NOTE: We save all meta data first...  We don't have a way maintain the right order between Meta data items and resource items today.
                ' Keep all meta data items if it is possible...
                If m_MetadataList IsNot Nothing AndAlso m_MetadataList.Count > 0 Then
                    Dim NewWriter As ResXResourceWriter = TryCast(ResXWriter, ResXResourceWriter)
                    If NewWriter IsNot Nothing Then
                        For Each entry As DictionaryEntry In m_MetadataList
                            NewWriter.AddMetadata(CStr(entry.Key), entry.Value)
                        Next
                    End If
                End If

                If m_ResourcesHash.Count > 0 Then
                    Dim resourceList As Resource() = New Resource(m_ResourcesHash.Count - 1) {}
                    Dim i As Integer = 0
                    For Each Resource As Resource In m_ResourcesHash.Values
                        resourceList(i) = Resource
                        i = i + 1
                    Next

                    Dim comparer As IComparer
                    If m_alphabetizedOrder Then
                        comparer = New AlphabetizedOrderComparer()
                    Else
                        comparer = New OriginalOrderComparer()
                    End If

                    Array.Sort(resourceList, comparer)

                    Dim failedList As String = Nothing
                    Dim extraMessage As String = Nothing

                    For i = 0 To resourceList.Length - 1
                        Dim resource As Resource = resourceList(i)
                        Try
                            ResXWriter.AddResource(resource.ResXDataNode.Name, resourceList(i).ResXDataNode)
                        Catch ex As Exception
                            ' UNDONE: we should study what we should do if this failed...
                            RethrowIfUnrecoverable(ex)

                            resource.SetTaskFromGetValueException(ex, ex)
                            If failedList IsNot Nothing Then
                                failedList = SR.GetString(SR.RSE_Err_NameList, failedList, resource.Name)
                            Else
                                failedList = SR.GetString(SR.RSE_Err_Name, resource.Name)
                                extraMessage = ex.Message
                            End If
                        End Try
                    Next

                    If failedList IsNot Nothing Then
                        RootComponent.RootDesigner.GetView().DsMsgBox(SR.GetString(SR.RSE_Err_CantSaveResouce_1Arg, failedList) & VB.vbCrLf & VB.vbCrLf & extraMessage, _
                            MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_CantSaveBadResouceItem)
                    End If
                End If

                ResXWriter.Generate()
            Else
                Throw New Exception("Must read resources before attempting to write")
            End If
        End Sub

#End Region

#Region "UI"


        ''' <summary>
        ''' Invalidates this resource in the resource editor view, which causes it to be updated on the next
        '''   paint.
        ''' </summary>
        ''' <param name="Resource">The resource to invalidate</param>
        ''' <param name="InvalidateThumbnail">If True, then the Resource's thumbnail is also invalidated so it will be regenerated on the next paint.</param>
        ''' <remarks></remarks>
        Public Sub InvalidateResourceInView(ByVal Resource As Resource, Optional ByVal InvalidateThumbnail As Boolean = False)
            If RootComponent.RootDesigner IsNot Nothing AndAlso RootComponent.RootDesigner.GetView() IsNot Nothing Then
                RootComponent.RootDesigner.GetView().InvalidateResource(Resource, InvalidateThumbnail)
            End If
        End Sub

#End Region

#Region "Task List integration"


#Region "ResourceTaskType enum"

        ''' <summary>
        ''' The types of errors which can occur for a Resource.
        ''' NOTE: These types are mutually exclusive.  I.e., each Resource is allowed to log
        '''   a single task list item for *each* of the values in this enum.  E.g., a resource
        '''   can have a task list item for bad link (CantInstantiateResource) and for
        '''   a bad Name, and both task items will show up in the task list.
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum ResourceTaskType
            'ID is bad or otherwise not a good idea
            BadName

            'Unable to instantiate the resource (bad link, assembly not found, etc.)
            CantInstantiateResource

            'Comments in a form's resx file will be stripped by the form designer.
            CommentsNotSupportedInThisFile
        End Enum

#End Region


#Region "Nested class - ResourceTaskSet"

        ''' <summary>
        ''' A set of task list entries for a single Resource.  We create one of these whenever we
        '''   associate a task list entry with a Resource.  It has an array of tasks, which should 
        '''   be considered as a set of "slots" for task list entries.  Each slot is of a different
        '''   kind.  A resource can have a single task list entry for each slot, and thus for
        '''   each distinct kind of task list entry or error/warning.
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class ResourceTaskSet
            'The number of error types that we have.  Calculated from the ResourceTaskType enum.
            Private Shared m_ErrorTypeCount As Integer

            'Backs Tasks property
            '  (could just have well been a hashtable as an array, but an array is more lightweight)
            Private m_Tasks() As ResourceTask



            ''' <summary>
            ''' Shared sub New.  Calculates m_ErrorTypeCount and verifies that the
            '''   enum types start with zero and are contiguous.  This is necessary in order
            '''   to use them as an index into a simple array.
            ''' </summary>
            ''' <remarks></remarks>
            Shared Sub New()
                m_ErrorTypeCount = System.Enum.GetValues(GetType(ResourceTaskType)).Length

#If DEBUG Then
                'Verify that the enums start with zero and are contiguous
                For Index As Integer = 0 To m_ErrorTypeCount - 1
                    Debug.Assert(CInt(System.Enum.GetValues(GetType(ResourceTaskType)).GetValue(Index)) = Index, _
                        "The values in ResourceErrorType must start at 0 and be contiguous")
                Next
#End If
            End Sub


            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub New()
                ReDim m_Tasks(m_ErrorTypeCount - 1)
            End Sub




            ''' <summary>
            ''' Gets the array (indexed by ResourceTaskType) of tasks in this set
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Tasks() As ResourceTask()
                Get
                    Return m_Tasks
                End Get
            End Property

        End Class

#End Region


#Region "Nested class - ResourceTask"

        ''' <summary>
        ''' A single task list entry for resources.  Contains a pointer back to the Resource
        '''   that is associated with it, for handling navigation (when the user double-clicks
        '''   on a task list entry).
        ''' </summary>
        ''' <remarks></remarks>
        Friend NotInheritable Class ResourceTask
            Inherits ErrorTask

            'The resource associated with this task list entry.
            Private m_Resource As Resource


            ''' <summary>
            ''' Constuctor.
            ''' </summary>
            ''' <param name="Resource"></param>
            ''' <remarks></remarks>
            Public Sub New(ByVal Resource As Resource)
                m_Resource = Resource
            End Sub


            ''' <summary>
            ''' The resource associated with this task list entry.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Resource() As Resource
                Get
                    Return m_Resource
                End Get
            End Property

        End Class

#End Region



        ''' <summary>
        ''' Returns True iff the specified Resource has any task list items.
        ''' </summary>
        ''' <param name="resource">The resource to look for task entries for.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ResourceHasTasks(ByVal Resource As Resource) As Boolean
            Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
            If TaskSet Is Nothing Then
                Return False
            End If

            'Check all task slots in the task set
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    'We found a task.
                    Return True
                End If
            Next

            Return False
        End Function


        ''' <summary>
        ''' Gets the task entry text for a particular resource and resource type.  Returns Nothing if there is
        '''   no such task.
        ''' </summary>
        ''' <param name="Resource">The task to get the text for.</param>
        ''' <param name="TaskType">The type of task list entry to retrieve for this Resource.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetResourceTaskMessage(ByVal Resource As Resource, ByVal TaskType As ResourceTaskType) As String
            Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
            If TaskSet Is Nothing Then
                Return Nothing
            End If

            Dim Task As Task = TaskSet.Tasks(TaskType)
            If Task Is Nothing Then
                Return Nothing
            End If

            'Found an entry.
            Return Task.Text
        End Function


        ''' <summary>
        ''' Gets the text from all task list entries for a particular Resource, separated by
        '''   CR/LF.
        ''' </summary>
        ''' <param name="Resource">The resource to look up task list entries for.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetResourceTaskMessages(ByVal Resource As Resource) As String
            Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
            If TaskSet Is Nothing Then
                Return Nothing
            End If

            Dim Messages As String = ""
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    If Messages <> "" Then
                        Messages &= Microsoft.VisualBasic.vbCrLf
                    End If

                    Messages &= TaskSet.Tasks(i).Text
                End If
            Next

            Return Messages
        End Function


        ''' <summary>
        ''' This handler gets called when the user double-clicks on a task list entry.
        ''' </summary>
        ''' <param name="sender">The task that was double-clicked.</param>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Private Sub OnTaskNavigate(ByVal sender As Object, ByVal e As EventArgs)
            Dim Task As ResourceTask = TryCast(sender, ResourceTask)
            If Task Is Nothing Then
                Debug.Fail("Navigate sender not a resourcetask?")
                Exit Sub
            End If

            If Task.Resource IsNot Nothing Then
                View.NavigateToResource(Task.Resource)
            Else
                Debug.Fail("Task list entry didn't contain a resource reference")
            End If
        End Sub


        ''' <summary>
        ''' Associates a particular task list text with a given resource.  If there is already a task list
        '''   entry associated with this resource and resource task type, this new one takes its place.
        ''' </summary>
        ''' <param name="Resource">The Resource for which the new task list entry will apply.</param>
        ''' <param name="TaskType">The type of task list entry (type of error/warning)</param>
        ''' <param name="Text">The text of the new task list entry.</param>
        ''' <param name="Priority">The priority of the new task list entry.</param>
        ''' <param name="HelpLink">The help link of the new task list entry.</param>
        ''' <param name="ErrorCategory">The ErrorCategory of the new task list entry. It is an Error or Warning.</param>
        ''' <remarks></remarks>
        Public Sub SetResourceTask(ByVal Resource As Resource, ByVal TaskType As ResourceTaskType, ByVal Text As String, ByVal Priority As TaskPriority, ByVal HelpLink As String, ByVal ErrorCategory As TaskErrorCategory)
            Debug.Assert(Resource IsNot Nothing)
            Dim taskProvider As ErrorListProvider = ErrorListProvider
            If taskProvider IsNot Nothing Then
                'Get current task set for this resource.  If none, then create one.
                Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
                If TaskSet Is Nothing Then
                    TaskSet = New ResourceTaskSet
                    m_ResourceErrorsHash.Add(Resource, TaskSet)
                End If

                'Optimization: If the task already exists with the correct Text and Priority, there's no need
                '  to update the task list, we can just leave things as they are.
                Dim OldTask As ResourceTask = TaskSet.Tasks(TaskType)
                If OldTask IsNot Nothing Then
                    If OldTask.Text.Equals(Text, StringComparison.Ordinal) _
                        AndAlso OldTask.Priority = Priority _
                        AndAlso OldTask.Resource Is Resource _
                    Then
                        'The task is already there and set up properly, so there's nothing to do.
                        Exit Sub
                    Else
                        'Need to remove the old task
                        taskProvider.Tasks.Remove(OldTask)
                    End If
                End If

                'Create the new task and put it in the task set.
                Dim Task As New ResourceTask(Resource)
                TaskSet.Tasks(TaskType) = Task
                With Task
                    AddHandler .Navigate, AddressOf OnTaskNavigate 'This sets up navigation
                    .CanDelete = False
                    '.Category = TaskCategory.BuildCompile
                    .Checked = False
                    .Document = RootComponent.RootDesigner.GetResXFileNameAndPath()
                    .HelpKeyword = HelpLink
                    .IsCheckedEditable = False
                    .IsPriorityEditable = False
                    .IsTextEditable = False
                    .Priority = Priority
                    .ErrorCategory = ErrorCategory
                    .Text = Text
                End With

                'And to the task list, and get the task list to show so that the user is aware
                '  there are errors.
                taskProvider.Tasks.Add(Task)

                ' We want to bring up the error list window without activating the window... It is especially true because we do validation at Idle time.
                Dim vsUIShell As IVsUIShell = TryCast(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                If vsUIShell IsNot Nothing Then
                    Dim taskProviderToolWindowID As Guid = New Guid(EnvDTE80.WindowKinds.vsWindowKindErrorList)
                    Dim vsWindowFrame As IVsWindowFrame = Nothing
                    If VSErrorHandler.Succeeded(vsUIShell.FindToolWindow(CUInt(__VSFINDTOOLWIN.FTW_fForceCreate), taskProviderToolWindowID, vsWindowFrame)) Then
                        If vsWindowFrame IsNot Nothing Then
                            If VSErrorHandler.Failed(vsWindowFrame.ShowNoActivate()) Then
                                Debug.Fail("Why we failed to activate the error window")
                            End If
                        End If
                    End If
                Else
                    Debug.Fail("Why we can't find IVsUIShell service?")
                End If

                'We need to invalidate the resource to ensure that the error icon shows up next to
                '  it.
                Resource.InvalidateUI()
            End If
        End Sub


        ''' <summary>
        ''' Clear a the slot for a particular task type in a particular resource.
        ''' </summary>
        ''' <param name="Resource">The resource from which the task list entry will be cleared.</param>
        ''' <param name="TaskType">The type of task list entry to clear, if it exists.</param>
        ''' <remarks></remarks>
        Public Sub ClearResourceTask(ByVal Resource As Resource, ByVal TaskType As ResourceTaskType)
            Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
            If TaskSet Is Nothing Then
                Exit Sub 'Nothing to clear
            End If

            Dim Task As Task = TaskSet.Tasks(TaskType)
            If Task IsNot Nothing Then
                'Remove the task for this task type, if it exists.
                If m_ErrorListProvider IsNot Nothing Then
                    m_ErrorListProvider.Tasks.Remove(Task)
                End If
                TaskSet.Tasks(TaskType) = Nothing

                'We need to invalidate the resource to ensure that the error icon next to it gets cleared.
                Resource.InvalidateUI()
            End If

            'If there are no more tasks for this Resource, we can remove the ResourceTaskSet
            '  entry from the hash table.
            Dim Empty As Boolean = True
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    Empty = False
                    Exit For
                End If
            Next
            If Empty Then
#If DEBUG Then
                Dim OldCount As Integer = m_ResourceErrorsHash.Count
#End If
                m_ResourceErrorsHash.Remove(key:=Resource)

#If DEBUG Then
                Debug.Assert(m_ResourceErrorsHash.Count = OldCount - 1)
#End If
            End If
        End Sub


        ''' <summary>
        ''' Clears all task list entries for the given resource.
        ''' </summary>
        ''' <param name="Resource">The resource to clear.</param>
        ''' <remarks></remarks>
        Public Sub ClearResourceTasks(ByVal Resource As Resource)
            Dim TaskSet As ResourceTaskSet = DirectCast(m_ResourceErrorsHash(Resource), ResourceTaskSet)
            If TaskSet IsNot Nothing Then
                'Remove all entries for this resource
                For i As Integer = 0 To TaskSet.Tasks.Length - 1
                    Dim Task As Task = TaskSet.Tasks(i)
                    If Task IsNot Nothing Then
                        If m_ErrorListProvider IsNot Nothing Then
                            m_ErrorListProvider.Tasks.Remove(Task)
                        End If
                    End If
                Next

                '... and then remove the task set itself from the hash table.
#If DEBUG Then
                Dim OldCount As Integer = m_ResourceErrorsHash.Count
#End If
                m_ResourceErrorsHash.Remove(key:=Resource)

#If DEBUG Then
                Debug.Assert(m_ResourceErrorsHash.Count = OldCount - 1)
#End If
            End If
        End Sub


        ''' <summary>
        ''' Adds a resource to the list of resources that need to be checked for errors
        '''   during idle time processing.  When we load a resource file, we only check
        '''   minimally for errors.  We don't do the more expensive check of instantiating
        '''   the resource, we save that for idle time.
        ''' </summary>
        ''' <param name="Resource">The Resource which should be delay-checked later for errors.</param>
        ''' <remarks>
        ''' It's okay to add the same resource multiple times.
        ''' </remarks>
        Public Sub DelayCheckResourceForErrors(ByVal Resource As Resource)
            Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Adding resource to list: " & Resource.Name)

            If m_ResourcesToDelayCheckForErrors.Count = 0 AndAlso Not m_DelayCheckSuspended Then
                'We need to hook up for idle-time processing so we can delay-check this resource.
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Hooking up idle-time processing")
                AddHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayCheckForErrors
            End If

            'Add the resource to the list
            If Not m_ResourcesToDelayCheckForErrors.Contains(Resource) Then
                m_ResourcesToDelayCheckForErrors.Add(Resource)
            End If
        End Sub


        ''' <summary>
        ''' Causes all the resources in the file to be again queued for validation during
        '''   idle time.
        ''' IMPORTANT NOTE: It is *not* necessary to call this function during resource file
        '''   load, because in that case the resources all gets added to the delay-check
        '''   list during component add.  This is only needed if something has changed that
        '''   might change the validation of some resources.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub DelayCheckAllResourcesForErrors()
            StopDelayingCheckingForErrors()
            If m_ResourcesHash IsNot Nothing Then
                For Each Entry As DictionaryEntry In m_ResourcesHash
                    Dim Resource As Resource = DirectCast(Entry.Value, Resource)
                    DelayCheckResourceForErrors(Resource)
                Next
            End If
        End Sub


        ''' <summary>
        ''' Removes a resource from the list of resources that need to be checked for
        '''   errors during idle time processing.  Generally the reason for removing it
        '''   is that it is being deleted and so is no longer valid.
        ''' </summary>
        ''' <param name="Resource">The resource to add.  If it doesn't exist, this call is a NOOP.</param>
        ''' <remarks></remarks>
        Private Sub RemoveResourceToDelayCheckForErrors(ByVal Resource As Resource)
            If m_ResourcesToDelayCheckForErrors.Contains(Resource) Then
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Removing resource from list: " & Resource.Name)
                m_ResourcesToDelayCheckForErrors.Remove(Resource)

                If m_ResourcesToDelayCheckForErrors.Count = 0 Then
                    'No more resources to check right now, so we should un-hook our idle-time processing.
                    Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Unhooking idle-time processing")
                    RemoveHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayCheckForErrors
                End If
            End If
        End Sub


        'CONSIDER: This event only fires once for every Windows message, which means it may take a while to
        '  get through all the resources and have them checked (processing only occurs *while* the user is
        '  interacting with the shell.
        '  Consider hooking up to the shell's MSO-based idle processing instead, where we can simply keep processing
        '  resources while the user is not interacting with the computer.  See Application.cs.  Might be possible
        '  to search OLE message filter for the current component manager and call FContinueIdle on it.
        '  Or we could continue processing for x milliseconds per call...

        ''' <summary>
        ''' Our idle-time processing which checks resources for errors to be added to the task
        '''   list.  This is used so that we delay loading resources from disk (makes our
        '''   start-up a lot faster).
        ''' </summary>
        ''' <param name="sender">Event sender.</param>
        ''' <param name="e">Event args.</param>
        ''' <remarks>
        ''' Idle-time processing is done on the main thread, so there's no need for synchronization.
        ''' We must keep our idle-time processing short, so we currently only process a single
        '''   resource per call.
        ''' </remarks>
        Private Sub OnDelayCheckForErrors(ByVal sender As Object, ByVal e As EventArgs)
            If m_MainThread IsNot Threading.Thread.CurrentThread Then
                Debug.Fail("Idle processing is supposed to occur on the main thread!")
                Exit Sub
            End If

            If m_ResourcesToDelayCheckForErrors.Count > 0 Then
                Dim Resource As Resource = DirectCast(m_ResourcesToDelayCheckForErrors(0), Resource)
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Processing: " & Resource.Name)

                'Check the resource for errors
                Resource.CheckForErrors(FastChecksOnly:=False)

                '... and remove it from our list.
                RemoveResourceToDelayCheckForErrors(Resource)
            Else
                Debug.Fail("Why didn't we unhook our idle-time processing if there were no more resources to process?")
                RemoveHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayCheckForErrors
            End If
        End Sub


        ''' <summary>
        ''' Stops delay-checking for errors, and removes ourselves from idle-time processing.  The list
        '''   of resources to delay-check for errors will be cleared.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub StopDelayingCheckingForErrors()
            While m_ResourcesToDelayCheckForErrors.Count > 0
                RemoveResourceToDelayCheckForErrors(DirectCast(m_ResourcesToDelayCheckForErrors(0), Resource))
            End While
        End Sub

        ''' <summary>
        '''  if suspendIt = true Suspends delay-checking for errors 
        '''   otherwise, resume the delay-checking process
        ''' </summary>
        ''' <param name="suspendIt"></param>
        ''' <remarks>We also use Idle time to load images. The delay checking should be low priority, and need to be disabled until we finish paining the screen.</remarks>
        Friend Sub SuspendDelayingCheckingForErrors(ByVal suspendIt As Boolean)
            If suspendIt <> m_DelayCheckSuspended Then
                m_DelayCheckSuspended = suspendIt
                If m_ResourcesToDelayCheckForErrors.Count > 0 Then
                    If m_DelayCheckSuspended Then
                        RemoveHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayCheckForErrors
                    Else
                        AddHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayCheckForErrors
                    End If
                End If
            End If
        End Sub
#End Region

#Region "Resource comparer"
        ''' <summary>
        '''  The AlphabetizedOrderComparer is used to sort resource items in alphabet order.
        ''' </summary>
        Private Class AlphabetizedOrderComparer
            Implements IComparer

            Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
                Dim r1 As Resource = CType(x, Resource)
                Dim r2 As Resource = CType(y, Resource)

                Return String.Compare(r1.Name, r2.Name, StringComparison.OrdinalIgnoreCase)
            End Function
        End Class

        ''' <summary>
        '''  The OriginalOrderComparer is used to sort resource items and keep the old item before new ones...
        ''' </summary>
        Private Class OriginalOrderComparer
            Implements IComparer

            Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
                Dim r1 As Resource = CType(x, Resource)
                Dim r2 As Resource = CType(y, Resource)

                If r1.OrderID = r2.OrderID Then
                    Return String.Compare(r1.Name, r2.Name, StringComparison.OrdinalIgnoreCase)
                Else
                    Return r1.OrderID - r2.OrderID
                End If
            End Function
        End Class
#End Region


#Region "Miscellaneous"

        ''' <summary>
        ''' Scan all resources items and add necessary reference to the project (if possible)
        ''' </summary>
        Friend Sub AddNecessaryReferenceToProject()
            AddNecessaryReferenceToProject(m_ResourcesHash.Values)
        End Sub

        ''' <summary>
        ''' Scan a list of resources items and add necessary reference to the project (if possible)
        ''' </summary>
        ''' <param name="Resources">A collection of resource items </param>
        ''' <remarks>For performance reasons, we processes a collection of items one time</remarks>
        Private Sub AddNecessaryReferenceToProject(ByVal Resources As ICollection)
            Debug.Assert(Resources IsNot Nothing, "Invalid Resources collection")

            Dim TypeResolutionService As ITypeResolutionService = View.GetTypeResolutionService()
            If TypeResolutionService IsNot Nothing Then
                ' TypeResolutionService should be there for all language projects. We should skip this function if it is not there.
                ' It could be the scenario that a resource file is opened directly.

                Dim vsLangProj As VSLangProj.VSProject = Nothing

                Dim typeNameCollection As New System.Collections.Specialized.StringCollection
                Dim assemblyCollection As New System.Collections.Specialized.StringCollection

                For Each Resource As Resource In Resources
                    Dim resourceType As Type = Nothing
                    Dim cachedValue As Object = Resource.CachedValue
                    If cachedValue IsNot Nothing Then
                        resourceType = cachedValue.GetType()
                    Else
                        ' If it has been resolved once, skip it
                        Dim typeName As String = Resource.ValueTypeName
                        If Not typeNameCollection.Contains(typeName) Then
                            typeNameCollection.Add(typeName)
                            resourceType = TypeResolutionService.GetType(typeName, False)
                        End If
                    End If

                    ' We should ignore, if we couldn't find type...
                    ' We also skip the mscorlib.dll
                    If resourceType IsNot Nothing AndAlso resourceType.Assembly IsNot GetType(String).Assembly Then
                        Dim assmeblyName As String = resourceType.Assembly.GetName().Name

                        ' skip the assembly if we have already processed it
                        If Not assemblyCollection.Contains(assmeblyName) Then
                            assemblyCollection.Add(assmeblyName)

                            Try
                                If vsLangProj Is Nothing Then
                                    Dim dteProject As EnvDTE.Project = ShellUtil.DTEProjectFromHierarchy(View.GetDesignerLoader().VsHierarchy)
                                    If dteProject IsNot Nothing Then
                                        vsLangProj = TryCast(dteProject.Object, VSLangProj.VSProject)
                                    End If

                                    ' NOTE: we only support project system has VsLangProj supporting.
                                    '  This function is not supported in other project systems, like Venus projects
                                    If vsLangProj Is Nothing Then
                                        Return
                                    End If
                                End If

                                If vsLangProj.References.Find(assmeblyName) Is Nothing Then
                                    ' Let the project system to handle the exactly version...
                                    vsLangProj.References.Add(assmeblyName)
                                End If
                            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                                ' We should ignore the error if the project system failed to do so..
                                If Not TypeOf ex Is CheckoutException Then
                                    Debug.Fail("Failed to add reference to assembly contining type: " & resourceType.Name & " Error: " & ex.Message)
                                End If

                                ' NOTE: we need consider to prompt the user an waring message. But it could be very annoying if we pop up many message boxes in one transaction.
                                '  We should consider a global service to collect all warning messages, and show in one dialog box when the transaction is commited.
                            End Try
                        End If
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Returns true iff this resource file is set up for strongly-typed code generation (i.e., a [resxname].vb file
        '''   is created from it).
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsGeneratedToCode() As Boolean

            ' Code gen is not supported currently for resw files
            If RootComponent IsNot Nothing AndAlso
               RootComponent.RootDesigner IsNot Nothing AndAlso
               RootComponent.RootDesigner.IsEditingResWFile() Then
                Return False
            End If

            ' Venus project does not support CustomTool property, but they generate code for all resource files under a special directory...
            If RootComponent IsNot Nothing AndAlso RootComponent.IsGlobalResourceInASP() Then
                Return True
            End If

            'Check the Custom Tool property (if there is one in this project type) to see if it's set
            Dim CustomToolValue As String = Nothing
            If RootComponent IsNot Nothing AndAlso RootComponent.RootDesigner IsNot Nothing Then
                Debug.Assert(RootComponent.RootDesigner.HasView)
                Dim View As ResourceEditorView = RootComponent.RootDesigner.GetView()
                CustomToolValue = View.GetCustomToolCurrentValue()
            End If

            Return CustomToolValue <> ""
        End Function


        ''' <summary>
        ''' Gets the CodeDomProvider for this ResX file, or Nothing if none found.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetCodeDomProvider() As CodeDomProvider
            Dim CodeDomProvider As CodeDomProvider = Nothing
            Dim CodeGenerator As ICodeGenerator = Nothing

            If RootComponent IsNot Nothing AndAlso RootComponent.IsGlobalResourceInASP() Then
                ' Venus project always use C# CodeDomProvider to generate StrongType code for the resource file.
                Return New Microsoft.CSharp.CSharpCodeProvider()
            End If

            If ServiceProvider IsNot Nothing Then
                Try
                    Dim VsmdCodeDomProvider As Designer.Interfaces.IVSMDCodeDomProvider = TryCast(ServiceProvider.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If VsmdCodeDomProvider IsNot Nothing Then
                        Return TryCast(VsmdCodeDomProvider.CodeDomProvider, CodeDomProvider)
                    End If
                Catch ex As System.Runtime.InteropServices.COMException
                    Debug.Assert(ex.ErrorCode = Interop.NativeMethods.E_FAIL OrElse ex.ErrorCode = Interop.NativeMethods.E_NOINTERFACE, "Unexpected COM error getting CodeDomProvider from service")
                    Return Nothing
                End Try
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Post a flush and run custom tool request it request not already posted
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DelayFlushAndRunCustomTool()
            If Not m_delayFlushAndRunCustomToolQueued Then
                If Me.View IsNot Nothing AndAlso Me.View.IsHandleCreated Then
                    Me.View.BeginInvoke(New MethodInvoker(AddressOf Me.DelayFlushAndRunCustomToolImpl))
                    m_delayFlushAndRunCustomToolQueued = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' Flush and run the single file generator 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DelayFlushAndRunCustomToolImpl()
            m_delayFlushAndRunCustomToolQueued = False
            If View IsNot Nothing AndAlso View.GetDesignerLoader() IsNot Nothing Then
                Try
                    View.GetDesignerLoader().RunSingleFileGenerator(True)
                Catch ex As Exception
                    Try
                        View.DsMsgBox(ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Catch ex2 As exception
                        Debug.Fail("Unable to show exception message for exception: " & ex.ToString())
                    End Try
                End Try
            End If
        End Sub

#End Region

    End Class

End Namespace

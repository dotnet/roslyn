Option Strict On
Option Explicit On

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Package
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports System
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Diagnostics
Imports System.IO


Namespace Microsoft.VisualStudio.Editors.DesignerFramework
    Friend MustInherit Class BaseDesignerLoader
        Inherits BasicDesignerLoader
        Implements IVsTextBufferDataEvents, IVsRunningDocTableEvents2, IDisposable

        Private m_SourceCodeControlManager As SourceCodeControlManager

        'We use the DesignerDocDataService class as a cheap way of getting check-in/out behavior.  See
        '  the Modify property.  We let it manage our DocData as its "primary" (in this case, only)
        '  doc data.  It will automatically track changes and handle check-in/check-out (see the
        '  Modifyi property).
        Protected m_DocDataService As DesignerDocDataService

        'Window events from the DTE that we can hook up to
        Protected WithEvents m_WindowEvents As EnvDTE.WindowEvents

        ' Support readOnly mode...
        Private m_ReadOnlyMode As Boolean
        Private m_ReadOnlyPrompt As String


        ''' <summary>
        ''' Attempts to check out the DocData manually (without dirtying the DocData).  
        ''' Will throw an exception if it fails.
        ''' </summary>
        ''' <param name="ProjectReloaded">[out] Set to True if the project was reloaded during the checkout.  Callers should check this value and avoid
        '''   touching the controls or project if it is true, but should simply exit early.  This can happen if the
        '''   project file was checked out and simultaneously updated to a newer version in some SCC systems.
        ''' </param>
        ''' <remarks>
        ''' This routine should be used sparingly.  It is automatically called whenever a component is changed
        '''   (by listening to ComponentChangeService), so it will not normally be needed.  But it might be
        '''   needed at the beginning of code where an action might be left half done if the checkout failure 
        '''   were to happen in the middle of things.
        ''' </remarks>
        Friend Overridable Sub ManualCheckOut(ByRef ProjectReloaded As Boolean)
            If m_ReadOnlyMode Then
                Throw New ApplicationException(m_ReadOnlyPrompt)
            End If

            Try
                If ManagingDynamicSetOfFiles Then
                    m_SourceCodeControlManager.ManagedFiles = FilesToCheckOut
                End If

                Dim RootDesigner As IRootDesigner = Nothing
                Dim View As BaseDesignerView = Nothing
                If LoaderHost IsNot Nothing AndAlso LoaderHost.RootComponent IsNot Nothing Then
                    RootDesigner = TryCast(LoaderHost.GetDesigner(LoaderHost.RootComponent), IRootDesigner)
                End If
                If RootDesigner IsNot Nothing Then
                    View = TryCast(RootDesigner.GetView(ViewTechnology.Default), BaseDesignerView)
                End If
                Debug.Assert(View IsNot Nothing, "ManualCheckOut: Unable to locate base designer view to call Enter/LeaveProjectCheckoutSection")

                If View IsNot Nothing Then
                    View.EnterProjectCheckoutSection()
                Else
                    Debug.Fail("ManualCheckOut: Unable to retrieve base designer view")
                End If
                Try

                    m_SourceCodeControlManager.EnsureFilesEditable()

                Finally
                    If View IsNot Nothing Then
                        View.LeaveProjectCheckoutSection()
                        ProjectReloaded = View.ProjectReloadedDuringCheckout
                    End If
                End Try
            Catch ex As Exception
                Switches.TraceSCC("Checkout failed: " & ex.Message)

                'Check-out has failed.  We need to handle this gracefully at all places in the UI where this could happen.
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Indicate if it is OK to edit the current set of managed files from a SCC perspective...
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overridable Function OkToEdit() As Boolean
            If m_ReadOnlyMode Then
                Return False
            End If

            If ManagingDynamicSetOfFiles Then
                m_SourceCodeControlManager.ManagedFiles = FilesToCheckOut
            End If
            Return m_SourceCodeControlManager.AreFilesEditable()
        End Function

        ''' <summary>
        ''' Indicate if we are managing a dynamic set of files (if the set of files to check out as a result of editing 
        ''' the primary file changes over time)
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' This can be used by editors that sometimes add files to the project system to either specify the project file (if the 
        ''' file isn't in the project and needs to be added) or the newly added file. One example is the settings designer's handling
        ''' of the app.config file
        '''</remarks>
        Protected Overridable ReadOnly Property ManagingDynamicSetOfFiles() As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Get the list of files that you want to check out in the ManualCheckout
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overridable ReadOnly Property FilesToCheckOut() As System.Collections.Generic.List(Of String)
            Get
                Dim projItem As EnvDTE.ProjectItem = ProjectItem
                Return Common.ShellUtil.FileNameAndGeneratedFileName(projItem)
            End Get
        End Property


        ''' <summary>
        ''' Set ReadOnly Mode or prompt message
        ''' </summary>
        ''' <param name="ReadOnlyMode"></param>
        ''' <param name="Message"></param>
        ''' <remarks></remarks>
        Friend Sub SetReadOnlyMode(ByVal ReadOnlyMode As Boolean, ByVal Message As String)
            m_ReadOnlyMode = ReadOnlyMode
            m_ReadOnlyPrompt = Message
        End Sub


        ''' <summary>
        ''' This protected property indicates if there have been any
        '''   changes made to the design surface.  The Flush method 
        '''   gets the value of this property to determine if it needs
        '''   to generate a code dom tree.  This property is set by
        '''   the designer loader when it detects a change to the 
        '''   design surface.  You can override this to perform
        '''   additional work, such as checking out a file from source
        '''   code control.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub OnModifying()
            Switches.TraceSCC("BaseDesignerLoader.OnModifying()")

            MyBase.OnModifying()
            'In order to get check-in/check-out to work, we need to notify the DocData
            '  that we are dirty.  DesignerDocDataService will notice this and try
            '  to check out the file automatically if needed.
            If DocData IsNot Nothing AndAlso Not DocData.Modified Then
                Dim ProjectReloaded As Boolean
                ManualCheckOut(ProjectReloaded) 'Nowhere to pass this flag back to, if this is needed, client can check the flag on BaseDesignerView
                DocData.Modify()
            End If
        End Sub


        Protected NotOverridable Overrides Sub PerformFlush(ByVal SerializationManager As IDesignerSerializationManager)
            HandleFlush(SerializationManager)
        End Sub

        Private m_LoadDeferred As Boolean = False
        Private m_DeferredLoaderService As IDesignerLoaderService
        Private m_DeferredLoadManager As IDesignerSerializationManager
        Protected NotOverridable Overrides Sub PerformLoad(ByVal SerializationManager As IDesignerSerializationManager)
            ' Are we aready loaded?  If not, then we must defer the entire process for later.
            '
            If DocData.Name Is Nothing OrElse DocData.Name.Length = 0 Then
                'Visual Studio has handed us a DocData before telling that doc data to load.  We have to handle
                '  this by deferring the actual load until later.  To do this, we use the dependent load
                '  feature of designer loaders.  When the doc data has loaded, we will note that fact by
                '  tracking IVsTextBufferDataEvents.OnLoadCompleted(), at which point we'll call into this
                '  function again.

                Debug.Assert(Not m_LoadDeferred, "Load already deferred in PerformLoad()")
                m_LoadDeferred = True

                m_DeferredLoaderService = DirectCast(GetService(GetType(IDesignerLoaderService)), IDesignerLoaderService)
                If m_DeferredLoaderService Is Nothing Then
                    Debug.Fail("Deferred load doc data requires support for IDesignerLoaderService")
                    Throw New NotSupportedException(SR.GetString(SR.DFX_IncompatibleBuffer))
                End If

                Debug.Assert(m_DeferredLoadManager Is Nothing)
                m_DeferredLoadManager = SerializationManager
                m_DeferredLoaderService.AddLoadDependency()
            Else
                'The DocData has already been loaded.  We can go ahead with the actual deserialization.

                '... BasicDesignerLoader requires that we call SetBaseComponentClassName() during load.
                SetBaseComponentClassName(GetBaseComponentClassName())

                '.. Allow the subclass to handle the deserialization.
                HandleLoad(SerializationManager)

                ' Fire the event when designer has been loaded...
                OnDesignerLoadCompleted()
            End If
        End Sub

        Private m_paneProviderService As DeferrableWindowPaneProviderServiceBase
        ''' <summary>
        ''' Provide a WindowPaneProviderService. Override if you want to provide a different provider pane
        ''' </summary>
        ''' <returns>A window pane provider service or NULL to indicate that no provider should be registered</returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetWindowPaneProviderService() As Microsoft.VisualStudio.Shell.Design.WindowPaneProviderService
            Try
                If m_paneProviderService Is Nothing Then
                    m_paneProviderService = New DeferrableWindowPaneProviderServiceBase(LoaderHost, Me.SupportToolbox)
                End If
            Catch Ex As ObjectDisposedException
                ' There is a slight possibility that the loader host is killed before we get a chance to try 
                ' to register ourselves.... No problemos, just ignore!
            End Try
            Return m_paneProviderService
        End Function

        Protected MustOverride Sub HandleFlush(ByVal SerializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)

        Protected MustOverride Sub HandleLoad(ByVal SerializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)

        'This must be overloaded to return the assembly-qualified name of the base component that is 
        '  being designed by this editor.  This information is required by the managed VSIP classes.
        Protected MustOverride Function GetBaseComponentClassName() As String

        Protected ReadOnly Property DocData() As DocData
            Get
                Debug.Assert(m_DocData IsNot Nothing)
                Return m_DocData
            End Get
        End Property

        Private m_VsTextBufferDataEventsCookie As Interop.NativeMethods.ConnectionPointCookie

        Protected Sub SetAllBufferText(ByVal Text As String)
            Dim Writer As New DocDataTextWriter(m_DocData)
            Try
                Writer.Write(Text)
            Finally
                Writer.Close()
            End Try
        End Sub

        Protected Function GetAllBufferText() As String
            Dim Reader As New DocDataTextReader(m_DocData)
            Try
                Return Reader.ReadToEnd()
            Finally
                Reader.Close()
            End Try
        End Function

        Protected WithEvents m_DocData As DocData

        'The "base" editor caption.  See SetBaseEditorCaption for more details.
        Private m_BaseEditorCaption As String = Nothing

        'The moniker of file that's loaded in the designer
        Private m_Moniker As String = Nothing

        Private m_Rdt As IVsRunningDocumentTable
        Private m_RdtEventsCookie As UInteger
        Private m_DocCookie As UInteger
        Private m_ProjectItemid As UInteger
        Private m_VsHierarchy As IVsHierarchy
        Private m_punkDocData As Object

        Friend ReadOnly Property VsHierarchy() As IVsHierarchy
            Get
                Return m_VsHierarchy
            End Get
        End Property

        Friend ReadOnly Property ProjectItemid() As System.UInt32
            Get
                Return m_ProjectItemid
            End Get
        End Property

        ''' <summary>
        ''' Project Item of the current Document
        ''' </summary>
        Friend ReadOnly Property ProjectItem() As EnvDTE.ProjectItem
            Get
                Return Common.DTEUtils.ProjectItemFromItemId(Me.VsHierarchy, Me.ProjectItemid)
            End Get
        End Property

#Region "IDisposeable design pattern"
        '/ <devdoc>
        '/     Disposes this designer loader.  The designer host will call this method
        '/     when the design document itself is being destroyed.  Once called, the
        '/     designer loader will never be called again.
        '/ </devdoc>
        Public Overloads Overrides Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)

            ' Clean up the hook to the windowevents...
            m_WindowEvents = Nothing

            ' OK, the BasicDesignerLoader doesn't follow the standard Dispose design pattern...
            ' We've gotta give it a chance to clean up as well!
            MyBase.Dispose()
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Dispose any resources owned by this instance
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overridable Overloads Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                Disconnect()
                If m_DocDataService IsNot Nothing Then
                    m_DocDataService.Dispose()
                    m_DocDataService = Nothing
                End If
                If m_DocData IsNot Nothing Then
                    m_DocData.Dispose()
                    m_DocData = Nothing 'This DocData's lifetime is determined by m_DocDataService
                End If

                UnadviseRunningDocTableEvents()
                m_Rdt = Nothing

                'Remove any services we proffered.
                '
                'Note: LoaderHost.RemoveService does not raise any exceptions if the service we're trying to
                '  remove isn't already there, so there's no need for a try/catch.
                LoaderHost.RemoveService(GetType(Microsoft.VisualStudio.Shell.Design.WindowPaneProviderService))
                LoaderHost.RemoveService(GetType(EnvDTE.ProjectItem))
                LoaderHost.RemoveService(GetType(IVsHierarchy))
            End If
            Debug.Assert(m_DocData Is Nothing, "Didn't dispose designer loader!?")
        End Sub

#End Region


        '/ <include file='doc\ShellTextBuffer.uex' path='docs/doc[@for="ShellTextBuffer.ReadOnly"]/*' />
        '/ <devdoc>
        '/      Determines if this file is read only.
        '/ </devdoc>

        Public ReadOnly Property DocDataIsReadOnly() As Boolean
            Get
                Return GetDocDataState(BUFFERSTATEFLAGS.BSF_FILESYS_READONLY Or BUFFERSTATEFLAGS.BSF_USER_READONLY)
            End Get
        End Property

        Private Function GetDocDataState(ByVal BitFlagToTest As TextManager.Interop.BUFFERSTATEFLAGS) As Boolean
            If m_DocData IsNot Nothing AndAlso m_DocData.Buffer IsNot Nothing Then
                Dim State As System.UInt32
                VSErrorHandler.ThrowOnFailure(m_DocData.Buffer.GetStateFlags(State))
                Return (State And BitFlagToTest) <> 0
            End If
            Return False
        End Function

        Public Enum EditorCaptionState
            AutoDetect
            [ReadOnly]
            NotReadOnly
        End Enum

        Public Function GetEditorCaption(ByVal Status As EditorCaptionState) As String
            Dim Caption As String = m_BaseEditorCaption
            If Caption Is Nothing Then
                Caption = ""
            End If

            If Status = EditorCaptionState.AutoDetect Then
                If m_DocData Is Nothing OrElse DocDataIsReadOnly Then
                    Status = EditorCaptionState.ReadOnly
                Else
                    Status = EditorCaptionState.NotReadOnly
                End If
            End If

            If Status = EditorCaptionState.ReadOnly Then
                'Append "[Read Only]" to the caption so far
                Caption = SR.GetString(SR.DFX_DesignerReadOnlyCaption, Caption)
            End If

            Return Caption
        End Function


        Private Sub Disconnect()
            If m_VsTextBufferDataEventsCookie IsNot Nothing Then
                m_VsTextBufferDataEventsCookie.Disconnect()
                m_VsTextBufferDataEventsCookie = Nothing
            End If
        End Sub

        ''' <summary>
        ''' Initialize the designer loader. This is called just after begin load, so we should
        ''' have a loader host here.
        ''' This is the place where we add services!
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            'In order to get free check-out behavior, we use the new VSIP class DesignerDocDataService.
            '  We pass it our punkDocData, and it wraps it with a new DocData class.  This DocData
            '  class is accessible as PrimaryDocData (in our case, it's the only doc data that the
            '  DesignerDocDataService instance will be handling).
            m_DocDataService = New DesignerDocDataService(LoaderHost, m_VsHierarchy, m_ProjectItemid, m_punkDocData)
            m_DocData = m_DocDataService.PrimaryDocData

            Dim WindowPaneProviderSvc As Microsoft.VisualStudio.Shell.Design.WindowPaneProviderService = GetWindowPaneProviderService()
            If WindowPaneProviderSvc IsNot Nothing Then
                LoaderHost.AddService(GetType(Microsoft.VisualStudio.Shell.Design.WindowPaneProviderService), WindowPaneProviderSvc)
            End If

            'Add the extender object as a service so we can query for it from the component/designer
            LoaderHost.AddService(GetType(EnvDTE.ProjectItem), ProjectItem)

            'Add the IVsHierarchy object as a service as well
            LoaderHost.AddService(GetType(IVsHierarchy), VsHierarchy)
        End Sub

        '/ <include file='doc\VSDDesignerLoader.uex' path='docs/doc[@for="VSDDesignerLoader.IVSMDDesignerLoader.Initialize"]/*' />
        '/ <devdoc>
        '/     This method is called to initialize the designer loader with the text
        '/     buffer to read from and a service provider through which we
        '/     can ask for services.
        '/ </devdoc>
        Friend Overridable Sub InitializeEx(ByVal ServiceProvider As Shell.ServiceProvider, ByVal moniker As String, ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal punkDocData As Object)

            If m_DocData IsNot Nothing Then
                Debug.Fail("BaseDesignerLoader.InitializeEx() should only be called once!")
                Return
            End If

            If moniker Is Nothing Then
                moniker = String.Empty
            End If

            m_Moniker = moniker
            m_VsHierarchy = Hierarchy
            m_ProjectItemid = ItemId
            m_punkDocData = punkDocData

            'Get the WindowEvents object
            m_WindowEvents = ProjectItem.DTE.Events.WindowEvents
            Debug.Assert(m_WindowEvents IsNot Nothing)

            ' If a random editor opens the file and locks it using an incompatible buffer, we need 
            ' to detect this.
            '
            If TypeOf punkDocData Is IVsTextBufferProvider Then
                Dim VsTextLines As TextManager.Interop.IVsTextLines = Nothing
                VSErrorHandler.ThrowOnFailure(CType(punkDocData, IVsTextBufferProvider).GetTextBuffer(VsTextLines))
                punkDocData = VsTextLines
            End If

            If TypeOf punkDocData Is TextManager.Interop.IVsTextStream Then
                Dim TextStream As IVsTextStream = DirectCast(punkDocData, IVsTextStream)
                If TextStream IsNot Nothing Then
                    m_VsTextBufferDataEventsCookie = New Interop.NativeMethods.ConnectionPointCookie(TextStream, Me, GetType(IVsTextBufferDataEvents))
                Else
                    Debug.Fail("Huh?")
                End If
            Else
                'Nope, this doc data is not in a format that we understand.  Throw an 
                '  intelligent error message (need to get the filename for the message)
                Dim FileName As String = String.Empty
                If TypeOf punkDocData Is TextManager.Interop.IVsUserData Then
                    Dim Guid As Guid = GetType(TextManager.Interop.IVsUserData).GUID
                    Dim vt As Object = Nothing
                    VSErrorHandler.ThrowOnFailure(CType(punkDocData, TextManager.Interop.IVsUserData).GetData(Guid, vt))
                    If TypeOf vt Is String Then
                        FileName = CStr(vt)
                        FileName = Path.GetFileName(FileName)
                    End If
                End If

                If FileName.Length > 0 Then
                    Throw New Exception(SR.GetString(SR.DFX_DesignerLoaderIVsTextStreamNotFound, FileName))
                Else
                    Throw New Exception(SR.GetString(SR.DFX_DesignerLoaderIVsTextStreamNotFoundNoFile))
                End If
            End If

            m_SourceCodeControlManager = New SourceCodeControlManager(ServiceProvider, Hierarchy)
            m_SourceCodeControlManager.ManagedFiles = FilesToCheckOut
        End Sub

        Public Overrides Sub BeginLoad(host As IDesignerLoaderHost)
            MyBase.BeginLoad(host)

            ' now that the base's BeginLoad has run, we can get services
            m_Rdt = TryCast(GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
            Debug.Assert(m_Rdt IsNot Nothing, "Couldn't get running document table")

            ' see if we can get the cookie from the RDT (it may not have been registered yet)
            m_DocCookie = GetDocCookie(m_Moniker)

            ' sign up for events from the RDT
            AdviseRunningDocTableEvents()
        End Sub

        'Sets the "base" editor caption for this designer.  Default is an empty string.
        'This will generally be either an empty string
        '  or else the string " [Design]", depending on the editor.
        '  The filename will automatically be placed into the caption, and this class
        '  will automatically handle adding "[Read Only]" to the caption as necessary.
        '  If this default behavior is good enough, then BaseEditorCaption can be
        '  left at its default setting.
        Protected Sub SetBaseEditorCaption(ByVal Caption As String)
            m_BaseEditorCaption = Caption
        End Sub

        Public Sub OnFileChanged(ByVal grfChange As UInteger, ByVal dwFileAttrs As UInteger) Implements TextManager.Interop.IVsTextBufferDataEvents.OnFileChanged
            Dim Frame As IVsWindowFrame = CType(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
            If Frame IsNot Nothing Then
                Dim Caption As String = GetEditorCaption(EditorCaptionState.AutoDetect)
                VSErrorHandler.ThrowOnFailure(Frame.SetProperty(__VSFPROPID.VSFPROPID_EditorCaption, Caption))
            End If
        End Sub

        ' Notifies client when the buffer is initialized with data either by a call to the methods:
        ' IVsPersistDocData::LoadDocData, IPersistStream::Load, IVsTextBuffer::InitializeContent, IPersistFile::Load, or
        ' IPersistFile::InitNew. This event is also fired if the text buffer executes a reload of its file in response to
        ' an IVsTextBufferDataEvents::OnFileChanged event, as when a file is edited outside of the environment.
        Public Function OnLoadCompleted(ByVal fReload As Integer) As Integer Implements TextManager.Interop.IVsTextBufferDataEvents.OnLoadCompleted
            If m_LoadDeferred Then
                Debug.Assert(m_DeferredLoaderService IsNot Nothing)

                m_LoadDeferred = False

                ' And now request the load.  We don't care about reloads here
                ' because we're not loaded yet.
                ' 
                Dim Errors As Collections.ICollection = Nothing
                Dim Successful As Boolean = True

                Try
                    PerformLoad(m_DeferredLoadManager)
                Catch ex As Exception
                    Errors = New Object() {ex}
                    Successful = False
                End Try

                m_DeferredLoaderService.DependentLoadComplete(Successful, Errors)
                If Not m_LoadDeferred Then
                    m_DeferredLoaderService = Nothing
                    m_DeferredLoadManager = Nothing
                End If
            End If
        End Function

        ''' <summary>
        ''' OnDesignerLoadCompleted will be called when we finish loading the designer
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub OnDesignerLoadCompleted()
        End Sub

        '**************************************************************************
        ';m_DocData_DataChanged
        '
        'Summary:
        '   Event handler for m_DocData.DataChanged.
        'Params:
        '   sender, e - standard event handler params.
        'Remarks:
        '   * Following Windows Form designer model. 
        '     See ndp\fx\src\vsip\packages\design\serialization\codedom\vscomdomdesignerloader.cs.
        '   * This is called whenever there is an external change to the DocData and we must reload.
        '     Reloading is deferred until the document is later activated, and reload is contingent 
        '     upon IsReloadNeeded returning true.
        '**************************************************************************
        Private Sub DocData_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_DocData.DataChanged
            'The DocData has been changed externally (either outside of VS or from another editor).
            '  We notify ourselves that we need to reload.  The reload doesn't actually happen until we
            '  have focus again and then hit idle time processing.
            'On the reload, the current root component and designer will be torn down and new ones created.
            '  However, the designer host is not torn down, so any services that you proffer to it will
            '  still be available after the reload.

            ' NoFlush: Causes the designer loader to anbandon any changes before reloading.
            Me.Reload(ReloadOptions.NoFlush)
        End Sub


        ''' <summary>
        ''' Indicates whether the window frame for this designer loader's designer should support the shell toolbox.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' For performance reasons, this defaults to false.  If the designer should support the toolbox, override
        '''   this and return True.
        ''' </remarks>
        Protected Overridable ReadOnly Property SupportToolbox() As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Run the custom tool (if any)
        ''' </summary>
        ''' <param name="flushBeforeRun">If true, flush before running the custom tool</param>
        ''' <remarks></remarks>
        Friend Overridable Sub RunSingleFileGenerator(ByVal flushBeforeRun As Boolean)
            If flushBeforeRun Then
                HandleFlush(Nothing)
            End If
            Try
                Dim projItem As EnvDTE.ProjectItem = ProjectItem
                If projItem IsNot Nothing Then
                    Dim vsProj As VSLangProj.VSProjectItem = TryCast(projItem.Object, VSLangProj.VSProjectItem)
                    If vsProj IsNot Nothing Then
                        vsProj.RunCustomTool()
                    End If
                End If
            Catch ex As Exception When Not Utils.IsUnrecoverable(ex)
                Debug.Fail(String.Format("Failed to run custom tool: {0}", ex))
            End Try
        End Sub

#Region "WindowEvents"

        ''' <summary>
        ''' This is called when a window is activated or deactivated.
        ''' </summary>
        ''' <param name="GotFocus">The Window Object that received the focus.</param>
        ''' <param name="LostFocus">The Window object that previously had the focus.</param>
        ''' <remarks>
        ''' We use this to know when the user has moved off of the designer.  When this happens,
        '''   we know it's time to commit pending changes on that designer.
        ''' </remarks>
        Private Sub m_WindowEvents_WindowActivated(ByVal GotFocus As EnvDTE.Window, ByVal LostFocus As EnvDTE.Window) Handles m_WindowEvents.WindowActivated
            Dim ThisProjectItem As EnvDTE.ProjectItem

            Try
                ThisProjectItem = ProjectItem
            Catch ex As ArgumentException
                ' When the designer is closed and the file is out of the solution, the ProjectItem will be gone when we receive this message.
                Exit Sub
            End Try

            If ThisProjectItem Is Nothing Then
                Exit Sub
            End If

            Switches.TracePDFocus(TraceLevel.Warning, "BaseDesignerLoader.WindowActivated")

            If LostFocus IsNot Nothing Then
                'This seems to throw sometimes, let's ignore if that happens
                Dim LostFocusProjectItem As EnvDTE.ProjectItem = Nothing
                Try
                    LostFocusProjectItem = LostFocus.ProjectItem
                Catch ex As Exception
                    Common.RethrowIfUnrecoverable(ex)
                End Try

                If LostFocusProjectItem Is ThisProjectItem Then
                    OnDesignerWindowActivated(False)
                End If
            End If
            If GotFocus IsNot Nothing Then
                'This seems to throw sometimes, let's ignore if that happens
                Dim GotFocusProjectItem As EnvDTE.ProjectItem = Nothing
                Try
                    GotFocusProjectItem = GotFocus.ProjectItem
                Catch ex As Exception
                    Common.RethrowIfUnrecoverable(ex)
                End Try

                If GotFocusProjectItem Is ThisProjectItem Then
                    OnDesignerWindowActivated(True)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Called when the document's window is activated or deactivated
        ''' </summary>
        ''' <param name="Activated">True if the document window has been activated, False if deactivated.</param>
        ''' <remarks></remarks>
        Protected Overridable Sub OnDesignerWindowActivated(ByVal Activated As Boolean)
        End Sub

#End Region

        ''' <summary>
        ''' Start listening to RDT events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub AdviseRunningDocTableEvents()
            If m_Rdt IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(m_Rdt.AdviseRunningDocTableEvents(Me, m_RdtEventsCookie))
            End If
        End Sub

        ''' <summary>
        ''' Stop listening to RDT events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnadviseRunningDocTableEvents()
            If m_RdtEventsCookie <> 0 Then
                If m_Rdt IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(m_Rdt.UnadviseRunningDocTableEvents(m_RdtEventsCookie))
                End If
                m_RdtEventsCookie = 0
            End If
        End Sub

        ''' <summary>
        ''' Returns a document's RDT cookie
        ''' </summary>
        ''' <param name="filename">The name of the document</param>
        ''' <returns>The document's cookie, or 0 if it's not in the RDT</returns>
        Private Function GetDocCookie(ByVal filename As String) As UInteger
            If filename Is Nothing Then
                Throw New ArgumentNullException("filename")
            End If

            Dim docCookie As UInteger = 0
            Dim rdt4 As IVsRunningDocumentTable4 = TryCast(m_Rdt, IVsRunningDocumentTable4)

            If rdt4 IsNot Nothing Then
                If rdt4.IsMonikerValid(filename) Then
                    docCookie = rdt4.GetDocumentCookie(filename)
                End If
            Else
                Debug.Fail("Couldn't get running document table")
            End If

            Return docCookie
        End Function

        ''' <summary>
        ''' Returns a document's RDT moniker
        ''' </summary>
        ''' <param name="docCookie">The document's cookie</param>
        ''' <returns>The document's moniker</returns>
        Private Function GetDocumentMoniker(ByVal docCookie As UInteger) As String
            Dim rdt4 As IVsRunningDocumentTable4 = TryCast(m_Rdt, IVsRunningDocumentTable4)
            Dim moniker As String = String.Empty

            If rdt4 IsNot Nothing Then
                If rdt4.IsCookieValid(docCookie) Then
                    moniker = rdt4.GetDocumentMoniker(docCookie)
                End If
            Else
                Debug.Fail("Couldn't get running document table")
            End If

            Return moniker
        End Function

        ''' <summary>
        ''' Determines if two paths are equivalent (i.e. differ only by case)
        ''' </summary>
        Private Function PathEquals(ByVal path1 As String, ByVal path2 As String) As Boolean
            Return String.Equals(path1, path2, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Updates the LoaderHost with the new instance for the service type
        ''' </summary>
        ''' <param name="serviceType">The type of the service to update</param>
        ''' <param name="serviceInstance">The new service instance</param>
        Private Sub UpdateService(ByVal serviceType As Type, ByVal serviceInstance As Object)
            ' remove the old instance
            LoaderHost.RemoveService(serviceType)

            ' add the new instance
            LoaderHost.AddService(serviceType, serviceInstance)
        End Sub

#Region "IVsRunningDocTableEvents2 Implementation"

        Private Function OnAfterAttributeChangeEx(docCookie As UInteger, attributes As UInteger, hierOld As IVsHierarchy, itemidOld As UInteger, pszMkDocumentOld As String, hierNew As IVsHierarchy, itemidNew As UInteger, pszMkDocumentNew As String) As Integer _
            Implements IVsRunningDocTableEvents2.OnAfterAttributeChangeEx

            ' if we don't have our cookie yet and a document is being initialized, see if it's ours
            If (m_DocCookie = 0) AndAlso ((attributes And __VSRDTATTRIB3.RDTA_DocumentInitialized) <> 0) AndAlso (m_Moniker.Length > 0) Then
                Dim moniker As String = GetDocumentMoniker(docCookie)

                If PathEquals(m_Moniker, moniker) Then
                    m_DocCookie = docCookie
                End If
            End If

            ' if this change was for our document, see if anything interesting changed
            If m_DocCookie = docCookie Then
                Dim updateProjectItemService As Boolean = False

                ' if the hierarchy was updated, remember the new value
                If ((attributes And __VSRDTATTRIB.RDTA_Hierarchy) <> 0) AndAlso (m_VsHierarchy Is hierOld) Then
                    m_VsHierarchy = hierNew

                    ' update the IVsHierarchy service with the new value
                    UpdateService(GetType(IVsHierarchy), m_VsHierarchy)

                    ' the hierarchy is a component of the ProjectItem, so we need to update the ProejctItem service as well
                    updateProjectItemService = True
                End If

                ' if the itemid was updated, remember the new value
                If ((attributes And __VSRDTATTRIB.RDTA_ItemID) <> 0) AndAlso (m_ProjectItemid = itemidOld) Then
                    m_ProjectItemid = itemidNew

                    ' the project item ID is a component of the ProjectItem, so we need to update the ProejctItem service
                    updateProjectItemService = True
                End If

                ' if the filename was updated, remember the new value
                If ((attributes And __VSRDTATTRIB.RDTA_MkDocument) <> 0) AndAlso PathEquals(m_Moniker, pszMkDocumentOld) Then
                    m_Moniker = pszMkDocumentNew
                End If

                ' update the ProjectItem service if we need to
                If updateProjectItemService = True Then
                    UpdateService(GetType(EnvDTE.ProjectItem), ProjectItem)
                End If
            End If
        End Function

#Region "RDT events we don't care about"

        Private Function OnAfterAttributeChange(ByVal docCookie As UInteger, ByVal attributes As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterAttributeChange, IVsRunningDocTableEvents2.OnAfterAttributeChange
        End Function

        Private Function OnAfterFirstDocumentLock(ByVal docCookie As UInteger, ByVal lockType As UInteger, ByVal readLocksRemaining As UInteger, ByVal editLocksRemaining As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterFirstDocumentLock, IVsRunningDocTableEvents2.OnAfterFirstDocumentLock
        End Function

        Private Function OnBeforeLastDocumentUnlock(ByVal docCookie As UInteger, ByVal lockType As UInteger, ByVal readLocksRemaining As UInteger, ByVal editLocksRemaining As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock, IVsRunningDocTableEvents2.OnBeforeLastDocumentUnlock
        End Function

        Private Function OnAfterDocumentWindowHide(ByVal docCookie As UInteger, ByVal frame As IVsWindowFrame) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterDocumentWindowHide, IVsRunningDocTableEvents2.OnAfterDocumentWindowHide
        End Function

        Private Function OnAfterSave(ByVal docCookie As UInteger) As Integer _
            Implements IVsRunningDocTableEvents.OnAfterSave, IVsRunningDocTableEvents2.OnAfterSave
        End Function

        Private Function OnBeforeDocumentWindowShow(ByVal docCookie As UInteger, ByVal firstShow As Integer, ByVal frame As IVsWindowFrame) As Integer _
            Implements IVsRunningDocTableEvents.OnBeforeDocumentWindowShow, IVsRunningDocTableEvents2.OnBeforeDocumentWindowShow
        End Function

#End Region
#End Region

    End Class
End Namespace

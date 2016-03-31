Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization.Formatters
Imports System.Windows.Forms
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports VB = Microsoft.VisualBasic
Imports VsTextBufferClass = Microsoft.VisualStudio.TextManager.Interop.VsTextBufferClass



Namespace Microsoft.VisualStudio.Editors.DesignerFramework


    ''' <summary>
    ''' This is the base editor factory for all designers in this assembly.  The
    '''     editor factory provides a way for the shell to get an editor for a
    '''     particular file type.  To create a new designer type, you must also inherit from this
    '''     editor factory base.  Be sure to give each new editor factory class a different guid.
    ''' </summary>
    ''' <remarks>
    '''     This file was copied from the frameworks: FX\src\VSDesigner\Designer\Microsoft\VisualStudio\Designer\Shell
    '''     We need our own version because the original version does not support logical views, and also because
    '''     the one from Windows Forms does not support custom DocData types.
    '''
    '''     This is the base editor factory for all designers in this assembly.  The
    '''     editor factory provides a way for the shell to get an editor for a
    '''     particular file type.  To create a new designer type, you must also inherit from this
    '''     editor factory base.  Be sure to give each new editor factory class a different guid.
    '''
    '''     Provides basic editor factory functionality for a specific designer.  Must be inherited for each 
    '''     designer type supported.  By default, only a single entry in the Open With… dialog box is shown by 
    '''     the shell for each editor factory—-therefore, in general there should be one editor factory per entry 
    '''     in the Open With… dialog.  There is one exception to this—a single designer can be made via the registry 
    '''     to have multiple entries in the Open With dialog for multiple views or encodings (e.g., “Text Editor”, 
    '''     “Text Editor with Encoding”).
    '''
    '''     ADDING A NEW DESIGNER TO THIS ASSEMBLY (this may not be all the steps required, but it'''s a start):
    '''
    '''     1) Add an editor factory—-inherit from BaseEditorFactory
    '''           a. Add a GUID attribute to the class
    '''           b. Override EditorGuide to return this GUID
    '''           c. Add registration for the file extension in the .vbgpp file (and also VBPackage, but that information
    '''                 is currently unused but might be used in the future by RegPkg), and have it point to this GUID
    '''           d. In Sub New, call MyBase.New() with the type of your designer loader class (see next step)
    '''     2) Add a designer loader—-inherit from BaseDesignerLoader
    '''           a. Override HandleLoad() to implement depersisting your data
    '''           b. Override HandleFlush() to implement persisting your data
    '''           c. Override GetBaseComponentClassName
    '''     3) Create a root component (inherits from Component, has Designer attribute), a root designer
    '''          (inherits from ComponentDesigner and implements IRootDesigner) and a view (probably inherits from
    '''           UserControl or ControlContainer)
    ''' </remarks>
    <CLSCompliant(False)> _
        Friend MustInherit Class BaseEditorFactory
        Implements IVsEditorFactory, IDisposable


#Region "Fields and Structures"

        Private m_Site As Object 'The site that owns this editor factory
        Private m_ServiceProvider As Shell.ServiceProvider 'The service provider from m_Site
        Private m_DesignerLoaderType As Type 'The type of designer loader to create.  Typically there is a separate designer loader class per editor factory (and therefore per designer type)
        Private Shared ReadOnly DefaultPhysicalViewName As String = Nothing 'The default physical view *must* be Nothing

#End Region


        ''' <summary>
        '''     Creates a new editor factory.
        ''' </summary>
        Public Sub New(ByVal DesignerLoaderType As Type)
            Debug.Assert(Not DesignerLoaderType Is Nothing)
            m_DesignerLoaderType = DesignerLoaderType
        End Sub


        ''' <summary>
        '''     Called by the VS shell before this editor is removed from the list of available
        '''     editor factories.
        ''' </summary>
        Public Overridable Function Close() As Integer Implements IVsEditorFactory.Close
            Dispose()
        End Function



        ''' <summary>
        ''' Creates a new native TextBuffer by CoCreating it from COM.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function CreateNewTextStreamBuffer() As IVsTextStream
            Dim LocalRegistry As ILocalRegistry = CType(m_ServiceProvider.GetService(GetType(ILocalRegistry)), ILocalRegistry)
            Dim TextStreamInstance As IVsTextStream = Nothing

            If LocalRegistry Is Nothing Then
                Debug.Fail("Shell did not offer local registry, so we can't create a text buffer.")
                Throw New COMException(SR.GetString(SR.DFX_NoLocalRegistry), Interop.NativeMethods.E_FAIL)
            End If

            Debug.Assert(Not GetType(VsTextBufferClass).GUID.Equals(System.Guid.Empty), "EE has munched on text buffer guid.")

            Try
                Dim GuidTemp As System.Guid = GetType(IVsTextStream).GUID
                Dim ObjPtr As IntPtr = IntPtr.Zero
                VSErrorHandler.ThrowOnFailure(LocalRegistry.CreateInstance(GetType(VsTextBufferClass).GUID, Nothing, GuidTemp, Interop.win.CLSCTX_INPROC_SERVER, ObjPtr))

                If Not ObjPtr.Equals(IntPtr.Zero) Then
                    TextStreamInstance = CType(Marshal.GetObjectForIUnknown(ObjPtr), IVsTextStream)

                    'In this case, we co-created the object ourselves via managed code, so we need 
                    '  to release it (as opposed to the comments above about not needing to release 
                    '  after using GetObjectForIUnknown)
                    Marshal.Release(ObjPtr)
                    ObjPtr = IntPtr.Zero
                End If
            Catch ex As Exception
                Debug.Fail("Failed to create VSTextBuffer Class. You need to check the guid is right?" + ex.ToString())
                Throw New COMException(SR.GetString(SR.DFX_UnableCreateTextBuffer), Interop.NativeMethods.E_FAIL)
            End Try

            Return TextStreamInstance
        End Function


        ''' <summary>
        ''' Given an existing docdata (if any), gets the correct DocData for use with a new
        '''   editor.  Default implementation: If there is an existing DocData and it's 
        '''   compatible, it uses that.  Otherwise it creates a new native TextBuffer.
        ''' </summary>
        ''' <param name="ExistingDocData">The existing DocData pointer, if any</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetOrCreateDocDataForNewEditor(ByVal ExistingDocData As Object) As Object
            Dim TextStreamInstance As IVsTextStream

            If ExistingDocData Is Nothing Then
                'No DocData was passed in.  That means that the file is currently opened in Visual Studio.
                '  In this case, we're supposed to create our own DocData from scratch.
                TextStreamInstance = CreateNewTextStreamBuffer()
            Else
                'A currently-existing DocData was passed in.  That means the file is already open, and 
                '  we're simply creating a new view on the existing DocData.  Verify that the current
                '  DocData supports IVsTextStream, or we will not be compatible.

                If TypeOf ExistingDocData Is IVsTextStream Then
                    TextStreamInstance = CType(ExistingDocData, IVsTextStream)
                Else
                    'Existing data is not a IVSTextStream.  Throw VS_E_INCOMPATIBLEDOCDATA to have the shell
                    '  ask if it should close the existing editor.
                    Throw New COMException(SR.GetString(SR.DFX_IncompatibleBuffer), Interop.NativeMethods.VS_E_INCOMPATIBLEDOCDATA)
                End If
            End If

            Return TextStreamInstance
        End Function


        ''' <summary>
        ''' Called in base classes when the editor factory is sited.  Before this time, ServiceProvider will not be available (and
        '''   should not be assumed afterwards).
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub OnSited()
        End Sub


        ''' <summary>
        ''' Provides the (constant) GUID for the subclassed editor factory.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Must be overridden.  Be sure to use the same GUID on the GUID attribute
        '''   attached to the inheriting class.
        ''' </remarks>
        Protected MustOverride ReadOnly Property EditorGuid() As System.Guid

        ''' <summary>
        ''' Provides the (constant) GUID for the command UI.  This is the guid used in the
        '''   CTC file for keybindings, toolbars, etc.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected MustOverride ReadOnly Property CommandUIGuid() As System.Guid


#Region "Private implementation"


        ''' <summary>
        ''' Used by the editor factory architecture to create editors that support data/view separation (for example, 
        '''    an editor could support the Window.NewWindow functionality).
        ''' </summary>
        ''' <param name="vscreateeditorflags">
        '''     Flags whose values are taken from the VSCREATEEDITORFLAGS enumeration 
        '''     which defines the conditions under which to create the editor. Only open or silent are valid.</param>
        ''' <param name="fileName">Moniker filename of the document </param>
        ''' <param name="physicalView">Name of the physical view. </param>
        ''' <param name="hierarchy">Pointer to the IVsHierarchy interface.</param>
        ''' <param name="itemid">Item identifier of this editor instance.</param>
        ''' <param name="ExistingDocDataPtr">
        '''   Must be the punkDocData object that is registered in the Running Document Table (RDT). This parameter is used
        '''     to determine if a document buffer has already been created. When an editor factory is asked to create a 
        '''     secondary view, then this parameter will be non-Nothing, indicating that there is no currently-opened 
        '''     document buffer. If the file is open, you can return VS_E_INCOMPATIBLEDOCDATA to get the environment to
        '''     ask the user if they want to close it. 
        ''' </param>
        ''' <param name="DocViewPtr">
        '''   Pointer to the IUnknown interface. Returns NULL if external editor exists, otherwise returns view of the document.
        ''' </param>
        ''' <param name="DocDataPtr">
        '''   Pointer to the IUnknown interface. Returns buffer for the document. 
        ''' </param>
        ''' <param name="caption">
        '''   Initial caption defined by the document editor for the document window. This is typically a string enclosed 
        '''   in square brackets, such as "[Form]". This value is passed as an input parameter to the
        '''   IVsUIShell::CreateDocumentWindow method. If the file is [ReadOnly] the caption will be set during load of the file. 
        '''   NOTE: This value will be appended to the name of the opened document in order to create the full caption used.
        '''     E.g., if the value " [Design]" is returned, and the file being edited is "Form1.vb", then the actual caption
        '''     will be "Form1.vb [Design]"
        ''' </param>
        ''' <param name="cmdUIGuid">
        '''   Returns the package GUID of the menus. It is also used in the .ctc file in the satellite DLL. Indicates which
        '''   menus and toolbars should be displayed when this document is active. 
        ''' </param>
        ''' <param name="fCanceled">
        '''   Return TRUE if the user pressed Cancel in the UI while creating the editor instance. 
        ''' </param>
        ''' 
        ''' If the method succeeds, it returns S_OK. If it fails, it should return an error code:
        ''' 
        '''   VS_E_UNSUPPORTEDFORMAT if the document has a format that cannot be opened in the editor.
        '''   VS_E_INCOMPATIBLEDOCDATA or E_NOINTERFACE if the document is open in an incompatible editor.
        '''   
        ''' CreateEditorInstance can be called on various editor factories in a loop attempting to find 
        '''   an editor that will successfully open the file.  VS_E_UNSUPPORTEDFORMAT will allow the loop 
        '''   to continue without closing the document if it is currently open. VS_E_INCOMPATIBLEDOCDATA 
        '''   will ask if the open document should be closed. Any other return will stop the loop from continuing.                                                                                                                                                                                                                                                                                                                                                                         If the constructed object referenced by ppunkDocData supports IOleCommandTarget, the object is included in the command routing chain of the Environment after the command is routed to the active object referenced by ppunkDocView.
        '''   
        Private Function IVsEditorFactory_CreateEditorInstance( _
                ByVal vscreateeditorflags As UInteger, _
                ByVal FileName As String, _
                ByVal PhysicalView As String, _
                ByVal Hierarchy As IVsHierarchy, _
                ByVal Itemid As UInteger, _
                ByVal ExistingDocDataPtr As IntPtr, _
                ByRef DocViewPtr As IntPtr, _
                ByRef DocDataPtr As IntPtr, _
                ByRef Caption As String, _
                ByRef CmdUIGuid As System.Guid, _
                ByRef FCanceled As Integer) As Integer _
        Implements IVsEditorFactory.CreateEditorInstance

            Try
                Dim ExistingDocData As Object = Nothing
                DocViewPtr = IntPtr.Zero
                DocDataPtr = IntPtr.Zero

                If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                    ExistingDocData = Marshal.GetObjectForIUnknown(ExistingDocDataPtr)
                    'Note: do *not* call Marshal.Release on ExistingDocData - the runtime will manage that automatically
                End If

                Dim DocView As Object = Nothing
                Dim DocData As Object = Nothing

                Dim CanceledAsBoolean As Boolean = False
                CreateEditorInstance(vscreateeditorflags, FileName, PhysicalView, Hierarchy, Itemid, ExistingDocData, _
                        DocView, DocData, Caption, CmdUIGuid, CanceledAsBoolean)

                If CanceledAsBoolean Then
                    FCanceled = 1
                Else
                    FCanceled = 0
                End If

                If Not (DocView Is Nothing) Then
                    DocViewPtr = Marshal.GetIUnknownForObject(DocView)
                End If
                If Not (DocData Is Nothing) Then
                    DocDataPtr = Marshal.GetIUnknownForObject(DocData)
                End If
                Return VSConstants.S_OK
            Catch ex As COMException
                Return ex.ErrorCode
            End Try
        End Function



        ''' <summary>
        ''' Creates a new editor for the given pile of flags.  Helper function for the overload
        '''   which implements IVsEditorFactory.CreateEditorInstance
        ''' </summary>
        ''' <param name="VsCreateEditorFlags"></param>
        ''' <param name="FileName"></param>
        ''' <param name="PhysicalView"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="ExistingDocData"></param>
        ''' <param name="DocView"></param>
        ''' <param name="DocData"></param>
        ''' <param name="Caption"></param>
        ''' <param name="CmdUIGuid"></param>
        ''' <param name="Canceled"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub CreateEditorInstance(ByVal VsCreateEditorFlags As System.UInt32, _
                ByVal FileName As String, _
                ByVal PhysicalView As String, _
                ByVal Hierarchy As IVsHierarchy, _
                ByVal ItemId As System.UInt32, _
                ByVal ExistingDocData As Object, _
                ByRef DocView As Object, _
                ByRef DocData As Object, _
                ByRef Caption As String, _
                ByRef CmdUIGuid As System.Guid, _
                ByRef Canceled As Boolean)
            Canceled = False
            CmdUIGuid = System.Guid.Empty

            Dim DesignerLoader As BaseDesignerLoader = Nothing

            Try
                Using New WaitCursor
                    Dim NewDocData As Object = Nothing

                    ' perform parameter validation and initialization.
                    '
                    If (VsCreateEditorFlags And CType(__VSCREATEEDITORFLAGS.CEF_OPENFILE Or __VSCREATEEDITORFLAGS.CEF_SILENT, System.UInt32)) = 0 Then
                        Throw Common.CreateArgumentException("vscreateeditorflags")
                    End If

                    DocView = Nothing
                    DocData = Nothing
                    Caption = Nothing

                    Dim DesignerService As IVSMDDesignerService = CType(m_ServiceProvider.GetService(GetType(IVSMDDesignerService)), IVSMDDesignerService)
                    If DesignerService Is Nothing Then
                        Throw New Exception(SR.GetString(SR.DFX_EditorNoDesignerService, FileName))
                    End If

                    ' Create our doc data if we don't have an existing one.
                    ' This call is protected so inherited classes can customize behavior.
                    NewDocData = GetOrCreateDocDataForNewEditor(ExistingDocData)

                    'Site the TextStream
                    If ExistingDocData Is Nothing Then
                        If TypeOf NewDocData Is IObjectWithSite Then
                            CType(NewDocData, IObjectWithSite).SetSite(m_Site)
                        End If
                    End If

                    ' Create and initialize our code stream.

                    'Create the appropriate designer loader
                    '  Note: there is no formal need to go through DesignerService.CreateDesignerLoader for this, but there's
                    '  nothing wrong with it, either.
                    Dim DesignerLoaderClassName As String = m_DesignerLoaderType.AssemblyQualifiedName
                    Dim DesignerLoaderObject As Object = DesignerService.CreateDesignerLoader(DesignerLoaderClassName)
                    If DesignerLoaderObject Is Nothing Then
                        Debug.Fail("DesignerService.CreateDesignerLoader() returned Nothing")
                    End If
                    If Not TypeOf (DesignerLoaderObject) Is BaseDesignerLoader Then
                        Debug.Fail("DesignerLoader was of an unexpected type.  This likely means that Microsoft.VisualStudio.Editors.dll was " _
                            & "loaded twice from two different locations (or from the same location but one with 8.3 and the other long paths).  " _
                            & VB.vbCrLf & DesignerLoaderObject.GetType.AssemblyQualifiedName)
                    End If
                    DesignerLoader = CType(DesignerLoaderObject, BaseDesignerLoader)

                    'Initialize the sucker
                    Debug.Assert(m_ServiceProvider IsNot Nothing)
                    DesignerLoader.InitializeEx(m_ServiceProvider, FileName, Hierarchy, ItemId, NewDocData)

                    'Now slam the two together and make a designer

                    '... Get a managed Designer (this will expose an IVsWindowPane to the shell)
                    Dim OleProvider As OLE.Interop.IServiceProvider = CType(m_ServiceProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Dim Designer As IVSMDDesigner = DesignerService.CreateDesigner(OleProvider, DesignerLoader)
                    Debug.Assert(Not (Designer Is Nothing), "Designer service should have thrown if it had a problem.")

                    'Set the out params
                    DocView = Designer.View 'Gets the object that can support IVsWindowPane
                    DocData = NewDocData

                    'Let the designer loader determine the initial caption (based on whether the file is
                    '  read only or not)
                    Dim CaptionReadOnlyStatus As BaseDesignerLoader.EditorCaptionState = BaseDesignerLoader.EditorCaptionState.NotReadOnly
                    Try
                        If ((New IO.FileInfo(FileName)).Attributes And FileAttributes.ReadOnly) <> 0 Then
                            CaptionReadOnlyStatus = BaseDesignerLoader.EditorCaptionState.ReadOnly
                        End If
                    Catch ex As Exception
                        Debug.Fail("Failed to get file read-only status")
                    End Try
                    Caption = DesignerLoader.GetEditorCaption(CaptionReadOnlyStatus)

                    'Set the command UI
                    CmdUIGuid = CommandUIGuid
                End Using
            Catch ex As COMException
                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If
                Throw
            Catch ex As Exception
                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If

                Throw New COMException(SR.GetString(SR.DFX_CreateEditorInstanceFailed_Ex, ex))
            End Try
        End Sub

        ''' <summary>
        ''' This method is called by the Environment (inside IVsUIShellOpenDocument::
        ''' OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a 
        ''' PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
        ''' desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a 
        ''' view appropriate for text view manipulation as by navigating to a find
        ''' result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type 
        ''' of view implementation that an IVsEditorFactory can create. 
        '''     
        ''' NOTE: Physical views are identified by a string of your choice with the 
        ''' one constraint that the default/primary physical view for an editor  
        ''' *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
        '''     
        ''' NOTE: It is essential that the implementation of MapLogicalView properly
        ''' validates that the LogicalView desired is actually supported by the editor.
        ''' If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
        '''     
        ''' NOTE: The special Logical Views supported by an Editor Factory must also 
        ''' be registered in the local registry hive. LOGVIEWID_Primary is implicitly 
        ''' supported by all editor types and does not need to be registered.
        ''' For example, an editor that supports a ViewCode/ViewDesigner scenario
        ''' might register something like the following:
        ''' HKLM\Software\Microsoft\VisualStudio\[CurrentVSVersion]\Editors\
        ''' {...guidEditor...}\
        ''' LogicalViews\
        ''' {...LOGVIEWID_TextView...} = s ''
        ''' {...LOGVIEWID_Code...} = s ''
        ''' {...LOGVIEWID_Debugging...} = s ''
        ''' {...LOGVIEWID_Designer...} = s 'Form'
        ''' </summary>
        Private Function IVsEditorFactory_MapLogicalView(ByRef LogicalView As System.Guid, ByRef PhysicalViewOut As String) As Integer Implements IVsEditorFactory.MapLogicalView
            Return MapLogicalView(LogicalView, PhysicalViewOut)
        End Function

        Protected MustOverride Function MapLogicalView(ByRef LogicalView As System.Guid, ByRef PhysicalViewOut As String) As Integer

        ''' <summary>
        ''' Returns the ServiceProvider
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Will not be available before OnSited is called.
        ''' </remarks>
        Protected ReadOnly Property ServiceProvider() As Shell.ServiceProvider
            Get
                Return m_ServiceProvider
            End Get
        End Property


        ''' <summary>
        '''     Called by the VS shell when it first initializes us.
        ''' </summary>
        Private Function IVsEditorFactory_SetSite(ByVal site As OLE.Interop.IServiceProvider) As Integer Implements IVsEditorFactory.SetSite
            SetSiteInternal(site)
        End Function


        ''' <summary>
        ''' Called by the VS shell when it first initializes us.  
        ''' </summary>
        ''' <param name="Site">The Site that will own this editor factory</param>
        ''' <remarks></remarks>
        Private Sub SetSiteInternal(ByVal Site As Object)
            'This same Site already set?  Or Site not yet initialized (= Nothing)?  If so, NOP.
            If Me.m_Site Is Site Then
                Debug.Fail("Why is this EditorFactory site:ed twice?")
                Exit Sub
            End If

            'Site is different - set it
            If m_ServiceProvider IsNot Nothing Then
                ' Let's make sure we dispose any old service provider we had...
                m_ServiceProvider.Dispose()
                m_ServiceProvider = Nothing
            End If
            Me.m_Site = Site
            If TypeOf Site Is OLE.Interop.IServiceProvider Then
                m_ServiceProvider = New Shell.ServiceProvider(CType(Site, OLE.Interop.IServiceProvider))
            End If

            Me.OnSited()
        End Sub


#Region "IDisposable standard pattern"
        ''' <summary>
        ''' Dispose my resources
        ''' </summary>
        ''' <remarks>Standard implementation pattern for IDisposable</remarks>
        Public Overloads Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Dispose my resources
        ''' </summary>
        ''' <remarks>Standard implementation pattern for IDisposable</remarks>
        Protected Overridable Overloads Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                If m_ServiceProvider IsNot Nothing Then
                    m_ServiceProvider.Dispose()
                    m_ServiceProvider = Nothing
                End If
                m_Site = Nothing
            End If
        End Sub

#End Region

#End Region

    End Class

End Namespace

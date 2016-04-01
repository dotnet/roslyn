' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell.Interop
Imports VB = Microsoft.VisualBasic
Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' The panel which parents the Designer window contained by the App Designer.
    '''   There is one of these per designer hosted.  They are added directly into the HostingPanel control
    '''   of ApplicationDesignerView as needed.
    ''' 
    ''' </summary>
    ''' <remarks>
    '''  This does not directly host Property pages, rather it hosts the designer (which would be a PropPageDesignerView for
    '''    property pages or the designer view for other designers).
    ''' </remarks>
    Public Class ApplicationDesignerPanel
        Inherits System.Windows.Forms.TableLayoutPanel
        Implements IVsWindowFrameNotify, IVsWindowFrameNotify2

        'Information used to create the designer
        Private _editorGuid As Guid
        Private _editorCaption As String
        Private _physicalView As String
        Private _editFlags As UInteger
        Private _pageHostingPanelLastSize As Drawing.Size
        Private _windowFrameLastSize As Drawing.Size
        Private _windowFrameShown As Boolean 'True iff ShowWindowFrame() has been called
        Public m_Debug_cWindowFrameShow As Integer = 0 '# of times the window frame has been shown
        Public m_Debug_cWindowFrameBoundsUpdated As Integer = 0 '# of times the window frame bounds have been changed

        Private _windowFrameNotifyCookie As UInteger

        ' Avoid recursive calls to close (we sometimes try to close our parent view,
        ' which in turn may try to close us)
        Private _inOnClose As Boolean

        Private _vsWindowFrame As IVsWindowFrame
        Private _serviceProvider As IServiceProvider

        'Document related items
        Private _mkDocument As String
        Private _customMkDocumentProvider As CustomDocumentMonikerProvider
        Private _hierarchy As IVsHierarchy
        Private _itemId As UInteger
        Private _docCookie As UInteger
        Private _ownerCaption As String
        Private _docData As Object
        'The DocView for the designer, if we were able to retrieve it (if we understood the designer type).  This would
        '  be a PropPageDesignerView for our hosted property pages, ResourceEditorView for the resource editor, etc.
        Private _docView As System.Windows.Forms.Control

        'This is Nothing if we're not displaying a property page
        Private _propertyPageInfo As PropertyPageInfo

        Private _tabTitle As String 'The title that should be used for this panel's tab
        Private _tabAutomationName As String 'The name for the tab that is not localized and is seen by QA automation tools

        'Provides a custom view control that can be displayed instead of hosting a designer.  We can display either
        '  a custom view provider or a hosted designer, but not both at once.
        Private _customViewProvider As CustomViewProvider

        ' child controls...
        ' We put a label when screen reader is running to show the name of the page. We use the hosting panel to host the read docView
        Private WithEvents _pageHostingPanel As System.Windows.Forms.Panel
        Private WithEvents _pageNameLabel As System.Windows.Forms.Label

        'True while in the process of creating the designer
        Private _creatingDesigner As Boolean

        'The owning project designer
        Private _view As ApplicationDesignerView


        ''' <summary>
        ''' Constructor for a designer panel to hold a property page
        ''' </summary>
        ''' <param name="View">The owning project designer view</param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="PropertyPageInfo"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal View As ApplicationDesignerView, ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal PropertyPageInfo As PropertyPageInfo)
            Me.New(View, Hierarchy, ItemId)

            Debug.Assert(PropertyPageInfo IsNot Nothing)
            _propertyPageInfo = PropertyPageInfo

            Debug.Assert(View IsNot Nothing)
            _view = View

            Me._editorGuid = GetType(PropPageDesigner.PropPageDesignerEditorFactory).GUID
        End Sub


        ''' <summary>
        ''' Constructor for non-property pages.
        ''' </summary>
        ''' <param name="View">The owning project designer view</param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal View As ApplicationDesignerView, ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger)
            Debug.Assert(View IsNot Nothing)
            Debug.Assert(Hierarchy IsNot Nothing)

            Dim ServiceProvider As IServiceProvider = DirectCast(View, IServiceProvider)
            Debug.Assert(ServiceProvider IsNot Nothing)
            _serviceProvider = ServiceProvider
            _view = View
            _hierarchy = Hierarchy
            _itemId = ItemId
            _ownerCaption = "%1"

            SuspendLayout()

            InitializeComponent()
            Me._pageNameLabel.Visible = AppDesCommon.Utils.IsScreenReaderRunning()

            ResumeLayout(False)

        End Sub

        ''' <summary>
        ''' Returns the PropertyPageInfo for this designer panel, if it corresponds to a property page.
        ''' Otherwise returns Nothing.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property PropertyPageInfo() As PropertyPageInfo
            Get
                Return _propertyPageInfo
            End Get
        End Property

        Public ReadOnly Property IsPropertyPage() As Boolean
            Get
                Dim ReturnValue As Boolean = (_propertyPageInfo IsNot Nothing)
                Debug.Assert(Not ReturnValue OrElse EditorGuid.Equals(GetType(PropPageDesigner.PropPageDesignerEditorFactory).GUID), _
                    "If it's a property page, the EditorGuid should be the PropPageDesigner's guid")
                Return (_propertyPageInfo IsNot Nothing)
            End Get
        End Property

        Public ReadOnly Property Hierarchy() As IVsHierarchy
            Get
                Return _hierarchy
            End Get
        End Property

        Public ReadOnly Property ItemId() As UInteger
            Get
                Return _itemId
            End Get
        End Property

        Public ReadOnly Property DocCookie() As UInteger
            Get
                Return _docCookie
            End Get
        End Property

        ''' <summary>
        ''' Provides a custom view control that can be displayed instead of hosting a designer.  We can display either
        '''   a custom view provider or a hosted designer, but not both at once.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property CustomViewProvider() As CustomViewProvider
            Get
                Return _customViewProvider
            End Get
            Set(ByVal value As CustomViewProvider)
                If _customViewProvider Is value Then
                    Exit Property
                End If

                'Close anything currently showing
                CloseFrame()
                _customViewProvider = value
                If Me.Visible Then
                    ShowDesigner()
                End If
            End Set
        End Property

        Public Sub ShowDesigner(Optional ByVal Show As Boolean = True)
            If _creatingDesigner Then
                Common.Switches.TracePDFocus(TraceLevel.Info, "ShowDesigner - exiting, recursive call")
                Return
            End If

            If Show Then
                If Me.Parent Is Nothing OrElse Not Me.Parent.Visible Then
                    'Debug.Fail("Showing designer when parent is not visible?")
                    Common.Switches.TracePDFocus(TraceLevel.Info, "ShowDesigner() - Parent is nothing or not visible - ignoring")
                    Return
                End If
                Common.Switches.TracePDFocus(TraceLevel.Warning, "ShowDesigner(True) on panel """ & Me.TabAutomationName & "/" & Me.TabTitle & """")

                If _vsWindowFrame Is Nothing Then
                    CreateDesigner()
                    Debug.Assert(CustomViewProvider IsNot Nothing OrElse _windowFrameShown, "Window frame didn't get shown?")
                    Exit Sub
                End If

                'Note that we need to call ShowWindowFrame() whether m_WindowFrameShown is true or not
                '  because we need to set the active designer correctly on focus changes.
                ShowWindowFrame()
            Else
                'Hide
                If (_vsWindowFrame IsNot Nothing) Then
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "ShowDesigner(False) on panel """ & Me.TabAutomationName & "/" & Me.TabTitle & """ (WindowFrame.Hide)")
                    Dim WindowFrameIsVisible As Boolean = (_vsWindowFrame.IsVisible() = NativeMethods.S_OK)
                    If WindowFrameIsVisible Then
                        Dim hr As Integer = _vsWindowFrame.Hide()
                        Debug.Assert(VSErrorHandler.Succeeded(hr), "Failure trying to hide WindowFrame, hr=0x" & VB.Hex(hr))
                    End If
                End If

                'PERF: calling Visible = false when Visible = false does real work.
                If Me.Visible Then
                    'PERF: setting a child to be visible false performs layout on the parent.
                    SuspendLayout()
                    Me.Visible = False
                    ResumeLayout(False)
                End If
                _windowFrameShown = False
            End If

        End Sub

        Private Sub CreateCustomView()
            If _customViewProvider IsNot Nothing Then
                If _customViewProvider.View Is Nothing Then
                    _customViewProvider.CreateView()
                    _customViewProvider.View.Dock = DockStyle.Fill
                    _customViewProvider.View.Visible = True
                    Me._pageHostingPanel.Controls.Add(_customViewProvider.View)
                End If
            End If
        End Sub

        Public Sub CreateDesigner()
            If _creatingDesigner Then
                Common.Switches.TracePDFocus(TraceLevel.Info, "CreateDesigner() - exiting, recursive call")
                Exit Sub
            End If

            Using New Common.WaitCursor()
                Common.Switches.TracePDPerfBegin("CreateDesigner")
                Common.Switches.TracePDFocus(TraceLevel.Warning, "CreateDesigner() on panel """ & Me.TabAutomationName & "/" & Me.TabTitle & """")
                _creatingDesigner = True
                Try
                    If _customViewProvider IsNot Nothing Then
                        'We're supposed to show a custom view.  If it's not already there, then create it.
                        CreateCustomView()
                        ShowWindowFrame()
                    Else
                        'Regular designer to be shown...
                        Dim PhysicalView As String = Nothing
                        Dim ExistingDocDataPtr As IntPtr
                        Dim LogicalViewGuid As Guid

                        Dim WindowFrame As IVsWindowFrame = Nothing

                        Dim VsUIShellOpenDocument As IVsUIShellOpenDocument = CType(_serviceProvider.GetService(GetType(IVsUIShellOpenDocument)), IVsUIShellOpenDocument)
                        Dim OleServiceProvider As OLE.Interop.IServiceProvider

                        Debug.Assert(VsUIShellOpenDocument IsNot Nothing, "Unable to get IVsUIShellOpenDocument")

                        Dim MkDocument As String = Me.MkDocument
                        If MkDocument = "" Then
                            Throw New ArgumentException("Invalid Document moniker")
                        End If

                        If Me.DocData IsNot Nothing Then
                            ExistingDocDataPtr = Marshal.GetIUnknownForObject(Me.DocData)
                        End If
                        OleServiceProvider = CType(_serviceProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                        Debug.Assert(OleServiceProvider IsNot Nothing, "Unable to get OleServiceProvider")

                        Try
                            Dim VsUIHierarchy As IVsUIHierarchy = Nothing
                            Dim ItemId As UInteger
                            Dim pDocInProj As Integer

                            'CONSIDER: IsDocumentInAProject is used to obtain the VsUIHierarchy and ItemId
                            'We should try and find a way to obtain this from the ProjectItem DTE object
                            VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.IsDocumentInAProject(MkDocument, VsUIHierarchy, ItemId, OleServiceProvider, pDocInProj))
                            If pDocInProj <> 0 Then
                                'Document was found so open it with the specified editor
                                If EditorGuid.Equals(System.Guid.Empty) Then
                                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.OpenDocumentViaProject(MkDocument, (LogicalViewGuid), OleServiceProvider, VsUIHierarchy, ItemId, WindowFrame))
                                Else
                                    Dim OpenHierachy As IVsUIHierarchy = Nothing
                                    Dim OpenItemId As UInteger
                                    Dim OpenWindowFrame As IVsWindowFrame = Nothing
                                    Dim fOpen As Integer

                                    'Is this file already opened in the specific editor we want to open it in?  If so, we will not be able to open it
                                    '  as a nested document window.
                                    Dim hr As Integer = VsUIShellOpenDocument.IsSpecificDocumentViewOpen(VsUIHierarchy, ItemId, MkDocument, (EditorGuid), PhysicalView, 0UI, OpenHierachy, OpenItemId, OpenWindowFrame, fOpen)
                                    Debug.Assert(VSErrorHandler.Succeeded(hr), "Unexpected failure from VsUIShellOpenDocument.IsSpecificDocumentViewOpen")
                                    If VSErrorHandler.Succeeded(hr) Then
                                        If fOpen <> 0 Then
                                            'Already open - show an error message asking them to close it first.
                                            Throw New ArgumentException(SR.GetString(SR.APPDES_EditorAlreadyOpen_1Arg, MkDocument))
                                        End If
                                    End If

                                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.OpenDocumentViaProjectWithSpecific(MkDocument, CUInt(__VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_DoOpen Or __VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseEditor) Or Me.EditFlags, (EditorGuid), PhysicalView, (LogicalViewGuid), OleServiceProvider, VsUIHierarchy, ItemId, WindowFrame))
                                End If
                                'Save the values returned
                                _itemId = ItemId
                                _hierarchy = VsUIHierarchy
                            Else
                                'Document is not in the project
                                'Must reinit these because IsDocumentInAProject overwrites previous values
                                'UNDONE: this service provider that we pass in to Open{Specific,Standard}Editor is what gets returned from
                                '  the windowframe's VSFPROPID_SPProjContext property.  Are we passing in the correct service provider for this?
                                '  It's not the same as what we're using to service requests via the property page site.
                                OleServiceProvider = CType(_serviceProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                                'Parens required around EditorGuid, LogicalGuid to prevent copyback assignment generated by VB compiler
                                If EditorGuid.Equals(System.Guid.Empty) Then
                                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.OpenStandardEditor(0, _
                                            MkDocument, _
                                            (EditorGuid), _
                                            EditorCaption, _
                                            CType(_hierarchy, IVsUIHierarchy), _
                                            VSITEMID.ROOT, _
                                            ExistingDocDataPtr, _
                                            OleServiceProvider, _
                                            WindowFrame))
                                ElseIf IsPropertyPage() OrElse IO.File.Exists(MkDocument) Then
                                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.OpenSpecificEditor(Me.EditFlags, _
                                            MkDocument, _
                                            (EditorGuid), _
                                            _physicalView, _
                                            (LogicalViewGuid), _
                                            EditorCaption, _
                                            DirectCast(_hierarchy, IVsUIHierarchy), _
                                            VSITEMID.ROOT, _
                                            ExistingDocDataPtr, _
                                            OleServiceProvider, _
                                            WindowFrame))
                                Else
                                    'The file doesn't exist (must have been deleted), so don't try to open it.
                                    Throw New ArgumentException(SR.GetString(SR.APPDES_FileNotFound_1Arg, MkDocument))
                                End If

                                Dim Value As Object = Nothing
                                VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_Hierarchy, Value))
                                _hierarchy = TryCast(Value, IVsHierarchy)

                                VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_ItemID, Value))
                                _itemId = Common.NoOverflowCUInt(Value)
                            End If

                            'We must make the panel visible before creating the window frame, because the control's HWND must be
                            '  created before passing it to devenv to use as a DocView.
                            Common.Switches.TracePDPerfBegin("Making ApplicationDesignerPanel visible and laying it out")
                            Me.SuspendLayout()
                            Me._pageHostingPanel.SuspendLayout()
                            Me.BringToFront()

                            'Force handle creation of this ApplicationDesignerPanel and its PageHostingPanel.
                            '  We can't use CreateControl because that only works if the control is already
                            '  visible, and these aren't yet (we don't want them to be because the location/size
                            '  haven't been finalized yet).  We need the handle created now so that we can pass
                            '  its HWND to the shell for DocView hosting.
                            Dim DummyHandle As IntPtr
                            'Must create the PageHostingPanel's parent's handle first ("Me"), because otherwise
                            '  its handle will get re-created later when it's re-parented
                            Debug.Assert(Me._pageHostingPanel.Parent Is Me)
                            DummyHandle = Me._pageHostingPanel.Parent.Handle
                            Dim PageHostingPanelHandle As IntPtr = Me._pageHostingPanel.Handle

                            'Since the ApplicationDesignerPanel starts out intentionally hidden, PerformLayout()
                            '  will not dock it to its parent size.  So we do that manually here to minimize
                            '  size changes.
                            Debug.Assert(Me.Parent.Size.Width <> 0 AndAlso Me.Parent.Size.Height <> 0)
                            Me.Size = Me.Parent.Size
                            Me._pageHostingPanel.ResumeLayout(False)
                            Me.ResumeLayout(True) 'Must give the PageHostingPanel a chance to dock properly to its parent
                            Common.Switches.TracePDPerfEnd("Making ApplicationDesignerPanel visible and laying it out")

                            'Parent window and parent frame must be set before the WindowFrame.Show call
                            Dim CurrentParentHwnd As Object = Nothing
                            VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID2.VSFPROPID_ParentHwnd, CurrentParentHwnd))
                            If CInt(CurrentParentHwnd) = 0 Then
                                '... But only try to set the parent hwnd/frame if they haven't been set already, otherwise this will fail
                                Debug.Assert(Me._pageHostingPanel.IsHandleCreated AndAlso Me._pageHostingPanel.Parent.IsHandleCreated, _
                                    "The panel which will host the nested designer has not had its handle created yet, that should have already happened. " _
                                    & "It will be force created now by get_Handle(), but when the window's parent hierarchy is created later, this may cause the panel's window " _
                                    & "to be re-created later, and Visual Studio will have the wrong HWND.")
                                VSErrorHandler.ThrowOnFailure(WindowFrame.SetProperty(__VSFPROPID2.VSFPROPID_ParentHwnd, PageHostingPanelHandle))
                                VSErrorHandler.ThrowOnFailure(WindowFrame.SetProperty(__VSFPROPID2.VSFPROPID_ParentFrame, _serviceProvider.GetService(GetType(IVsWindowFrame))))

                                AdviseWindowFrameNotify(WindowFrame)
                            End If

                            If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                                Marshal.Release(ExistingDocDataPtr)
                            End If

                            'Add the Document Cookie to our list
                            Dim CookieObj As Object = Nothing
                            Dim DocDataObj As Object = Nothing
                            Dim EditorCaptionObject As Object = Nothing
                            Dim DocViewObject As Object = Nothing

                            VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_DocData, _docData))
                            VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_DocCookie, CookieObj))
                            _docCookie = Common.NoOverflowCUInt(CookieObj)

                            VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_DocView, DocViewObject))

                            'Get the DocView for those we can
                            Dim DesignerWindowPane As Microsoft.VisualStudio.Shell.Design.DesignerWindowPane
                            DesignerWindowPane = TryCast(DocViewObject, Microsoft.VisualStudio.Shell.Design.DesignerWindowPane)
                            If DesignerWindowPane IsNot Nothing Then
                                Dim WindowPaneControl As System.Windows.Forms.Control = TryCast(DesignerWindowPane.Window, System.Windows.Forms.Control)
                                If WindowPaneControl IsNot Nothing Then
                                    _docView = DirectCast(WindowPaneControl, System.Windows.Forms.Control).Controls(0)
                                End If
                            End If

                            'Get the editor caption to use as the tab text
                            VSErrorHandler.ThrowOnFailure(WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_EditorCaption, EditorCaptionObject))
                            If TypeOf EditorCaptionObject Is String Then '(might be Nothing)
                                Me.EditorCaption = DirectCast(EditorCaptionObject, String)
                            End If

                            If _propertyPageInfo IsNot Nothing AndAlso _propertyPageInfo.Site IsNot Nothing Then
                                'Set up the service provider for the property page site.
                                'The service provider that we want comes from the PropPageDesignerView
                                '  (the DocView), so that property pages querying services receive
                                '  the inner window frame, etc., rather than getting services from
                                '  the outer window frame (the application designer).
                                Debug.Assert(_propertyPageInfo.Site.BackingServiceProvider Is Nothing, "Service provider in property page site set twice")
                                Debug.Assert(_docView IsNot Nothing AndAlso TypeOf _docView Is PropPageDesigner.PropPageDesignerView _
                                    AndAlso TypeOf _docView Is IServiceProvider)
                                _propertyPageInfo.Site.BackingServiceProvider = TryCast(_docView, IServiceProvider)

#If DEBUG Then
                                'Verify that property pages can get to native services such as IVsWindowFrame
                                '  (needed to hook up help) through the service provider in the property page
                                '  site.
                                Dim spOLE As OLE.Interop.IServiceProvider = TryCast(_propertyPageInfo.Site, OLE.Interop.IServiceProvider)
                                If spOLE IsNot Nothing Then
                                    Dim sp As New Shell.ServiceProvider(spOLE)
                                    Dim iwf As IVsWindowFrame = TryCast(sp.GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                                    Debug.Assert(iwf IsNot Nothing AndAlso iwf Is WindowFrame, _
                                        "Unable to access the correct IVsWindowFrame for a property page through its property page site via " _
                                            & "native IServiceProvider")
                                End If
#End If
                            End If

                            'Make the window frame visible
                            Me.VsWindowFrame = WindowFrame
                            ShowWindowFrame()

                            _view.DelayRefreshDirtyIndicators()
                        Finally
                            'If Me.VsWindowFrame did not execute, VsWindowFrame will not have been set
                            If WindowFrame IsNot Nothing AndAlso _vsWindowFrame IsNot WindowFrame Then
                                CloseFrameInternal(WindowFrame, __FRAMECLOSE.FRAMECLOSE_NoSave)
                                _docData = Nothing
                                _docView = Nothing
                            End If
                        End Try
                    End If

                    Common.Switches.TracePDPerfEnd("CreateDesigner")
                Finally
                    _creatingDesigner = False
                End Try
            End Using
        End Sub

        Protected Overridable Sub ShowWindowFrame()
            Common.Switches.TracePDFocus(TraceLevel.Warning, "ShowWindowFrame()")
            If VsWindowFrame IsNot Nothing Then
                'Initialization should have happened first to allow the project designer to settle down before we try activing 
                '  the window frame, otherwise it leads to lots of back and forth between the project designer and property 
                '  page(designers) being the active designer.
                Debug.Assert(_view.InitializationComplete)

                'Show the window frame
                Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... VsWindowFrame.Show()")
                Common.Switches.TracePDPerfBegin("VsWindowFrame.Show")

                'We have to show the windowframe before calling UpdateWindowFrameSize because
                '  SetFramePos() will fail if the window frame doesn't have an HWND.
                '  Note that it is first shown with a non-zero location, so if there is
                '  anything visible on the HWND that we've set as VSFPROPID_ParentHwnd, it
                '  will be briefly visible in the wrong location causing flicker before we
                '  are able to update the frame size and location.

                'If Not m_WindowFrameShown Then
                '    Debug.Assert(Not Me.Visible, "Flicker warning - ApplicationDesignerPanel should be Visible=False the first time calling VsWindowFrame.Show()")
                'End If

                Dim hr As Integer = VsWindowFrame.Show()
                Debug.Assert(VSErrorHandler.Succeeded(hr), "VsWindowFrame.Show() failed with hr=" & VB.Hex(hr))
                _windowFrameShown = True
#If DEBUG Then
                m_Debug_cWindowFrameShow += 1
#End If
                Common.Switches.TracePDPerfEnd("VsWindowFrame.Show")
                UpdateSelection()

                'Set the window frame's correct size/location
                UpdateWindowFrameBounds()

                'Now make this ApplicationDesignerPanel visible.  We do this after showing the window frame
                '  and settings its size/location because we can't control where the window frame first 
                '  shows up when make visible.
                Common.Switches.TracePDPerfBegin("Setting ApplicationDesignerPanel.Visible = True")
                Me.Visible = True

                'Because of the way we've initialized things so that the panel is not visible until
                '  after the window frame is activated, the focus on the window pane control will have
                '  already happened (see DesignerWindowPaneBase.View_GotFocus), and trying to forward 
                '  the focus to the actual child controls of the pane may have failed because a parent
                '  window (this panel) was hidden.  So now that we're visible, ensure that focus is
                '  set up properly in the child window frame.
                If VsWindowFrame IsNot Nothing Then
                    VsWindowFrame.Show()
                End If

                Common.Switches.TracePDPerfEnd("Setting ApplicationDesignerPanel.Visible = True")
                Debug.Assert(Me._pageHostingPanel.Visible)
            ElseIf CustomViewProvider IsNot Nothing Then
                CreateCustomView()
                Me.Visible = True
                UpdateSelection()
                UpdateWindowFrameBounds()

                If Not TryActivateParentFrameToHandleCommandsForCustomView() Then
                    Debug.Fail("Failed to Activate Parent Frame!")
                End If

                ' set the focus to the custom view...
                If _customViewProvider.View IsNot Nothing Then
                    _customViewProvider.View.Focus()
                End If
            Else
                Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... No VsWindowFrame or CustomViewProvider - ignoring")
            End If
        End Sub

        Private Function TryActivateParentFrameToHandleCommandsForCustomView() As Boolean
            ' Since CustomViews are not contained in their own WindowFrame, the parent
            ' frame needs to be activated so that it can handle commands while the
            ' CustomView is showing.
            ' See Dev10 Bug 641849.
            Dim parentFrame As IVsWindowFrame2 = TryCast(Me._view.WindowFrame, IVsWindowFrame2)
            If parentFrame Is Nothing Then
                Return False
            End If

            Return VSErrorHandler.Succeeded(parentFrame.ActivateOwnerDockedWindow())
        End Function

        ''' <summary>
        ''' Notify the shell of the current selection so that the Project menus etc. are correct
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateSelection()
            Common.Switches.TracePDPerfBegin("ApplicationDesignerPanel.UpdateSelection")
            'When we call IVsWindowFrame.Hide() on a tab (when changing to another tab), the shell nulls out the 
            '  window frame's selection container's hierarchy/itemid pointer by calling 
            '  OnSelectChangeEx(NULL, AppDesVSITEMID_NIL, NULL, NULL) in CDockObjSite::WindowHidden(), so that the window 
            '  frame no longer contributes to the selection context.  This is perhaps questionable but hasn't caused 
            '  problems before because most docviews don't hide themselves (tool windows do, but they consciously set 
            '  their selection context).  But this means that when we re-show the window frame, the hierarchy/itemid 
            '  have been removed, and the inner hierarchy's (the project's) hierarchy is no longer associated with the 
            '  docview - it goes back to the default, which is the outer hierarchy (the solution).
            'This causes menus (like the Project and Build menu) to get their context from the solution instead of the
            '  Project, which means that lots of menu items are incorrectly disabled.
            'The solution is to specifically set the inner hierarchy/itemid to be the active selection in the window 
            '  frame's selection context when we re-show the window frame.  This is what we got when originally creating
            '  the window frame because we pass in the project hierarchy and itemid when calling OpenSpecificEditor.
            Dim punkTrackSelection As IntPtr = IntPtr.Zero
            Dim punkInnerHierarchy As IntPtr = IntPtr.Zero
            Dim pVsHierarchyInnerHierarchy As IntPtr = IntPtr.Zero
            Try
                'Determine which VsWindowFrame we're using
                Dim WindowFrame As IVsWindowFrame = Nothing
                WindowFrame = Me.VsWindowFrame
                If WindowFrame Is Nothing Then
                    'We're not showing a nested window frame, so use the windowframe for the application 
                    '  designer.
                    Debug.Assert(_view IsNot Nothing, "No application designer?")
                    If _view IsNot Nothing Then
                        WindowFrame = _view.WindowFrame
                        Debug.Assert(WindowFrame IsNot Nothing, "Application designer doesn't have a WindowFrame?")
                    End If
                End If

                If WindowFrame Is Nothing Then
                    Exit Sub
                End If

                'Get the ServiceProvider for the window frame
                Dim FrameServiceProviderObject As Object = Nothing
                Dim hr As Integer
                hr = WindowFrame.GetProperty(__VSFPROPID.VSFPROPID_SPFrame, FrameServiceProviderObject)
                If VSErrorHandler.Failed(hr) OrElse FrameServiceProviderObject Is Nothing Then
                    Debug.Fail("Could not get VSFPROPID_SPFrame from frame (hr=0x" & VB.Hex(hr) & ")")
                Else
                    Dim FrameServiceProvider As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = DirectCast(FrameServiceProviderObject, Microsoft.VisualStudio.OLE.Interop.IServiceProvider)

                    'QueryService for IVsTrackSelectionEx
                    hr = FrameServiceProvider.QueryService(GetType(SVsTrackSelectionEx).GUID, GetType(IVsTrackSelectionEx).GUID, punkTrackSelection)
                    If VSErrorHandler.Failed(hr) OrElse punkTrackSelection.Equals(IntPtr.Zero) Then
                        Debug.Fail("Could not get IVsTrackSelectionEx from frame's service provider (hr=0x" & VB.Hex(hr) & ")")
                    Else
                        Dim TrackSelection As IVsTrackSelectionEx = DirectCast(Marshal.GetObjectForIUnknown(punkTrackSelection), IVsTrackSelectionEx)

                        'Set the active hierarchy/itemid in this window frame to be that of the inner hierarchy (i.e., the project) rather than
                        '  the outer (solution)
                        Const SELCONTAINER_DONTCHANGE As Integer = -1
                        punkInnerHierarchy = Marshal.GetIUnknownForObject(_hierarchy)
                        VSErrorHandler.ThrowOnFailure(Marshal.QueryInterface(punkInnerHierarchy, GetType(IVsHierarchy).GUID, pVsHierarchyInnerHierarchy))
                        VSErrorHandler.ThrowOnFailure(TrackSelection.OnSelectChangeEx( _
                            pVsHierarchyInnerHierarchy, VSITEMID.ROOT, _
                            Nothing, _
                            New IntPtr(SELCONTAINER_DONTCHANGE)))
                    End If
                End If
            Finally
                If Not punkTrackSelection.Equals(IntPtr.Zero) Then
                    Marshal.Release(punkTrackSelection)
                End If
                If Not punkInnerHierarchy.Equals(IntPtr.Zero) Then
                    Marshal.Release(punkInnerHierarchy)
                End If
                If Not pVsHierarchyInnerHierarchy.Equals(IntPtr.Zero) Then
                    Marshal.Release(pVsHierarchyInnerHierarchy)
                End If
            End Try
            Common.Switches.TracePDPerfEnd("ApplicationDesignerPanel.UpdateSelection")
        End Sub

        ''' <summary>
        ''' Get/set the window frame owned by this panel
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property VsWindowFrame() As IVsWindowFrame
            Get
                Return _vsWindowFrame
            End Get
            Set(ByVal Value As IVsWindowFrame)
                If Value IsNot _vsWindowFrame Then
                    Common.Switches.TracePDFocus(TraceLevel.Info, "ApplicationDesignerPanel.set_VsWindowFrame")
                    CloseFrame()
                    _vsWindowFrame = Value

                    'If we're already visible, show with the new window frame
                    If Me.Visible And Not _creatingDesigner Then
                        ShowWindowFrame()
                    End If

                End If
            End Set
        End Property

        ''' <summary>
        ''' The Guid used by the editor for this panel.  For property pages, this is always
        '''   PropPageDesignerView's guid.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property EditorGuid() As Guid
            Get
                Return _editorGuid
            End Get
            Set(ByVal Value As Guid)
                If IsPropertyPage Then
                    Debug.Fail("Cannot change EditorGuid for property page designer panels")
                    Exit Property
                End If
                Debug.Assert(_editorGuid.Equals(Guid.Empty), "EditorGuid set multiple times")
                _editorGuid = Value
            End Set
        End Property

        ''' <summary>
        ''' The Guid used by the object actually represented in this panel.  For property pages,
        '''   this is the guid of the property page.  For all others, this is the same as EditorGuid.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ActualGuid() As Guid
            Get
                If _propertyPageInfo IsNot Nothing Then
                    Return _propertyPageInfo.Guid
                Else
                    Return _editorGuid
                End If
            End Get
        End Property

        ''' <summary>
        ''' Physical view guid for the editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property PhysicalView() As String
            Get
                Return _physicalView
            End Get
            Set(ByVal Value As String)
                _physicalView = Value
            End Set
        End Property

        ''' <summary>
        ''' The editor's caption
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property EditorCaption() As String
            Get
                Return _editorCaption
            End Get
            Set(ByVal Value As String)
                _editorCaption = Value
            End Set
        End Property

        ''' <summary>
        ''' The filename moniker of the file which is being edited by the editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property MkDocument() As String
            Get
                If _mkDocument <> "" Then
                    Return _mkDocument
                ElseIf _customMkDocumentProvider IsNot Nothing Then
                    Return _customMkDocumentProvider.GetDocumentMoniker()
                Else
                    Return ""
                End If
            End Get
            Set(ByVal Value As String)
                Debug.Assert(_mkDocument Is Nothing, "MkDocument set multiple times")
                _mkDocument = Value
            End Set
        End Property

        ''' <summary>
        ''' The filename moniker of the file which is being edited by the editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property CustomMkDocumentProvider() As CustomDocumentMonikerProvider
            Get
                Return _customMkDocumentProvider
            End Get
            Set(ByVal Value As CustomDocumentMonikerProvider)
                Debug.Assert(_customMkDocumentProvider Is Nothing OrElse Value Is Nothing, "m_CustomMkDocumentProvider set multiple times")
                _customMkDocumentProvider = Value
            End Set
        End Property

        ''' <summary>
        ''' The DocData for the editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DocData() As Object
            Get
                Return _docData
            End Get
            Set(ByVal Value As Object)
                _docData = Value
            End Set
        End Property

        ''' <summary>
        ''' The DocView for the editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DocView() As System.Windows.Forms.Control
            Get
                Return _docView
            End Get
            Set(ByVal Value As System.Windows.Forms.Control)
                _docView = Value
            End Set
        End Property

        ''' <summary>
        ''' Flags to be passed to VsUIShellOpenDocument
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property EditFlags() As UInteger
            Get
                Return _editFlags
            End Get
            Set(ByVal Value As UInteger)
                _editFlags = Value
            End Set
        End Property

        ''' <summary>
        ''' The title that should be used for this panel's tab
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property TabTitle() As String
            Get
                Return _tabTitle
            End Get
            Set(ByVal value As String)
                _tabTitle = value

                ' We set Window.Text to be the page Title to help screen reader.
                If String.IsNullOrEmpty(value) Then
                    Me.Text = String.Empty
                Else
                    Me.Text = SR.GetString(SR.APPDES_PageName, value)
                End If

                Me._pageNameLabel.Text = Me.Text
            End Set
        End Property

        ''' <summary>
        ''' The name for the tab that is not localized and is seen by QA automation tools
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property TabAutomationName() As String
            Get
                Return _tabAutomationName
            End Get
            Set(ByVal value As String)
                _tabAutomationName = value
            End Set
        End Property


        ''' <summary>
        ''' We need to keep the native window (created by devenv) that hosts our property page the
        '''   same size as the hosting panel (as if it were dock filled).  This sets the native
        '''   window's size to the size of the hosting panel.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub UpdateWindowFrameBounds()
            'Note: don't need to wory about updating size of CustomView because it's set to dock fill
            If VsWindowFrame IsNot Nothing Then
                If Not _windowFrameShown Then
                    'SetFramePos will fail if the window frame is not yet visible, so nothing to do
                    Exit Sub
                End If

                If _windowFrameLastSize <> _pageHostingPanel.Size Then
#If DEBUG Then
                    m_Debug_cWindowFrameBoundsUpdated += 1
#End If
                    Common.Switches.TracePDPerfBegin("UpdateWindowFrameSize: setting window frame position/size: " & _pageHostingPanel.Size.ToString())
                    Dim nullGuid As Guid
                    Dim hr As Integer = VsWindowFrame.SetFramePos(VSSETFRAMEPOS.SFP_fSize Or VSSETFRAMEPOS.SFP_fMove, nullGuid, 0, 0, _pageHostingPanel.Width, _pageHostingPanel.Height)
                    If VSErrorHandler.Failed(hr) Then
                        Common.Switches.TracePDFocus(TraceLevel.Error, "VsWindowFrame.SetFramePos() failed with hr=" & VB.Hex(hr))
                    End If
                    _windowFrameLastSize = _pageHostingPanel.Size
                    Common.Switches.TracePDPerfEnd("UpdateWindowFrameSize: setting window frame position/size: " & _pageHostingPanel.Size.ToString())
                End If
            End If
        End Sub

        ''' <summary>
        ''' When the size of our host panel is changed, we need to adjust the native window frame...
        ''' </summary>
        Private Sub PageHostingPanel_Layout(ByVal sender As Object, ByVal e As LayoutEventArgs) Handles _pageHostingPanel.Layout
            'PERF: it's more performant to handle Layout for control property changes because SizeChanged happens after the layout
            '  and will cause another layout.
            Common.Switches.TracePDPerf(e, "ApplicationDesignerPanel.OnPanelLayout")
            UpdateWindowFrameBounds()
            Common.Switches.TracePDPerfEnd("ApplicationDesignerPanel.OnPanelLayout")
        End Sub

        ''' <summary>
        ''' Commits any pending changes on the designer
        ''' </summary>
        ''' <returns>return False if it failed</returns>
        ''' <remarks></remarks>
        Public Function CommitPendingEdit() As Boolean
            If VsWindowFrame IsNot Nothing Then
                Dim docViewObject As Object = Nothing
                Dim hr As Integer = VsWindowFrame.GetProperty(__VSFPROPID.VSFPROPID_DocView, docViewObject)
                If NativeMethods.Succeeded(hr) AndAlso docViewObject IsNot Nothing Then
                    Dim vsWindowPanelCommit As IVsWindowPaneCommit = TryCast(docViewObject, IVsWindowPaneCommit)
                    If vsWindowPanelCommit IsNot Nothing Then
                        Dim commitFailed As Integer = 0
                        hr = vsWindowPanelCommit.CommitPendingEdit(commitFailed)
                        Return (NativeMethods.Succeeded(hr) AndAlso commitFailed = 0)
                    End If
                End If
            End If
            Return True
        End Function

        Public Sub CloseFrame()
            If _vsWindowFrame IsNot Nothing Then
                'Store locally and clear before CloseFrame call to prevent usage during shutdown
                Dim Frame As IVsWindowFrame = VsWindowFrame
                _vsWindowFrame = Nothing

                'By the time we get here, we will have already saved all child documents if the user chose to save them
                '  (via ApplicationDesignerWindowPane.SaveChildren()), and if the user chose no, we must not save them now,
                '  so we send in NoSave.
                CloseFrameInternal(Frame, __FRAMECLOSE.FRAMECLOSE_NoSave)
            End If
            If _customViewProvider IsNot Nothing AndAlso _customViewProvider.View IsNot Nothing Then
                Me._pageHostingPanel.Controls.Remove(_customViewProvider.View)
                _customViewProvider.CloseView()
            End If
            _windowFrameShown = False
        End Sub

        Protected Overridable Sub CloseFrameInternal(ByVal WindowFrame As IVsWindowFrame, ByVal flags As __FRAMECLOSE)
            If WindowFrame IsNot Nothing Then
                UnadviseWindowFrameNotify(WindowFrame)
                Dim hr As Integer = WindowFrame.CloseFrame(Common.NoOverflowCUInt(flags))
            End If
        End Sub

        Private Sub AdviseWindowFrameNotify(ByVal windowFrame As IVsWindowFrame)
            Debug.Assert(windowFrame IsNot Nothing)
            Dim windowFrame2 As IVsWindowFrame2 = TryCast(windowFrame, IVsWindowFrame2)
            Debug.Assert(windowFrame2 IsNot Nothing, "Couldn't get IVsWindowFrame2 from IVsWindowFrame")
            If windowFrame2 IsNot Nothing Then
                Debug.Assert(_windowFrameNotifyCookie = 0, "Advised IVsWindowFrameNotify multiple times?")
                VSErrorHandler.ThrowOnFailure(windowFrame2.Advise(Me, _windowFrameNotifyCookie))
                Debug.Assert(_windowFrameNotifyCookie <> 0)
            End If
        End Sub

        Private Sub UnadviseWindowFrameNotify(ByVal windowFrame As IVsWindowFrame)
            Debug.Assert(windowFrame IsNot Nothing)
            If _windowFrameNotifyCookie <> 0 Then
                Dim windowFrame2 As IVsWindowFrame2 = TryCast(windowFrame, IVsWindowFrame2)
                Debug.Assert(windowFrame2 IsNot Nothing, "Couldn't get IVsWindowFrame2 from IVsWindowFrame")
                If windowFrame2 IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(windowFrame2.Unadvise(_windowFrameNotifyCookie))
                    _windowFrameNotifyCookie = 0
                End If
            End If
        End Sub

        Private Sub ApplicationDesignerPanel_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
            If VsWindowFrame IsNot Nothing Then
                If Me.Visible Then
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerPanel_VisibleChanged - Visible=True - checking if should ShowWindowFrame")
                    If Not _windowFrameShown And Not _creatingDesigner Then
                        Me.ShowWindowFrame()
                    End If
                Else
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerPanel_VisibleChanged - Visible=False => WindowFrame.Hide()")
                    Common.Switches.TracePDPerf("ApplicationDesignerPanel_VisibleChanged - Visible=False => WindowFrame.Hide()")
                    Dim hr As Integer = VsWindowFrame.Hide()
                    _windowFrameShown = False
                End If
            End If
        End Sub

        ''' <summary>
        ''' This function is called when the designer window is activated or deactivated
        ''' </summary>
        Public Sub OnWindowActivated(ByVal activated As Boolean)
            Common.Switches.TracePDFocus(TraceLevel.Warning, "ApplicationDesignerPanel.OnWindowActivated")
            Dim designView As IVsEditWindowNotify = TryCast(DocView, IVsEditWindowNotify)
            If designView IsNot Nothing Then
                designView.OnActivated(activated)
            End If
        End Sub


        ''' <summary>
        ''' Returns true if this page should show the '*' dirty indicator in its tab
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsDirty() As Boolean
            If IsPropertyPage Then
                'CONSIDER: If we decide to support IVsDocDataContainer, then DocDatas contained by the property
                '  page should be queried for dirty state.

                Dim PropPageView As PropPageDesigner.PropPageDesignerView
                PropPageView = TryCast(Me.DocView, PropPageDesigner.PropPageDesignerView)
                If PropPageView IsNot Nothing Then
                    Return PropPageView.ShouldShowDirtyIndicator()
                Else
                    'Must have had error loading
                End If
            Else
                Dim PersistDocData As IVsPersistDocData = TryCast(DocData, IVsPersistDocData)
                If PersistDocData IsNot Nothing Then
                    Dim fDirty As Integer = 0
                    If VSErrorHandler.Succeeded(PersistDocData.IsDocDataDirty(fDirty)) Then
                        Return fDirty <> 0
                    End If
                End If
            End If

            Return False
        End Function


#If DEBUG Then
        ''' <summary>
        ''' We want to get notified if the HWND is re-created, because that's a bad thing...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PageHostingPanel_HandleCreated(ByVal sender As Object, ByVal e As System.EventArgs) Handles _pageHostingPanel.HandleCreated
            If _vsWindowFrame IsNot Nothing Then
                Debug.Fail("PageHostingPanel handle was recreated after the nested window frame's ParentHwnd property was set to its HWND.  " _
                    & "This is bad because now VS has a pointer to the wrong HWND, and the HWND VS was using has been destroyed.")
            End If
        End Sub
#End If


#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                ' Dispose managed resources.

                If _propertyPageInfo IsNot Nothing Then
                    _propertyPageInfo.Dispose()
                    _propertyPageInfo = Nothing
                End If

                CloseFrame()

                Debug.Assert(Not _windowFrameNotifyCookie <> 0, "Disposing without unadvising IVsWindowFrameNotify2.")
                If _customViewProvider IsNot Nothing Then
                    _customViewProvider.Dispose()
                    _customViewProvider = Nothing
                End If
            End If

            MyBase.Dispose(disposing)
        End Sub
#End Region

        '''<summary>
        ''' Initilize layout...
        '''</summary>
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Me._pageHostingPanel = New System.Windows.Forms.Panel
            Me._pageNameLabel = New System.Windows.Forms.Label
            Me.SuspendLayout()
            '
            'PageHostingPanel
            '
            Me._pageHostingPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me._pageHostingPanel.Name = "PageHostingPanel"
            Me._pageHostingPanel.Margin = New System.Windows.Forms.Padding(0, 0, 0, 0)
            Me._pageHostingPanel.TabIndex = 1
            Me._pageHostingPanel.Text = "PageHostingPanel" 'For debugging
            '
            'PageNameLabel
            '
            Me._pageNameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me._pageNameLabel.AutoSize = True
            Me._pageNameLabel.Location = New System.Drawing.Point(3, 3)
            Me._pageNameLabel.Name = "PageNameLabel"
            Me._pageNameLabel.Margin = New System.Windows.Forms.Padding(14, 14, 14, 3)
            Me._pageNameLabel.TabIndex = 0
            Me._pageNameLabel.Text = "Project Page"  ' it will get replaced with the real page name later...
            Me._pageNameLabel.Visible = False
            '
            'ApplicationDesignerPanel
            '
            Me.ClientSize = New System.Drawing.Size(292, 266)
            Me.ColumnCount = 1
            Me.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.Controls.Add(Me._pageNameLabel, 0, 0)
            Me.Controls.Add(Me._pageHostingPanel, 0, 1)
            Me.RowCount = 2
            Me.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.Name = "ApplicationDesignerPanel"
            Me.Text = "Application Designer Page"
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

        'PERF: Debug tracing of Layout handling...
        Protected Overrides Sub OnLayout(ByVal levent As System.Windows.Forms.LayoutEventArgs)
            Common.Switches.TracePDPerfBegin(levent, "ApplicationDesignerPanel.OnLayout()")
            MyBase.OnLayout(levent)
            Common.Switches.TracePDPerfEnd("ApplicationDesignerPanel.OnLayout()")
        End Sub

#Region "IVsWindowFrameNotify implementation"
        'This interface is implemented only because IVsWindowFrame.Advise() requires an IVsWindowFrameNotify
        '  implementation, even though we only care about the IVsWindowFrameNotify2 methods.
        Private Function IVsWindowFrameNotify_OnDockableChange(ByVal fDockable As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify.OnDockableChange
            Return VSConstants.S_OK
        End Function

        Private Function IVsWindowFrameNotify_OnMove() As Integer Implements Shell.Interop.IVsWindowFrameNotify.OnMove
            Return VSConstants.S_OK
        End Function

        Private Function IVsWindowFrameNotify_OnShow(ByVal fShow As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify.OnShow
            Return VSConstants.S_OK
        End Function

        Private Function IVsWindowFrameNotify_OnSize() As Integer Implements Shell.Interop.IVsWindowFrameNotify.OnSize
            Return VSConstants.S_OK
        End Function
#End Region

#Region "IVsWindowFrameNotify2 implementation"
        ''' <summary>
        ''' The shell may explicitly close this window frame. We need to forward this to our parent window frame (app designer)
        ''' to avoid that we just close this property page and leave the app designer open...
        ''' </summary>
        ''' <param name="pgrfSaveOptions"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnClose(ByRef pgrfSaveOptions As UInteger) As Integer Implements Shell.Interop.IVsWindowFrameNotify2.OnClose
            If _inOnClose Then
                Return NativeMethods.S_OK
            End If
            Try
                _inOnClose = True
                If _serviceProvider IsNot Nothing Then
                    Dim parentFrame As IVsWindowFrame = DirectCast(_serviceProvider.GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                    If parentFrame IsNot Nothing Then
                        'The frame's data should already have been saved by now, so we can send 
                        '  NoSave.  At any rate, we're not the owner of the frame, we're just hooked up
                        '  for notifications, and the save options that get passed in are meaningless
                        '  anyway.
                        Return parentFrame.CloseFrame(CUInt(Shell.Interop.__FRAMECLOSE.FRAMECLOSE_NoSave))
                    End If
                Else
                    Return NativeMethods.S_OK
                End If
            Finally
                _inOnClose = False
            End Try
            Return NativeMethods.S_OK
        End Function
#End Region

        Private _vsUIShell5 As IVsUIShell5
        Public ReadOnly Property VsUIShell5 As IVsUIShell5
            Get
                If _vsUIShell5 Is Nothing Then
                    _vsUIShell5 = TryCast(_serviceProvider.GetService(GetType(IVsUIShell)), IVsUIShell5)
                End If

                Return _vsUIShell5

            End Get
        End Property
    End Class

End Namespace


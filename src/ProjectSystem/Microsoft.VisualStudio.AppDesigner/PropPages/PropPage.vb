Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports NativeMethods = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods

Namespace Microsoft.VisualStudio.Editors.PropertyPages


#Region "Internal interface definitions"

    ' ******************
    '
    ' IMPORTANT NOTE: 
    '
    ' These interfaces are internal to our property page implementation.  They are marked public for those assemblies that have 
    '   classes that inherit from PropPageUserControlBase, but they are not useful for assemblies implementing pages for the project
    '   designer to host without using PropPageUserControlBase.  These interfaces should never be construed to have communication
    '   between the property pages and the property page designer view or the project designer, because that would make it impossible
    '   for third parties to write property pages with the same functionality (because they are not in a non-versioned common
    '   assembly).  
    '
    'The only interface necessary for third parties to implement for property pages that the project designer can host is
    '   IPropertyPage.  The interfaces in Microsoft.VisualStudio.ManagedInterfaces.dll are optional for third parties to implement
    '   in order to hook in to our undo functionality and control group validation.
    '
    ' *******************



    ''' <summary>
    ''' This is an interface interface used to communicate between the PropPageBase class and any property pages (those that inherit
    '''   from PropPageUserControlBase).
    ''' It is our internal equivalent of IPropertyPage2.
    ''' </summary>
    ''' <remarks></remarks>
    <ComVisible(False)> _
        Public Interface IPropertyPageInternal
        Sub Apply()
        Sub Help(ByVal HelpDir As String)
        Function IsPageDirty() As Boolean
        Sub SetObjects(ByVal objects() As Object)
        Sub SetPageSite(ByVal base As IPropertyPageSiteInternal)
        Sub EditProperty(ByVal dispid As Integer)
        Function GetHelpContextF1Keyword() As String ' Gets the F1 keyword to push into the user context for this property page

    End Interface


    ''' <summary>
    ''' This is an interface interface that subclasses of PropPageUserControlBase use to communicate with their site (PropPageBaseClass)
    '''   (i.e., it is internal to our property page implementation - public for those assemblies that have classes that inherit from
    '''   PropPageUserControlBase, but not useful for those implementing pages without using PropPageUserControlBase).
    ''' It is our internal equivalent of IPropertyPageSite.
    ''' </summary>
    ''' <remarks></remarks>
    <ComVisible(False)> _
    Public Interface IPropertyPageSiteInternal
        Sub OnStatusChange(ByVal flags As PROPPAGESTATUS)
        Function GetLocaleID() As Integer
        Function TranslateAccelerator(ByVal msg As Message) As Integer
        Function GetService(ByVal ServiceType As Type) As Object
        ReadOnly Property IsImmediateApply() As Boolean
    End Interface


    <Flags(), ComVisible(False)> _
    Public Enum PROPPAGESTATUS
        Dirty = 1
        Validate = 2
        Clean = 4
    End Enum


#If False Then 'Not currently needed by any pages, consider exposing if needed in the future
    <ComVisible(False)> _
    Public Interface IVsDocDataContainer
        Function GetDocDataCookies() As UInteger()
    End Interface
#End If


#End Region


#Region "PropPageBase"

    Public MustInherit Class PropPageBase
        Implements IPropertyPage2
        Implements IPropertyPageSiteInternal
        Implements IVsProjectDesignerPage
#If False Then
        Implements IVsDocDataContainer
#End If

        Private m_FormType As System.Type
        Private m_PropPage As Control
        Private m_PageSite As IPropertyPageSite
        Private m_IsDirty As Boolean
        Private Const SW_HIDE As Integer = 0
        Private m_size As System.Drawing.Size
        Private m_DefaultSize As System.Drawing.Size
        Private m_DocString As String
        Private m_HelpFile As String
        Private m_HelpContext As UInteger
        Private m_Title As String
        Private m_Objects As Object()
        Private m_PrevParent As IntPtr
        Private m_dispidFocus As Integer
        Private m_hostedInNative As Boolean = False
        Private m_wasSetParentCalled As Boolean

        Protected Sub New()
        End Sub

        Protected MustOverride Function CreateControl() As Control 'CONSIDER: this appears to be used only for the default implementation of GetDefaultSize - better way of doing this?  Is this a performance hit?

#Region "IPropertyPageSiteInternal"

        Protected Sub OnStatusChange(ByVal flags As PROPPAGESTATUS) Implements IPropertyPageSiteInternal.OnStatusChange
            If m_PageSite IsNot Nothing Then
                m_PageSite.OnStatusChange(CType(flags, UInteger))
            End If
        End Sub


        Protected Function GetLocaleID() As Integer Implements IPropertyPageSiteInternal.GetLocaleID
            Dim localeID As UInteger
            If m_PageSite IsNot Nothing Then
                m_PageSite.GetLocaleID(localeID)
            Else
                Debug.Fail("PropertyPage site not set")
            End If
            Return CType(localeID, Integer)
        End Function


        ''' <summary>
        ''' Instructs the page site to process a keystroke if it desires.
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This function can be called by a property page to give the site a chance to a process a message
        '''   before the page does.  Return S_OK to indicate we have handled it, S_FALSE to indicate we did not
        '''   process it, and E_NOTIMPL to indicate that the site does not support keyboard processing.
        ''' </remarks>
        Protected Function TranslateAccelerator(ByVal msg As Message) As Integer Implements IPropertyPageSiteInternal.TranslateAccelerator
            'Delegate to the actual site.
            If m_PageSite IsNot Nothing Then
                Dim _msg As Microsoft.VisualStudio.OLE.Interop.MSG
                _msg.hwnd = msg.HWnd
                _msg.message = CType(msg.Msg, UInteger)
                _msg.wParam = msg.WParam
                _msg.lParam = msg.LParam

                Return m_PageSite.TranslateAccelerator(New Microsoft.VisualStudio.OLE.Interop.MSG(0) {_msg})
            Else
                'Returning S_FALSE indicates we have no handled the message
                Return NativeMethods.S_FALSE
            End If

        End Function

        ''' <summary>
        ''' Calls GetService on site first, then on pagesite if service wasn't found
        ''' </summary>
        ''' <param name="ServiceType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetService(ByVal ServiceType As Type) As Object Implements IPropertyPageSiteInternal.GetService
            'Proffer the actual IPropertyPageSite as a service
            If ServiceType.Equals(GetType(IPropertyPageSite)) Then
                Return m_PageSite
            End If

            Dim sp As System.IServiceProvider = TryCast(m_PageSite, System.IServiceProvider)
            If sp IsNot Nothing Then
                Return sp.GetService(ServiceType)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' Returns whether or not the property page hosted in this site should be with 
        '''   immediate-apply mode or not
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property IsImmediateApply() As Boolean Implements IPropertyPageSiteInternal.IsImmediateApply
            Get
                'Current implementation is always immediate-apply for
                '  modeless property pages
                Return True
            End Get
        End Property


#End Region


        Protected Overridable Property DocString() As String
            Get
                Return m_DocString
            End Get
            Set(ByVal value As String)
                m_DocString = value
            End Set
        End Property


        Protected MustOverride ReadOnly Property ControlType() As System.Type

        Protected Overridable ReadOnly Property ControlTypeForResources() As Type
            Get
                Return Me.ControlType
            End Get
        End Property


        Protected MustOverride ReadOnly Property Title() As String

        Protected Overridable Property HelpFile() As String
            Get
                Return m_HelpFile
            End Get
            Set(ByVal value As String)
                m_HelpFile = value
            End Set
        End Property


        Protected Overridable Property HelpContext() As UInteger
            Get
                Return m_HelpContext
            End Get
            Set(ByVal value As UInteger)
                m_HelpContext = value
            End Set
        End Property

        Protected Overridable Property DefaultSize() As System.Drawing.Size
            Get
                If m_DefaultSize.Width = 0 Then 'CONSIDER: Need a better mechanism than assuming the resources are available.  Perf hit?
                    Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(ControlTypeForResources)
                    Dim mySize As System.Drawing.Size = CType(resources.GetObject("$this.Size"), System.Drawing.Size)
                    If mySize.IsEmpty Then
                        'Check for ClientSize if Size is not found
                        mySize = CType(resources.GetObject("$this.ClientSize"), System.Drawing.Size)
                    End If
                    m_DefaultSize = mySize
                End If
                Return m_DefaultSize
            End Get
            Set(ByVal value As System.Drawing.Size)
                m_DefaultSize = value
            End Set
        End Property


        Protected Overridable ReadOnly Property Objects() As Object()
            Get
                Return m_Objects
            End Get
        End Property


        Private Sub IPropertyPage2_Activate(ByVal hWndParent As System.IntPtr, ByVal pRect() As Microsoft.VisualStudio.OLE.Interop.RECT, ByVal bModal As Integer) Implements IPropertyPage2.Activate, IPropertyPage.Activate

            If m_PropPage IsNot Nothing Then
                Debug.Assert(Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetParent(m_PropPage.Handle).Equals(hWndParent), "Property page already Activated with different parent")
                Return
            End If

            m_size.Width = pRect(0).right - pRect(0).left
            m_size.Height = pRect(0).bottom - pRect(0).top

            Create(hWndParent)

            'PERF: Delay making the property page visible until we have moved it to its correct location
            If m_PropPage IsNot Nothing Then
                m_PropPage.SuspendLayout()
            End If
            Move(pRect)
            If m_PropPage IsNot Nothing Then
                m_PropPage.ResumeLayout(True)
            End If

            m_PropPage.Visible = True

            'Make sure we set focus to the active control.  This would normally
            '  happen on window activate automatically, but the property page
            '  isn't hosted until after that.
            m_PropPage.Focus()

            'If the first-focused control on the page is a TextBox, select all its text.  This is normally done
            '  automatically by Windows Forms, but the handling of double-click by the shell before the mouse up
            '  confuses Windows Forms in thinking the textbox was activated by being clicked, so it doesn't happen
            '  in this case.
            If TypeOf m_PropPage Is ContainerControl AndAlso TypeOf DirectCast(m_PropPage, ContainerControl).ActiveControl Is TextBox Then
                DirectCast(DirectCast(m_PropPage, ContainerControl).ActiveControl, TextBox).SelectAll()
            End If

            If m_dispidFocus <> -1 Then
                CType(m_PropPage, IPropertyPageInternal).EditProperty(m_dispidFocus)
            End If

        End Sub


        Private Function IPropertyPage_Apply() As Integer Implements IPropertyPage.Apply
            Apply()
            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK
        End Function

        Private Sub IPropertyPage2_Apply() Implements IPropertyPage2.Apply
            Apply()
        End Sub

        Protected Overridable Sub Apply()
            If Not m_PropPage Is Nothing Then
                Dim page As IPropertyPageInternal = CType(m_PropPage, IPropertyPageInternal)
                page.Apply()
            End If
        End Sub

        Private Sub IPropertyPage2_Deactivate() Implements IPropertyPage2.Deactivate, IPropertyPage.Deactivate
            Deactivate()
        End Sub

        Protected Overridable Sub Deactivate()

            If Not m_PropPage Is Nothing Then

                m_PropPage.SuspendLayout() 'No need for more layouts...
                If m_PropPage.Parent IsNot Nothing AndAlso Not m_hostedInNative Then
                    'We sited ourselves by setting the Windows Forms Parent property
                    m_PropPage.Parent = Nothing
                ElseIf m_wasSetParentCalled Then
                    'We sited ourselves via a native SetParent call
                    Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.SetParent(m_PropPage.Handle, m_PrevParent)
                End If

                m_PropPage.Dispose()
                m_PropPage = Nothing

            End If

        End Sub

        Private Sub IPropertyPage2_GetPageInfo(ByVal pPageInfo() As PROPPAGEINFO) Implements IPropertyPage2.GetPageInfo, IPropertyPage.GetPageInfo
            GetPageInfo(pPageInfo)
        End Sub

        Private Sub GetPageInfo(ByVal pPageInfo() As PROPPAGEINFO)

            If (pPageInfo Is Nothing) Then
                Throw New ArgumentNullException("pPageInfo")
            End If

            pPageInfo(0).cb = 4 + 4 + 8 + 4 + 4 + 4
            pPageInfo(0).dwHelpContext = Me.HelpContext
            pPageInfo(0).pszDocString = Me.DocString
            pPageInfo(0).pszHelpFile = Me.HelpFile
            pPageInfo(0).pszTitle = Me.Title
            pPageInfo(0).SIZE.cx = Me.DefaultSize.Width
            pPageInfo(0).SIZE.cy = Me.DefaultSize.Height

        End Sub


        Private Sub IPropertyPage2_Help(ByVal strHelpDir As String) Implements IPropertyPage2.Help, IPropertyPage.Help
            Help(strHelpDir)
        End Sub

        Protected Overridable Sub Help(ByVal strHelpDir As String)

            If m_PropPage Is Nothing Then
                Return
            End If

            Dim page As IPropertyPageInternal = CType(m_PropPage, IPropertyPageInternal)
            page.Help(strHelpDir)

        End Sub

        Private Function IPropertyPage2_IsPageDirty() As Integer Implements IPropertyPage2.IsPageDirty, IPropertyPage.IsPageDirty
            Return IsPageDirty()
        End Function

        Protected Overridable Function IsPageDirty() As Integer

            If m_PropPage Is Nothing Then
                Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_FALSE
            End If

            Try
                If CType(m_PropPage, IPropertyPageInternal).IsPageDirty() Then
                    Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK
                End If
            Catch ex As Exception
                Debug.Fail("Received an exception from IPropertyPageInternal.IsPageDirty" & vbCrLf & ex.ToString())
                Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.E_FAIL
            End Try

            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_FALSE

        End Function


        Private Sub IPropertyPage2_Move(ByVal pRect() As Microsoft.VisualStudio.OLE.Interop.RECT) Implements IPropertyPage2.Move, IPropertyPage.Move
            Move(pRect)
        End Sub

        Private Sub Move(ByVal pRect() As Microsoft.VisualStudio.OLE.Interop.RECT)
            ' we need to adjust the size of the page if it's autosize or if we're native (in which
            ' case we're going to adjust the size of our secret usercontrol instead) See the Create
            ' for more info about the panel
            If m_PropPage IsNot Nothing AndAlso pRect IsNot Nothing AndAlso pRect.Length <> 0 AndAlso (m_PropPage.AutoSize OrElse m_hostedInNative) Then
                Dim minSize As System.Drawing.Size = m_PropPage.MinimumSize

                ' we have to preserve these to set the size of our scrolling panel
                Dim height As Integer = pRect(0).bottom - pRect(0).top
                Dim width As Integer = pRect(0).right - pRect(0).left

                ' we'll use these to set the page size since they'll respect the minimums
                Dim minRespectingHeight As Integer = pRect(0).bottom - pRect(0).top
                Dim minRespectingWidth As Integer = pRect(0).right - pRect(0).left

                If height < minSize.Height Then
                    minRespectingHeight = minSize.Height
                End If
                If width < minSize.Width Then
                    minRespectingWidth = minSize.Width
                End If

                m_PropPage.Bounds = New Rectangle(pRect(0).left, pRect(0).top, minRespectingWidth, minRespectingHeight)
                ' if we're in native, set our scrolling panel to be the exact size that we were
                ' passed so if we need scroll bars, they show up properly
                If m_hostedInNative Then
                    m_PropPage.Parent.Size = New System.Drawing.Size(width, height)
                End If

            End If

        End Sub


        Private Sub IPropertyPage2_SetObjects(ByVal cObjects As UInteger, ByVal objects() As Object) Implements IPropertyPage2.SetObjects, IPropertyPage.SetObjects
            SetObjects(cObjects, objects)
        End Sub

        Protected Overridable Sub SetObjects(ByVal cObjects As UInteger, ByVal objects() As Object)

            'Debug.Assert seems to have problems during shutdown - don't do the check
            'Debug.Assert((objects Is Nothing AndAlso cObjects = 0) OrElse (objects IsNot Nothing AndAlso objects.Length = cObjects), "Unexpected arguments")

            m_Objects = objects
            Debug.Assert(objects Is Nothing OrElse objects.Length = 0 OrElse objects(0) IsNot Nothing)

            If m_PropPage IsNot Nothing Then

                CType(m_PropPage, IPropertyPageInternal).SetObjects(m_Objects)

            End If

        End Sub


        Private Sub IPropertyPage2_SetPageSite(ByVal PageSite As IPropertyPageSite) Implements IPropertyPage2.SetPageSite, IPropertyPage.SetPageSite
            SetPageSite(PageSite)
        End Sub

        Private Sub SetPageSite(ByVal PageSite As IPropertyPageSite)
            If PageSite IsNot Nothing AndAlso m_PageSite IsNot Nothing Then
                Throw New COMException("PageSite", NativeMethods.E_UNEXPECTED)
            End If

            m_PageSite = PageSite
        End Sub

        Private Sub IPropertyPage2_Show(ByVal nCmdShow As UInteger) Implements IPropertyPage2.Show, IPropertyPage.Show
            Show(nCmdShow)
        End Sub

        Private Sub Show(ByVal nCmdShow As UInteger)

            If (m_PropPage Is Nothing) Then
                Throw New InvalidOperationException("Form not created")
            End If

            ' if we're in native, show/hide our secret scrolling panel too
            ' See Create(hWnd) for more info on where that comes from
            If nCmdShow <> SW_HIDE Then
                If m_hostedInNative Then
                    m_PropPage.Parent.Show()
                End If
                m_PropPage.Show()
                SetHelpContext()
            Else
                If m_hostedInNative Then
                    m_PropPage.Parent.Hide()
                End If
                m_PropPage.Hide()
            End If

        End Sub


        ''' <summary>
        ''' Sets the help context into the help service for this property page.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetHelpContext()
            If Me.m_PageSite IsNot Nothing Then
                Dim sp As System.IServiceProvider = TryCast(m_PageSite, System.IServiceProvider)
                'Note: we have to get the right help service - GetService through the property page's
                '  accomplishes this (it goes through the PropPageDesignerRootDesigner).  There is a
                '  separate help service associated with each window frame.
                Dim HelpService As IHelpService = TryCast(sp.GetService(GetType(IHelpService)), IHelpService)
                If HelpService IsNot Nothing Then
                    Dim HelpKeyword As String = Nothing
                    Dim PropertyPageContext As IPropertyPageInternal = TryCast(m_PropPage, IPropertyPageInternal)
                    If PropertyPageContext IsNot Nothing Then
                        HelpKeyword = PropertyPageContext.GetHelpContextF1Keyword()
                    End If
                    If HelpKeyword Is Nothing Then
                        HelpKeyword = String.Empty
                    End If
                    HelpService.AddContextAttribute("Keyword", HelpKeyword, HelpKeywordType.F1Keyword)
                Else
                    Debug.Fail("Page site doesn't proffer IHelpService")
                End If
            Else
                Debug.Fail("Page site not a service provider - can't set help context for page")
            End If
        End Sub


        ''' <summary>
        ''' Instructs the property page to process the keystroke described in pMsg.
        ''' </summary>
        ''' <param name="pMsg"></param>
        ''' <returns>
        ''' S_OK if the property page handled the accelerator, S_FALSE if the property page handles accelerators, but this one was not useful to it,
        '''   S_NOTIMPL if the property page does not handle accelerators, or E_POINTER if the address in pMsg is not valid. For example, it may be NULL.
        ''' </returns>
        ''' <remarks></remarks>
        Private Function IPropertyPage2_TranslateAccelerator(ByVal pMsg() As Microsoft.VisualStudio.OLE.Interop.MSG) As Integer Implements IPropertyPage2.TranslateAccelerator, IPropertyPage.TranslateAccelerator
            Return TranslateAccelerator(pMsg)
        End Function


        ''' <summary>
        ''' Instructs the property page to process the keystroke described in pMsg.
        ''' </summary>
        ''' <param name="pMsg"></param>
        ''' <returns>
        ''' S_OK if the property page handled the accelerator, S_FALSE if the property page handles accelerators, but this one was not useful to it,
        '''   S_NOTIMPL if the property page does not handle accelerators, or E_POINTER if the address in pMsg is not valid. For example, it may be NULL.
        ''' </returns>
        ''' <remarks></remarks>
        Protected Overridable Function TranslateAccelerator(ByVal pMsg() As Microsoft.VisualStudio.OLE.Interop.MSG) As Integer
            If pMsg Is Nothing Then
                Return NativeMethods.E_POINTER
            End If

            If Not m_PropPage Is Nothing Then
                Dim m As Message = Message.Create(pMsg(0).hwnd, CType(pMsg(0).message, Integer), pMsg(0).wParam, pMsg(0).lParam)
                Dim used As Boolean = False

                'Preprocessing should be passed to the control whose handle the message refers to.
                Dim target As Control = Control.FromChildHandle(m.HWnd)
                If target IsNot Nothing Then
                    used = target.PreProcessMessage(m)
                End If

                If used Then
                    pMsg(0).message = CType(m.Msg, UInteger)
                    pMsg(0).wParam = m.WParam
                    pMsg(0).lParam = m.LParam
                    'Returning S_OK indicates we handled the message ourselves
                    Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK
                End If
            End If

            'Returning S_FALSE indicates we have not handled the message
            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_FALSE
        End Function


        Private Sub IPropertyPage2_EditProperty(ByVal dispid As Integer) Implements IPropertyPage2.EditProperty
            EditProperty(dispid)
        End Sub

        Private Function EditProperty(ByVal dispid As Integer) As Integer
            Dim retVal As Integer = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK

            m_dispidFocus = dispid

            If m_PropPage IsNot Nothing Then
                Dim page As IPropertyPageInternal = CType(m_PropPage, IPropertyPageInternal)
                page.EditProperty(dispid)
            End If

            Return retVal
        End Function

        Private Function Create(ByVal hWndParent As IntPtr) As IntPtr

            m_PropPage = CreateControl()
            Debug.Assert(TypeOf m_PropPage Is IPropertyPageInternal)
            m_PropPage.Visible = False 'PERF: Delay making the property page visible until we have moved it to its correct location
            m_PropPage.SuspendLayout()
            Try

                'PERF: Set the page site before setting up the parent, so that the page has the opportunity
                '  to properly set its Font before being shown
                CType(m_PropPage, IPropertyPageInternal).SetPageSite(CType(Me, IPropertyPageSiteInternal))

                If Not (TypeOf m_PropPage Is IPropertyPageInternal) Then
                    Throw New InvalidOperationException("Control must implement IPropertyPageInternal")
                End If
                m_PrevParent = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetParent(m_PropPage.Handle)

                Common.Switches.TracePDPerf("PropPage.Create: Setting the property page's parent")

                Dim ParentControl As Control = Control.FromHandle(hWndParent)
                Dim AlwaysUseSetParent As Boolean = False
#If DEBUG Then
                AlwaysUseSetParent = Common.Switches.PDAlwaysUseSetParent.Enabled
#End If
                If ParentControl IsNot Nothing AndAlso Not AlwaysUseSetParent Then
                    m_PropPage.Parent = ParentControl
                    Debug.Assert(m_PropPage.Parent IsNot Nothing, "Huh?  Deactivate() logic depends on this.")
                Else
                    'Not a managed window, use the win32 api method
                    m_hostedInNative = True
                    ' in order to have scroll bars properly appear in large fonts, wrap
                    ' the page in a usercontrol (since it supports AutoScroll) that won't
                    ' scale with fonts. Move(rect) will set the proper size.
                    Dim sizingParent As New UserControl()
                    sizingParent.AutoScaleMode = AutoScaleMode.None
                    sizingParent.AutoScroll = True
                    sizingParent.Text = "SizingParent" 'For debugging purposes (Spy++)
                    m_PropPage.Parent = sizingParent
                    Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.SetParent(sizingParent.Handle, hWndParent)
                    m_wasSetParentCalled = True
                    Debug.Assert(m_PropPage.Parent Is Nothing OrElse AlwaysUseSetParent, "Huh?  Deactivate() logic depends on this.")
                End If

                'Site the undo manager if we have one and the page supports it
                If (m_PropPageUndoSite IsNot Nothing) AndAlso (TypeOf m_PropPage Is IVsProjectDesignerPage) Then
                    CType(m_PropPage, IVsProjectDesignerPage).SetSite(m_PropPageUndoSite)
                End If

                'If the SetObjects call was cached, we need to do the SetObjects now
                If (Not m_Objects Is Nothing) AndAlso (m_Objects.Length > 0) Then
                    Dim page As IPropertyPageInternal = CType(m_PropPage, IPropertyPageInternal)
                    page.SetObjects(m_Objects)
                End If

                Return m_PropPage.Handle

            Finally
                'We don't want to lay out until we've set our size correctly (in IPropertyPage2_Activate)
                m_PropPage.ResumeLayout(False)
            End Try
        End Function


#Region "IVsProjectDesignerPage"
        Private m_PropPageUndoSite As IVsProjectDesignerPageSite

        ''' <summary>
        ''' Gets the current value for the given property.  This value will be serialized using the binary serializer, and saved for
        '''   use later by Undo and Redo operations.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetProperty(ByVal PropertyName As String) As Object Implements IVsProjectDesignerPage.GetProperty
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Return Page.GetProperty(PropertyName)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' Tells the property page to set the given value for the given property.  This is called during Undo and Redo operations.  The
        '''   page should also update its UI for the given property.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub SetProperty(ByVal PropertyName As String, ByVal Value As Object) Implements IVsProjectDesignerPage.SetProperty
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If (Page IsNot Nothing) Then
                Page.SetProperty(PropertyName, Value)
            End If
        End Sub


        ''' <summary>
        ''' Notifies the property page of the IVsProjectDesignerPageSite
        ''' </summary>
        ''' <param name="site"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub SetSite(ByVal site As IVsProjectDesignerPageSite) Implements IVsProjectDesignerPage.SetSite
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Page.SetSite(site)
            End If
            m_PropPageUndoSite = site
        End Sub

        ''' <summary>
        ''' Returns true if the given property supports returning and setting multiple values at the same time in order to support
        '''   Undo and Redo operations when multiple configurations are selected by the user.  This function should always return the
        '''   same value for a given property (i.e., it does not depend on whether multiple configurations have currently been passed in
        '''   to SetObjects, but simply whether this property supports multiple-value undo/redo).
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function SupportsMultipleValueUndo(ByVal PropertyName As String) As Boolean Implements IVsProjectDesignerPage.SupportsMultipleValueUndo
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Return Page.SupportsMultipleValueUndo(PropertyName)
            Else
                Return False
            End If
        End Function


        ''' <summary>
        ''' Gets the current values for the given property, one for each of the objects (configurations) that may be affected by a property
        '''   change and need to be remembered for Undo purposes.  The set of objects passed back normally should be the same objects that
        '''   were given to the page via SetObjects (but this is not required).
        '''   This function is called for a property if SupportsMultipleValueUndo returns true for that property.  If 
        ''' SupportsMultipleValueUndo returns false, or this function returns False, then GetProperty is called instead.
        ''' </summary>
        ''' <param name="PropertyName">The property to read values from</param>
        ''' <param name="Objects">[out] The set of objects (configurations) whose properties should be remembered by Undo</param>
        ''' <param name="Values">[out] The current values of the property for each configuration (corresponding to Objects)</param>
        ''' <returns>True if the property has multiple values to be read.</returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetPropertyMultipleValues(ByVal PropertyName As String, ByRef Objects As Object(), ByRef Values As Object()) As Boolean Implements IVsProjectDesignerPage.GetPropertyMultipleValues
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Return Page.GetPropertyMultipleValues(PropertyName, Objects, Values)
            Else
                Objects = Nothing
                Values = Nothing
                Return False
            End If
        End Function


        ''' <summary>
        ''' Tells the property page to set the given values for the given properties, one for each of the objects (configurations) passed
        '''   in.  This property is called if the corresponding previous call to GetPropertyMultipleValues succeeded, otherwise
        '''   SetProperty is called instead.
        ''' Note that the Objects values are not required to be a subset of the objects most recently passed in through SetObjects.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Objects"></param>
        ''' <param name="Values"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub SetPropertyMultipleValues(ByVal PropertyName As String, ByVal Objects() As Object, ByVal Values() As Object) Implements IVsProjectDesignerPage.SetPropertyMultipleValues
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Page.SetPropertyMultipleValues(PropertyName, Objects, Values)
            End If
        End Sub


        ''' <summary>
        ''' Finish all pending validations
        ''' </summary>
        ''' <returns>Return false if validation failed, and the customer wants to fix it (not ignore it)</returns>
        ''' <remarks></remarks>
        Public Function FinishPendingValidations() As Boolean Implements IVsProjectDesignerPage.FinishPendingValidations
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Return Page.FinishPendingValidations()
            End If
            Return True
        End Function


        ''' <summary>
        ''' Called when the page is activated or deactivated
        ''' </summary>
        ''' <param name="activated"></param>
        ''' <remarks></remarks>
        Public Sub OnWindowActivated(ByVal activated As Boolean) Implements IVsProjectDesignerPage.OnActivated
            Dim Page As IVsProjectDesignerPage = TryCast(m_PropPage, IVsProjectDesignerPage)
            If Page IsNot Nothing Then
                Page.OnActivated(activated)
            End If
        End Sub

#End Region

#If False Then
#Region "IVsDocDataContainer"
        ''' <summary>
        ''' Provides a mechanism for property pages to expose docdatas that need to be saved in the Project Designer
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetDocDataCookies() As UInteger() Implements IVsDocDataContainer.GetDocDataCookies
            If TypeOf m_PropPage Is IVsDocDataContainer Then
                Return DirectCast(m_PropPage, IVsDocDataContainer).GetDocDataCookies()
            End If

            Return New UInteger() {}
        End Function
#End Region
#End If

    End Class

#End Region


#Region "VBPropPageBase"

    Public MustInherit Class VBPropPageBase
        Inherits PropPageBase

        Protected Sub New()
            'The follow entry is a dummy value - without something stuffed in here the
            '  property page will NOT show the help button. The F1 keyword is the real 
            '  help context
            MyBase.New()
            Me.HelpContext = 1
            Me.HelpFile = "VBREF.CHM"
        End Sub

    End Class

#End Region

End Namespace

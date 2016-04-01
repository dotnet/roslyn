' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public Class ProjectDesignerTabControl
        Inherits ContainerControl

        'A list of all buttons currently contained by the control
        Private _buttonCollection As New List(Of ProjectDesignerTabButton)

        Private _renderer As ProjectDesignerTabRenderer 'The renderer to use for painting.  May not be Nothing.
        Private _selectedItem As ProjectDesignerTabButton ' Currently-seleted item.  May be Nothing.
        Private _hoverItem As ProjectDesignerTabButton ' Currently-hovered item.  May be Nothing.
        Private _hostingPanel As Panel

        'The overflow button for displaying tabs which can't currently fit
        Public WithEvents OverflowButton As Button

        'The overflow menu that gets displayed when the overflow button is pressed
        Private _overflowMenu As New ContextMenuStrip
        Private _overflowTooltip As New ToolTip

        'Backs up the ServiceProvider property
        Private _serviceProvider As IServiceProvider

        ''' <summary>
        '''  Listen for font/color changes from the shell
        ''' </summary>
        ''' <remarks></remarks>
        Private WithEvents _broadcastMessageEventsHelper As Common.ShellUtil.BroadcastMessageEventsHelper

        Private ReadOnly _defaultOverflowBorderColor As Color = SystemColors.MenuText
        Private ReadOnly _defaultOverflowHoverColor As Color = SystemColors.Highlight



#Region " Component Designer generated code "


        Public Sub New()
            _renderer = New ProjectDesignerTabRenderer(Me)

            SuspendLayout()
            Try
                ' This call is required by the Component Designer.
                InitializeComponent()

                Initialize()
            Finally
                ResumeLayout()
            End Try
        End Sub 'New


        'Control override dispose to clean up the component list.
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
                If _broadcastMessageEventsHelper IsNot Nothing Then
                    _broadcastMessageEventsHelper.Dispose()
                    _broadcastMessageEventsHelper = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub 'Dispose

        'Required by the Control Designer
        Private _components As System.ComponentModel.IContainer


        ' NOTE: The following procedure is required by the Component Designer
        ' It can be modified using the Component Designer.  Do not modify it
        ' using the code editor.
        '<System.Diagnostics.DebuggerNonUserCode()> 
        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me._components = New System.ComponentModel.Container

            'No scrollbars
            Me.AutoScroll = False

            Me.ResumeLayout()
        End Sub

#End Region

        ''' <summary>
        ''' Initialization
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub Initialize()
            _hostingPanel = New Panel()
            _hostingPanel.BackColor = PropertyPages.PropPageUserControlBase.PropPageBackColor
            _hostingPanel.Visible = True
            _hostingPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right
            _hostingPanel.AutoScroll = True
            _hostingPanel.Text = "HostingPanel" 'For debugging
            _hostingPanel.AccessibleName = SR.GetString(SR.APPDES_HostingPanelName)

            'Add any initialization after the InitializeComponent() call
            '
            Me.Name = "DesignerTabControl"
            Me.Padding = New System.Windows.Forms.Padding(0)
            Me.Size = New System.Drawing.Size(144, 754)
            Me.TabIndex = 0
            Me.DoubleBuffered = True
            Me.Controls.Add(_hostingPanel)

            SetUpOverflowButton()
        End Sub 'InitTabInfo


        ''' <summary>
        ''' Create the tab overflow button.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetUpOverflowButton()
            'Note: the renderer will position the button, so we don't need to.
            OverflowButton = New ImageButton("Microsoft.VisualStudio.Editors.ApplicationDesigner.OverflowImage", Color.Lime)
            With OverflowButton
                .Name = "OverflowButton"
                .Text = ""
                .AccessibleName = SR.GetString(SR.APPDES_OverflowButton_AccessibilityName)
                .FlatAppearance.BorderColor = _defaultOverflowBorderColor
                .FlatAppearance.MouseOverBackColor = _defaultOverflowHoverColor
                .Size = New Size(18, 18)
                .Visible = False 'Don't show it until we need it
                _overflowTooltip.SetToolTip(OverflowButton, SR.GetString(SR.APPDES_OverflowButton_Tooltip))

            End With
            MyBase.Controls.Add(OverflowButton)
        End Sub


        ''' <summary>
        ''' The service provider to use when querying for services related to hosting this control
        '''   instead of the Visual Studio shell.
        ''' Default is Nothing.  If not set, then behavior will be independent of the Visual Studio
        '''   shell (e.g., colors will default to system or fallback colors instead of using the
        '''   shell's color service). 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ServiceProvider() As IServiceProvider
            Get
                Return _serviceProvider
            End Get
            Set(ByVal value As IServiceProvider)
                _serviceProvider = value
                _renderer.ServiceProvider = value

                If _serviceProvider IsNot Nothing Then
                    OnGotServiceProvider()
                End If
            End Set
        End Property


        ''' <summary>
        ''' Called when a non-empty service provider is given to the control.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub OnGotServiceProvider()
            'We now should have access to the color provider service
            Dim vsUiShell As IVsUIShell = Nothing
            Dim vsUiShell2 As IVsUIShell2 = DirectCast(vsUiShell, IVsUIShell2)
            If _serviceProvider IsNot Nothing Then
                vsUiShell = DirectCast(_serviceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                If vsUiShell IsNot Nothing Then
                    vsUiShell2 = DirectCast(vsUiShell, IVsUIShell2)
                End If
            End If

            OverflowButton.FlatAppearance.BorderColor = AppDesCommon.ShellUtil.GetColor(vsUiShell2, Shell.Interop.__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_BORDER, _defaultOverflowBorderColor)
            OverflowButton.FlatAppearance.MouseOverBackColor = AppDesCommon.ShellUtil.GetColor(vsUiShell2, Shell.Interop.__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_HOVER, _defaultOverflowHoverColor)

            If _broadcastMessageEventsHelper IsNot Nothing Then
                _broadcastMessageEventsHelper.Dispose()
                _broadcastMessageEventsHelper = Nothing
            End If
            If _serviceProvider IsNot Nothing Then
                _broadcastMessageEventsHelper = New Common.ShellUtil.BroadcastMessageEventsHelper(_serviceProvider)
            End If
        End Sub


        ''' <summary>
        ''' Returns an enumerable set of tab buttons
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property TabButtons() As IEnumerable(Of ProjectDesignerTabButton)
            Get
                Return _buttonCollection
            End Get
        End Property


        ''' <summary>
        ''' Clears all the tab buttons off of the control
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub ClearTabs()
            _buttonCollection.Clear()
            InvalidateLayout()
        End Sub


        ''' <summary>
        ''' Gets a tab button by index
        ''' </summary>
        ''' <param name="index"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetTabButton(ByVal index As Integer) As ProjectDesignerTabButton
            Return _buttonCollection(index)
        End Function


        ''' <summary>
        ''' The number of tab buttons, including those not currently visible
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property TabButtonCount() As Integer
            Get
                Return _buttonCollection.Count
            End Get
        End Property


        ''' <summary>
        ''' Get the panel that is used to host controls on the right-hand side
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property HostingPanel() As Panel
            Get
                Return _hostingPanel
            End Get
        End Property


        ''' <summary>
        ''' Perform layout
        ''' </summary>
        ''' <param name="levent"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnLayout(ByVal levent As LayoutEventArgs)
            Common.Switches.TracePDPerfBegin(levent, "DesignerTabControl.OnLayout()")

            _renderer.PerformLayout() 'This can affect the layout of other controls on this page
            MyBase.OnLayout(levent)

            Invalidate()
            Common.Switches.TracePDPerfEnd("DesignerTabControl.OnLayout()")
        End Sub 'OnLayout


        ''' <summary>
        ''' Causes the layout to be refreshed
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub InvalidateLayout()
            PerformLayout()
        End Sub


        ''' <summary>
        ''' Adds a new tab to the control
        ''' </summary>
        ''' <param name="Title">The user-friendly, localizable text for the tab that will be displayed.</param>
        ''' <param name="AutomationName">Non-localizable name to be used for QA automation.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function AddTab(ByVal Title As String, ByVal AutomationName As String) As Integer
            SuspendLayout()
            Dim newIndex As Integer
            Try
                Dim Button As New ProjectDesignerTabButton()
                Button.Text = Title
                Button.Name = AutomationName
                _buttonCollection.Add(Button)
                newIndex = _buttonCollection.Count - 1
                Controls.Add(Button)
                Button.SetIndex(newIndex)
                Button.Visible = True

                If SelectedItem Is Nothing Then
                    SelectedItem = Button
                End If
            Finally
                ResumeLayout()
            End Try

            Return newIndex
        End Function 'AddTab


        ''' <summary>
        ''' Tracks the last item for paint logic
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property HoverItem() As ProjectDesignerTabButton
            Get
                Return _hoverItem
            End Get
        End Property


        ''' <summary>
        ''' Currently selected button
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property SelectedItem() As ProjectDesignerTabButton
            Get
                Return _selectedItem
            End Get
            Set(ByVal value As ProjectDesignerTabButton)
                Dim oldSelectedItem As ProjectDesignerTabButton = Nothing

                If _selectedItem Is value Then
                    Return
                End If
                If _selectedItem IsNot Nothing Then
                    oldSelectedItem = _selectedItem
                    _selectedItem.Invalidate()
                End If
                _selectedItem = value

                'If the selected item was the hover item, then clear it
                If _hoverItem Is value Then
                    _hoverItem = Nothing
                End If
                If _selectedItem IsNot Nothing Then
                    _selectedItem.Visible = True 'Must be visible in order to properly get the focus
                    If _selectedItem.CanFocus AndAlso SelectedItem.TabStop Then
                        Common.Switches.TracePDFocus(TraceLevel.Warning, "ProjectDesignerTabControl.set_SelectedItem - Setting focus to selected tab")
                        _selectedItem.Focus()
                    End If
                    _selectedItem.Invalidate()
                End If

                ' Fire state change event to notify the screen reader...
                If oldSelectedItem IsNot Nothing Then
                    CType(oldSelectedItem.AccessibilityObject, ControlAccessibleObject).NotifyClients(AccessibleEvents.StateChange)
                End If
                If _selectedItem IsNot Nothing Then
                    CType(_selectedItem.AccessibilityObject, ControlAccessibleObject).NotifyClients(AccessibleEvents.StateChange)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Currently selected button
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property SelectedIndex() As Integer
            Get
                If _selectedItem Is Nothing Then
                    Return -1
                End If

                Return _selectedItem.ButtonIndex
            End Get
            Set(ByVal value As Integer)
                If value = -1 Then
                    SelectedItem = Nothing
                Else
                    SelectedItem = _buttonCollection(value)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Keep painting from happening during WM_PAINT.  We'll paint everything during OnPaintBackground.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        End Sub


        ''' <summary>
        ''' Everything will paint in the background, except buttons which handle their own painting
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
            Renderer.RenderBackground(e.Graphics)
        End Sub


        ''' <summary>
        ''' Occurs when a button is clicked.
        ''' </summary>
        ''' <param name="item">The tab button which has been clicked.</param>
        ''' <remarks></remarks>
        Public Overridable Sub OnItemClick(ByVal item As ProjectDesignerTabButton)
            SelectedItem = item
        End Sub


        ''' <summary>
        ''' Occurs when the mouse enters a button's area
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        ''' <remarks></remarks>
        Public Sub OnItemEnter(ByVal e As EventArgs, ByVal item As ProjectDesignerTabButton)
            If _hoverItem IsNot item Then
                _hoverItem = item
                item.Invalidate()
            End If
        End Sub


        ''' <summary>
        ''' Occurs when the mouse leaves a button's area
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        ''' <remarks></remarks>
        Public Sub OnItemLeave(ByVal e As EventArgs, ByVal item As ProjectDesignerTabButton)
            If _hoverItem Is item Then
                _hoverItem = Nothing
                item.Invalidate()
            End If
        End Sub


        ''' <summary>
        ''' Occurs when a tab button gets focus
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        ''' <remarks></remarks>
        Public Overridable Sub OnItemGotFocus(ByVal e As EventArgs, ByVal item As ProjectDesignerTabButton)
        End Sub

        ''' <summary>
        ''' Create customized accessible object
        ''' </summary>
        Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
            Return New DesignerTabControlAccessibleObject(Me)
        End Function


        ''' <summary>
        ''' Retrieves the renderer used for this tab control
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Renderer() As ProjectDesignerTabRenderer
            Get
                Return _renderer
            End Get
        End Property

        ''' <summary>
        ''' Overflow button has been clicked.  Bring up menu of non-visible tab items.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub OverflowButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles OverflowButton.Click
            'Set up to use VS colors
            If _serviceProvider IsNot Nothing Then
                Dim uiSvc As IUIService = DirectCast(_serviceProvider.GetService(GetType(IUIService)), IUIService)
                'Set up the menu font and toolstrip renderer
                If uiSvc IsNot Nothing Then
                    Dim Renderer As ToolStripProfessionalRenderer = DirectCast(uiSvc.Styles("VsRenderer"), ToolStripProfessionalRenderer)
                    If Renderer IsNot Nothing Then
                        _overflowMenu.Renderer = Renderer
                    End If

                    Dim NewFont As Font = DirectCast(uiSvc.Styles("DialogFont"), Drawing.Font)
                    If NewFont IsNot Nothing Then
                        _overflowMenu.Font = NewFont
                    End If

                    Dim CommandBarTextActiveColor As Color = DirectCast(uiSvc.Styles("CommandBarTextActive"), Color)
                    _overflowMenu.ForeColor = CommandBarTextActiveColor

                    Dim CommandBarMenuBackgroundGradientEndColor As Color = DirectCast(uiSvc.Styles("CommandBarMenuBackgroundGradientEnd"), Color)
                    _overflowMenu.BackColor = CommandBarMenuBackgroundGradientEndColor

                End If
            End If

            'Remove old menu items and handlers
            For Each Item As ToolStripMenuItem In _overflowMenu.Items
                RemoveHandler Item.Click, AddressOf OverflowMenuItemClick
            Next
            _overflowMenu.Items.Clear()

            'Create a menu structure for the buttons, and let the user select from that.  We include in the overflow
            '  menu only buttons which are not currently visible in the available space.
            For Each button As ProjectDesignerTabButton In TabButtons
                If Not button.Visible Then
                    Dim MenuItem As New ToolStripMenuItem()
                    With MenuItem
                        .Text = button.TextWithDirtyIndicator
                        .Name = "Overflow_" & button.Name 'For automation - should not be localized
                        AddHandler .Click, AddressOf OverflowMenuItemClick
                        .Tag = button
                    End With
                    _overflowMenu.Items.Add(MenuItem)
                End If
            Next

            If _overflowMenu.Items.Count > 0 Then
                'Show the overflow menu
                Dim OverflowMenuDistanceFromButtonButtonLeft As Size = New Size(-2, 2)
                _overflowMenu.Show(Me, _
                    OverflowButton.Left + OverflowMenuDistanceFromButtonButtonLeft.Width, _
                    OverflowButton.Bottom + OverflowMenuDistanceFromButtonButtonLeft.Height)
            Else
                Debug.Fail("How did the overflow button get clicked if there are no items to show in the overflow area?")
            End If
        End Sub


        ''' <summary>
        ''' Happens when the user clicks on an entry in the overflow menu.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub OverflowMenuItemClick(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim MenuItem As ToolStripMenuItem = DirectCast(sender, ToolStripMenuItem)
            Dim Button As ProjectDesignerTabButton = DirectCast(MenuItem.Tag, ProjectDesignerTabButton)
            Debug.Assert(Button IsNot Nothing)
            If Button IsNot Nothing Then
                'Click it
                OnItemClick(Button)

                'We need to ensure that the selected button becomes and stays visible in the tabs now.
                '  We do that by setting it as the preferred button for the switchable slot.
                '(This must be done after OnItemClick, because otherwise the selected item will be
                '  wrong and the renderer gives preference to the selected item.)
                _renderer.PreferredButtonForSwitchableSlot = Button
            End If
        End Sub

        ''' <summary>
        ''' We've gotta tell the renderer whenver the system colors change...
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wparam"></param>
        ''' <param name="lparam"></param>
        ''' <remarks></remarks>
        Private Sub m_BroadcastMessageEventsHelper_BroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr) Handles _broadcastMessageEventsHelper.BroadcastMessage
            Select Case msg
                Case AppDesInterop.win.WM_PALETTECHANGED, AppDesInterop.win.WM_SYSCOLORCHANGE, AppDesInterop.win.WM_THEMECHANGED
                    _renderer.CreateGDIObjects(True)
            End Select
        End Sub

        '*************************************************
        '* Private Class ImageButton
        '*************************************************

        ''' <summary>
        ''' A button that has these characteristics:
        '''   a) contains a transparent image from resources
        '''   b) has flatstyle
        '''   c) shows a border only when the mouse hovers over it
        ''' </summary>
        ''' <remarks></remarks>
        Private Class ImageButton
            Inherits Button

            Public Sub New()
                'We don't want it to get focus.  Also, if we don't do this, it will have
                '  a border size too large when it does obtain focus (or thinks it does).  
                '  Setting TabStop=False itsn't enough.
                SetStyle(ControlStyles.Selectable, False)

                MyBase.FlatStyle = System.Windows.Forms.FlatStyle.Flat
                MyBase.FlatAppearance.BorderSize = 0 'No border until the mouse is over it
                MyBase.TabStop = True
                MyBase.BackColor = Color.Transparent 'Need to let gradients show through the image when not hovered over
            End Sub

            Public Sub New(ByVal ImageResourceId As String, ByVal TransparentColor As Color)
                Me.New()

                'Get the image and make it transparent
                Dim Image As Image = AppDesCommon.GetManifestBitmapTransparent(ImageResourceId, TransparentColor, GetType(Microsoft.VisualStudio.Editors.ApplicationDesigner.ProjectDesignerTabControl).Assembly)
                MyBase.Image = Image
            End Sub

            ''' <summary>
            ''' Occurs when the mouse enters the button
            ''' </summary>
            ''' <param name="e"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnMouseEnter(ByVal e As System.EventArgs)
                MyBase.OnMouseEnter(e)

                'No border unless the mouse is over the button
                FlatAppearance.BorderSize = 1
                BackColor = FlatAppearance.MouseOverBackColor
            End Sub


            ''' <summary>
            ''' Occurs when the mouse leaves the button
            ''' </summary>
            ''' <param name="e"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
                MyBase.OnMouseLeave(e)

                'No border unless the mouse is over the button
                FlatAppearance.BorderSize = 0
                BackColor = Color.Transparent
            End Sub

        End Class

        '''<summary>
        ''' custom build accessible object class
        '''</summary>
        Private Class DesignerTabControlAccessibleObject
            Inherits ControlAccessibleObject

            ' button which this accessible object belongs to
            Private _tabControl As ProjectDesignerTabControl

            Public Sub New(ByVal owner As ProjectDesignerTabControl)
                MyBase.New(owner)
                _tabControl = owner
            End Sub

            ''' <summary>
            ''' Description
            ''' </summary>
            Public Overrides ReadOnly Property Description() As String
                Get
                    Return SR.GetString(SR.APPDES_TabListDescription)
                End Get
            End Property

            ''' <summary>
            ''' Role - it is a tab List
            ''' </summary>
            Public Overrides ReadOnly Property Role() As AccessibleRole
                Get
                    Return AccessibleRole.PageTabList
                End Get
            End Property

            ''' <summary>
            ''' Value - the name of the active page
            ''' </summary>
            Public Overrides Property Value() As String
                Get
                    If _tabControl.SelectedItem IsNot Nothing Then
                        Return _tabControl.SelectedItem.Text
                    Else
                        Return Nothing
                    End If
                End Get
                Set(ByVal value As String)
                End Set
            End Property

        End Class

    End Class

End Namespace

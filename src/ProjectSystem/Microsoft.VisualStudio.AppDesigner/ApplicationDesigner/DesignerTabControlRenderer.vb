' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms


Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public NotInheritable Class ProjectDesignerTabRenderer


        ' +---------+  +---------------------------------+
        ' |Selected  > |                                 |
        ' +---------+  |                                 |
        ' |Hover    |  |                                 |
        ' +---------+  |                                 |
        '  Standard    |                                 |
        '              |                                 |
        '  Standard    |                                 |
        '              |         Hosting Panel           |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              |                                 |
        '              +---------------------------------+

#Region "Colors"

        ' Background of the entire control 
        Private _controlBackgroundColor As Color

        ' Tab button foreground/background 
        Private _buttonForegroundColor As Color
        Private _buttonBackgroundColor as Color

        ' Tab button selected foreground/background 
        Private _selectedButtonForegroundColor As Color
        Private _selectedButtonBackgroundColor as Color

        ' Tab button hover foreground/background
        Private _hoverButtonForegroundColor As Color
        Private _hoverButtonBackgroundColor as Color

#End Region

#Region "GDI objects"

        ' Background of the control 
        Private _controlBackgroundBrush As SolidBrush

        ' Tab button foreground/background 
        Private _buttonBackgroundBrush as Brush

        ' Tab button selected foreground/background 
        Private _selectedButtonBackgroundBrush as Brush

        ' Tab button hover foreground/background
        Private _hoverButtonBackgroundBrush as Brush

#End Region

        ' Category guid for project designer theme colors 
        Private Shared ReadOnly s_projectDesignerThemeCategory As New Guid("ef1a2d2c-5d16-4ddb-8d04-79d0f6c1c56e")

        'The left X position for all tab buttons
        Private _buttonsLocationX As Integer
        'The top Y position for the topmost button
        Private _buttonsLocationY As Integer 'Start y location
        'Smallest text width to allow for in the buttons, even if all of the buttons have text wider than this value
        Private Const s_minimumButtonTextWidthSpace As Integer = 25

        'The width and height to use for all of the buttons.
        Private Const s_defaultButtonHeight = 24
        Private _buttonHeight As Integer
        Private _buttonWidth As Integer

        Private Const s_buttonTextLeftOffset As Integer = 8 'Padding from left side of the button to where the tab text is drawn
        Private Const s_buttonTextRightSpace As Integer = 8 'Extra space to leave after tab text
        Private _visibleButtonSlots As Integer '# of button positions we are currently displaying (min = 1), the rest go into overflow when not enough room
        Private _buttonPagePadding As Padding

        'The width/height of the downward-slanting line underneath the buttons (not including the two curved ends' (arcs') width/height)

        Private Const s_buttonBorderWidth As Integer = 1   'Thickness of each half of the separators between buttons

        Private Const s_overflowButtonTopOffset As Integer = 2 'Offset of overflow button (the button's edge, not the glyph inside it) from the bottom of the bottommost button
        Private Const s_overflowButtonRightOffset As Integer = 2 'Offset of right edge of overflow button from vertical line 3

        Private _tabControlRect As Rectangle 'The entire area of the tab control, including the tabs and panel area

        'Pointer to the ProjectDesignerTabControl which owns this renderer.  May not be null
        Private _owner As ProjectDesignerTabControl

        'The service provider to use.  May be Nothing
        Private _serviceProvider As IServiceProvider

        'Backs the PreferredButtonInSwitchableSlot property
        Private _preferredButtonForSwitchableSlot As ProjectDesignerTabButton

        'True if currently in UpdateCacheStatus
        Private _updatingCache As Boolean

        'True if currently in CreateGDIObjects
        Private _creatingGDIObjects As Boolean

        'True if the GDI objects have been created
        Private _GDIObjectsCreated As Boolean

        'True if the gradient brushes have been created
        Private _gradientBrushesCreated As Boolean

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="owner">The ProjectDesignerTabControl control which owns and contains this control.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal owner As ProjectDesignerTabControl)
            If owner Is Nothing Then
                Throw New ArgumentNullException("owner")
            End If
            _owner = owner

            _buttonPagePadding = New Padding(9, 5, 9, 5)
        End Sub 'New


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
                If _GDIObjectsCreated Then
                    'If we've already created GDI stuff/layout, we will need to re-create them.  Otherwise
                    '  we just wait for on-demand.
                    CreateGDIObjects(True)
                End If
                _owner.Invalidate()
            End Set
        End Property


        ''' <summary>
        ''' Attempts to obtain a point to an IVsUIShell2 interface.
        ''' </summary>
        ''' <value>The IVsUIShell2 service if found, otherwise null</value>
        ''' <remarks>Uses the publicly-obtained ServiceProvider property, if it was set.</remarks>
        Private ReadOnly Property VsUIShell2Service() As IVsUIShell2
            Get
                Dim sp As IServiceProvider = ServiceProvider
                If sp IsNot Nothing Then
                    Dim vsUiShell As IVsUIShell = DirectCast(sp.GetService(GetType(IVsUIShell)), IVsUIShell)
                    If vsUiShell IsNot Nothing Then
                        Dim uIShell2Service As IVsUIShell2 = DirectCast(vsUiShell, IVsUIShell2)
                        Return uIShell2Service
                    End If
                End If

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Attempts to obtain the IVsUIShell5 interface.
        ''' </summary>
        ''' <value>The IVsUIShell5 service if found, otherwise null</value>
        ''' <remarks>Uses the publicly-obtained ServiceProvider property, if it was set.</remarks>
        Private ReadOnly Property VsUIShell5Service() As IVsUIShell5
            Get
                Dim sp As IServiceProvider = ServiceProvider
                If sp IsNot Nothing Then
                    Dim vsUiShell As IVsUIShell = TryCast(sp.GetService(GetType(IVsUIShell)), IVsUIShell)
                    If vsUiShell IsNot Nothing Then
                        Dim uIShell2Service As IVsUIShell5 = TryCast(vsUiShell, IVsUIShell5)
                        Return uIShell2Service
                    End If
                End If

                Return Nothing
            End Get
        End Property


        ''' <summary>
        ''' The button, if any, which is the preferred button to show up in the switchable slot.
        '''   The switchable slot is the last button position if some buttons are not visible.  The
        '''   button that is shown in this slot may change, since when the user selects a button
        '''   from the overflow menu, we have to be able to show that button and show that it's selected.
        '''   We do that by placing it into the switchable slot and not showing the button that was
        '''   previously there.
        ''' Note: this is not necessarily the same as the currently-selected button, because once a button
        '''   is pulled into the switchable slot, we want it to stay there unless we have to change it.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property PreferredButtonForSwitchableSlot() As ProjectDesignerTabButton
            Get
                Return _preferredButtonForSwitchableSlot
            End Get
            Set(ByVal value As ProjectDesignerTabButton)
                If value IsNot _preferredButtonForSwitchableSlot Then
                    _preferredButtonForSwitchableSlot = value
                    _owner.Invalidate()
                    UpdateCacheState()
                    value.Invalidate()
                End If
            End Set
        End Property


        ''' <summary>
        ''' Creates GDI objects that we keep around, if they have not already been created
        ''' </summary>
        ''' <param name="ForceUpdate">If True, the GDI objects are updated if they have already been created.</param>
        ''' <remarks></remarks>
        Public Sub CreateGDIObjects(Optional ByVal ForceUpdate As Boolean = False)
            If _creatingGDIObjects Then
                Exit Sub
            End If
            If _GDIObjectsCreated AndAlso Not ForceUpdate Then
                Exit Sub
            End If

            Try
                _creatingGDIObjects = True

                'Get Colors from the shell
                Dim VsUIShell As IVsUIShell5 = VsUIShell5Service

                'NOTE: The defaults given here are the system colors.  Since this control is not currently
                '  being used outside of Visual Studio, this is okay because we don't expect to fail to get colors from the
                '  color service.  If we make the control available as a stand-alone component, we would need to add logic to
                '  do the right thing when not hosted inside Visual Studio, and change according to the theme.


                _controlBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "Background", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Control)

                _buttonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "CategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.ControlText)
                _buttonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "CategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Control)

                _selectedButtonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "SelectedCategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.HighlightText)
                _selectedButtonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "SelectedCategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Highlight)

                _hoverButtonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "MouseOverCategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.HighlightText)
                _hoverButtonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, s_projectDesignerThemeCategory, "MouseOverCategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.HotTrack)

                'Get GDI objects
                _controlBackgroundBrush = New SolidBrush(_controlBackgroundColor)
                _buttonBackgroundBrush = New SolidBrush(_buttonBackgroundColor)
                _selectedButtonBackgroundBrush = New SolidBrush(_selectedButtonBackgroundColor)
                _hoverButtonBackgroundBrush = New SolidBrush(_hoverButtonBackgroundColor)

                If ForceUpdate Then
                    'Colors may have changed, need to update state.  Also, the gradient brushes are
                    '  created in UpdateCacheState (because they depend on button sizes), so we need
                    '  to create them, too
                    _owner.Invalidate()
                    UpdateCacheState()
                End If

                _GDIObjectsCreated = True
            Finally
                _creatingGDIObjects = False
            End Try
        End Sub




#Region "Size/position helpers"


        ''' <summary>
        ''' Performs layout for the associated tab control
        ''' </summary>
        Public Sub PerformLayout()
            Common.Switches.TracePDPerfBegin("DesignerTabControlRenderer.PerformLayout()")
            UpdateCacheState()
            Common.Switches.TracePDPerfEnd("DesignerTabControlRenderer.PerformLayout()")
        End Sub


        ''' <summary>
        ''' Computes all layout-related, cached state, if it is not currently valid.
        '''   Otherwise immediately returns.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub UpdateCacheState()
            If _updatingCache Then
                Exit Sub
            End If
            If ServiceProvider Is Nothing Then
                Exit Sub
            End If

            _updatingCache = True
            Try
                CreateGDIObjects()

                Dim rect As Rectangle = _owner.Bounds

                ' Calling this calculates the button width and height for each tab, we need the height for calculating the VerticalButtonSpace 
                CalcLineOffsets(rect)

                'Calculate the number of buttons we have space to show
                Dim VerticalButtonSpace As Integer = _owner.Height - _buttonPagePadding.Vertical - _owner.OverflowButton.Height
                Dim MaxButtonsSpace As Integer = VerticalButtonSpace \ _buttonHeight

                _visibleButtonSlots = Math.Min(_owner.TabButtonCount, MaxButtonsSpace)
                If _visibleButtonSlots < 1 Then
                    _visibleButtonSlots = 1 ' Must show at least one button
                End If


                rect = _owner.ClientRectangle
                _tabControlRect = rect

                'Reposition the tab panel
                Dim BoundingRect As Rectangle = Rectangle.FromLTRB(_tabControlRect.Left + _buttonWidth + _buttonPagePadding.Left, _tabControlRect.Top, _tabControlRect.Right, _tabControlRect.Bottom)
                _owner.HostingPanel.Bounds = BoundingRect

                SetButtonPositions()

                _gradientBrushesCreated = True
            Finally
                _updatingCache = False
            End Try
        End Sub


        ''' <summary>
        ''' Retrieves the width in pixels of the widest text in any of the buttons.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetLargestButtonTextSize() As Size
            'Calculate required text width
            Dim maxTextWidth As Integer = 0
            Dim maxTextHeight As Integer = 0
            Dim button As ProjectDesignerTabButton
            For Each button In _owner.TabButtons
                Dim size As Size = TextRenderer.MeasureText(button.Text, button.Font)
                maxTextWidth = Math.Max(size.Width, maxTextWidth)
                maxTextHeight = Math.Max(size.Height, maxTextHeight)
            Next button

            Return New Size(Math.Max(maxTextWidth + s_buttonTextRightSpace, s_minimumButtonTextWidthSpace), maxTextHeight) 'Add buffer for right hand side
        End Function

        ''' <summary>
        ''' Calculates the positions of various lines in the UI
        ''' </summary>
        ''' <param name="rect"></param>
        Private Sub CalcLineOffsets(ByVal rect As Rectangle)
            'const int yOffset = 10;
            Dim width As Integer = rect.Width
            Dim height As Integer = rect.Height
            Dim minimumWidth, minimumHeight As Integer

            Dim largestButtonTextSize As Size = GetLargestButtonTextSize()

            ' Calculate the height of the tab button, we either take the max of either the default size or the text size + padding. 
            _buttonHeight = Math.Max(largestButtonTextSize.Height + _buttonPagePadding.Vertical, s_defaultButtonHeight)

            'Now calculate the minimum width 
            minimumWidth = _owner.HostingPanel.MinimumSize.Width + 1 + _buttonPagePadding.Right + 1
            width = Math.Max(width, minimumWidth)

            'Now calculate required height 
            minimumHeight = _owner.HostingPanel.MinimumSize.Height + 1 + _buttonPagePadding.Bottom + 1
            height = Math.Max(height, minimumHeight)

            ' Calcuate the required height by tab button area...
            Dim panelMinimumHeight As Integer = _buttonHeight * _visibleButtonSlots + 1 + _buttonPagePadding.Bottom + 1
            height = Math.Max(height, panelMinimumHeight)

            _owner.MinimumSize = New Size(minimumWidth, minimumHeight)

            'Add 2 for extra horizontal line above first tab and after last tab

            'Set location of top,left corner where tab buttons begin
            _buttonsLocationX = _buttonPagePadding.Left
            _buttonsLocationY = _buttonPagePadding.Top

            'Calculate width of tab button
            _buttonWidth = largestButtonTextSize.Width + _buttonPagePadding.Horizontal + 20
        End Sub



        ''' <summary>
        ''' Sets the positions of all the owner's buttons.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetButtonPositions()
            'Adjust all the button positions

            'The currently-selected tab must take preference over the current setting of the preferred
            '  button.  If the selected tab would not be visible, set it into the preferred button for
            '  the switchable slot.
            Dim SwitchableSlotIndex As Integer = _visibleButtonSlots - 1
            If _owner.SelectedItem IsNot Nothing AndAlso _owner.SelectedIndex >= SwitchableSlotIndex Then
                Debug.Assert(_owner.SelectedItem IsNot Nothing)
                PreferredButtonForSwitchableSlot = _owner.SelectedItem
            End If

            Dim Index As Integer
            Dim Button As ProjectDesignerTabButton
            Dim PreferredButton As ProjectDesignerTabButton = Me.PreferredButtonForSwitchableSlot
            Dim PreferredButtonIndex As Integer 'Index of the button referred to by PreferredButtonForSwitchableSlot, or else -1
#If DEBUG Then
            Dim SwitchableSlotShown As Boolean = False 'Whether we have shown a button in the switchable slot
#End If
            Dim OverflowNeeded As Boolean = (_visibleButtonSlots < _owner.TabButtonCount)

            'Find the preferred button for the switchable slot
            PreferredButtonIndex = -1
            Index = 0
            If PreferredButton IsNot Nothing Then
                For Each Button In _owner.TabButtons
                    If Button Is PreferredButton Then
                        PreferredButtonIndex = Index
                        Exit For
                    End If
                    Index += 1
                Next
                Debug.Assert(PreferredButtonIndex >= 0, "The preferred button does not exist?")
            End If

            Index = 0
            For Each Button In _owner.TabButtons
                Button.Size = New Size(_buttonWidth, _buttonHeight)

                If Index < SwitchableSlotIndex Then
                    'This button is definitely visible
                    SetButtonBounds(Button, Index)
                    Button.Visible = True
                Else
                    Debug.Assert(OverflowNeeded OrElse Index = SwitchableSlotIndex)
                    SetButtonBounds(Button, SwitchableSlotIndex)

                    Dim ShowButtonInSwitchableSlot As Boolean = False
                    If PreferredButton Is Nothing OrElse PreferredButtonIndex < SwitchableSlotIndex Then
                        'There is no preference for which button is in the switchable slot (or the preferred
                        '  button is already visible above the switchable slot), so 
                        '  we choose the button whose index matches that slot.
                        ShowButtonInSwitchableSlot = (Index = SwitchableSlotIndex)
                    ElseIf Button Is PreferredButton Then
                        ShowButtonInSwitchableSlot = True
                    Else
                        'We haven't seen the preferred button yet, but it does exist, so we will
                        '  see it eventually.  Therefore this button must be set to be invisible.
                        ShowButtonInSwitchableSlot = False
                    End If

#If DEBUG Then
                    If ShowButtonInSwitchableSlot Then
                        Debug.Assert(Not SwitchableSlotShown, "We already showed a button in the switchable slot.")
                        SwitchableSlotShown = True
                    End If
#End If

                    'Set the button visible if it's the one we're showing in the switchable slot.
                    Button.Visible = ShowButtonInSwitchableSlot
                End If

                Index += 1
            Next Button

#If DEBUG Then
            Debug.Assert(SwitchableSlotShown OrElse Not OverflowNeeded, "We never showed a button in the switchable slot.")
#End If

            'Now calculate the position of the overflow button, and whether it should be visible
            _owner.OverflowButton.Location = New Point( _
                _buttonsLocationX + _buttonWidth - s_overflowButtonTopOffset - _owner.OverflowButton.Width, _
                _buttonsLocationY + _visibleButtonSlots * _buttonHeight + s_overflowButtonTopOffset)
            _owner.OverflowButton.Visible = OverflowNeeded
        End Sub

        Private Function SetButtonBounds(ByVal Button As Button, ByVal ButtonIndex As Integer) As Size
            Button.Size = New Size(_buttonWidth, _buttonHeight)
            Button.Location = New Point(_buttonsLocationX, _buttonsLocationY + ButtonIndex * _buttonHeight)
        End Function
#End Region

#Region "Paint routines"


        ''' <summary>
        ''' The main painting routine.
        ''' </summary>
        ''' <param name="g">The Graphics object to paint to.</param>
        ''' <remarks></remarks>
        Public Sub RenderBackground(ByVal g As Graphics)
            CreateGDIObjects()

            If Not _gradientBrushesCreated Then
                Debug.Fail("PERF/FLICKER WARNING: ProjectDesignerTabRenderer.RenderBackground() called before fully initialized")
                Exit Sub
            End If

            'Fill layers bottom up
            '... Background
            g.FillRectangle(_controlBackgroundBrush, _tabControlRect)
        End Sub


        ''' <summary>
        ''' Renders the UI for a button
        ''' </summary>
        ''' <param name="g">The Graphics object of the button to paint to.</param>
        ''' <param name="button">The button to paint.</param>
        ''' <param name="IsSelected">Whether the given button is the currently-selected button.</param>
        ''' <param name="IsHovered">Whether the given button is to be drawn in the hovered state.</param>
        ''' <remarks>
        ''' The graphics object g is the button's, so coordinates are relative to the button left/top
        ''' </remarks>
        Public Sub RenderButton(ByVal g As Graphics, ByVal button As ProjectDesignerTabButton, ByVal IsSelected As Boolean, ByVal IsHovered As Boolean)
            CreateGDIObjects()

            Dim backgroundBrush As Brush = _buttonBackgroundBrush
            Dim foregroundColor As Color = _buttonForegroundColor ' TextRenderer.DrawText takes a color over a brush 

            If IsSelected Then
                backgroundBrush = _selectedButtonBackgroundBrush
                foregroundColor = _selectedButtonForegroundColor
            Else If IsHovered Then
                backgroundBrush = _hoverButtonBackgroundBrush
                foregroundColor = _hoverButtonForegroundColor
            End If

            Const TriangleWidth As Integer = 6
            Const TriangleHeight As Integer = 12

            ' Triangle starts at the width of the control minus the width of the triangle
            Dim triangleHorizontalStart As Integer = (button.Width - TriangleWidth)

            ' Find relative start of triangle, half of the height of the control minus half of the height of the triangle
            Dim triangleVerticalStart As Single = (CSng(button.Height) / 2) - (CSng(TriangleHeight) / 2)

            g.FillRectangle(backgroundBrush, New Rectangle(0, 0, triangleHorizontalStart, button.Height))


            ' Draw the "arrow" part of the tab button if the item is selected for hovered 
            If IsSelected Then

                ' Create an array of points which describes the path of the triangle
                Dim trainglePoints() As PointF =
                {
                    New PointF(triangleHorizontalStart - 1, triangleVerticalStart), _
                    New PointF(button.Width, CSng(triangleVerticalStart) + CSng(TriangleHeight) / 2), _
                    New PointF(triangleHorizontalStart - 1, triangleVerticalStart + TriangleHeight)
                }

                Using trianglePath As New GraphicsPath()

                    trianglePath.AddPolygon(trainglePoints)

                    ' Draw the rectangle using Fill Rectangle with the default smoothing mode
                    ' If the rectangle is drawn with SmoothingMode.HighQuality / AntiAliased it
                    ' gets "soft" edges so only the triangle part of the path is drawn with this mode

                    Dim previousSmoothingMode = g.SmoothingMode
                    g.SmoothingMode = SmoothingMode.HighQuality

                    ' Draw the traingle part of the path
                    g.FillPath(backgroundBrush, trianglePath)
                    g.SmoothingMode = previousSmoothingMode

                End Using
            End If

            Dim textRect As New Rectangle(s_buttonTextLeftOffset, 0, button.Width - s_buttonTextLeftOffset, button.Height)
            TextRenderer.DrawText(g, button.TextWithDirtyIndicator, button.Font, textRect, foregroundColor, TextFormatFlags.Left Or TextFormatFlags.SingleLine Or TextFormatFlags.VerticalCenter)
        End Sub 'RenderButton 

#End Region
    End Class

End Namespace

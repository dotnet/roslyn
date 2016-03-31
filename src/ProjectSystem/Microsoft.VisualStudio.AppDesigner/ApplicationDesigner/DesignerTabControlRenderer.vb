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
        Private controlBackgroundColor As Color

        ' Tab button foreground/background 
        Private buttonForegroundColor As Color
        Private buttonBackgroundColor as Color

        ' Tab button selected foreground/background 
        Private selectedButtonForegroundColor As Color
        Private selectedButtonBackgroundColor as Color

        ' Tab button hover foreground/background
        Private hoverButtonForegroundColor As Color
        Private hoverButtonBackgroundColor as Color

#End Region

#Region "GDI objects"

        ' Background of the control 
        Private controlBackgroundBrush As SolidBrush

        ' Tab button foreground/background 
        Private buttonBackgroundBrush as Brush

        ' Tab button selected foreground/background 
        Private selectedButtonBackgroundBrush as Brush

        ' Tab button hover foreground/background
        Private hoverButtonBackgroundBrush as Brush

#End Region

        ' Category guid for project designer theme colors 
        Private Shared ReadOnly ProjectDesignerThemeCategory As New Guid("ef1a2d2c-5d16-4ddb-8d04-79d0f6c1c56e")

        'The left X position for all tab buttons
        Private buttonsLocationX As Integer
        'The top Y position for the topmost button
        Private buttonsLocationY As Integer 'Start y location
        'Smallest text width to allow for in the buttons, even if all of the buttons have text wider than this value
        Private Const minimumButtonTextWidthSpace As Integer = 25

        'The width and height to use for all of the buttons.
        Private Const DefaultButtonHeight = 24
        Private buttonHeight As Integer
        Private buttonWidth As Integer

        Private Const buttonTextLeftOffset As Integer = 8 'Padding from left side of the button to where the tab text is drawn
        Private Const buttonTextRightSpace As Integer = 8 'Extra space to leave after tab text
        Private visibleButtonSlots As Integer '# of button positions we are currently displaying (min = 1), the rest go into overflow when not enough room
        Private buttonPagePadding As Padding

        'The width/height of the downward-slanting line underneath the buttons (not including the two curved ends' (arcs') width/height)

        Private Const buttonBorderWidth As Integer = 1   'Thickness of each half of the separators between buttons

        Private Const OverflowButtonTopOffset As Integer = 2 'Offset of overflow button (the button's edge, not the glyph inside it) from the bottom of the bottommost button
        Private Const OverflowButtonRightOffset As Integer = 2 'Offset of right edge of overflow button from vertical line 3

        Private tabControlRect As Rectangle 'The entire area of the tab control, including the tabs and panel area

        'Pointer to the ProjectDesignerTabControl which owns this renderer.  May not be null
        Private m_Owner As ProjectDesignerTabControl

        'The service provider to use.  May be Nothing
        Private m_serviceProvider As IServiceProvider

        'Backs the PreferredButtonInSwitchableSlot property
        Private m_PreferredButtonForSwitchableSlot As ProjectDesignerTabButton

        'True if currently in UpdateCacheStatus
        Private m_UpdatingCache As Boolean

        'True if currently in CreateGDIObjects
        Private m_CreatingGDIObjects As Boolean

        'True if the GDI objects have been created
        Private m_GDIObjectsCreated As Boolean

        'True if the gradient brushes have been created
        Private m_GradientBrushesCreated As Boolean

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="owner">The ProjectDesignerTabControl control which owns and contains this control.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal owner As ProjectDesignerTabControl)
            If owner Is Nothing Then
                Throw New ArgumentNullException("owner")
            End If
            m_Owner = owner

            buttonPagePadding = New Padding(9, 5, 9, 5)
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
                Return m_serviceProvider
            End Get
            Set(ByVal value As IServiceProvider)
                m_serviceProvider = value
                If m_GDIObjectsCreated Then
                    'If we've already created GDI stuff/layout, we will need to re-create them.  Otherwise
                    '  we just wait for on-demand.
                    CreateGDIObjects(True)
                End If
                m_Owner.Invalidate()
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
                Return m_PreferredButtonForSwitchableSlot
            End Get
            Set(ByVal value As ProjectDesignerTabButton)
                If value IsNot m_PreferredButtonForSwitchableSlot Then
                    m_PreferredButtonForSwitchableSlot = value
                    m_Owner.Invalidate()
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
            If m_CreatingGDIObjects Then
                Exit Sub
            End If
            If m_GDIObjectsCreated AndAlso Not ForceUpdate Then
                Exit Sub
            End If

            Try
                m_CreatingGDIObjects = True

                'Get Colors from the shell
                Dim VsUIShell As IVsUIShell5 = VsUIShell5Service

                'NOTE: The defaults given here are the system colors.  Since this control is not currently
                '  being used outside of Visual Studio, this is okay because we don't expect to fail to get colors from the
                '  color service.  If we make the control available as a stand-alone component, we would need to add logic to
                '  do the right thing when not hosted inside Visual Studio, and change according to the theme.


                controlBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "Background", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Control)

                buttonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "CategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.ControlText)
                buttonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "CategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Control)

                selectedButtonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "SelectedCategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.HighlightText)
                selectedButtonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "SelectedCategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Highlight)

                hoverButtonForegroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "MouseOverCategoryTab", __THEMEDCOLORTYPE.TCT_Foreground, SystemColors.HighlightText)
                hoverButtonBackgroundColor = AppDesCommon.ShellUtil.GetDesignerThemeColor(VsUIShell, ProjectDesignerThemeCategory, "MouseOverCategoryTab", __THEMEDCOLORTYPE.TCT_Background, SystemColors.HotTrack)

                'Get GDI objects
                controlBackgroundBrush = New SolidBrush(controlBackgroundColor)
                buttonBackgroundBrush = New SolidBrush(buttonBackgroundColor)
                selectedButtonBackgroundBrush = New SolidBrush(selectedButtonBackgroundColor)
                hoverButtonBackgroundBrush = New SolidBrush(hoverButtonBackgroundColor)

                If ForceUpdate Then
                    'Colors may have changed, need to update state.  Also, the gradient brushes are
                    '  created in UpdateCacheState (because they depend on button sizes), so we need
                    '  to create them, too
                    m_Owner.Invalidate()
                    UpdateCacheState()
                End If

                m_GDIObjectsCreated = True
            Finally
                m_CreatingGDIObjects = False
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
        Sub UpdateCacheState()
            If m_UpdatingCache Then
                Exit Sub
            End If
            If ServiceProvider Is Nothing Then
                Exit Sub
            End If

            m_UpdatingCache = True
            Try
                CreateGDIObjects()

                Dim rect As Rectangle = m_Owner.Bounds

                ' Calling this calculates the button width and height for each tab, we need the height for calculating the VerticalButtonSpace 
                CalcLineOffsets(rect)

                'Calculate the number of buttons we have space to show
                Dim VerticalButtonSpace As Integer = m_Owner.Height - buttonPagePadding.Vertical - m_Owner.OverflowButton.Height
                Dim MaxButtonsSpace As Integer = VerticalButtonSpace \ buttonHeight

                visibleButtonSlots = Math.Min(m_Owner.TabButtonCount, MaxButtonsSpace)
                If visibleButtonSlots < 1 Then
                    visibleButtonSlots = 1 ' Must show at least one button
                End If


                rect = m_Owner.ClientRectangle
                tabControlRect = rect

                'Reposition the tab panel
                Dim BoundingRect As Rectangle = Rectangle.FromLTRB(tabControlRect.Left + buttonWidth + buttonPagePadding.Left, tabControlRect.Top, tabControlRect.Right, tabControlRect.Bottom)
                m_Owner.HostingPanel.Bounds = BoundingRect

                SetButtonPositions()

                m_GradientBrushesCreated = True
            Finally
                m_UpdatingCache = False
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
            For Each button In m_Owner.TabButtons
                Dim size As Size = TextRenderer.MeasureText(button.Text, button.Font)
                maxTextWidth = Math.Max(size.Width, maxTextWidth)
                maxTextHeight = Math.Max(size.Height, maxTextHeight)
            Next button

            Return New Size(Math.Max(maxTextWidth + buttonTextRightSpace, minimumButtonTextWidthSpace), maxTextHeight) 'Add buffer for right hand side
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
            buttonHeight = Math.Max(largestButtonTextSize.Height + buttonPagePadding.Vertical, DefaultButtonHeight)

            'Now calculate the minimum width 
            minimumWidth = m_Owner.HostingPanel.MinimumSize.Width + 1 + buttonPagePadding.Right + 1
            width = Math.Max(width, minimumWidth)

            'Now calculate required height 
            minimumHeight = m_Owner.HostingPanel.MinimumSize.Height + 1 + buttonPagePadding.Bottom + 1
            height = Math.Max(height, minimumHeight)

            ' Calcuate the required height by tab button area...
            Dim panelMinimumHeight As Integer = buttonHeight * visibleButtonSlots + 1 + buttonPagePadding.Bottom + 1
            height = Math.Max(height, panelMinimumHeight)

            m_Owner.MinimumSize = New Size(minimumWidth, minimumHeight)

            'Add 2 for extra horizontal line above first tab and after last tab

            'Set location of top,left corner where tab buttons begin
            buttonsLocationX = buttonPagePadding.Left
            buttonsLocationY = buttonPagePadding.Top

            'Calculate width of tab button
            buttonWidth = largestButtonTextSize.Width + buttonPagePadding.Horizontal + 20
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
            Dim SwitchableSlotIndex As Integer = visibleButtonSlots - 1
            If m_Owner.SelectedItem IsNot Nothing AndAlso m_Owner.SelectedIndex >= SwitchableSlotIndex Then
                Debug.Assert(m_Owner.SelectedItem IsNot Nothing)
                PreferredButtonForSwitchableSlot = m_Owner.SelectedItem
            End If

            Dim Index As Integer
            Dim Button As ProjectDesignerTabButton
            Dim PreferredButton As ProjectDesignerTabButton = Me.PreferredButtonForSwitchableSlot
            Dim PreferredButtonIndex As Integer 'Index of the button referred to by PreferredButtonForSwitchableSlot, or else -1
#If DEBUG Then
            Dim SwitchableSlotShown As Boolean = False 'Whether we have shown a button in the switchable slot
#End If
            Dim OverflowNeeded As Boolean = (visibleButtonSlots < m_Owner.TabButtonCount)

            'Find the preferred button for the switchable slot
            PreferredButtonIndex = -1
            Index = 0
            If PreferredButton IsNot Nothing Then
                For Each Button In m_Owner.TabButtons
                    If Button Is PreferredButton Then
                        PreferredButtonIndex = Index
                        Exit For
                    End If
                    Index += 1
                Next
                Debug.Assert(PreferredButtonIndex >= 0, "The preferred button does not exist?")
            End If

            Index = 0
            For Each Button In m_Owner.TabButtons
                Button.Size = New Size(buttonWidth, buttonHeight)

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
            m_Owner.OverflowButton.Location = New Point( _
                buttonsLocationX + buttonWidth - OverflowButtonTopOffset - m_Owner.OverflowButton.Width, _
                buttonsLocationY + visibleButtonSlots * buttonHeight + OverflowButtonTopOffset)
            m_Owner.OverflowButton.Visible = OverflowNeeded
        End Sub

        Private Function SetButtonBounds(ByVal Button As Button, ByVal ButtonIndex As Integer) As Size
            Button.Size = New Size(buttonWidth, buttonHeight)
            Button.Location = New Point(buttonsLocationX, buttonsLocationY + ButtonIndex * buttonHeight)
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

            If Not m_GradientBrushesCreated Then
                Debug.Fail("PERF/FLICKER WARNING: ProjectDesignerTabRenderer.RenderBackground() called before fully initialized")
                Exit Sub
            End If

            'Fill layers bottom up
            '... Background
            g.FillRectangle(controlBackgroundBrush, tabControlRect)
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

            Dim backgroundBrush As Brush = buttonBackgroundBrush
            Dim foregroundColor As Color = buttonForegroundColor ' TextRenderer.DrawText takes a color over a brush 

            If IsSelected Then
                backgroundBrush = selectedButtonBackgroundBrush
                foregroundColor = selectedButtonForegroundColor
            Else If IsHovered Then
                backgroundBrush = hoverButtonBackgroundBrush
                foregroundColor = hoverButtonForegroundColor
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

            Dim textRect As New Rectangle(buttonTextLeftOffset, 0, button.Width - buttonTextLeftOffset, button.Height)
            TextRenderer.DrawText(g, button.TextWithDirtyIndicator, button.Font, textRect, foregroundColor, TextFormatFlags.Left Or TextFormatFlags.SingleLine Or TextFormatFlags.VerticalCenter)
        End Sub 'RenderButton 

#End Region
    End Class

End Namespace

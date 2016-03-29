 '******************************************************************************
'* ProjectDesignerTabButton.vb
'*
'* Copyright (C) 1999-2004 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public Class ProjectDesignerTabButton
        Inherits Button

        Private m_Index As Integer
        Private m_DirtyIndicator As Boolean


        Public Sub New()
            SetStyle(ControlStyles.SupportsTransparentBackColor, True)
            BackColor = Color.Transparent
            SetStyle(ControlStyles.Opaque Or ControlStyles.StandardClick, False)

            ' If the UserMouse style is set, the control does its own processing
            '   of mouse messages (keeps Control's WmMouseDown from calling into DefWndProc)
            SetStyle(ControlStyles.UserMouse Or ControlStyles.UserPaint, True)

            Me.FlatStyle = System.Windows.Forms.FlatStyle.Flat

            'We need the tab buttons to be able to receive focus, so that we can 
            '  redirect focus back to the selected page when the shell is activated.
            SetStyle(ControlStyles.Selectable, True)
            Me.TabStop = True
        End Sub 'New


        ''' <summary>
        ''' True if the dirty indicator should be display
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DirtyIndicator() As Boolean
            Get
                Return m_DirtyIndicator
            End Get
            Set(ByVal value As Boolean)
                If value <> m_DirtyIndicator Then
                    m_DirtyIndicator = value
                    Invalidate()
                End If
            End Set
        End Property


        ''' <summary>
        ''' Returns the text of the tab button, with the dirty indicator if it is on.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property TextWithDirtyIndicator() As String
            Get
                'If the dirty indicator is on, append "*" to the text
                Dim ButtonText As String = Me.Text
                If DirtyIndicator Then
                    ButtonText &= "*"
                End If

                Return ButtonText
            End Get
        End Property


        ''' <summary>
        ''' The location of the button.  Should not be changed directly except
        '''   by the tab control itself.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>

        Public Shadows Property Location() As Point
            Get
                Return MyBase.Location
            End Get
            Set(ByVal value As Point) 'Make inaccessible except to this assembly 'CONSIDER: this is non-CLS-compliant, should change if make control public
                MyBase.Location = value
            End Set
        End Property


        Public ReadOnly Property ButtonIndex() As Integer
            Get
                Return m_Index
            End Get
        End Property

        Public Sub SetIndex(ByVal index As Integer)
            m_Index = index
        End Sub


        Private ReadOnly Property ParentTabControl() As ProjectDesignerTabControl
            Get
                Return DirectCast(Me.Parent, ProjectDesignerTabControl)
            End Get
        End Property


        Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.Renderer.RenderButton(e.Graphics, Me, Me Is parent.SelectedItem, Me Is parent.HoverItem)
            End If
        End Sub


        '''<summary>
        '''</summary>
        '''<remarks> We need handle OnClick to make Accessiblity work... </remarks>
        Protected Overrides Sub OnClick(ByVal e As System.EventArgs)
            MyBase.OnClick(e)

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemClick(Me)
            End If
        End Sub


        Protected Overrides Sub OnMouseEnter(ByVal e As System.EventArgs)
            MyBase.OnMouseEnter(e)
            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemEnter(e, Me)
            End If
        End Sub


        Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
            MyBase.OnMouseLeave(e)

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemLeave(e, Me)
            End If
        End Sub


        Protected Overrides Sub OnGotFocus(ByVal e As System.EventArgs)
            Common.Switches.TracePDFocus(TraceLevel.Warning, "ProjectDesignerTabButton.OnGotFocus - forwarding to parent")
            MyBase.OnGotFocus(e)

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemGotFocus(e, Me)
            End If
            Invalidate()
        End Sub

        ''' <summary>
        ''' Create customized accessible object
        ''' </summary>
        Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
            Return New DesignerTabButtonAccessibleObject(Me)
        End Function

        ''' <summary>
        ''' accessible state
        ''' </summary>
        Public ReadOnly Property AccessibleState() As AccessibleStates
            Get
                Dim parent As ProjectDesignerTabControl = ParentTabControl
                If parent IsNot Nothing AndAlso Me Is parent.SelectedItem Then
                    Return AccessibleStates.Selectable Or AccessibleStates.Selected
                Else
                    Return AccessibleStates.Selectable
                End If
            End Get
        End Property

        '''<summary>
        ''' custom build accessible object class
        '''</summary>
        Private Class DesignerTabButtonAccessibleObject
            Inherits ButtonBaseAccessibleObject

            ' button which this accessible object belongs to
            Private m_Button As ProjectDesignerTabButton

            Public Sub New(ByVal owner As ProjectDesignerTabButton)
                MyBase.New(owner)
                m_Button = owner
            End Sub

            ''' <summary>
            ''' accessible state
            ''' </summary>
            Public Overrides ReadOnly Property State() As AccessibleStates
                Get
                    Return m_Button.AccessibleState
                End Get
            End Property

            ''' <summary>
            ''' Default action name.
            ''' </summary>
            Public Overrides ReadOnly Property DefaultAction() As String
                Get
                    Return SR.GetString(SR.APPDES_TabButtonDefaultAction)
                End Get
            End Property

            ''' <summary>
            ''' Role - it is a tab page
            ''' </summary>
            Public Overrides ReadOnly Property Role() As AccessibleRole
                Get
                    Return AccessibleRole.PageTab
                End Get
            End Property

            ''' <summary>
            ''' Do the default action - select the tab
            ''' </summary>
            Public Overrides Sub DoDefaultAction()
                m_Button.PerformClick()
            End Sub

        End Class

    End Class

End Namespace

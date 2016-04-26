'******************************************************************************
'* ToolStripImageComboBox.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports System.Math
Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

#If False Then 'Not currently needed

    '@ <summary>
    '@ This class is a toolstrip item that contains a combobox which is capable
    '@   of holding images and having a different font for each entry in it.
    '@ </summary>
    '@ <remarks></remarks>
    Friend Class ToolStripImageComboBox
        Inherits ToolStripComboBox

        'The underlying combobox control (this class does not inherit from ComboBox, but rather
        '  from ToolStripComboBox, which *has* a ComboBox control.
        'We have a variable for it here so we can hook up events to it.
        Private WithEvents UnderlyingComboBox As ComboBox

        '# of pixels for padding each combobox entry at the top and bottom
        Private Const CellPaddingYPixels As Integer = 1

        '# of pixels for padding each combobox entry at the left and right
        Private Const CellPaddingXPixels As Integer = 2

        '# of pixels between the image and the start of the text entry
        Private Const ImageTextPaddingPixels As Integer = 3

        'Our list of items that we expose via the Items property
        Private m_ImageComboItems As ObjectCollection


#Region "Nested class - ObjectCollection"

        '@ <summary>
        '@ The collection class that we return for the Items property.  This simply
        '@   makes it possible to use code like ImageComboBox.Items.Add(...)
        '@ </summary>
        '@ <remarks></remarks>
        Friend Shadows Class ObjectCollection
            'The actual underlying combobox
            Private m_UnderlyingComboBox As ComboBox


            '@ <summary>
            '@ Constructor
            '@ </summary>
            '@ <param name="UnderlyingComboBox">The actual underlying combobox control that we're wrapping with our collection</param>
            '@ <remarks></remarks>
            Friend Sub New(ByVal UnderlyingComboBox As ComboBox)
                Debug.Assert(UnderlyingComboBox IsNot Nothing)
                m_UnderlyingComboBox = UnderlyingComboBox
            End Sub


            '@ <summary>
            '@ Adds a new item to the collection
            '@ </summary>
            '@ <param name="ItemText">The text for this combobox entry</param>
            '@ <param name="ItemImage">The image for this combobox entry (Nothing is acceptable)</param>
            '@ <param name="UserData">[Optional] User-defined data for this combobox entry</param>
            '@ <returns>The newly-inserted ImageComboBoxItem</returns>
            '@ <remarks></remarks>
            Public Function Add(ByVal ItemText As String, ByVal ItemImage As Image, Optional ByVal UserData As Object = Nothing) As ImageComboBoxItem
                Dim Item As New ImageComboBoxItem(m_UnderlyingComboBox, ItemText, ItemImage, UserData)
                m_UnderlyingComboBox.Items.Add(Item)
                Return Item
            End Function


            '@ <summary>
            '@ Retrieves an item from the collection
            '@ </summary>
            '@ <param name="Index">Zero-based index of the item</param>
            '@ <value></value>
            '@ <remarks></remarks>
            Default Public ReadOnly Property Item(ByVal Index As Integer) As ImageComboBoxItem
                Get
                    Return DirectCast(m_UnderlyingComboBox.Items(Index), ImageComboBoxItem)
                End Get
            End Property


            '@ <summary>
            '@ Count of the number of items in this collection
            '@ </summary>
            '@ <value></value>
            '@ <remarks></remarks>
            Public ReadOnly Property Count() As Integer
                Get
                    Return m_UnderlyingComboBox.Items.Count
                End Get
            End Property


            '@ <summary>
            '@ Gets an enumerator for this collection
            '@ </summary>
            '@ <returns></returns>
            '@ <remarks></remarks>
            Public Function GetEnumerator() As System.Collections.IEnumerator
                Return m_UnderlyingComboBox.Items.GetEnumerator()
            End Function
        End Class

#End Region


#Region "Nested class - ImageComboBoxItem"

        '@ <summary>
        '@ Represents a single item in the image combo box
        '@ </summary>
        '@ <remarks></remarks>
        Public Class ImageComboBoxItem
            'Backing fields for public properties
            Private m_Text As String
            Private m_Image As Image
            Private m_Font As Font
            Private m_UserData As Object

            'The parent combobox for this item
            Private m_Parent As ComboBox

            '@ <summary>
            '@ Constructor.
            '@ </summary>
            '@ <param name="Parent">The parent combobox for this entry</param>
            '@ <param name="Text">The text for this combobox entry</param>
            '@ <param name="Image">The image for this combobox entry (Nothing is acceptable)</param>
            '@ <param name="UserData">[Optional] User-defined data for this combobox entry</param>
            '@ <remarks></remarks>
            Public Sub New(ByVal Parent As ComboBox, ByVal Text As String, ByVal Image As Image, Optional ByVal UserData As Object = Nothing)
                Debug.Assert(Parent IsNot Nothing)
                m_Text = Text
                m_Image = Image
                m_UserData = UserData
                m_Parent = Parent
            End Sub


            '@ <summary>
            '@ Converts the item to a string.
            '@ </summary>
            '@ <returns></returns>
            '@ <remarks></remarks>
            Public Overrides Function ToString() As String
                Return m_Text
            End Function


            '@ <summary>
            '@ The text for this combobox entry
            '@ </summary>
            '@ <value></value>
            '@ <remarks></remarks>
            Public Property Text() As String
                Get
                    Return m_Text
                End Get
                Set(ByVal Value As String)
                    m_Text = Value
                    RefreshComboBoxIfShowingThisItem()
                End Set
            End Property


            '@ <summary>
            '@ The image for this combobox entry.  May be Nothing.
            '@ </summary>
            '@ <value></value>
            '@ <remarks></remarks>
            Public Property Image() As Image
                Get
                    Return m_Image
                End Get
                Set(ByVal Value As Image)
                    If m_Image IsNot Value Then
                        m_Image = Value
                        RefreshComboBoxIfShowingThisItem()
                    End If
                End Set
            End Property


            '@ <summary>
            '@ User-defined data for this combobox entry.  May be Nothing.
            '@ </summary>
            '@ <value></value>
            '@ <remarks></remarks>
            Public Property UserData() As Object
                Get
                    Return m_UserData
                End Get
                Set(ByVal Value As Object)
                    m_UserData = Value
                End Set
            End Property


            '@ <summary>
            '@ The font for this combobox entry.
            '@ </summary>
            '@ <value></value>
            '@ <remarks></remarks>
            Public Property Font() As Font
                Get
                    If m_Font Is Nothing Then
                        Return m_Parent.Font
                    Else
                        Return m_Font
                    End If
                End Get
                Set(ByVal Value As Font)
                    If m_Font IsNot Value Then
                        m_Font = Value
                        RefreshComboBoxIfShowingThisItem()
                    End If
                End Set
            End Property


            '@ <summary>
            '@ Checks if this item is currently visible in the combobox, and if it
            '@   is, causes the combobox to refresh itself (because something in
            '@   the item has changed)
            '@ </summary>
            '@ <remarks></remarks>
            Private Sub RefreshComboBoxIfShowingThisItem()
                If m_Parent.DroppedDown OrElse m_Parent.SelectedItem Is Me Then
                    m_Parent.Refresh()
                End If
            End Sub
        End Class

#End Region


#Region "ToolStripImageComboBox methods and properties"

        '@ <summary>
        '@ Constructor.
        '@ </summary>
        '@ <remarks></remarks>
        Public Sub New()
            MyBase.New()

            UnderlyingComboBox = MyBase.ComboBox
            With UnderlyingComboBox
                .FlatStyle = FlatStyle.Popup
                .DropDownStyle = ComboBoxStyle.DropDownList
                .DrawMode = Windows.Forms.DrawMode.OwnerDrawVariable 'sets up owner drawing
            End With

            m_ImageComboItems = New ObjectCollection(UnderlyingComboBox)
        End Sub


        '@ <summary>
        '@ Items property.  Allows stuff like ImageComboBox.Items.Add(...)
        '@ </summary>
        '@ <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> Public Shadows ReadOnly Property Items() As ObjectCollection
            Get
                Return m_ImageComboItems
            End Get
        End Property


        '@ <summary>
        '@ Determines the preferred size of this combobox, given a particular maximum constraining
        '@   size.
        '@ </summary>
        '@ <param name="ConstrainingSize">The size that must be fit into.</param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Public Overrides Function GetPreferredSize(ByVal ConstrainingSize As Size) As Size
            Dim BasePreferredSize As Size = MyBase.GetPreferredSize(ConstrainingSize)

            'Determine the maximum width/height of all the entries
            Dim MaxItemsWidth, MaxItemsHeight As Integer
            Using Graphics As Graphics = UnderlyingComboBox.CreateGraphics()
                For Index As Integer = 0 To Items.Count - 1
                    Dim ItemSize As Size = GetItemSize(Index, Graphics)
                    MaxItemsWidth = Max(ItemSize.Width, MaxItemsWidth)
                    MaxItemsHeight = Max(ItemSize.Height, MaxItemsHeight)
                Next
            End Using

            'Add room for the dropdown (using scrollbar measurements from the system)
            Dim ApproximateExtraWidthOnComboBox As Integer = SystemInformation.VerticalScrollBarWidth + 3 * 2 'plus extra padding
            Dim ApproximateExtraHeightOnComboBox As Integer = SystemInformation.VerticalScrollBarArrowHeight
            Dim ImageComboPreferredSize As Size = New Size( _
                Min(MaxItemsWidth + ApproximateExtraWidthOnComboBox, ConstrainingSize.Width), _
                Max(MaxItemsHeight, SystemInformation.VerticalScrollBarArrowHeight))

            'If the base control prefers a larger size, use that, otherwise use ours.  I'm assuming they
            '  properly constrain to the given size.
            Dim PreferredSize As Size = New Size(Max(BasePreferredSize.Width, ImageComboPreferredSize.Width), Max(BasePreferredSize.Height, ImageComboPreferredSize.Height))
            Return PreferredSize
        End Function


        '@ <summary>
        '@ Sets the dropdown height of the combobox to an appropriate value.
        '@ </summary>
        '@ <remarks>Currently this is calculated as the total height of all items in the combobox.  If there were 
        '@    a large number of items, this algorithm would need to be refined.</remarks>
        Private Sub SetDropdownHeight()
            Dim TotalItemsHeight As Integer
            Using Graphics As Graphics = UnderlyingComboBox.CreateGraphics()
                For Index As Integer = 0 To Items.Count - 1
                    TotalItemsHeight += GetItemSize(Index, Graphics).Height
                Next
            End Using

            Const BorderWidthPixels As Integer = 1 'Pixels for the border around the dropdown drawn by the control
            Me.DropDownHeight = TotalItemsHeight + BorderWidthPixels * 2
        End Sub


        '@ <summary>
        '@ Event handler for when the combobox is dropped down.
        '@ </summary>
        '@ <remarks></remarks>
        Private Sub ImageComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.DropDown
            SetDropdownHeight()
        End Sub


        '@ <summary>
        '@ Event handler for measuring a given entry.
        '@ </summary>
        '@ <param name="sender"></param>
        '@ <param name="e"></param>
        '@ <remarks></remarks>
        Private Sub ImageComboBox_MeasureItem(ByVal sender As Object, ByVal e As System.Windows.Forms.MeasureItemEventArgs) Handles UnderlyingComboBox.MeasureItem
            If e.Index >= 0 Then
                Dim Size As Size = GetItemSize(e.Index, e.Graphics)
                e.ItemWidth = CInt(Ceiling(Size.Width))
                e.ItemHeight = CInt(Ceiling(Size.Height))
            End If
        End Sub


        '@ <summary>
        '@ Event handler for owner drawing a given item.
        '@ </summary>
        '@ <param name="sender"></param>
        '@ <param name="e"></param>
        '@ <remarks></remarks>
        Private Sub ImageComboBox_DrawItem(ByVal sender As Object, ByVal e As System.Windows.Forms.DrawItemEventArgs) Handles UnderlyingComboBox.DrawItem
            'Index = 0 if there are no entries
            If e.Index >= 0 Then
                Dim Item As ImageComboBoxItem = Items(e.Index)

                e.DrawBackground()

                'Center text vertically (but don't draw it yet)
                Dim TextSize As Size = e.Graphics.MeasureString(Item.Text, Item.Font, New Point(0, 0), StringFormat.GenericDefault).ToSize()
                Dim TextTop As Integer = e.Bounds.Top + (e.Bounds.Height - TextSize.Height) \ 2
                Dim TextLeft As Integer = e.Bounds.Left + CellPaddingXPixels

                'Draw the image
                If Item.Image IsNot Nothing Then
                    Dim ImageTop As Integer = e.Bounds.Top + (e.Bounds.Height - Item.Image.Height) \ 2
                    Dim ImageLeft As Integer = e.Bounds.Left + CellPaddingXPixels
                    TextLeft = e.Bounds.Left + Item.Image.Width + ImageTextPaddingPixels

                    e.Graphics.DrawImage(Item.Image, ImageLeft, ImageTop)
                End If

                'Draw the text
                Using Brush As New SolidBrush(e.ForeColor)
                    e.Graphics.DrawString(Item.Text, Item.Font, Brush, TextLeft, TextTop)
                End Using

                e.DrawFocusRectangle()
            End If
        End Sub


        '@ <summary>
        '@ Calcualtes the size of a single entry in the combo
        '@ </summary>
        '@ <param name="Index">Index of the item to measure</param>
        '@ <param name="Graphics">The graphics object to use for measuring</param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function GetItemSize(ByVal Index As Integer, ByVal Graphics As Graphics) As Size
            Dim Item As ImageComboBoxItem = Items(Index)
            Dim TextSize As SizeF = Graphics.MeasureString(Item.Text, Item.Font, New PointF(0, 0), StringFormat.GenericDefault)
            Dim TotalSize As New Size(CInt(Ceiling(TextSize.Width)), CInt(Ceiling(TextSize.Height)))

            If Item.Image IsNot Nothing Then
                TotalSize.Width += Item.Image.Width + ImageTextPaddingPixels
                TotalSize.Height = Max(TotalSize.Height, Item.Image.Height)
            End If

            TotalSize.Width += 2 * CellPaddingXPixels
            TotalSize.Height += 2 * CellPaddingYPixels

            Return TotalSize
        End Function

#End Region

    End Class

#End If

End Namespace

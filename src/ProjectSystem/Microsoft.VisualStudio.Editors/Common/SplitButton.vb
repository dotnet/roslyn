' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Infer On
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles
Imports Microsoft.VisualStudio.PlatformUI

Namespace Microsoft.VisualStudio.Editors.Common

    Friend Class ShowCustomContextMenuEventArgs
        Inherits EventArgs

        Public Handled As Boolean
    End Class

    Friend Class SplitButton
        Inherits Button

        Private _state As PushButtonState = PushButtonState.Normal
        Private _pushButtonWidth As Integer = 14
        Private _dropDownRectangle As New Rectangle
        Private _showCustomContextMenuWasHandled As Boolean

        Public Event ShowCustomContextMenu(ByVal e As ShowCustomContextMenuEventArgs)

        Public Sub New()
            MyBase.New()

            Me.AccessibleRole = AccessibleRole.SplitButton
            Me._pushButtonWidth = DpiHelper.LogicalToDeviceUnitsX(_pushButtonWidth)
        End Sub

        Protected Overrides Sub OnPaint(ByVal pevent As PaintEventArgs)
            MyBase.OnPaint(pevent)

            Dim g = pevent.Graphics

            Dim bounds = New Rectangle(0, 0, Width, Height)
            Dim FormatFlags = TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter

            ButtonRenderer.DrawButton(g, bounds, State)

            Dim singlePixelWidth = DpiHelper.LogicalToDeviceUnitsX(1)
            Dim dividerLineVerticalPadding = DpiHelper.LogicalToDeviceUnitsY(4)

            _dropDownRectangle = New Rectangle(
                x:=bounds.Right - _pushButtonWidth - singlePixelWidth,
                y:=dividerLineVerticalPadding,
                width:=_pushButtonWidth,
                height:=bounds.Height - (dividerLineVerticalPadding * 2))

            ' Draw divider line
            If RightToLeft.Equals(RightToLeft.Yes) Then
                _dropDownRectangle.X = bounds.Left + singlePixelWidth

                g.DrawLine(SystemPens.ButtonHighlight,
                    x1:=bounds.Left + _pushButtonWidth,
                    y1:=dividerLineVerticalPadding,
                    x2:=bounds.Left + _pushButtonWidth,
                    y2:=bounds.Bottom - dividerLineVerticalPadding)

                g.DrawLine(SystemPens.ButtonShadow,
                    x1:=bounds.Left + _pushButtonWidth + singlePixelWidth,
                    y1:=dividerLineVerticalPadding,
                    x2:=bounds.Left + _pushButtonWidth + singlePixelWidth,
                    y2:=bounds.Bottom - dividerLineVerticalPadding)

                bounds.Offset(_pushButtonWidth, 0)
                bounds.Width = bounds.Width - _pushButtonWidth
            Else
                g.DrawLine(SystemPens.ButtonHighlight,
                    x1:=bounds.Right - _pushButtonWidth,
                    y1:=dividerLineVerticalPadding,
                    x2:=bounds.Right - _pushButtonWidth,
                    y2:=bounds.Bottom - dividerLineVerticalPadding)

                g.DrawLine(SystemPens.ButtonShadow,
                    x1:=bounds.Right - _pushButtonWidth - singlePixelWidth,
                    y1:=dividerLineVerticalPadding,
                    x2:=bounds.Right - _pushButtonWidth - singlePixelWidth,
                    y2:=bounds.Bottom - dividerLineVerticalPadding)

                bounds.Width = bounds.Width - _pushButtonWidth
            End If

            PaintArrow(g, _dropDownRectangle)

            'If we don't use mnemonic, set formatFlag to NoPrefix as this will show the ampersand
            If Not UseMnemonic Then
                FormatFlags = FormatFlags Or TextFormatFlags.NoPrefix

                'else if we don't show keyboard cues, set formatFlag to HidePrefix as this will hide
                'the ampersand if we don't press down the alt key
            ElseIf Not ShowKeyboardCues Then
                FormatFlags = FormatFlags Or TextFormatFlags.HidePrefix
            End If

            If Not String.IsNullOrEmpty(Me.Text) Then
                Dim foreColor = SystemColors.ControlText
                If Not Me.Enabled Then
                    foreColor = SystemColors.GrayText
                End If

                TextRenderer.DrawText(g, Me.Text, Me.Font, bounds, foreColor, FormatFlags)
            End If

            If Focused Then
                bounds.Inflate(
                    width:=DpiHelper.LogicalToDeviceUnitsX(-4),
                    height:=DpiHelper.LogicalToDeviceUnitsY(-4))

                ControlPaint.DrawFocusRectangle(g, bounds)
            End If
        End Sub

        Protected Overrides Sub OnKeyDown(ByVal kevent As KeyEventArgs)
            If kevent.KeyCode.Equals(Keys.Down) Then
                ShowContextMenuOrContextMenuStrip()
            End If
        End Sub

        Protected Overrides Function IsInputKey(ByVal keyData As Keys) As Boolean
            If keyData.Equals(Keys.Down) Then
                Return True
            End If
            Return MyBase.IsInputKey(keyData)
        End Function

        Protected Overrides Sub OnEnabledChanged(ByVal e As EventArgs)
            SetButtonDrawState()
            MyBase.OnEnabledChanged(e)
        End Sub

        Protected Overrides Sub OnGotFocus(ByVal e As EventArgs)
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.Default
            End If
        End Sub

        Protected Overrides Sub OnLostFocus(ByVal e As EventArgs)
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.Normal
            End If
        End Sub

        Protected Overrides Sub OnMouseEnter(ByVal e As EventArgs)
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.Hot
            End If
        End Sub

        Protected Overrides Sub OnMouseLeave(ByVal e As EventArgs)
            If Not State.Equals(PushButtonState.Disabled) AndAlso Not State.Equals(PushButtonState.Pressed) Then
                If Me.Focused Then
                    State = PushButtonState.Default
                Else
                    State = PushButtonState.Normal
                End If
            End If
        End Sub

        Public Overrides Function GetPreferredSize(ByVal proposedSize As Size) As Size
            Dim preferredSize = MyBase.GetPreferredSize(proposedSize)
            If Not String.IsNullOrEmpty(Me.Text) AndAlso ((TextRenderer.MeasureText(Me.Text, Me.Font).Width + _pushButtonWidth) > preferredSize.Width) Then
                Return preferredSize + New Size(_pushButtonWidth, 0)
            End If

            Return preferredSize
        End Function

        Protected Overrides Sub OnMouseDown(ByVal mevent As MouseEventArgs)
            _showCustomContextMenuWasHandled = False
            If _dropDownRectangle.Contains(mevent.Location) Then
                ShowContextMenuOrContextMenuStrip()
            Else
                State = PushButtonState.Pressed
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(ByVal mevent As MouseEventArgs)
            If _showCustomContextMenuWasHandled Then
                Return
            End If

            If Me.ContextMenuStrip Is Nothing OrElse Not Me.ContextMenuStrip.Visible Then
                SetButtonDrawState()
                If Me.Bounds.Contains(Me.Parent.PointToClient(Cursor.Position)) And Not (_dropDownRectangle.Contains(mevent.Location) AndAlso Me.ContextMenuStrip IsNot Nothing) Then
                    MyBase.OnClick(New EventArgs())
                End If
            End If
        End Sub

        Private Sub ShowContextMenuOrContextMenuStrip()
            State = PushButtonState.Pressed
            If Me.ContextMenuStrip IsNot Nothing Then
                AddHandler Me.ContextMenuStrip.Closed, AddressOf Me.ContextMenuStrip_Closed
                Me.ContextMenuStrip.Show(Me, 0, Me.Height)
            Else
                Dim e As New ShowCustomContextMenuEventArgs
                Try
                    RaiseEvent ShowCustomContextMenu(e)
                Catch ex As Exception
                End Try

                If e.Handled Then
                    _showCustomContextMenuWasHandled = True
                End If

                SetButtonDrawState()
            End If
        End Sub

        Private Sub ContextMenuStrip_Closed(ByVal sender As Object, ByVal e As ToolStripDropDownClosedEventArgs)
            Dim cms = CType(sender, ContextMenuStrip)
            RemoveHandler cms.Closed, AddressOf Me.ContextMenuStrip_Closed

            SetButtonDrawState()
        End Sub

        Private Sub SetButtonDrawState()
            If Not Me.Enabled Then
                State = PushButtonState.Disabled
            ElseIf Me.Bounds.Contains(Me.Parent.PointToClient(Cursor.Position)) Then
                State = PushButtonState.Hot
            ElseIf Me.Focused Then
                State = PushButtonState.Default
            Else
                State = PushButtonState.Normal
            End If
        End Sub

        Private Property State() As PushButtonState
            Get
                Return _state
            End Get
            Set(ByVal value As PushButtonState)
                If Not _state.Equals(value) Then
                    _state = value
                    Invalidate()
                End If
            End Set
        End Property

        Private Sub PaintArrow(ByVal g As Graphics, ByVal dropDownRect As Rectangle)
            Dim middle = New Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2), Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2))

            ' if the width is odd - favor pushing it over one pixel right.
            middle.X += (dropDownRect.Width Mod 2)

            Dim leftOffset = DpiHelper.LogicalToDeviceUnitsX(3)
            Dim rightOffset = DpiHelper.LogicalToDeviceUnitsX(3)
            Dim topOffset = DpiHelper.LogicalToDeviceUnitsY(1)
            Dim bottomOffset = DpiHelper.LogicalToDeviceUnitsY(2)

            Dim arrow = New Point(2) {
                New Point(
                    x:=middle.X - leftOffset,
                    y:=middle.Y - topOffset),
                New Point(
                    x:=middle.X + rightOffset,
                    y:=middle.Y - topOffset),
                New Point(
                    x:=middle.X,
                    y:=middle.Y + bottomOffset)
            }

            g.FillPolygon(SystemBrushes.ControlText, arrow)
        End Sub

    End Class

End Namespace

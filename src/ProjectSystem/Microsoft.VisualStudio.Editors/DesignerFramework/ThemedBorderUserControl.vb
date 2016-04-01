' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles

Namespace Microsoft.VisualStudio.Editors.DesignerFramework
    Friend Class ThemedBorderUserControl
        Private _borderPen As Pen
        Private Const s_WS_EX_CLIENTEDGE As Integer = &H200
        Private Const s_WS_BORDER As Integer = &H800000

        Public Sub New()
            _borderPen = New Pen(VisualStyleInformation.TextControlBorder)
        End Sub

        Protected Overrides ReadOnly Property CreateParams() As CreateParams
            Get
                Dim cp As CreateParams = MyBase.CreateParams
                cp.ExStyle = cp.ExStyle And Not s_WS_EX_CLIENTEDGE
                cp.Style = cp.Style And Not s_WS_BORDER
                If Not UseVisualStyles Then

                    Select Case Me.BorderStyle
                        Case System.Windows.Forms.BorderStyle.Fixed3D
                            cp.ExStyle = cp.ExStyle Or s_WS_EX_CLIENTEDGE
                        Case System.Windows.Forms.BorderStyle.FixedSingle
                            cp.Style = cp.Style Or s_WS_BORDER
                    End Select
                End If
                Return cp
            End Get
        End Property

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
                _borderPen.Dispose()
            End If

            MyBase.Dispose(disposing)
        End Sub

        Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
            ' we have to get a new pen everytime
            _borderPen.Dispose()
            _borderPen = New Pen(VisualStyleInformation.TextControlBorder)
            If UseVisualStyles Then
                If Me.BorderStyle = Windows.Forms.BorderStyle.Fixed3D Then
                    e.Graphics.DrawRectangle(_borderPen, New Rectangle(0, 0, Width - 1, Height - 1))
                End If
            End If
            MyBase.OnPaint(e)
        End Sub

        Protected Overrides ReadOnly Property DefaultPadding() As System.Windows.Forms.Padding
            Get
                Return New Padding(1)
            End Get
        End Property

        Private ReadOnly Property UseVisualStyles() As Boolean
            Get
                Return VisualStyles.VisualStyleRenderer.IsSupported
            End Get
        End Property



    End Class
End Namespace

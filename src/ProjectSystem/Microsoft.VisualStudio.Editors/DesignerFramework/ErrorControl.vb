' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.DesignerFramework


    ''' <summary>
    ''' This is a Windows control that is shown when there is an exception loading a designer or property page.
    ''' All it does is display an error message and an error icon.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ErrorControl

        Private _firstGotFocus As Boolean = True
        Private _sizingLabel As Label

        Friend Sub New()
            ' This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Add any initialization after the InitializeComponent() call.
            Me.IconGlyph.Image = SystemIcons.Error.ToBitmap()

            ' A label used for determining the preferred size of the text in the textbox
            _sizingLabel = New Label()
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Text">The error text to display</param>
        ''' <remarks></remarks>
        Friend Sub New(ByVal Text As String)
            Me.New()
            Me.Text = Text
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ex">The exception to display</param>
        ''' <remarks></remarks>
        Friend Sub New(ByVal ex As Exception)
            Me.New(Common.DebugMessageFromException(ex))
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="errors">A list of exceptions or error messages to display</param>
        ''' <remarks></remarks>
        Friend Sub New(ByVal errors As ICollection)
            Me.New()

            Dim TextBuilder As New System.Text.StringBuilder

            For Each er As Object In errors
                TextBuilder.Append(er.ToString())
                TextBuilder.Append(Microsoft.VisualBasic.vbCrLf)
            Next

            Text = TextBuilder.ToString()
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides Property Text() As String
            Get
                Return ErrorText.Text
            End Get
            Set(ByVal value As String)
                MyBase.Text = value
                Me.ErrorText.Text = value
            End Set
        End Property


        ''' <summary>
        ''' Fires when the ErrorText gets focus
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ErrorText_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles ErrorText.GotFocus
            If _firstGotFocus Then
                'The first time a textbox gets focus, WinForms selects all text in it.  That
                '  doesn't really make sense in this case, so set it back to no selection.
                Me.ErrorText.SelectionLength = 0
                Me.ErrorText.SelectionStart = Me.ErrorText.Text.Length
                _firstGotFocus = False
            End If
        End Sub


        ''' <summary>
        ''' Get the preferred size of the control, expanding 
        ''' </summary>
        ''' <param name="proposedSize"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function GetPreferredSize(ByVal proposedSize As System.Drawing.Size) As System.Drawing.Size
            If proposedSize.Width = 0 Then
                Return MyBase.GetPreferredSize(proposedSize)
            End If

            Dim sizeBeyondTheTextbox As Size = Drawing.Size.Subtract(Me.Size, Me.ErrorText.Size)

            'Use a label of the same size to determine the preferred size.  We use the
            '  suggested width, and expand the height as needed.
            _sizingLabel.Font = Me.ErrorText.Font
            _sizingLabel.Text = Me.ErrorText.Text & Microsoft.VisualBasic.vbCrLf & Microsoft.VisualBasic.vbCrLf & " " 'Add an extra line of buffer
            _sizingLabel.Width = proposedSize.Width - sizeBeyondTheTextbox.Width
            _sizingLabel.AutoSize = False

            Dim textPreferredSize As Size = _sizingLabel.GetPreferredSize(New Size(_sizingLabel.Width, 0))
            Return Drawing.Size.Add(textPreferredSize, sizeBeyondTheTextbox)
        End Function

    End Class

End Namespace

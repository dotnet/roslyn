Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms
Namespace Microsoft.VisualStudio.Editors.Package
    Public Class EditorToolsOptionsPanel

        Private _tabSize As Integer
        Private _indentSize As Integer

        '*
        '* Only allow numerical (and control i.e. backspace) characters
        '*
        Private Sub FilterNumericInput(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) _
                Handles _tabSizeTextBox.KeyPress, _indentSizeTextBox.KeyPress
            If Char.IsDigit(e.KeyChar) Or Char.IsControl(e.KeyChar) Then
                e.Handled = False
            Else
                e.Handled = True
            End If
        End Sub

        '*
        '* Set property IndentSize. 
        '* On GET
        '*  Get property
        '* On SET
        '*  Update UI (TextBox) to reflect new settings
        '*  Update previous valid setting to new value
        '*
        Friend Property IndentSize() As Integer
            Get
                IndentSize = _indentSize
            End Get
            Set(ByVal Value As Integer)
                _IndentSizeTextBox.Text = Value.ToString()
                _indentSize = Value
            End Set
        End Property

        '*
        '* Property IndentType. 
        '* On GET
        '*  Get property
        '* On SET
        '*  Update UI (RadioButtons) to reflect new settings
        '*
        Friend Property IndentType() As EnvDTE.vsIndentStyle
            Get
                If _indentTypeBlockRadioButton.Checked Then
                    IndentType = EnvDTE.vsIndentStyle.vsIndentStyleDefault
                ElseIf _indentTypeNoneRadioButton.Checked Then
                    IndentType = EnvDTE.vsIndentStyle.vsIndentStyleNone
                Else
                    IndentType = EnvDTE.vsIndentStyle.vsIndentStyleSmart
                End If
            End Get
            Set(ByVal Value As EnvDTE.vsIndentStyle)
                _indentTypeBlockRadioButton.Checked = (Value = EnvDTE.vsIndentStyle.vsIndentStyleDefault)
                _indentTypeNoneRadioButton.Checked = (Value = EnvDTE.vsIndentStyle.vsIndentStyleNone)
                _indentTypeSmartRadioButton.Checked = (Value = EnvDTE.vsIndentStyle.vsIndentStyleSmart)
            End Set
        End Property

        Friend Property LineNumbers() As Boolean
            Get
                LineNumbers = _LineNumbersCheckBox.Checked
            End Get
            Set(ByVal Value As Boolean)
                _LineNumbersCheckBox.Checked = Value
            End Set
        End Property

        '*
        '* Set property TabSize. 
        '* On GET
        '*  Get property
        '* On SET
        '*  Update UI (TextBox) to reflect new settings
        '*  Update previous valid setting to new value
        '*
        Friend Property TabSize() As Integer
            Get
                TabSize = _tabSize
            End Get
            Set(ByVal Value As Integer)
                _TabSizeTextBox.Text = Value.ToString()
                _tabSize = Value
            End Set
        End Property

        Friend Property WordWrap() As Boolean
            Get
                WordWrap = _WordWrapCheckBox.Checked
            End Get
            Set(ByVal Value As Boolean)
                _WordWrapCheckBox.Checked = Value
            End Set
        End Property


        '*
        '* Try to set TabSize to the value in the tabsize text box. If failure (i.e non-numerical value)
        '* reset the contents of the text box to previous value...
        '*
        Private Sub tabSizeTextBox_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles _tabSizeTextBox.TextChanged
            Try
                TabSize = UInt16.Parse(_tabSizeTextBox.Text)
            Catch ex As Exception
                ' Revert to old value
                _tabSizeTextBox.Text = _tabSize.ToString()
                _tabSizeTextBox.SelectionStart = _tabSizeTextBox.Text.Length()
            End Try
        End Sub

        '*
        '* Try to set IndentSize to the value in the indentsize text box. If failure (i.e non-numerical value)
        '* reset the contents of the text box to previous value...
        '*
        Private Sub indentSizeTextBox_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles _indentSizeTextBox.TextChanged
            Try
                IndentSize = UInt16.Parse(_indentSizeTextBox.Text)
            Catch ex As Exception
                ' Revert to old value
                _indentSizeTextBox.Text = IndentSize.ToString()
                _indentSizeTextBox.SelectionStart = _indentSizeTextBox.Text.Length()
            End Try
        End Sub

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The exception will be thrown when validation failed...
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ValidationException
        Inherits ApplicationException

        Private _validationResult As ValidationResult
        Private _control As Control

        Public Sub New(ByVal result As ValidationResult, ByVal message As String, Optional ByVal control As Control = Nothing, Optional ByVal InnerException As Exception = Nothing)
            MyBase.New(message, InnerException)

            _validationResult = result
            _control = control
        End Sub

        Public ReadOnly Property Result() As ValidationResult
            Get
                Return _validationResult
            End Get
        End Property

        Public Sub RestoreFocus()
            If _control IsNot Nothing Then
                _control.Focus()
                If TypeOf _control Is TextBox Then
                    CType(_control, TextBox).SelectAll()
                End If
            End If
        End Sub
    End Class
End Namespace

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The exception will be thrown when validation failed...
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ValidationException
        Inherits ApplicationException

        Private m_validationResult As ValidationResult
        Private m_control As Control

        Public Sub New(ByVal result As ValidationResult, ByVal message As String, Optional ByVal control As Control = Nothing, Optional ByVal InnerException As Exception = Nothing)
            MyBase.New(message, InnerException)

            m_validationResult = result
            m_control = control
        End Sub

        Public ReadOnly Property Result() As ValidationResult
            Get
                Return m_validationResult
            End Get
        End Property

        Public Sub RestoreFocus()
            If m_control IsNot Nothing Then
                m_control.Focus()
                If TypeOf m_control Is TextBox Then
                    CType(m_control, TextBox).SelectAll()
                End If
            End If
        End Sub
    End Class
End Namespace

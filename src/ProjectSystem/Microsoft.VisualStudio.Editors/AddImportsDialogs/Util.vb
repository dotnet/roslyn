Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Module Util
        Public Function ProcessMnemonicString(ByVal input As String) As Nullable(Of Char)
            Dim mnemonicChar As Nullable(Of Char) = Nothing
            Dim i As Integer = 0

            While i < input.Length
                If (input(i) = "&"c) Then
                    If (i + 1 < input.Length) Then
                        If (input(i + 1) <> "&"c) Then
                            Exit While
                        Else
                            i += 2
                            Continue While
                        End If
                    End If
                End If
                i += 1
            End While

            If (i < input.Length - 1) Then
                mnemonicChar = input(i + 1)

                Dim first As String = ""
                If (i > 0) Then
                    first = input.Substring(0, i)
                End If

                Dim second As String = ""
                If (i < input.Length - 2) Then
                    second = input.Substring(i + 2, input.Length - i - 2)
                End If
            End If

            Return mnemonicChar
        End Function

        Function NextControl(ByVal c As Control) As Control
            Return CType(c.Tag, ControlNavigationInfo).NextControl
        End Function

        Function PreviousControl(ByVal c As Control) As Control
            Return CType(c.Tag, ControlNavigationInfo).PreviousControl
        End Function

        Sub SetNavigationInfo(ByVal c As Control, ByVal nextControl As Control, ByVal previousControl As Control)
            c.Tag = New ControlNavigationInfo(nextControl, previousControl)
        End Sub
    End Module
End Namespace
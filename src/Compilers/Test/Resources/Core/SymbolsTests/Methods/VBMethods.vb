' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library VBMethods.vb

Public Class C1
    
    Public Sub M1(x As Integer)
    End Sub


    Public Sub M2(<System.Runtime.InteropServices.Optional()> ByVal x As Integer)
    End Sub

    Public Sub M3(Optional x As Integer = 11)
    End Sub

    Public Sub M4(<System.Runtime.InteropServices.Optional(),
                  System.Runtime.InteropServices.DefaultParameterValue(12)> ByVal x As Integer)
    End Sub

    Public Sub M5(ByVal x As Integer)
    End Sub

    Public Sub M6(ByVal x As Integer)
    End Sub

    Public Sub M7(Of T)(ByVal x As Integer)
    End Sub

    Public Sub M8(Of T)(ByVal x As Integer)
    End Sub

    Public Sub M9(ByVal x As Integer)
    End Sub

    Public Sub M9(Of T)(ByVal x As Integer)
    End Sub

    Public Sub M10(Of T1)(ByVal x As T1)
    End Sub

    Public Function M11(Of T2, T3 As C1)(ByVal x As T2) As T3
        Return Nothing
    End Function

    Public Sub M12(ByVal x As Integer)
    End Sub

    Public Declare Auto Function LoadLibrary Lib "Kernel32.dll" (ByVal lpFileName As String) As IntPtr

End Class

Public Structure EmptyStructure

End Structure

Public Class C2(Of T4)

    Public Sub M1(Of T5)(ByVal x As T5, ByVal y As T4)
    End Sub

End Class

Public MustInherit Class Modifiers1

    Public MustOverride Sub M1()

    Public Overridable Sub M2()
    End Sub

    Public Overloads Sub M3()
    End Sub

    Public Sub M4()
    End Sub

    Public Shadows Sub M5()
    End Sub

    Public MustOverride Overloads Sub M6()

    Public Overridable Overloads Sub M7()
    End Sub

    Public MustOverride Shadows Sub M8()

    Public Overridable Shadows Sub M9()
    End Sub
End Class

Public MustInherit Class Modifiers2
    Inherits Modifiers1

    Public MustOverride Overrides Sub M1()

    Public NotOverridable Overrides Sub M2()
    End Sub

    Public MustOverride Overloads Overrides Sub M6()

    Public NotOverridable Overloads Overrides Sub M7()
    End Sub

    ' 'Overrides' and 'Shadows' cannot be combined
    'Public MustOverride Overrides Shadows Sub M8()

    ' 'Overrides' and 'Shadows' cannot be combined
    'Public NotOverridable Overrides Shadows Sub M9()
    'End Sub
End Class

Public MustInherit Class Modifiers3
    Inherits Modifiers1

    Public Overrides Sub M1()
    End Sub

    Public Overloads Overrides Sub M6()
    End Sub

End Class

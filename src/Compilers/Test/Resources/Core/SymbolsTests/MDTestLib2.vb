' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'
' vbc /t:library MDTestLib2.vb /r:MDTestLib1.dll
' 
Public Class TC5(Of TC5_T1, TC5_T2)
    Inherits C1(Of TC5_T1).C2(Of TC5_T2)
End Class

Public Class TC6(Of TC6_T1)
    Inherits C1(Of TC6_T1).C3

    Class TC9(Of TC9_T1)
        Inherits TC6(Of TC6_T1)
    End Class
End Class

Public Class TC7(Of TC7_T1, TC7_T2)
    Inherits C1(Of TC7_T1).C3.C4(Of TC7_T2)
End Class

Public Class TC8
    Inherits C1(Of System.Type)
End Class

Public Class TC10

    Public Sub M1()
    End Sub

    Protected Sub M2(ByVal m1_1 As Integer)
    End Sub

    Protected Function M3 As TC8
        Return Nothing 
    End Function

    Friend Function M4(ByRef x As C1(Of System.Type), ByRef y As TC8) As C1(Of System.Type)
        Return Nothing
    End Function

    Protected Friend Sub M5(ByRef x(,,) As C1(Of System.Type), ByRef  y() As TC8)
    End Sub
End Class

Namespace CorTypes

    Namespace NS ' This namespace will force us to lookup a top level type that hasn't been loaded yet.
        Class Base(Of T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)
        End Class
    End Namespace

    Class Derived
        Inherits NS.Base(Of Boolean,
                      SByte,
                      Byte,
                      Short,
                      UShort,
                      Integer,
                      UInteger,
                      Long,
                      ULong,
                      Single,
                      Double,
                      Char,
                      String, 
                      System.IntPtr, 
                      System.UIntPtr,
                      Object)

    End Class

    Class Base(Of T1, T2)
    End Class

    Class Derived1
        Inherits Base(Of Integer(), Double(,))
    End Class

End Namespace

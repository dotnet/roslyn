' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'vbc /vbruntime- /t:library CL3.vb /r:CL2.dll /out:CL3.dll

Public Class CL3_C1
    Inherits CL2_C1


    Public Shared Function Test1() As Object
        Return Nothing
    End Function

    Public Shared Function Test2() As CL2_C1
        Return Nothing
    End Function

    Public Function Test3() As CL2_C1
        Return Nothing
    End Function
End Class

Public Class CL3_C2
    Public Shared Function Test1() As CL2_C1
        Return Nothing
    End Function

    Public x As CL2_C1

    Public Sub Test1(x As Integer)
    End Sub

    Public Sub Test2(x As Integer)
    End Sub

    Public Shared Function Test3() As CL2_C1
        Return Nothing
    End Function

    Public Shared Sub Test4(x As CL3_C1)
    End Sub

    Public Shared Sub Test4(x As CL3_C3)
    End Sub

    Public y As CL3_C3
    Public z As CL3_C4
    Public u As CL3_C5()
    Public v As System.Action(Of CL3_C5)

    Public w As CL3_D1

    Public Shared Function Test5() As CL3_C2
        Return Nothing
    End Function

    Default Property P1(x As CL3_C3) As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)

        End Set
    End Property

    Property P2 As CL3_C1
        Get
            Return Nothing
        End Get
        Set(value As CL3_C1)

        End Set
    End Property

End Class


Public Class CL3_C3
    Implements CL2_I1, CL2_I2
End Class


Public Class CL3_C4
    Inherits CL3_C1
End Class

Public Class CL3_C5
    Inherits CL3_C3
End Class

Public Delegate Sub CL3_D1(x As CL2_C1)

Public Structure CL3_S1
    Implements CL2_I1
End Structure

Public Interface CL3_I1
    Inherits CL2_I1
End Interface

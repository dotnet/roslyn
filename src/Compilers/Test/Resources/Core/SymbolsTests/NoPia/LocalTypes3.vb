' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' vbc /t:library /vbruntime- LocalTypes3.vb /l:Pia1.dll

Imports System.Collections.Generic

Public Class LocalTypes3

    Public Function Test1() As C31(Of C33).I31(Of C33)
        Return Nothing
    End Function

    Public Function Test2() As C31(Of C33).I31(Of I1)
        Return Nothing
    End Function

    Public Function Test3() As C31(Of I1).I31(Of C33)
        Return Nothing
    End Function

    Public Function Test4() As C31(Of C33).I31(Of I32(Of I1))
        Return Nothing
    End Function

    Public Function Test5() As C31(Of I32(Of I1)).I31(Of C33)
        Return Nothing
    End Function

    Public Function Test6() As List(Of I1)
        Return Nothing
    End Function

End Class


Public Class C31(Of T)
    Public Interface I31(Of S)
    End Interface
End Class

Public Class C32(Of T)
End Class

Public Interface I32(Of S)
End Interface

Public Class C33
End Class

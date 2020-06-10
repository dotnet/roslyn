' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:module /out:Source3Module.netmodule /vbruntime- Source3.vb /r:c1.dll,c4.dll,c7.dll
' vbc /t:library /out:c3.dll /vbruntime- Source3.vb /r:c1.dll,c4.dll,c7.dll


Public Class C3
    Public Function Goo() As C1(Of C3).C2(Of C4)
        Return Nothing
    End Function

    Public Shared Function Bar() As C6(Of C4)
        Return Nothing
    End Function

    Public Function Goo1() As C8(Of C7)
        Return Nothing
    End Function

    Public Sub Goo2(ByRef x1(,) As C300,
                    <System.Runtime.InteropServices.Out()> ByRef x2 As C4,
                    ByRef x3() As C7,
                    Optional ByVal x4 As C4 = Nothing)
    End Sub

    Friend Overridable Function Goo3(Of TGoo3 As C4)() As TGoo3
        Return Nothing
    End Function

    Public Function Goo4() As C8(Of C4)
        Return Nothing
    End Function

    Public MustInherit Class C301
        Implements I1

    End Class

    Friend Class C302

    End Class

End Class

Public Class C6(Of T As New)
End Class

Public Class C300
End Class

Public Interface I1
End Interface

Namespace ns1

    Namespace ns2

        Public Class C303
        End Class

    End Namespace

    Public Class C304

        Public Class C305
        End Class

    End Class

End Namespace

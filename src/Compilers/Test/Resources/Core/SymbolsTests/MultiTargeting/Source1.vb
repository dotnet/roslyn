' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:module /out:Source1Module.netmodule /vbruntime- Source1.vb
' vbc /t:library /out:c1.dll /vbruntime- Source1.vb

Public Class C1(Of T)
    Public Class C2(Of S)
        Public Function Goo() As C1(Of T).C2(Of S)
            Return Nothing
        End Function
    End Class
End Class

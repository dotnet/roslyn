' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library /vbruntime- LocalTypes2.vb /l:Pia1.dll

Public Class LocalTypes2

    Public Sub Test2(ByVal x As S1, ByVal y As NS1.S2)
    End Sub

End Class

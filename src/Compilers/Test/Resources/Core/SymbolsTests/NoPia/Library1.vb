' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library /vbruntime- /r:LocalTypes1.dll Library1.vb

Class Library1
    Sub Test()
        Dim x As New LocalTypes1
    End Sub
End Class

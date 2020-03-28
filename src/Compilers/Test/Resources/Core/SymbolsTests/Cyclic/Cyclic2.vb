' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'vbc /t:library /vbruntime- Cyclic2.vb /r:Cyclic1.dll

Public Class Class2
Sub Goo
Dim x As New Class1
End Sub
End Class

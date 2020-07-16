' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library /vbruntime- VBFields.vb

Public Class VBFields(Of T)

    Public Shared F1 As T
    Protected ReadOnly F2 As Integer
    Friend F3 As Integer
    Protected Friend F4 As Integer
    Protected Const F5 As Integer = 5

End Class

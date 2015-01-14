' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' vbc /t:library /vbruntime- VBFields.vb

Public Class VBFields(Of T)

    Public Shared F1 As T
    Protected ReadOnly F2 As Integer
    Friend F3 As Integer
    Protected Friend F4 As Integer
    Protected Const F5 As Integer = 5

End Class

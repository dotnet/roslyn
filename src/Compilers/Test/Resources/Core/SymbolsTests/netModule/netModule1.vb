' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'vbc /t:module /vbruntime- netModule1.vb

Public Class Class1

    Public Class Class3

        Private Class Class5
        End Class
    End Class
End Class

Namespace NS1
    Public Class Class4
        Private Class Class6
        End Class

        Public Class Class7
        End Class
    End Class

    Friend Class Class8
    End Class
End Namespace

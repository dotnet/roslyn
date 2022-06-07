' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:Library /out:MTTestLib2.Dll MTTestLib2_V1.vb /r:MTTestLib1.Dll
' vbc /t:module /out:MTTestModule2.netmodule MTTestLib2_V1.vb /r:MTTestLib1.Dll

Public Class Class4
    Function Goo() As Class1
        Return Nothing
    End Function

    Public Bar As Class1

    Public Class Class4_1

    End Class
End Class

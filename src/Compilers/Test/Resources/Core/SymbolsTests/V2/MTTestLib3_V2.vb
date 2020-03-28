' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:Library /out:MTTestLib3.Dll MTTestLib3_V2.vb /r:MTTestLib1.Dll /r:..\V1\MTTestLib2.Dll
' vbc /t:module /out:MTTestModule3.netmodule MTTestLib3_V2.vb /r:MTTestLib1.Dll /r:..\V1\MTTestLib2.Dll

Public Class Class5
    Function Goo1() As Class1
        Return Nothing
    End Function

    Function Goo2() As Class2
        Return Nothing
    End Function

    Function Goo3() As Class4
        Return Nothing
    End Function

    Public Bar1 As Class1
    Public Bar2 As Class2
    Public Bar3 As Class4
End Class

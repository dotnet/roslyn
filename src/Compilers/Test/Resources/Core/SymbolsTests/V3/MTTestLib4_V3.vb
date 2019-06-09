' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' vbc /t:Library /out:MTTestLib4.Dll MTTestLib4_V3.vb /r:MTTestLib1.Dll /r:..\V1\MTTestLib2.Dll /r:..\V2\MTTestLib3.Dll
' vbc /t:module /out:MTTestModule4.netmodule MTTestLib4_V3.vb /r:MTTestLib1.Dll /r:..\V1\MTTestLib2.Dll /r:..\V2\MTTestLib3.Dll

Public Class Class6
    Function Goo1() As Class1
        Return Nothing
    End Function

    Function Goo2() As Class2
        Return Nothing
    End Function

    Function Goo3() As Class3
        Return Nothing
    End Function

    Function Goo4() As Class4
        Return Nothing
    End Function

    Function Goo5() As Class5
        Return Nothing
    End Function

    Public Bar1 As Class1
    Public Bar2 As Class2
    Public Bar3 As Class3
    Public Bar4 As Class4
    Public Bar5 As Class5

End Class

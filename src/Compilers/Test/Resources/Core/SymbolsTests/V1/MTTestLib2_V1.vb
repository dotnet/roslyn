' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

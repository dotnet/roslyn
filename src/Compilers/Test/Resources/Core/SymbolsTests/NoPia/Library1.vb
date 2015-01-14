' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' vbc /t:library /vbruntime- /r:LocalTypes1.dll Library1.vb

Class Library1
    Sub Test()
        Dim x As New LocalTypes1
    End Sub
End Class

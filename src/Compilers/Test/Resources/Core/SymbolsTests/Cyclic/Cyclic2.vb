' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'vbc /t:library /vbruntime- Cyclic2.vb /r:Cyclic1.dll

Public Class Class2
Sub Goo
Dim x As New Class1
End Sub
End Class

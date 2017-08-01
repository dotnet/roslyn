' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'vbc /t:library /vbruntime- MultiModule.vb /addmodule:mod2.netmodule,mod3.netmodule

Public Class Class1
Sub Goo()
Dim x = {1,2}
Dim y = x.Count()
End Sub
End Class

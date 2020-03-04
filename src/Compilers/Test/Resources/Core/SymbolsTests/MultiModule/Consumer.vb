' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'vbc /t:library /vbruntime- Consumer.vb /r:MultiModule.dll

Public Class Derived1
    Inherits Class1
End Class

Public Class Derived2
    Inherits Class2
End Class

Public Class Derived3
    Inherits Class3
End Class

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

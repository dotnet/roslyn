' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'vbc /t:library /out:MTTestLib1.Dll MTTestLib1_V3.vb 
'vbc /t:module /out:MTTestModule1.netmodule MTTestLib1_V3.vb 

<Assembly: System.Reflection.AssemblyVersion("3.0.0.0")> 
<Assembly: System.Reflection.AssemblyFileVersion("3.0.0.0")> 

Public Class Class1

End Class

Public Class Class2

End Class

Public Class Class3

End Class

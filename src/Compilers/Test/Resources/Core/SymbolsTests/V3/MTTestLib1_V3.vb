' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

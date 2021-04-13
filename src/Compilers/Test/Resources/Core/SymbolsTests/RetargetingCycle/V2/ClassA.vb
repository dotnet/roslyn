' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc ClassA.vb /target:library /vbruntime-  /r:ClassB.dll

'<Assembly: System.Reflection.AssemblyVersion("2.0.0.0")> 
'<Assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")> 

Public Class ClassA
	Inherits ClassB
End Class

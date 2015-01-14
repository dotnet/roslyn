' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' vbc ClassA.vb /target:library /vbruntime-  /r:ClassB.dll

'<Assembly: System.Reflection.AssemblyVersion("2.0.0.0")> 
'<Assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")> 

Public Class ClassA
	Inherits ClassB
End Class

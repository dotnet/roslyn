' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /r:Class1.dll /target:library  class2.vb

Public Class C2
	'Inherits C1
End Class

Public Interface I2
	'Inherits I0, I1
End Interface

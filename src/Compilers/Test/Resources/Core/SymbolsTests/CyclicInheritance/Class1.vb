' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /target:library  class1.vb

Public Class C1
	Inherits C2
End Class

Public Interface I0

End Interface

Public Interface I1
	Inherits I2
End Interface

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' csc.exe /target:library /out:TypeForwarderLib.dll TypeForwarder1.cs
' vbc.exe /t:library /r:TypeForwarderLib.dll TypeForwarder.vb
' del TypeForwarderLib.dll
' csc.exe /target:library /out:TypeForwarderBase.dll TypeForwarder2.cs
' csc.exe /target:library /out:TypeForwarderLib.dll /r:TypeForwarderBase.dll  TypeForwarder3.cs

Public Class Derived 
    inherits Base
End Class


Public Class GenericDerived(Of K) 
    inherits GenericBase(Of K)
End Class

Public class GenericDerived1(Of K, L) 
    inherits GenericBase(Of K).NestedGenericBase(Of L)
End Class

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library /vbruntime- C.vb /r:A.dll,B.Dll

Imports System.Collections.Generic
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b5826C")> 
<Assembly: ImportedFromTypeLib("C.dll")> 

<ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f200C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface IC
    Function F1() As IA
    Function F2() As IB
End Interface

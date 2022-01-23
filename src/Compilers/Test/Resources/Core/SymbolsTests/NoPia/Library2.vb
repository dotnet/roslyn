' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' vbc /t:library /vbruntime- /r:Pia1.dll,Pia5.dll Library2.vb

Imports System.Collections.Generic
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58260")>
<Assembly: ImportedFromTypeLib("Library2.dll")>


<ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2002"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface I7
    Function Goo() As List(Of I5)
    Function Bar() As List(Of I1)
End Interface

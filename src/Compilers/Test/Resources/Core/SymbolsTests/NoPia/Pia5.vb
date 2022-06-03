' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58259")> 
<Assembly: ImportedFromTypeLib("Pia5.dll")> 


<ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c05"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface I5
    Function Goo() As List(Of I6)
End Interface

<ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c06"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface I6
End Interface

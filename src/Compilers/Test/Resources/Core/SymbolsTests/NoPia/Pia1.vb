' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


' vbc /t:library /vbruntime- Pia1.vb
' vbc /t:library /vbruntime- Pia1.vb /out:Pia1Copy.dll


Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<Assembly: ImportedFromTypeLib("Pia1.dll")>


<ComImport, Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c01"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface I1
    Sub Sub1(ByVal x As Integer)
End Interface

Public Structure S1
    Public F1 As Integer
End Structure

Namespace NS1
    <ComImport, Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c02"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    Public Interface I2
        Sub Sub1(ByVal x As Integer)
    End Interface

    Public Structure S2
        Public F1 As Integer
    End Structure
End Namespace

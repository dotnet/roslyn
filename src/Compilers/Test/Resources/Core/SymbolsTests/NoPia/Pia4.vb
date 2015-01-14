' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


' vbc /t:library /vbruntime- Pia4.vb


Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")> 
<Assembly: ImportedFromTypeLib("Pia4.dll")> 


<ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c01"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface I1
    Sub Sub1(ByVal x As Integer)
End Interface

Public Structure S1
    Public F1 As Integer
End Structure

Namespace NS1
    <ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c02"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    Public Interface I2
        Sub Sub1(ByVal x As Integer)
    End Interface

    Public Structure S2
        Public F1 As Integer
    End Structure
End Namespace

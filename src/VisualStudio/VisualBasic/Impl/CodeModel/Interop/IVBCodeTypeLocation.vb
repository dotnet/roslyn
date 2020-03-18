' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("69A529CD-84E3-4ee8-9918-A540CB827993")>
    Friend Interface IVBCodeTypeLocation
        ReadOnly Property ExternalLocation As String
    End Interface
End Namespace

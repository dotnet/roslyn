' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("69A529CD-84E3-4ee8-9918-A540CB827993")>
    Friend Interface IVBCodeTypeLocation
        ReadOnly Property ExternalLocation As String
    End Interface
End Namespace

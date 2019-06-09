' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("ECB551D4-4493-4e5b-8CAC-7279967E11A9")>
    Friend Interface IVBPartialMethodExtender
        ReadOnly Property IsPartial As Boolean
        ReadOnly Property IsDeclaration As Boolean
    End Interface
End Namespace

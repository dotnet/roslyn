﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("B9C808ED-D30A-4cff-9253-8E0F3669656A")>
    Friend Interface IVBAutoPropertyExtender
        ReadOnly Property IsAutoImplemented As Boolean
    End Interface
End Namespace

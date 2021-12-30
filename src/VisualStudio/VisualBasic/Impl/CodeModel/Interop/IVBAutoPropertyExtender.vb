' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("B9C808ED-D30A-4cff-9253-8E0F3669656A")>
    Friend Interface IVBAutoPropertyExtender
        ReadOnly Property IsAutoImplemented As Boolean
    End Interface
End Namespace

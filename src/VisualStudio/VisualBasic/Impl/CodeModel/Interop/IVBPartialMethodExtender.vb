' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

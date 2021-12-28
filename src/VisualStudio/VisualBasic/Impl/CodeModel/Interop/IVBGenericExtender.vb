' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
    <ComImport>
    <InterfaceType(ComInterfaceType.InterfaceIsDual)>
    <Guid("2AD71E0D-AD9E-4735-BD4A-AA9AC430A883")>
    Friend Interface IVBGenericExtender
        ReadOnly Property GetBaseTypesCount As Integer
        ReadOnly Property GetBaseGenericName(index As Integer) As String
        ReadOnly Property GetImplementedTypesCount As Integer
        ReadOnly Property GetImplTypeGenericName(index As Integer) As String
    End Interface
End Namespace

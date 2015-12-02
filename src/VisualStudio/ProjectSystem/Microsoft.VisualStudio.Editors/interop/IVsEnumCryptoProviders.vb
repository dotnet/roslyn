'//------------------------------------------------------------------------------
'/// <copyright file="IVsStrongNameKeys.cs" company="Microsoft">
'//     Copyright (c) Microsoft Corporation.  All rights reserved.
'// </copyright>                                                                
'//------------------------------------------------------------------------------

#If 0 Then
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports System

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), Guid("f7fc33a9-10da-42be-9f88-9700e583ec33"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    Friend Interface IVsEnumCryptoProviders
        <PreserveSig()> _
        Function [Next](<[In]()> ByVal celt As UInteger, <Out(), MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.BStr, sizeParamIndex:=0)> ByVal Providers As String(), <Out()> ByRef celtFetched As UInteger) As Integer
        <PreserveSig()> _
                Function Reset() As Integer
    End Interface

End Namespace
#End If

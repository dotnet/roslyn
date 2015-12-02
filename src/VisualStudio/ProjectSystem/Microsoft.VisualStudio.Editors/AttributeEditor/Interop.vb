'******************************************************************************
'    Copyright (C) 2002-2003, Microsoft Corporation.  All Rights Reserved.
'    Information Contained Herein Is Proprietary and Confidential.
'
'    Purpose:
'
'        This file defines the interop interfaces for the Permission Service
'
'******************************************************************************
Option Strict On
Option Explicit On 

Imports Microsoft.VisualStudio.Shell
Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.VBAttributeEditor.Interop

    '--------------------------------------------------------------------------
    ' IVbPermissionSetService:
    '     Interface for the permission set service
    '     Must be kept in sync with its unmanaged version in vbidl.idl
    '--------------------------------------------------------------------------
    <GuidAttribute("9DDDA35B-A903-4eca-AAFF-5716AF592D74")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <CLSCompliant(False)> _
    <ComImport()> _
    Friend Interface IVbPermissionSetService

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function ComputeZonePermissionSet( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal strAppManifestFileName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal strTargetZone As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal strExcludedPermissions As String) _
            As <MarshalAs(UnmanagedType.IUnknown)> Object

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function IsAvailableInProject( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal strPermissionSet As String, _
            <InAttribute(), MarshalAs(UnmanagedType.IUnknown)> ByVal ProjectPermissionSet As Object, _
            <OutAttribute(), MarshalAs(UnmanagedType.Bool)> ByRef isAvailable As Boolean) _
            As Integer

        ' Returns S_FALSE if there is no tip
        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function GetRequiredPermissionsTip( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal strPermissionSet As String, _
            <OutAttribute(), MarshalAs(UnmanagedType.BStr)> ByRef strTip As String) _
            As Integer

    End Interface

End Namespace

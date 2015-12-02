'******************************************************************************
'* ILangInactiveCfgPropertyNotifySink.vb
'*
'* Copyright (C) Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports System

Imports Microsoft.VisualStudio.OLE.Interop

Namespace Microsoft.VisualStudio.Editors.Interop


    <ComImport(), System.Runtime.InteropServices.Guid("20bd130e-bcd6-4977-a7da-121555dca33b"), _
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), _
    CLSCompliant(False), ComVisible(False)> _
        Friend Interface ILangInactiveCfgPropertyNotifySink

        <PreserveSig()> _
        Function OnChanged(ByVal dispid As Integer, <MarshalAs(UnmanagedType.LPWStr)> ByVal wszConfigName As String) As Integer

    End Interface

End Namespace

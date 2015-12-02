'-----------------------------------------------------------------------
' <copyright file="VBReferenceChangedService.vb" company="Microsoft Corporation">
'     Copyright (c) Microsoft Corporation.  All rights reserved.
'     Information Contained Herein is Proprietary and Confidential.
' </copyright>
'-----------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Shell.Interop

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Editors.Package

Namespace Microsoft.VisualStudio.Editors.VBRefChangedSvc

    ''' ;VBReferenceChangedService
    ''' <summary>
    ''' This service will be called by the VB compiler when a reference change ocurred in the VB Project.
    ''' This will initiate the My Extensibility service.
    ''' </summary>
    ''' <remarks>
    ''' - This service is exposed in vbpackage.vb.
    ''' - Registration for this service is in SetupAuthoring\vb\registry\Microsoft.VisualStudio.Eidtors.vrg_33310.ddr
    '''   and Microsoft.VisualStudio.Editors.vbexpress.vrg_33310.ddr.
    ''' </remarks>
    <CLSCompliant(False)> _
    Friend Class VBReferenceChangedService
        Implements Interop.IVbReferenceChangedService

        Public Sub New()
        End Sub

        Private Function ReferenceAdded( _
            <[In](), MarshalAs(UnmanagedType.IUnknown)> ByVal pHierarchy As Object, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyPath As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyName As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyVersion As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyInfo As String _
        ) As Integer _
        Implements Interop.IVbReferenceChangedService.ReferenceAdded

            MyExtensibility.MyExtensibilitySolutionService.Instance.ReferenceAdded( _
                TryCast(pHierarchy, IVsHierarchy), strAssemblyInfo)

            Return NativeMethods.S_OK
        End Function

        Private Function ReferenceRemoved( _
            <[In](), MarshalAs(UnmanagedType.IUnknown)> ByVal pHierarchy As Object, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyPath As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyName As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyVersion As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> ByVal strAssemblyInfo As String _
        ) As Integer _
        Implements Interop.IVbReferenceChangedService.ReferenceRemoved

            MyExtensibility.MyExtensibilitySolutionService.Instance.ReferenceRemoved( _
                TryCast(pHierarchy, IVsHierarchy), strAssemblyInfo)

            Return NativeMethods.S_OK
        End Function
    End Class

End Namespace


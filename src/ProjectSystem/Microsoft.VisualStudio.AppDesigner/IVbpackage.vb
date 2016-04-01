' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.AppDesInterop


Namespace Microsoft.VisualStudio.Editors

    Public Interface IVBPackage


        Function GetLastShownApplicationDesignerTab(ByVal projectHierarchy As IVsHierarchy) As Integer

        Sub SetLastShownApplicationDesignerTab(ByVal projectHierarchy As IVsHierarchy, ByVal tab As Integer)

        ReadOnly Property GetService(ByVal serviceType As Type) As Object

        ReadOnly Property MenuCommandService() As IMenuCommandService

    End Interface


    Public Class VBPackageUtils

        Private Shared s_editorsPackage As IVBPackage
        Public Delegate Function getServiceDelegate(ByVal ServiceType As Type) As Object
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="GetService"></param>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property PackageInstance(ByVal GetService As getServiceDelegate) As IVBPackage
            Get
                If s_editorsPackage Is Nothing Then
                    Dim shell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                    Dim pPackage As IVsPackage = Nothing
                    If shell IsNot Nothing Then
                        Dim hr As Integer = shell.IsPackageLoaded(New Guid(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.VBPackage_GUID), pPackage)
                        Debug.Assert(NativeMethods.Succeeded(hr) AndAlso pPackage IsNot Nothing, "VB editors package not loaded?!?")
                    End If

                    s_editorsPackage = TryCast(pPackage, IVBPackage)
                End If
                Return s_editorsPackage
            End Get
        End Property
    End Class
End Namespace

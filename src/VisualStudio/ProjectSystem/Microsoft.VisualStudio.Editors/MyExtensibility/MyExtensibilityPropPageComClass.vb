'--------------------------------------------------------------------
' <copyright file="MyExtensibilityPropPageComClass.vb" company="Microsoft">
'    Copyright (c) Microsoft Corporation.  All rights reserved.
'    Information Contained Herein Is Proprietary and Confidential.
' </copyright>
'--------------------------------------------------------------------
Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.InteropServices
Imports Res = My.Resources.MyExtensibilityRes

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' ;MyExtensibilityPropPageComClass
    ''' <summary>
    ''' COM class for My Extensions property page.
    ''' </summary>
    <Guid("F24459FC-E883-4A8E-9DA2-AEF684F0E1F4"), _
    ComVisible(True), _
    CLSCompliant(False)> _
    Public Class MyExtensibilityPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(MyExtensibilityPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As System.Windows.Forms.Control
            Return New MyExtensibilityPropPage()
        End Function

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return Res.PropertyPageTab
            End Get
        End Property
    End Class
End Namespace

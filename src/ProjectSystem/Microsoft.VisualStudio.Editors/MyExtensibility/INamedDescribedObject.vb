'--------------------------------------------------------------------
' <copyright file="INamedDescribedObject.vb" company="Microsoft">
'    Copyright (c) Microsoft Corporation.  All rights reserved.
'    Information Contained Herein Is Proprietary and Confidential.
' </copyright>
'--------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;INamedDescribedObject
    ''' <summary>
    ''' Shared interface implemented by MyExtensionsProjectFile and MyExtensionTemplate
    ''' to display them in a list view / list box.
    ''' </summary>
    Friend Interface INamedDescribedObject
        ReadOnly Property DisplayName() As String
        ReadOnly Property Description() As String
    End Interface
End Namespace

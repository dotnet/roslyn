' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
    <ComVisible(True)>
    <ComDefaultInterface(GetType(IVBCodeTypeLocation))>
    Public Class CodeTypeLocationExtender
        Implements IVBCodeTypeLocation

        Friend Shared Function Create(externalLocation As String) As IVBCodeTypeLocation
            Dim result = New CodeTypeLocationExtender(externalLocation)
            Return CType(ComAggregate.CreateAggregatedObject(result), IVBCodeTypeLocation)
        End Function

        Private Sub New(externalLocation As String)
            Me.ExternalLocation = externalLocation
        End Sub

        Public ReadOnly Property ExternalLocation As String Implements IVBCodeTypeLocation.ExternalLocation
    End Class
End Namespace

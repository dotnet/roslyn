' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private ReadOnly _externalLocation As String

        Private Sub New(externalLocation As String)
            _externalLocation = externalLocation
        End Sub

        Public ReadOnly Property ExternalLocation As String Implements IVBCodeTypeLocation.ExternalLocation
            Get
                Return _externalLocation
            End Get
        End Property
    End Class
End Namespace

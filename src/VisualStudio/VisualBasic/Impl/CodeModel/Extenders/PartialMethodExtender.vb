' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
    <ComVisible(True)>
    <ComDefaultInterface(GetType(IVBPartialMethodExtender))>
    Public Class PartialMethodExtender
        Implements IVBPartialMethodExtender

        Friend Shared Function Create(isDeclaration As Boolean, isPartial As Boolean) As IVBPartialMethodExtender
            Dim result = New PartialMethodExtender(isDeclaration, isPartial)
            Return CType(ComAggregate.CreateAggregatedObject(result), IVBPartialMethodExtender)
        End Function

        Private ReadOnly _isDeclaration As Boolean
        Private ReadOnly _isPartial As Boolean

        Private Sub New(isDeclaration As Boolean, isPartial As Boolean)
            _isDeclaration = isDeclaration
            _isPartial = isPartial
        End Sub

        Public ReadOnly Property IsDeclaration As Boolean Implements IVBPartialMethodExtender.IsDeclaration
            Get
                Return _isDeclaration
            End Get
        End Property

        Public ReadOnly Property IsPartial As Boolean Implements IVBPartialMethodExtender.IsPartial
            Get
                Return _isPartial
            End Get
        End Property
    End Class
End Namespace

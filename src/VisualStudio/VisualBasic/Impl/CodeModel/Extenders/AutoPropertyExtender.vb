' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
    <ComVisible(True)>
    <ComDefaultInterface(GetType(IVBAutoPropertyExtender))>
    Public Class AutoPropertyExtender
        Implements IVBAutoPropertyExtender

        Friend Shared Function Create(isAutoImplemented As Boolean) As IVBAutoPropertyExtender
            Dim result = New AutoPropertyExtender(isAutoImplemented)
            Return CType(ComAggregate.CreateAggregatedObject(result), IVBAutoPropertyExtender)
        End Function

        Private ReadOnly _isAutoImplemented As Boolean

        Private Sub New(isAutoImplemented As Boolean)
            _isAutoImplemented = isAutoImplemented
        End Sub

        Public ReadOnly Property IsAutoImplemented As Boolean Implements IVBAutoPropertyExtender.IsAutoImplemented
            Get
                Return _isAutoImplemented
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from well-known custom attributes applied on an event.
    ''' </summary>
    Friend Class EventWellKnownAttributeData
        Inherits CommonEventWellKnownAttributeData

#Region "NonSerializedAttribute"
        Private _hasNonSerializedAttribute As Boolean = False
        Friend Property HasNonSerializedAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasNonSerializedAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasNonSerializedAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace

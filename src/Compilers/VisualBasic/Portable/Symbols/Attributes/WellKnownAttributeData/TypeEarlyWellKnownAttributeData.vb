' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from early well-known custom attributes applied on a type.
    ''' </summary>
    Friend Class TypeEarlyWellKnownAttributeData
        Inherits CommonTypeEarlyWellKnownAttributeData

        Private _hasVisualBasicEmbeddedAttribute As Boolean = False
        Friend Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasVisualBasicEmbeddedAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasVisualBasicEmbeddedAttribute = value
                SetDataStored()
            End Set
        End Property

        Private _hasAttributeForExtensibleInterface As Boolean = False
        Friend Property HasAttributeForExtensibleInterface As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasAttributeForExtensibleInterface
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasAttributeForExtensibleInterface = value
                SetDataStored()
            End Set
        End Property

    End Class
End Namespace

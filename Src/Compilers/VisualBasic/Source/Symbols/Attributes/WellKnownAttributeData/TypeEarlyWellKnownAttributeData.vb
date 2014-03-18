' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from early well-known custom attributes applied on a type.
    ''' </summary>
    Friend Class TypeEarlyWellKnownAttributeData
        Inherits CommonTypeEarlyWellKnownAttributeData

        Private m_hasEmbeddedAttribute As Boolean = False
        Friend Property HasEmbeddedAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasEmbeddedAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasEmbeddedAttribute = value
                SetDataStored()
            End Set
        End Property

        Private m_hasAttributeForExtensibleInterface As Boolean = False
        Friend Property HasAttributeForExtensibleInterface As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasAttributeForExtensibleInterface
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasAttributeForExtensibleInterface = value
                SetDataStored()
            End Set
        End Property

    End Class
End Namespace
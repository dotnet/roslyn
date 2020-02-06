' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from early well-known custom attributes applied on a method.
    ''' </summary>
    Friend Class MethodEarlyWellKnownAttributeData
        Inherits CommonMethodEarlyWellKnownAttributeData

#Region "ExtensionAttribute"
        Private _isExtensionMethod As Boolean = False
        Friend Property IsExtensionMethod As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._isExtensionMethod
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._isExtensionMethod = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace

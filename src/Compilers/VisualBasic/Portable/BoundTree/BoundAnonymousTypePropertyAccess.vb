' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundAnonymousTypePropertyAccess

        Private ReadOnly _lazyPropertySymbol As New Lazy(Of PropertySymbol)(AddressOf LazyGetProperty)

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me._lazyPropertySymbol.Value
            End Get
        End Property

        Private Function LazyGetProperty() As PropertySymbol
            Return Me.Binder.GetAnonymousTypePropertySymbol(Me.PropertyIndex)
        End Function

    End Class

End Namespace

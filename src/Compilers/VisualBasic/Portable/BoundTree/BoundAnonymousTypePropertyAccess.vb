' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundAnonymousTypePropertyAccess

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

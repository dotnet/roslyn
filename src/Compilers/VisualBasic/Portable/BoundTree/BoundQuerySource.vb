' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundQuerySource

        Public Sub New(source As BoundExpression)
            Me.New(source.Syntax, source, source.Type)
            Debug.Assert(source.IsValue() AndAlso Not source.IsLValue)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Expression.ExpressionSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                Return Expression.ResultKind
            End Get
        End Property
    End Class

End Namespace

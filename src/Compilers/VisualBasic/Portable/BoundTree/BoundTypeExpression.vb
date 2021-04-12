' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundTypeExpression

        Public Sub New(syntax As SyntaxNode, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, Nothing, Nothing, type, hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return If(DirectCast(Me.AliasOpt, Symbol), Me.Type)
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundTypeExpression

        Public Sub New(syntax As VisualBasicSyntaxNode, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, Nothing, Nothing, type, hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return If(DirectCast(Me.AliasOpt, Symbol), Me.Type)
            End Get
        End Property
    End Class
End Namespace

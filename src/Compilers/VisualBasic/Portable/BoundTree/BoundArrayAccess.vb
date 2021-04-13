' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundArrayAccess
        Inherits BoundExpression

        Public Sub New(syntax As SyntaxNode, expression As BoundExpression, indices As ImmutableArray(Of BoundExpression), type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, expression, indices, True, type, hasErrors)
        End Sub

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundArrayAccess
            If _IsLValue Then
                Return Update(_Expression, _Indices, False, Type)
            End If

            Return Me
        End Function

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Not Me.Expression.IsLValue)

            For Each index In Me.Indices
                Debug.Assert(Not index.IsLValue)
            Next
        End Sub
#End If
    End Class

End Namespace

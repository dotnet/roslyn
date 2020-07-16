' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitEraseStatement(node As BoundEraseStatement) As BoundNode
            If node.Clauses.Length = 1 Then
                '  create assignment statement
                Dim clause As BoundAssignmentOperator = node.Clauses(0)
                Return Visit(New BoundExpressionStatement(clause.Syntax, clause))
            End If

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            For Each clause As BoundAssignmentOperator In node.Clauses
                '  create assignment statement
                statements.Add(DirectCast(Visit(New BoundExpressionStatement(clause.Syntax, clause)), BoundStatement))
            Next

            Return New BoundStatementList(node.Syntax, statements.ToImmutableAndFree())
        End Function

    End Class
End Namespace

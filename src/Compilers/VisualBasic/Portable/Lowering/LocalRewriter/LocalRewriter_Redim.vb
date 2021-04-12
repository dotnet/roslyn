' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitRedimStatement(node As BoundRedimStatement) As BoundNode
            ' NOTE: bound redim statement node represents a group of redim clauses; each of  
            '       those can be considered as a standalone statement. This rewrite just returns 
            '       the rewritten redim clause in case there is only one of them or groups 
            '       rewritten redim clauses into bound statement list node if there are more
            '
            ' This rewrite cannot be done later because we specify property access in VisitRedimClause 
            ' which need to be rewritten by call rewriter. We also want to see original property access 
            ' nodes to be able to enforce correct UseTwice semantics

            If node.Clauses.Length = 1 Then
                Return Me.Visit(node.Clauses(0))

            Else
                Dim statements = New BoundStatement(node.Clauses.Length - 1) {}
                For i = 0 To node.Clauses.Length - 1
                    statements(i) = DirectCast(Me.Visit(node.Clauses(i)), BoundStatement)
                Next
                Return New BoundStatementList(node.Syntax, statements.AsImmutableOrNull())
            End If
        End Function
    End Class
End Namespace

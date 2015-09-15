' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitExpressionStatement(node As BoundExpressionStatement) As BoundNode

            '  All calls to omitted methods can be removed
            If IsOmittedBoundCall(node.Expression) Then
                Return Nothing
            End If

            Dim rewritten = DirectCast(MyBase.VisitExpressionStatement(node), BoundStatement)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewritten = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, rewritten, canThrow:=True)
            End If

            Return MarkStatementWithSequencePoint(rewritten)
        End Function

        Private Function IsOmittedBoundCall(expression As BoundExpression) As Boolean
            If (Me._flags And RewritingFlags.AllowOmissionOfConditionalCalls) = RewritingFlags.AllowOmissionOfConditionalCalls Then
                Select Case expression.Kind
                    Case BoundKind.ConditionalAccess
                        Return IsOmittedBoundCall(DirectCast(expression, BoundConditionalAccess).AccessExpression)

                    Case BoundKind.Call
                        Return DirectCast(expression, BoundCall).Method.CallsAreOmitted(expression.Syntax, expression.SyntaxTree)
                End Select
            End If

            Return False
        End Function
    End Class
End Namespace

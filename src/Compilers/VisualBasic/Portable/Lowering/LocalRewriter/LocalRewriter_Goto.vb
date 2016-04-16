' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitGotoStatement(node As BoundGotoStatement) As BoundNode

            If node.LabelExpressionOpt IsNot Nothing Then
                ' we are removing the bound label expression from the bound goto because this expression is no longer needed
                ' for the emit phase. It is even doing harm to e.g. the stack depth calculation because this expression
                ' would not need to be pushed to the stack.
                node = node.Update(node.Label, labelExpressionOpt:=Nothing)
            End If

            Dim rewritten = DirectCast(MyBase.VisitGotoStatement(node), BoundStatement)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewritten = Concat(RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax), rewritten)
            End If

            Return MarkStatementWithSequencePoint(rewritten)
        End Function
    End Class
End Namespace


' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitContinueStatement(node As BoundContinueStatement) As BoundNode
            Dim boundGoto As BoundStatement = New BoundGotoStatement(node.Syntax, node.Label, Nothing)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                boundGoto = Concat(RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax), boundGoto)
            End If

            Return MarkStatementWithSequencePoint(boundGoto)
        End Function
    End Class
End Namespace

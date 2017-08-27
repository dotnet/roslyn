' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitExitStatement(node As BoundExitStatement) As BoundNode
            Dim boundGoto As BoundStatement = New BoundGotoStatement(node.Syntax, node.Label, Nothing)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                boundGoto = Concat(RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax), boundGoto)
            End If

            If Instrument(node, boundGoto) Then
                boundGoto = _instrumenterOpt.InstrumentExitStatement(node, boundGoto)
            End If

            Return boundGoto
        End Function
    End Class
End Namespace

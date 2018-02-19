﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("MidAssignmentSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class MidAssignmentSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of AssignmentStatementSyntax)

        Protected Overrides Function GetIntrinsicOperatorDocumentation(node As AssignmentStatementSyntax, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)
            Return {New MidAssignmentDocumentation()}
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) AndAlso
               token.Parent.Kind = SyntaxKind.ArgumentList AndAlso
               token.Parent.IsParentKind(SyntaxKind.MidExpression) AndAlso
               token.Parent.Parent.IsParentKind(SyntaxKind.MidAssignmentStatement)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return False
        End Function

        Protected Overrides Function IsArgumentListToken(node As AssignmentStatementSyntax, token As SyntaxToken) As Boolean
            Return node.Left.IsKind(SyntaxKind.MidExpression) AndAlso
                DirectCast(node.Left, MidExpressionSyntax).ArgumentList.Span.Contains(token.SpanStart) AndAlso
                DirectCast(node.Left, MidExpressionSyntax).ArgumentList.CloseParenToken <> token
        End Function

        Protected Overrides Function GetCurrentArgumentStateWorker(node As SyntaxNode, position As Integer) As SignatureHelpState
            Return MyBase.GetCurrentArgumentStateWorker(DirectCast(DirectCast(node, AssignmentStatementSyntax).Left, MidExpressionSyntax).ArgumentList, position)
        End Function
    End Class
End Namespace

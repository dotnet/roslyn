' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCallReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(optionSet, cancellationToken)
        End Function

        Private Shared Function SimplifyCallStatement(
            callStatement As CallStatementSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExecutableStatementSyntax

            If callStatement.CanRemoveCallKeyword(semanticModel) Then
                Dim leading = callStatement.GetLeadingTrivia()

                Dim resultNode = SyntaxFactory.ExpressionStatement(callStatement.Invocation) _
                             .WithLeadingTrivia(leading)

                resultNode = SimplificationHelpers.CopyAnnotations(callStatement, resultNode)

                Return resultNode
            End If

            ' We don't know how to simplify this.
            Return callStatement
        End Function

    End Class
End Namespace

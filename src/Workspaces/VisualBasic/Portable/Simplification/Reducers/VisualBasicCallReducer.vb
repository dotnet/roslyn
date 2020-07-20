' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCallReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Private Shared ReadOnly s_simplifyCallStatement As Func(Of CallStatementSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyCallStatement

        Private Shared Function SimplifyCallStatement(
            callStatement As CallStatementSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExecutableStatementSyntax

            If callStatement.CanRemoveCallKeyword() Then
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

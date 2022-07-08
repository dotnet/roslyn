' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicNameReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Private Shared ReadOnly s_simplifyName As Func(Of ExpressionSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode) = AddressOf SimplifyName

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Public Overrides Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
            Return True
        End Function

        Private Overloads Shared Function SimplifyName(
            node As ExpressionSyntax,
            semanticModel As SemanticModel,
            options As SimplifierOptions,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            Dim replacementNode As ExpressionSyntax = Nothing
            Dim issueSpan As TextSpan
            If Not ExpressionSimplifier.Instance.TrySimplify(
                node, semanticModel, DirectCast(options, VisualBasicSimplifierOptions),
                replacementNode, issueSpan, cancellationToken) Then

                Return node
            End If

            node = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation)
            Return node.WithoutAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace

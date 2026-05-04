' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend MustInherit Class AbstractVisualBasicReducer
        Inherits AbstractReducer

        Protected Sub New(pool As ObjectPool(Of IReductionRewriter))
            MyBase.New(pool)
        End Sub

        Protected Shared ReadOnly s_reduceParentheses As Func(Of ParenthesizedExpressionSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode) = AddressOf ReduceParentheses

        Protected Shared Function ReduceParentheses(
            node As ParenthesizedExpressionSyntax,
            semanticModel As SemanticModel,
            options As SimplifierOptions,
            cancellationToken As CancellationToken
        ) As SyntaxNode

            If node.CanRemoveParentheses(semanticModel, cancellationToken) Then
                ' TODO(DustinCa): We should not be skipping elastic trivia below.
                ' However, the formatter seems to mess up trailing trivia in some
                ' cases if elastic trivia is there -- and it's not clear why.
                ' Specifically removing the elastic trivia formatting rule doesn't
                ' have any effect.
                Dim resultNode = VisualBasicSyntaxFacts.Instance.Unparenthesize(node)
                resultNode = SimplificationHelpers.CopyAnnotations(node, resultNode)

                Return resultNode
            End If

            ' We don't know how to simplify this.
            Return node
        End Function

        Public NotOverridable Overrides Function IsApplicable(options As SimplifierOptions) As Boolean
            Return IsApplicable(CType(options, VisualBasicSimplifierOptions))
        End Function

        Public MustOverride Overloads Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
    End Class
End Namespace

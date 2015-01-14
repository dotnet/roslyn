' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend MustInherit Class AbstractVisualBasicReducer
        Inherits AbstractReducer

        Protected Shared Function ReduceParentheses(
            node As ParenthesizedExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If node.CanRemoveParentheses(semanticModel, cancellationToken) Then
                ' TODO(DustinCa): We should not be skipping elastic trivia below.
                ' However, the formatter seems to mess up trailing trivia in some
                ' cases if elastic trivia is there -- and it's not clear why.
                ' Specifically removing the elastic trivia formatting rule doesn't
                ' have any effect.

                Dim leadingTrivia = node.OpenParenToken.LeadingTrivia _
                    .Concat(node.OpenParenToken.TrailingTrivia) _
                    .Where(Function(n) Not n.IsElastic) _
                    .Concat(node.Expression.GetLeadingTrivia())

                Dim trailingTrivia = node.Expression.GetTrailingTrivia() _
                    .Concat(node.CloseParenToken.LeadingTrivia) _
                    .Where(Function(n) Not n.IsElastic) _
                    .Concat(node.CloseParenToken.TrailingTrivia)

                Dim resultNode = node.Expression _
                    .WithLeadingTrivia(leadingTrivia) _
                    .WithTrailingTrivia(trailingTrivia)

                resultNode = SimplificationHelpers.CopyAnnotations(node, resultNode)

                Return resultNode
            End If

            ' We don't know how to simplify this.
            Return node
        End Function

    End Class
End Namespace

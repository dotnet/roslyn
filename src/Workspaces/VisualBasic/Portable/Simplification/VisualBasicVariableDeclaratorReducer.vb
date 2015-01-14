' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicVariableDeclaratorReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(optionSet, cancellationToken)
        End Function

        Private Overloads Shared Function SimplifyVariableDeclarator(
            node As VariableDeclaratorSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As SyntaxNode
            Dim replacementNode As SyntaxNode = Nothing
            Dim issueSpan As TextSpan

            If Not node.TryReduceVariableDeclaratorWithoutType(
                semanticModel,
                replacementNode,
                issueSpan,
                optionSet,
                cancellationToken) Then
                Return node
            End If

            replacementNode = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation)
            Return replacementNode.WithoutAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace


' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicInferredMemberNameReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New VisualBasicInferredMemberNameReducer.Rewriter(optionSet, cancellationToken)
        End Function

        Private Shared Function SimplifyTupleName(
            node As SimpleArgumentSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As SimpleArgumentSyntax

            If node.NameColonEquals Is Nothing OrElse Not node.IsParentKind(SyntaxKind.TupleExpression) Then
                Return node
            End If

            Dim inferredName = node.Expression.TryGetInferredMemberName()

            If inferredName Is Nothing OrElse
                Not CaseInsensitiveComparison.Equals(inferredName, node.NameColonEquals.Name.Identifier.ValueText) Then

                Return node
            End If

            Return node.WithNameColonEquals(Nothing).WithTriviaFrom(node)
        End Function

        Private Shared Function SimplifyNamedFieldInitializer(node As NamedFieldInitializerSyntax, arg2 As SemanticModel, arg3 As OptionSet, arg4 As CancellationToken) As SyntaxNode
            Dim inferredName = node.Expression.TryGetInferredMemberName()

            If inferredName Is Nothing OrElse
                    Not CaseInsensitiveComparison.Equals(inferredName, node.Name.Identifier.ValueText) Then
                Return node
            End If

            Return SyntaxFactory.InferredFieldInitializer(node.Expression).WithTriviaFrom(node)
        End Function

    End Class
End Namespace

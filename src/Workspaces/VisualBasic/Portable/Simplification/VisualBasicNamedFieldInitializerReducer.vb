' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicNamedFieldInitializerReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(optionSet, cancellationToken)
        End Function

        Private Shared Function SimplifyNamedFieldInitializer(
            node As NamedFieldInitializerSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As SyntaxNode

            ' Reduces "New With {.X = X}" (NamedFieldInitializer) to "New With {X}" (InferredFieldInitializer)

            Dim implicitName As String = Nothing

            Dim identifier = TryCast(node.Expression, IdentifierNameSyntax)
            Dim memberAccess = TryCast(node.Expression, MemberAccessExpressionSyntax)

            If identifier IsNot Nothing Then
                implicitName = identifier.Identifier.Text
            ElseIf memberAccess IsNot Nothing Then
                implicitName = memberAccess.Name?.Identifier.Text
            End If

            Dim explicitName = node.Name?.Identifier.Text

            Return If(implicitName IsNot Nothing AndAlso implicitName = explicitName,
                SyntaxFactory.InferredFieldInitializer(node.Expression),
                DirectCast(node, SyntaxNode))
        End Function
    End Class
End Namespace

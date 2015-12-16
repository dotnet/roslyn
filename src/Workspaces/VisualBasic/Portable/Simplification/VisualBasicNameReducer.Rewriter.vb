' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicNameReducer
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New(optionSet, cancellationToken)
            End Sub

            Private Function SimplifyVisit(Of TExpression As SyntaxNode)(
                node As TExpression,
                baseVisit As Func(Of TExpression, SyntaxNode)) As SyntaxNode

                Dim oldAlwaysSimplify = Me._alwaysSimplify
                Me._alwaysSimplify = Me._alwaysSimplify OrElse node.HasAnnotation(Simplifier.Annotation)

                Dim result = baseVisit(node)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Private Function DoSimplifyExpression(Of TExpression As ExpressionSyntax)(
                node As TExpression,
                baseVisit As Func(Of TExpression, SyntaxNode)) As SyntaxNode

                Return SimplifyVisit(node, Function(n)
                                               Return SimplifyExpression(n, newNode:=baseVisit(n), simplifier:=AddressOf SimplifyName)
                                           End Function)
            End Function

            Public Overrides Function VisitGenericName(node As GenericNameSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitGenericName)
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitIdentifierName)
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitQualifiedName)
            End Function

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitMemberAccessExpression)
            End Function

            Public Overrides Function VisitNullableType(node As NullableTypeSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitNullableType)
            End Function

            Public Overrides Function VisitArrayType(node As ArrayTypeSyntax) As SyntaxNode
                Return DoSimplifyExpression(node, AddressOf MyBase.VisitArrayType)
            End Function

        End Class
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicNameReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitGenericName(node As GenericNameSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitGenericName(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitIdentifierName(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitQualifiedName(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitMemberAccessExpression(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Public Overrides Function VisitNullableType(node As NullableTypeSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitNullableType(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function

            Public Overrides Function VisitArrayType(node As ArrayTypeSyntax) As SyntaxNode
                Dim oldAlwaysSimplify = Me._alwaysSimplify
                If Not Me._alwaysSimplify Then
                    Me._alwaysSimplify = node.HasAnnotation(Simplifier.Annotation)
                End If

                Dim result = SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitArrayType(node),
                    simplifier:=s_simplifyName)

                Me._alwaysSimplify = oldAlwaysSimplify

                Return result
            End Function
        End Class
    End Class
End Namespace

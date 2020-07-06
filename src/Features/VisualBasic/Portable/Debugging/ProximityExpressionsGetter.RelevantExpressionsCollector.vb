' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging
    Partial Friend Class VisualBasicProximityExpressionsService
        Public Class RelevantExpressionsCollector
            Inherits VisualBasicSyntaxWalker

            Private ReadOnly _includeDeclarations As Boolean
            Private ReadOnly _expressions As IList(Of ExpressionSyntax)

            Public Sub New(includeDeclarations As Boolean, expressions As IList(Of ExpressionSyntax))
                MyBase.New(SyntaxWalkerDepth.Node)
                _includeDeclarations = includeDeclarations
                _expressions = expressions
            End Sub

            Private Sub AddExpression(node As ExpressionSyntax)
                If node.IsRightSideOfDotOrBang() OrElse
                   TypeOf node.Parent Is TypeArgumentListSyntax OrElse
                   TypeOf node.Parent Is AsClauseSyntax OrElse
                   (node.Parent.Parent IsNot Nothing AndAlso TypeOf node.Parent.Parent Is AsClauseSyntax) OrElse
                   TypeOf node.Parent Is ImplementsClauseSyntax Then
                    Return
                End If

                If node.Parent.IsKind(SyntaxKind.NameColonEquals) Then
                    Return
                End If

                _expressions.Add(node)
            End Sub

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                AddExpression(node)

                MyBase.VisitIdentifierName(node)
            End Sub

            Public Overrides Sub VisitModifiedIdentifier(node As ModifiedIdentifierSyntax)
                If _includeDeclarations Then
                    _expressions.Add(SyntaxFactory.IdentifierName(node.Identifier))
                End If

                MyBase.VisitModifiedIdentifier(node)
            End Sub

            Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
                AddExpression(node)

                MyBase.VisitQualifiedName(node)
            End Sub

            Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
                _expressions.Add(node)

                MyBase.VisitMemberAccessExpression(node)
            End Sub
        End Class
    End Class
End Namespace

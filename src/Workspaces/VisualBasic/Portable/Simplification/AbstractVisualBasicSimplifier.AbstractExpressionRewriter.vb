' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend MustInherit Class AbstractVisualBasicReducer
        Friend MustInherit Class AbstractExpressionRewriter
            Inherits VisualBasicSyntaxRewriter
            Implements IExpressionRewriter

            Protected ReadOnly CancellationToken As CancellationToken
            Private ReadOnly _processedParentNodes As HashSet(Of SyntaxNode)
            Private _hasMoreWork As Boolean
            Protected _alwaysSimplify As Boolean
            Private ReadOnly _simplificationOptions As OptionSet
            Private _semanticModel As SemanticModel

            Protected Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New()
                Me.CancellationToken = cancellationToken
                Me._simplificationOptions = optionSet
                _processedParentNodes = New HashSet(Of SyntaxNode)
            End Sub

            Public ReadOnly Property HasMoreWork As Boolean Implements IExpressionRewriter.HasMoreWork
                Get
                    Return _hasMoreWork
                End Get
            End Property

            Private Shared Function GetParentNode(expression As ExpressionSyntax) As SyntaxNode
                Return expression _
                    .AncestorsAndSelf() _
                    .OfType(Of ExpressionSyntax)() _
                    .LastOrDefault()
            End Function

            Private Shared Function GetParentNode(statement As StatementSyntax) As SyntaxNode
                Return statement _
                    .AncestorsAndSelf() _
                    .OfType(Of StatementSyntax)() _
                    .Where(Function(s) Not s.IsMemberBlock()) _
                    .LastOrDefault()
            End Function

            Protected Function SimplifyNode(Of TNode As SyntaxNode)(
                node As TNode,
                newNode As SyntaxNode,
                parentNode As SyntaxNode,
                simplifyFunc As Func(Of TNode, SemanticModel, OptionSet, CancellationToken, SyntaxNode)
            ) As SyntaxNode

                Debug.Assert(parentNode IsNot Nothing)

                CancellationToken.ThrowIfCancellationRequested()

                If Not _alwaysSimplify AndAlso Not node.HasAnnotation(Simplifier.Annotation) Then
                    Return newNode
                End If

                If node IsNot newNode OrElse _processedParentNodes.Contains(parentNode) Then
                    _hasMoreWork = True
                    Return newNode
                End If

                If Not node.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation) Then
                    Dim simplifiedNode = simplifyFunc(node, _semanticModel, _simplificationOptions, CancellationToken)
                    If simplifiedNode IsNot node Then
                        _processedParentNodes.Add(parentNode)
                        _hasMoreWork = True
                        Return simplifiedNode
                    End If
                End If

                Return node
            End Function

            Protected Function SimplifyToken(
                token As SyntaxToken,
                newToken As SyntaxToken,
                simplifyFunc As Func(Of SyntaxToken, SemanticModel, OptionSet, CancellationToken, SyntaxToken)
            ) As SyntaxToken

                If token.Kind = SyntaxKind.None Then
                    Return newToken
                End If

                Dim parentNode = token.Parent

                If TypeOf (parentNode) Is ExpressionSyntax Then
                    parentNode = GetParentNode(DirectCast(parentNode, ExpressionSyntax))
                ElseIf TypeOf (parentNode) Is StatementSyntax Then
                    parentNode = GetParentNode(DirectCast(parentNode, StatementSyntax))
                End If

                Debug.Assert(parentNode IsNot Nothing)

                CancellationToken.ThrowIfCancellationRequested()

                If Not _alwaysSimplify AndAlso Not token.HasAnnotation(Simplifier.Annotation) Then
                    Return newToken
                End If

                If token <> newToken OrElse _processedParentNodes.Contains(parentNode) Then
                    _hasMoreWork = True
                    Return newToken
                End If

                If Not token.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation) Then
                    Dim simplifiedToken = simplifyFunc(token, _semanticModel, _simplificationOptions, CancellationToken)
                    If simplifiedToken <> token Then
                        _processedParentNodes.Add(parentNode)
                        _hasMoreWork = True

                        Return simplifiedToken
                    End If
                End If

                Return newToken
            End Function

            Protected Function SimplifyExpression(Of TExpression As ExpressionSyntax)(
                expression As TExpression,
                newNode As SyntaxNode,
                simplifier As Func(Of TExpression, SemanticModel, OptionSet, CancellationToken, SyntaxNode)
            ) As SyntaxNode

                Return SimplifyNode(expression, newNode, GetParentNode(expression), simplifier)
            End Function

            Protected Function SimplifyStatement(Of TStatement As StatementSyntax)(
                statement As TStatement,
                newNode As SyntaxNode,
                simplifier As Func(Of TStatement, SemanticModel, OptionSet, CancellationToken, SyntaxNode)
            ) As SyntaxNode

                Return SimplifyNode(statement, newNode, GetParentNode(statement), simplifier)
            End Function

            Public Function VisitNodeOrToken(nodeOrToken As SyntaxNodeOrToken, semanticModel As SemanticModel, simplifyAllDescendants As Boolean) As SyntaxNodeOrToken Implements IExpressionRewriter.VisitNodeOrToken
                _semanticModel = DirectCast(semanticModel, semanticModel)
                _alwaysSimplify = simplifyAllDescendants
                _hasMoreWork = False
                _processedParentNodes.Clear()

                If nodeOrToken.IsNode Then
                    Return Visit(nodeOrToken.AsNode)
                Else
                    Return VisitToken(nodeOrToken.AsToken())
                End If
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBase(node, Function(statements As SyntaxList(Of StatementSyntax)) node.WithStatements(statements))
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBase(node, Function(statements As SyntaxList(Of StatementSyntax)) node.WithStatements(statements))
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBase(node, Function(statements As SyntaxList(Of StatementSyntax)) node.WithStatements(statements))
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBase(node, Function(statements As SyntaxList(Of StatementSyntax)) node.WithStatements(statements))
            End Function

            Private Function VisitMethodBlockBase(node As MethodBlockBaseSyntax, updateFunc As Func(Of SyntaxList(Of StatementSyntax), MethodBlockBaseSyntax)) As MethodBlockBaseSyntax
                ' Certain reducers for VB (escaping, parentheses) require to operate on the entire method body, rather than individual statements.
                ' Hence, we need to reduce the entire method body as a single unit.
                ' However, there is no SyntaxNode for the method body or statement list, hence NodesAndTokensToReduceComputer added the MethodBlockBaseSyntax to the list of nodes to be reduced.
                ' Here we make sure that we reduce only the statement list inside the MethodBlockBaseSyntax.

                ' Note that if any of the nodes/tokens in the method declaration needed to be reduced, they would be handed separately to us and would be reduced appropriately.

                Dim rewrittenBody = VisitList(node.Statements)
                If Not rewrittenBody = node.Statements Then
                    Return updateFunc(rewrittenBody)
                End If

                Return node
            End Function
        End Class
    End Class
End Namespace

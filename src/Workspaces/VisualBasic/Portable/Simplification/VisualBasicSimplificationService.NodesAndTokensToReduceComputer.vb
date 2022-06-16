' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification

    Partial Friend Class VisualBasicSimplificationService
        Inherits AbstractSimplificationService(Of ExpressionSyntax, ExecutableStatementSyntax, CrefReferenceSyntax)

        Private Class NodesAndTokensToReduceComputer
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _nodesAndTokensToReduce As List(Of NodeOrTokenToReduce)
            Private ReadOnly _isNodeOrTokenOutsideSimplifySpans As Func(Of SyntaxNodeOrToken, Boolean)

            Private _simplifyAllDescendants As Boolean
            Private _insideSpeculatedNode As Boolean

            ''' <summary>
            ''' Computes a list of nodes and tokens that need to be reduced in the given syntax root.
            ''' </summary>
            Public Shared Function Compute(root As SyntaxNode, isNodeOrTokenOutsideSimplifySpans As Func(Of SyntaxNodeOrToken, Boolean)) As ImmutableArray(Of NodeOrTokenToReduce)
                Dim reduceNodeComputer = New NodesAndTokensToReduceComputer(isNodeOrTokenOutsideSimplifySpans)
                reduceNodeComputer.Visit(root)
                Return reduceNodeComputer._nodesAndTokensToReduce.ToImmutableArray()
            End Function

            Private Sub New(isNodeOrTokenOutsideSimplifySpans As Func(Of SyntaxNodeOrToken, Boolean))
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._isNodeOrTokenOutsideSimplifySpans = isNodeOrTokenOutsideSimplifySpans
                Me._nodesAndTokensToReduce = New List(Of NodeOrTokenToReduce)()
                Me._simplifyAllDescendants = False
                Me._insideSpeculatedNode = False
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                If Me._isNodeOrTokenOutsideSimplifySpans(node) Then
                    If Me._simplifyAllDescendants Then
                        ' One of the ancestor nodes is within a simplification span, but this node Is outside all simplification spans.
                        ' Add DontSimplifyAnnotation to node to ensure it doesn't get simplified.
                        Return node.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation)
                    Else
                        Return node
                    End If
                End If

                Dim savedSimplifyAllDescendants = Me._simplifyAllDescendants
                Me._simplifyAllDescendants = Me._simplifyAllDescendants OrElse node.HasAnnotation(Simplifier.Annotation)

                If Not Me._insideSpeculatedNode AndAlso SpeculationAnalyzer.CanSpeculateOnNode(node) Then
                    If Me._simplifyAllDescendants OrElse node.DescendantNodesAndTokens(s_containsAnnotations, descendIntoTrivia:=True).Any(s_hasSimplifierAnnotation) Then
                        Me._insideSpeculatedNode = True
                        Dim rewrittenNode = MyBase.Visit(node)
                        Me._nodesAndTokensToReduce.Add(New NodeOrTokenToReduce(rewrittenNode, _simplifyAllDescendants, node))
                        Me._insideSpeculatedNode = False
                    End If
                ElseIf node.ContainsAnnotations OrElse savedSimplifyAllDescendants Then
                    If Not Me._insideSpeculatedNode AndAlso
                    IsNodeVariableDeclaratorOfFieldDeclaration(node) AndAlso
                    Me._simplifyAllDescendants Then
                        Me._nodesAndTokensToReduce.Add(New NodeOrTokenToReduce(node, False, node))
                    End If

                    node = MyBase.Visit(node)
                End If

                Me._simplifyAllDescendants = savedSimplifyAllDescendants
                Return node
            End Function

            Private Shared Function IsNodeVariableDeclaratorOfFieldDeclaration(node As SyntaxNode) As Boolean
                Return node IsNot Nothing AndAlso node.Kind() = SyntaxKind.VariableDeclarator AndAlso
                    node.Parent IsNot Nothing AndAlso node.Parent.Kind() = SyntaxKind.FieldDeclaration
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If Me._isNodeOrTokenOutsideSimplifySpans(token) Then
                    If Me._simplifyAllDescendants Then
                        ' One of the ancestor nodes is within a simplification span, but this token Is outside all simplification spans.
                        ' Add DontSimplifyAnnotation to token to ensure it doesn't get simplified.
                        Return token.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation)
                    Else
                        Return token
                    End If
                End If

                Dim savedSimplifyAllDescendants = Me._simplifyAllDescendants
                Me._simplifyAllDescendants = Me._simplifyAllDescendants OrElse token.HasAnnotation(Simplifier.Annotation)

                If Me._simplifyAllDescendants AndAlso Not Me._insideSpeculatedNode AndAlso token.Kind <> SyntaxKind.None Then
                    Me._nodesAndTokensToReduce.Add(New NodeOrTokenToReduce(token, simplifyAllDescendants:=True, originalNodeOrToken:=token))
                End If

                If token.ContainsAnnotations OrElse savedSimplifyAllDescendants Then
                    token = MyBase.VisitToken(token)
                End If

                Me._simplifyAllDescendants = savedSimplifyAllDescendants
                Return token
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                If trivia.HasStructure Then
                    Dim savedInsideSpeculatedNode = Me._insideSpeculatedNode
                    Me._insideSpeculatedNode = False
                    MyBase.VisitTrivia(trivia)
                    Me._insideSpeculatedNode = savedInsideSpeculatedNode
                End If

                Return trivia
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBaseSyntax(node,
                    Function(n, b, s, e)
                        Return DirectCast(n, MethodBlockSyntax).Update(node.Kind, DirectCast(b, MethodStatementSyntax), s, e)
                    End Function)
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBaseSyntax(node,
                    Function(n, b, s, e)
                        Return DirectCast(n, OperatorBlockSyntax).Update(DirectCast(b, OperatorStatementSyntax), s, e)
                    End Function)
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBaseSyntax(node,
                    Function(n, b, s, e)
                        Return DirectCast(n, ConstructorBlockSyntax).Update(DirectCast(b, SubNewStatementSyntax), s, e)
                    End Function)
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxNode
                Return VisitMethodBlockBaseSyntax(node,
                    Function(n, b, s, e)
                        Return DirectCast(n, AccessorBlockSyntax).Update(node.Kind, DirectCast(b, AccessorStatementSyntax), s, e)
                    End Function)
            End Function

            Private Function VisitMethodBlockBaseSyntax(node As MethodBlockBaseSyntax, updateFunc As Func(Of MethodBlockBaseSyntax, MethodBaseSyntax, SyntaxList(Of StatementSyntax), EndBlockStatementSyntax, MethodBlockBaseSyntax)) As MethodBlockBaseSyntax
                If Me._isNodeOrTokenOutsideSimplifySpans(node) Then
                    If Me._simplifyAllDescendants Then
                        ' One of the ancestor nodes Is within a simplification span, but this node Is outside all simplification spans.
                        ' Add DontSimplifyAnnotation to node to ensure it doesn't get simplified.
                        Return node.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation)
                    Else
                        Return node
                    End If
                End If

                Dim savedSimplifyAllDescendants = Me._simplifyAllDescendants
                Me._simplifyAllDescendants = Me._simplifyAllDescendants OrElse node.HasAnnotation(Simplifier.Annotation)

                Dim begin = DirectCast(Visit(node.BlockStatement), MethodBaseSyntax)
                Dim endStatement = DirectCast(Visit(node.EndBlockStatement), EndBlockStatementSyntax)

                ' Certain reducers for VB (escaping, parentheses) require to operate on the entire method body, rather than individual statements.
                ' Hence, we need to reduce the entire method body as a single unit.
                ' However, there is no SyntaxNode for the method body or statement list, hence we add the MethodBlockBaseSyntax to the list of nodes to be reduced.
                ' Subsequently, when the AbstractReducer is handed a MethodBlockBaseSyntax, it will reduce only the statement list inside it.
                If Me._simplifyAllDescendants OrElse
                   node.Statements.Any(Function(s) s.DescendantNodesAndTokensAndSelf(s_containsAnnotations, descendIntoTrivia:=True).Any(s_hasSimplifierAnnotation)) Then
                    Me._insideSpeculatedNode = True
                    Dim statements = VisitList(node.Statements)
                    Dim rewrittenNode = updateFunc(node, node.BlockStatement, statements, node.EndBlockStatement)
                    Me._nodesAndTokensToReduce.Add(New NodeOrTokenToReduce(rewrittenNode, Me._simplifyAllDescendants, node))
                    Me._insideSpeculatedNode = False
                End If

                Me._simplifyAllDescendants = savedSimplifyAllDescendants
                Return node
            End Function
        End Class
    End Class
End Namespace

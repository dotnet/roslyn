﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class SyntaxUtilities
        <Conditional("DEBUG")>
        Public Shared Sub AssertIsBody(syntax As SyntaxNode, allowLambda As Boolean)
            ' lambda/query
            If LambdaUtilities.IsLambdaBody(syntax) Then
                Debug.Assert(allowLambda)
                Debug.Assert(TypeOf syntax Is ExpressionSyntax OrElse TypeOf syntax Is LambdaHeaderSyntax)
                Return
            End If

            ' sub/function/ctor/operator/accessor
            If TypeOf syntax Is MethodBlockBaseSyntax Then
                Return
            End If

            ' field/property initializer
            If TypeOf syntax Is ExpressionSyntax Then
                If syntax.Parent.Kind = SyntaxKind.EqualsValue Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                ElseIf syntax.Parent.Kind = SyntaxKind.AsNewClause Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                End If
            End If

            ' field array initializer
            If TypeOf syntax Is ArgumentListSyntax Then
                Debug.Assert(syntax.Parent.IsKind(SyntaxKind.ModifiedIdentifier))
                Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                Return
            End If

            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Shared Function GetBody(node As LambdaExpressionSyntax) As SyntaxList(Of SyntaxNode)
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(node, MultiLineLambdaExpressionSyntax).Statements
                Case SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression
                    Return SyntaxFactory.SingletonList(DirectCast(node, SingleLineLambdaExpressionSyntax).Body)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Public Shared Sub FindLeafNodeAndPartner(leftRoot As SyntaxNode,
                                          leftPosition As Integer,
                                          rightRoot As SyntaxNode,
                                          <Out> ByRef leftNode As SyntaxNode,
                                          <Out> ByRef rightNodeOpt As SyntaxNode)
            leftNode = leftRoot
            rightNodeOpt = rightRoot
            While True
                If rightNodeOpt IsNot Nothing AndAlso leftNode.RawKind <> rightNodeOpt.RawKind Then
                    rightNodeOpt = Nothing
                End If

                Dim childIndex As Integer = 0
                Dim leftChild = leftNode.ChildThatContainsPosition(leftPosition, childIndex)
                If leftChild.IsToken Then
                    Return
                End If

                If rightNodeOpt IsNot Nothing Then
                    Dim rightNodeChildNodesAndTokens = rightNodeOpt.ChildNodesAndTokens()
                    If childIndex >= 0 AndAlso childIndex < rightNodeChildNodesAndTokens.Count Then
                        rightNodeOpt = rightNodeChildNodesAndTokens(childIndex).AsNode()
                    Else
                        rightNodeOpt = Nothing
                    End If
                End If

                leftNode = leftChild.AsNode()
            End While
        End Sub

        Public Shared Function FindPartner(leftRoot As SyntaxNode, rightRoot As SyntaxNode, leftNode As SyntaxNode) As SyntaxNode
            ' Finding a partner of a zero-width node is complicated and not supported atm
            Debug.Assert(leftNode.FullSpan.Length > 0)

            Dim originalLeftNode = leftNode
            Dim leftPosition = leftNode.SpanStart
            leftNode = leftRoot
            Dim rightNode = rightRoot

            While leftNode IsNot originalLeftNode
                Debug.Assert(leftNode.RawKind = rightNode.RawKind)

                Dim childIndex = 0
                Dim leftChild = leftNode.ChildThatContainsPosition(leftPosition, childIndex)

                ' Can only happen when searching for zero-width node.
                Debug.Assert(Not leftChild.IsToken)

                rightNode = rightNode.ChildNodesAndTokens()(childIndex).AsNode()
                leftNode = leftChild.AsNode()
            End While

            Return rightNode
        End Function

        Public Shared Function IsParameterlessConstructor(declaration As SyntaxNode) As Boolean
            If Not declaration.IsKind(SyntaxKind.ConstructorBlock) Then
                Return False
            End If

            Dim ctor = DirectCast(declaration, ConstructorBlockSyntax)
            Return ctor.BlockStatement.ParameterList.Parameters.Count = 0
        End Function

        Public Shared Function HasBackingField(propertyDeclaration As SyntaxNode) As Boolean
            Return propertyDeclaration.IsKind(SyntaxKind.PropertyStatement) AndAlso
                   Not DirectCast(propertyDeclaration, PropertyStatementSyntax).Modifiers.Any(SyntaxKind.MustOverrideKeyword)
        End Function

        Public Shared Function IsAsyncMethodOrLambda(declarationOrBody As SyntaxNode) As Boolean
            Return GetModifiers(declarationOrBody).Any(SyntaxKind.AsyncKeyword)
        End Function

        Public Shared Function IsIteratorMethodOrLambda(declaration As SyntaxNode) As Boolean
            Return GetModifiers(declaration).Any(SyntaxKind.IteratorKeyword)
        End Function

        Public Shared Function GetAwaitExpressions(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' skip lambda bodies
            Return ImmutableArray.CreateRange(body.DescendantNodes(AddressOf LambdaUtilities.IsNotLambda).
                Where(Function(n) n.IsKind(SyntaxKind.AwaitExpression)))
        End Function

        Public Shared Function GetYieldStatements(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' enumerate statements:
            Return ImmutableArray.CreateRange(body.DescendantNodes(Function(n) TypeOf n IsNot ExpressionSyntax).
                Where(Function(n) n.IsKind(SyntaxKind.YieldStatement)))

        End Function

        Public Shared Function GetModifiers(declarationOrBody As SyntaxNode) As SyntaxTokenList
            Select Case declarationOrBody.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Return DirectCast(declarationOrBody, MethodBlockBaseSyntax).BlockStatement.Modifiers

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declarationOrBody, LambdaExpressionSyntax).SubOrFunctionHeader.Modifiers

                Case SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return DirectCast(declarationOrBody, LambdaHeaderSyntax).Modifiers
            End Select

            Return Nothing
        End Function
    End Class
End Namespace

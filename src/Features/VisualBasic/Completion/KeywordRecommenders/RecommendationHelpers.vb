' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders
    Friend Module RecommendationHelpers
        <Extension()>
        Friend Function IsInLambda(syntaxTree As SyntaxTree, token As SyntaxToken) As Boolean
            Return token.GetAncestor(Of LambdaExpressionSyntax)() IsNot Nothing
        End Function

        Friend Function IsOnErrorStatement(node As SyntaxNode) As Boolean
            Return TypeOf node Is OnErrorGoToStatementSyntax OrElse TypeOf node Is OnErrorResumeNextStatementSyntax
        End Function

        ''' <summary>
        ''' Returns the parent of the node given. node may be null, which will cause this function to return null.
        ''' </summary>
        <Extension()>
        Friend Function GetParentOrNull(node As SyntaxNode) As SyntaxNode
            Return If(node Is Nothing, Nothing, node.Parent)
        End Function

        <Extension()>
        Friend Function IsFollowingCompleteAsNewClause(token As SyntaxToken) As Boolean
            Dim asNewClause = token.GetAncestor(Of AsNewClauseSyntax)()
            If asNewClause Is Nothing Then
                Return False
            End If

            Dim lastToken As SyntaxToken
            Select Case asNewClause.NewExpression.Kind
                Case SyntaxKind.ObjectCreationExpression
                    Dim objectCreation = DirectCast(asNewClause.NewExpression, ObjectCreationExpressionSyntax)
                    lastToken = If(objectCreation.ArgumentList IsNot Nothing,
                                   objectCreation.ArgumentList.CloseParenToken,
                                   asNewClause.Type.GetLastToken(includeZeroWidth:=True))
                Case SyntaxKind.AnonymousObjectCreationExpression
                    Dim anonymousObjectCreation = DirectCast(asNewClause.NewExpression, AnonymousObjectCreationExpressionSyntax)
                    lastToken = If(anonymousObjectCreation.Initializer IsNot Nothing,
                                   anonymousObjectCreation.Initializer.CloseBraceToken,
                                   asNewClause.Type.GetLastToken(includeZeroWidth:=True))
                Case SyntaxKind.ArrayCreationExpression
                    Dim arrayCreation = DirectCast(asNewClause.NewExpression, ArrayCreationExpressionSyntax)
                    lastToken = If(arrayCreation.Initializer IsNot Nothing,
                                   arrayCreation.Initializer.CloseBraceToken,
                                   asNewClause.Type.GetLastToken(includeZeroWidth:=True))
                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select

            Return token = lastToken
        End Function

        <Extension()>
        Private Function IsLastTokenOfObjectCreation(token As SyntaxToken, objectCreation As ObjectCreationExpressionSyntax) As Boolean
            If objectCreation Is Nothing Then
                Return False
            End If

            Dim lastToken = If(objectCreation.ArgumentList IsNot Nothing,
                               objectCreation.ArgumentList.CloseParenToken,
                               objectCreation.Type.GetLastToken(includeZeroWidth:=True))

            Return token = lastToken
        End Function

        <Extension()>
        Friend Function IsFollowingCompleteObjectCreationInitializer(token As SyntaxToken) As Boolean
            Dim variableDeclarator = token.GetAncestor(Of VariableDeclaratorSyntax)()
            If variableDeclarator Is Nothing Then
                Return False
            End If

            Dim objectCreation = token.GetAncestors(Of ObjectCreationExpressionSyntax)() _
                                    .Where(Function(oc) oc.Parent IsNot Nothing AndAlso
                                                        oc.Parent.Kind <> SyntaxKind.AsNewClause AndAlso
                                                        variableDeclarator.Initializer IsNot Nothing AndAlso
                                                        variableDeclarator.Initializer.Value Is oc) _
                                    .FirstOrDefault()

            Return token.IsLastTokenOfObjectCreation(objectCreation)
        End Function

        <Extension()>
        Friend Function IsFollowingCompleteObjectCreation(token As SyntaxToken) As Boolean
            Dim objectCreation = token.GetAncestor(Of ObjectCreationExpressionSyntax)()

            Return token.IsLastTokenOfObjectCreation(objectCreation)
        End Function

        <Extension()>
        Friend Function LastJoinKey(collection As SeparatedSyntaxList(Of JoinConditionSyntax)) As ExpressionSyntax
            Dim lastJoinCondition = collection.LastOrDefault()

            If lastJoinCondition IsNot Nothing Then
                Return lastJoinCondition.Right
            Else
                Return Nothing
            End If
        End Function

        <Extension()>
        Friend Function IsFromIdentifierNode(token As SyntaxToken, identifierSyntax As IdentifierNameSyntax) As Boolean
            Return _
                identifierSyntax IsNot Nothing AndAlso
                token = identifierSyntax.Identifier AndAlso
                identifierSyntax.Identifier.GetTypeCharacter() = TypeCharacter.None
        End Function

        <Extension()>
        Friend Function IsFromIdentifierNode(token As SyntaxToken, identifierSyntax As ModifiedIdentifierSyntax) As Boolean
            Return _
                identifierSyntax IsNot Nothing AndAlso
                token = identifierSyntax.Identifier AndAlso
                identifierSyntax.Identifier.GetTypeCharacter() = TypeCharacter.None
        End Function

        <Extension()>
        Friend Function IsFromIdentifierNode(token As SyntaxToken, node As SyntaxNode) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Dim identifierName = TryCast(node, IdentifierNameSyntax)
            If token.IsFromIdentifierNode(identifierName) Then
                Return True
            End If

            Dim modifiedIdentifierName = TryCast(node, ModifiedIdentifierSyntax)
            If token.IsFromIdentifierNode(modifiedIdentifierName) Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsFromIdentifierNode(Of TParent As SyntaxNode)(token As SyntaxToken, identifierNodeSelector As Func(Of TParent, SyntaxNode)) As Boolean
            Dim ancestor = token.GetAncestor(Of TParent)()
            If ancestor Is Nothing Then
                Return False
            End If

            Return token.IsFromIdentifierNode(identifierNodeSelector(ancestor))
        End Function

        Friend Function CreateRecommendedKeywordForIntrinsicOperator(kind As SyntaxKind,
                                                                     firstLine As String,
                                                                     glyph As Glyph,
                                                                     intrinsicOperator As AbstractIntrinsicOperatorDocumentation,
                                                                     Optional semanticModel As SemanticModel = Nothing,
                                                                     Optional position As Integer = -1) As RecommendedKeyword

            Return New RecommendedKeyword(SyntaxFacts.GetText(kind), glyph,
                Function(c)
                    Dim stringBuilder As New StringBuilder
                    stringBuilder.AppendLine(firstLine)
                    stringBuilder.AppendLine(intrinsicOperator.DocumentationText)

                    Dim appendParts = Sub(parts As IEnumerable(Of SymbolDisplayPart))
                                          For Each part In parts
                                              stringBuilder.Append(part.ToString())
                                          Next
                                      End Sub

                    appendParts(intrinsicOperator.PrefixParts)

                    For i = 0 To intrinsicOperator.ParameterCount - 1
                        If i <> 0 Then
                            stringBuilder.Append(", ")
                        End If

                        appendParts(intrinsicOperator.GetParameterDisplayParts(i))
                    Next

                    appendParts(intrinsicOperator.GetSuffix(semanticModel, position, Nothing, c))

                    Return stringBuilder.ToString().ToSymbolDisplayParts()
                End Function,
                isIntrinsic:=True)
        End Function
    End Module
End Namespace

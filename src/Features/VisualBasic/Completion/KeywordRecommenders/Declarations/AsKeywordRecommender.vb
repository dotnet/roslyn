' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "As" keyword in all types of declarations.
    ''' </summary>
    Friend Class AsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            Dim asKeyword = SpecializedCollections.SingletonEnumerable(
                                New RecommendedKeyword("As", VBFeaturesResources.AsKeywordToolTip))

            ' Query: Aggregate x |
            ' Query: Group Join x |
            If targetToken.IsFromIdentifierNode(Of CollectionRangeVariableSyntax)(Function(collectionRange) collectionRange.Identifier) Then
                Return asKeyword
            End If

            ' Query: Let x |
            Dim expressionRangeVariable = targetToken.GetAncestor(Of ExpressionRangeVariableSyntax)()
            If expressionRangeVariable IsNot Nothing AndAlso expressionRangeVariable.NameEquals IsNot Nothing Then
                If targetToken.IsFromIdentifierNode(expressionRangeVariable.NameEquals.Identifier) Then
                    Return asKeyword
                End If
            End If

            ' For x |
            If targetToken.IsFromIdentifierNode(Of ForStatementSyntax)(Function(forStatement) forStatement.ControlVariable) Then
                Return asKeyword
            End If

            ' For Each x |
            If targetToken.IsFromIdentifierNode(Of ForEachStatementSyntax)(Function(forEachStatement) forEachStatement.ControlVariable) Then
                Return asKeyword
            End If

            ' All parameter types. In this case we have to drill into the parameter to make sure untyped
            If targetToken.IsFromIdentifierNode(Of ParameterSyntax)(Function(parameter) parameter.Identifier) Then
                Return asKeyword
            End If

            ' Sub Foo(Of T |
            If targetToken.IsChildToken(Of TypeParameterSyntax)(Function(typeParameter) typeParameter.Identifier) Then
                Return asKeyword
            End If

            ' Enum Foo |
            If targetToken.IsChildToken(Of EnumStatementSyntax)(Function(enumDeclaration) enumDeclaration.Identifier) Then
                Return asKeyword
            End If

            ' Catch foo
            If targetToken.IsFromIdentifierNode(Of CatchStatementSyntax)(Function(catchStatement) catchStatement.IdentifierName) Then
                Return asKeyword
            End If

            ' Function x() |
            ' Operator x() |
            If targetToken.IsChildToken(Of ParameterListSyntax)(Function(paramList) paramList.CloseParenToken) Then
                Dim methodDeclaration = targetToken.GetAncestor(Of MethodBaseSyntax)()
                If methodDeclaration.IsKind(SyntaxKind.FunctionStatement, SyntaxKind.OperatorStatement,
                                                          SyntaxKind.DeclareFunctionStatement, SyntaxKind.DelegateFunctionStatement,
                                                          SyntaxKind.PropertyStatement, SyntaxKind.FunctionLambdaHeader) Then
                    Return asKeyword
                End If
            End If

            ' Function Foo |
            If targetToken.IsChildToken(Of MethodStatementSyntax)(Function(functionDeclaration) functionDeclaration.Identifier) AndAlso
                Not targetToken.GetAncestor(Of MethodBaseSyntax)().IsKind(SyntaxKind.SubStatement) Then
                Return asKeyword
            End If

            ' Property Foo |
            If targetToken.IsChildToken(Of PropertyStatementSyntax)(Function(propertyDeclaration) propertyDeclaration.Identifier) Then
                Return asKeyword
            End If

            ' Custom Event Foo |
            If targetToken.IsChildToken(Of EventStatementSyntax)(Function(eventDeclaration) eventDeclaration.Identifier) Then
                Return asKeyword
            End If

            ' Using foo |
            Dim usingStatement = targetToken.GetAncestor(Of UsingStatementSyntax)()
            If usingStatement IsNot Nothing AndAlso usingStatement.Expression IsNot Nothing AndAlso Not usingStatement.Expression.IsMissing Then
                If usingStatement.Expression Is targetToken.Parent Then
                    Return asKeyword
                End If
            End If

            ' Public Async |
            ' Public Iterator |
            ' but not...
            ' Async |
            ' Iterator |
            If context.IsTypeMemberDeclarationKeywordContext AndAlso
              (targetToken.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse targetToken.HasMatchingText(SyntaxKind.IteratorKeyword)) Then
                Dim parentField = targetToken.Parent.FirstAncestorOrSelf(Of FieldDeclarationSyntax)()
                If parentField IsNot Nothing AndAlso parentField.GetFirstToken() <> targetToken Then
                    Return asKeyword
                Else
                    Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                End If
            End If

            ' Dim foo |
            ' Using foo as new O, foo2 |
            Dim variableDeclarator = targetToken.GetAncestor(Of VariableDeclaratorSyntax)()
            If variableDeclarator IsNot Nothing Then
                If variableDeclarator.Names.Any(Function(name) name.Identifier = targetToken AndAlso name.Identifier.GetTypeCharacter() = TypeCharacter.None) Then
                    Return asKeyword
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace

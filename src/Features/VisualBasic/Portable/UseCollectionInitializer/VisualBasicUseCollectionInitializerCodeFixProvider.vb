' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCollectionInitializer), [Shared]>
    Friend Class VisualBasicUseCollectionInitializerCodeFixProvider
        Inherits AbstractUseCollectionInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetNewStatement(
                statement As StatementSyntax, objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of ExpressionStatementSyntax)) As StatementSyntax
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In matches
                For Each trivia In match.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Return newStatement.WithLeadingTrivia(totalTrivia)
        End Function

        Private Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of ExpressionStatementSyntax)) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectCollectionInitializer(
                    CreateCollectionInitializer(matches)))
        End Function

        Private Function CreateCollectionInitializer(
                matches As ImmutableArray(Of ExpressionStatementSyntax)) As CollectionInitializerSyntax
            Dim nodesAndTokens = New List(Of SyntaxNodeOrToken)

            For i = 0 To matches.Length - 1
                Dim expressionStatement = matches(i)

                Dim newExpression As ExpressionSyntax
                Dim invocationExpression = DirectCast(expressionStatement.Expression, InvocationExpressionSyntax)
                Dim arguments = invocationExpression.ArgumentList.Arguments
                If arguments.Count = 1 Then
                    newExpression = arguments(0).GetExpression()
                Else
                    newExpression = SyntaxFactory.CollectionInitializer(
                        SyntaxFactory.SeparatedList(
                            arguments.Select(Function(a) a.GetExpression()),
                            arguments.GetSeparators()))
                End If

                newExpression = newExpression.WithLeadingTrivia(SyntaxFactory.ElasticMarker)

                If i < matches.Length - 1 Then
                    nodesAndTokens.Add(newExpression)
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                Else
                    newExpression = newExpression.WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                    nodesAndTokens.Add(newExpression)
                End If
            Next

            Return SyntaxFactory.CollectionInitializer(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                SyntaxFactory.SeparatedList(Of ExpressionSyntax)(nodesAndTokens),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
        End Function
    End Class
End Namespace

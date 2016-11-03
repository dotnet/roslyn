' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCollectionInitializer), [Shared]>
    Friend Class VisualBasicUseCollectionInitializerCodeFixProvider
        Inherits AbstractUseCollectionInitializerCodeFixProvider(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of ExpressionStatementSyntax)) As ObjectCreationExpressionSyntax

            Dim initializer = SyntaxFactory.ObjectCollectionInitializer(
                CreateCollectionInitializer(matches))

            If objectCreation.ArgumentList IsNot Nothing AndAlso
               objectCreation.ArgumentList.Arguments.Count = 0 Then

                objectCreation = objectCreation.WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia())).
                                                WithArgumentList(Nothing)
            End If

            Return objectCreation.WithoutTrailingTrivia().
                                  WithInitializer(initializer).
                                  WithTrailingTrivia(objectCreation.GetTrailingTrivia())
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
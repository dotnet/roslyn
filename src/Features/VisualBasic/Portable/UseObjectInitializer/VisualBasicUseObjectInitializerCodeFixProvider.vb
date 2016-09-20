' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseObjectInitializer), [Shared]>
    Friend Class VisualBasicUseObjectInitializerCodeFixProvider
        Inherits AbstractUseObjectInitializerCodeFixProvider(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As List(Of Match(Of AssignmentStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax))) As ObjectCreationExpressionSyntax

            Dim statement = objectCreation.FirstAncestorOrSelf(Of StatementSyntax)

            Dim openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken).
                                          WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            Dim closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken).
                                           WithLeadingTrivia(GetLeadingWhitespace(statement))

            Dim fieldInitializers = CreateFieldInitializers(matches)

            Dim initializer = SyntaxFactory.ObjectMemberInitializer(fieldInitializers).
                                            WithOpenBraceToken(openBrace)
            '.
            'WithCloseBraceToken(closeBrace)

            Return objectCreation.WithoutTrailingTrivia().
                                  WithInitializer(initializer)
        End Function

        Private Function GetLeadingWhitespace(statement As StatementSyntax) As SyntaxTriviaList
            Dim triviaList = statement.GetLeadingTrivia()
            Dim result = New List(Of SyntaxTrivia)
            For i = triviaList.Count - 1 To 0 Step -1
                Dim trivia = triviaList(i)
                If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                    result.Add(trivia)
                Else
                    Exit For
                End If
            Next

            Return SyntaxFactory.TriviaList(result)
        End Function

        Private Function CreateFieldInitializers(
                matches As List(Of Match(Of AssignmentStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax))) As SeparatedSyntaxList(Of FieldInitializerSyntax)
            Dim nodesAndTokens = New List(Of SyntaxNodeOrToken)

            For i = 0 To matches.Count - 1
                Dim match = matches(i)

                Dim rightValue = match.Initializer
                If i < matches.Count - 1 Then
                    rightValue = rightValue.WithoutTrailingTrivia()
                End If
                Dim initializer = SyntaxFactory.NamedFieldInitializer(
                    keyKeyword:=Nothing,
                    dotToken:=match.MemberAccessExpression.OperatorToken,
                    name:=DirectCast(match.MemberAccessExpression.Name, IdentifierNameSyntax),
                    equalsToken:=match.Statement.OperatorToken,
                    expression:=rightValue).WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)

                nodesAndTokens.Add(initializer)
                If i < matches.Count - 1 Then
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken)
                    '.
                    'WithTrailingTrivia(match.Initializer.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                End If
            Next

            Return SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(nodesAndTokens)
        End Function
    End Class
End Namespace
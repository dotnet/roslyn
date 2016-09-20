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
                options As DocumentOptionSet,
                objectCreation As ObjectCreationExpressionSyntax,
                matches As List(Of Match(Of AssignmentStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax))) As ObjectCreationExpressionSyntax

            Dim statement = objectCreation.FirstAncestorOrSelf(Of StatementSyntax)

            Dim openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken).
                                          WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            Dim closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken).
                                           WithLeadingTrivia(GetLeadingWhitespace(statement))

            Dim indentation = options.GetOption(FormattingOptions.IndentationSize).CreateIndentationString(
                options.GetOption(FormattingOptions.UseTabs),
                options.GetOption(FormattingOptions.TabSize))
            Dim indentationTrivia = SyntaxFactory.WhitespaceTrivia(indentation)
            Dim fieldInitializers = CreateFieldInitializers(matches, indentationTrivia)

            Dim initializer = SyntaxFactory.ObjectMemberInitializer(fieldInitializers).
                                            WithOpenBraceToken(openBrace).
                                            WithCloseBraceToken(closeBrace)

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
                matches As List(Of Match(Of AssignmentStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax)),
                indentation As SyntaxTrivia) As SeparatedSyntaxList(Of FieldInitializerSyntax)
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
                    expression:=rightValue).WithLeadingTrivia(match.Statement.GetLeadingTrivia())

                initializer = Indent(initializer, indentation)

                nodesAndTokens.Add(initializer)
                If i < matches.Count - 1 Then
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(match.Initializer.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                End If
            Next

            Return SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(nodesAndTokens)
        End Function

        Private Function Indent(initializer As NamedFieldInitializerSyntax, indentation As SyntaxTrivia) As NamedFieldInitializerSyntax
            Dim rewriter = New IdentationRewriter(indentation)
            Return DirectCast(rewriter.Visit(initializer), NamedFieldInitializerSyntax)
        End Function

        Private Class IdentationRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _indentation As SyntaxTrivia
            Private _seenNewLine As Boolean = True

            Public Sub New(indentation As SyntaxTrivia)
                _indentation = indentation
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token.Kind = SyntaxKind.None Then
                    Return token
                End If

                Dim result = token
                If _seenNewLine Then
                    result = IndentToken(token)
                End If
                _seenNewLine = token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia)
                Return result
            End Function

            Private Function IndentToken(token As SyntaxToken) As SyntaxToken
                Dim allTrivia = New List(Of SyntaxTrivia)

                For Each trivia In token.LeadingTrivia
                    If _seenNewLine Then
                        allTrivia.Add(_indentation)
                    End If

                    allTrivia.Add(trivia)
                    _seenNewLine = trivia.Kind = SyntaxKind.WhitespaceTrivia
                Next

                Return token.WithLeadingTrivia(SyntaxFactory.TriviaList(allTrivia))
            End Function
        End Class
    End Class
End Namespace
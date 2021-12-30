' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Partial Friend Class EndConstructStatementVisitor

        Public Overrides Function VisitIfStatement(node As IfStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of MultiLineIfBlockSyntax)().Any(Function(block) block.EndIfStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End If"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax) As AbstractEndConstructResult
            Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
            Dim indentedWhitespace = aligningWhitespace & "    "

            Dim whitespaceTrivia = {SyntaxFactory.WhitespaceTrivia(aligningWhitespace)}.ToSyntaxTriviaList()
            Dim endOfLine = SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter)

            Dim elseBlock As ElseBlockSyntax = Nothing

            If node.ElseClause IsNot Nothing Then
                Dim trailingTrivia = If(node.ElseClause.ElseKeyword.HasTrailingTrivia AndAlso node.ElseClause.ElseKeyword.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia),
                                        node.ElseClause.ElseKeyword.TrailingTrivia,
                                        {endOfLine}.ToSyntaxTriviaList())
                elseBlock = SyntaxFactory.ElseBlock(SyntaxFactory.ElseStatement(SyntaxFactory.Token(whitespaceTrivia, SyntaxKind.ElseKeyword, trailingTrivia)),
                                           ConvertSingleLineStatementsToMultiLineStatements(node.ElseClause.Statements, indentedWhitespace))
            End If

            Dim ifBlock = SyntaxFactory.MultiLineIfBlock(
                                            SyntaxFactory.IfStatement(node.IfKeyword, node.Condition, node.ThenKeyword).WithTrailingTrivia(endOfLine),
                                            ConvertSingleLineStatementsToMultiLineStatements(node.Statements, indentedWhitespace),
                                            New SyntaxList(Of ElseIfBlockSyntax),
                                            elseBlock,
                                                  SyntaxFactory.EndIfStatement(
                                                      SyntaxFactory.Token(whitespaceTrivia, SyntaxKind.EndKeyword, {SyntaxFactory.WhitespaceTrivia(" ")}.ToSyntaxTriviaList(), "End"),
                                                      SyntaxFactory.Token(Nothing, SyntaxKind.IfKeyword, {endOfLine}.ToSyntaxTriviaList(), "If")))

            Dim position = If(ifBlock.Statements.Any(), ifBlock.Statements(0).SpanStart, ifBlock.IfStatement.Span.End + _state.NewLineCharacter.Length)
            Dim ifNodeToken As SyntaxNodeOrToken = ifBlock
            Return New ReplaceSpanResult(node.FullSpan.ToSnapshotSpan(_subjectBuffer.CurrentSnapshot), ifNodeToken.ToFullString(), position)
        End Function

        ''' <summary>
        ''' Given a separatedSyntaxList of statements separated by colons, converts them to a
        ''' separate syntax list of statements separated by newlines
        ''' </summary>
        ''' <param name="statements">The list of statements to convert.</param>
        ''' <param name="indentedWhitespace">The whitespace to indent with.</param>
        Private Function ConvertSingleLineStatementsToMultiLineStatements(statements As SyntaxList(Of StatementSyntax), indentedWhitespace As String) As SyntaxList(Of StatementSyntax)
            If statements = Nothing OrElse statements.Count = 0 Then
                ' Return an empty statement with a newline
                Return SyntaxFactory.List({DirectCast(SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken, SyntaxFactory.TriviaList(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter)))), StatementSyntax)})
            End If

            Dim indentedWhitespaceTrivia = SpecializedCollections.SingletonEnumerable(SyntaxFactory.WhitespaceTrivia(indentedWhitespace))
            Dim newList As New List(Of StatementSyntax)(capacity:=statements.Count)
            Dim triviaLeftForNextStatement As IEnumerable(Of SyntaxTrivia) = New List(Of SyntaxTrivia)

            ' If the last statement itself is an End If statement, we should skip it
            Dim lastStatementToProcess = statements.Count - 1

            If statements.LastOrDefault().IsKind(SyntaxKind.EndIfStatement) Then
                lastStatementToProcess = statements.Count - 2
            End If

            For i = 0 To lastStatementToProcess
                Dim statement = statements(i)

                ' Add the new whitespace on the start of the statement
                If statement.Kind <> SyntaxKind.EmptyStatement OrElse statement.HasTrailingTrivia Then
                    Dim leadingTrivia = indentedWhitespaceTrivia.Concat(triviaLeftForNextStatement.Concat(statement.GetLeadingTrivia()).WithoutLeadingWhitespaceOrEndOfLine())
                    statement = statement.WithLeadingTrivia(leadingTrivia)
                End If

                ' We want to drop any whitespace trivia from the
                ' end
                Dim trailingTrivia = New List(Of SyntaxTrivia)
                Dim separator As SyntaxTrivia = Nothing

                Dim lastToken = statement.GetLastToken(includeZeroWidth:=True)
                For Each trivia In lastToken.TrailingTrivia
                    If trivia.Kind = SyntaxKind.ColonTrivia Then
                        separator = trivia
                        Exit For
                    End If

                    trailingTrivia.Add(trivia)
                Next

                Do While trailingTrivia.Count > 0 AndAlso trailingTrivia.Last().Kind = SyntaxKind.WhitespaceTrivia
                    trailingTrivia.RemoveAt(trailingTrivia.Count - 1)
                Loop

                If separator.Kind <> SyntaxKind.None OrElse Not trailingTrivia.Any Then
                    trailingTrivia.Add(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter))
                End If

                statement = statement.WithTrailingTrivia(trailingTrivia)
                newList.Add(statement)

                triviaLeftForNextStatement = lastToken.TrailingTrivia.SkipWhile(Function(t) t <> separator).Where(Function(t) t.Kind <> SyntaxKind.ColonTrivia)
            Next

            Return SyntaxFactory.List(newList)
        End Function

    End Class
End Namespace

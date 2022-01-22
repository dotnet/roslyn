' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTreeExtensions
        ''' <summary>
        ''' check whether given token is the last token of a statement that ends with end of line trivia or an elastic trivia
        ''' </summary>
        <Extension()>
        Public Function IsLastTokenOfStatementWithEndOfLine(token As SyntaxToken) As Boolean
            If Not token.HasTrailingTrivia Then
                Return False
            End If

            ' easy case
            Dim trailing = token.TrailingTrivia
            If trailing.Count = 1 Then
                Dim trivia = trailing.First()

                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    Return token.IsLastTokenOfStatement()
                End If

                Return False
            End If

            ' little bit more expansive case
            For Each trivia In trailing
                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    Return token.IsLastTokenOfStatement()
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' check whether given token is the last token of a statement by walking up the spine
        ''' </summary>
        <Extension()>
        Public Function IsLastTokenOfStatement(
                token As SyntaxToken,
                Optional checkColonTrivia As Boolean = False,
                <Out> Optional ByRef statement As StatementSyntax = Nothing) As Boolean
            Dim current = token.Parent
            While current IsNot Nothing
                If current.FullSpan.End <> token.FullSpan.End Then
                    Return False
                End If

                If TypeOf current Is StatementSyntax Then
                    statement = DirectCast(current, StatementSyntax)
                    Dim colonTrivia = GetTrailingColonTrivia(DirectCast(current, StatementSyntax))
                    If Not PartOfSingleLineLambda(current) AndAlso Not PartOfMultilineLambdaFooter(current) Then
                        If checkColonTrivia Then
                            If colonTrivia Is Nothing Then
                                Return current.GetLastToken(includeZeroWidth:=True) = token
                            End If
                        Else
                            Return current.GetLastToken(includeZeroWidth:=True) = token
                        End If
                    End If
                End If

                current = current.Parent
            End While

            Return False
        End Function

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowImplicitBoxing:=False)>
        Private Function GetTrailingColonTrivia(statement As StatementSyntax) As SyntaxTrivia?
            If Not statement.HasTrailingTrivia Then
                Return Nothing
            End If

            Return statement _
                    .GetTrailingTrivia() _
                    .FirstOrNull(Function(t) t.Kind = SyntaxKind.ColonTrivia)
        End Function

        Private Function PartOfSingleLineLambda(node As SyntaxNode) As Boolean
            While node IsNot Nothing
                If TypeOf node Is MultiLineLambdaExpressionSyntax Then Return False
                If TypeOf node Is SingleLineLambdaExpressionSyntax Then Return True
                node = node.Parent
            End While

            Return False
        End Function

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowCaptures:=False)>
        Private Function PartOfMultilineLambdaFooter(node As SyntaxNode) As Boolean
            For Each n In node.AncestorsAndSelf
                Dim multiLine = TryCast(n, MultiLineLambdaExpressionSyntax)
                If multiLine Is Nothing Then
                    Continue For
                End If

                If (multiLine.EndSubOrFunctionStatement Is node) Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Finds the token being touched by this position. Unlike the normal FindTrivia helper, this helper will prefer
        ''' trivia to the left rather than the right if the position is on the border.
        ''' </summary>
        ''' <param name="syntaxTree">The syntaxTree to search.</param>
        ''' <param name="position">The position to find trivia.</param>
        <Extension()>
        Public Function FindTriviaToLeft(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxTrivia
            Return FindTriviaToLeft(syntaxTree.GetRoot(cancellationToken), position)
        End Function

        Private Function FindTriviaToLeft(nodeOrToken As SyntaxNodeOrToken, position As Integer) As SyntaxTrivia
recurse:
            For Each child In nodeOrToken.ChildNodesAndTokens().Reverse()
                If (child.FullSpan.Start < position) AndAlso (position <= child.FullSpan.End) Then
                    If child.IsNode Then
                        nodeOrToken = child
                        GoTo recurse
                    Else
                        For Each trivia In child.GetTrailingTrivia.Reverse
                            If (trivia.SpanStart < position) AndAlso (position <= child.FullSpan.End) Then
                                Return trivia
                            End If
                        Next

                        For Each trivia In child.GetLeadingTrivia.Reverse
                            If (trivia.SpanStart < position) AndAlso (position <= child.FullSpan.End) Then
                                Return trivia
                            End If
                        Next
                    End If
                End If
            Next

            Return Nothing
        End Function

        <Extension()>
        Public Function IsInNonUserCode(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Return _
                syntaxTree.IsEntirelyWithinComment(position, cancellationToken) OrElse
                syntaxTree.IsEntirelyWithinStringOrCharOrNumericLiteral(position, cancellationToken) OrElse
                syntaxTree.IsInInactiveRegion(position, cancellationToken) OrElse
                syntaxTree.IsWithinPartialMethodDeclaration(position, cancellationToken)
        End Function

        <Extension()>
        Public Function IsWithinPartialMethodDeclaration(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Dim declaration = token.GetAncestor(Of MethodStatementSyntax)
            If declaration IsNot Nothing AndAlso declaration.Modifiers.Any(SyntaxKind.PartialKeyword) Then
                Return True
            End If

            Dim block = token.GetAncestor(Of MethodBlockSyntax)
            If block IsNot Nothing AndAlso block.BlockStatement.Modifiers.Any(SyntaxKind.PartialKeyword) Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsEntirelyWithinComment(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim trivia = syntaxTree.FindTriviaToLeft(position, cancellationToken)

            If trivia.IsKind(SyntaxKind.CommentTrivia, SyntaxKind.DocumentationCommentTrivia) AndAlso trivia.SpanStart <> position Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsEntirelyWithinStringLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)

            If token.IsKind(SyntaxKind.StringLiteralToken) Then
                Return token.SpanStart < position AndAlso position < token.Span.End OrElse AtEndOfIncompleteStringOrCharLiteral(token, position, """")
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsEntirelyWithinStringOrCharOrNumericLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)

            If Not token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken, SyntaxKind.DecimalLiteralToken, SyntaxKind.IntegerLiteralToken,
                                SyntaxKind.DateLiteralToken, SyntaxKind.FloatingLiteralToken) Then
                Return False
            End If

            ' Check if it's within a completed token.
            If token.SpanStart < position AndAlso position < token.Span.End Then
                Return True
            End If

            ' If it is a numeric literal, all checks are done and we're okay.
            If token.IsKind(SyntaxKind.IntegerLiteralToken, SyntaxKind.DecimalLiteralToken,
                            SyntaxKind.DateLiteralToken, SyntaxKind.FloatingLiteralToken) Then
                Return False
            End If

            ' For char or string literals, check if we're at the end of an incomplete literal.
            Dim lastChar = If(token.IsKind(SyntaxKind.CharacterLiteralToken), "'", """")

            Return AtEndOfIncompleteStringOrCharLiteral(token, position, lastChar)
        End Function

        Private Function AtEndOfIncompleteStringOrCharLiteral(token As SyntaxToken, position As Integer, lastChar As String) As Boolean
            ' Check if it's a token that was started, but not ended
            Dim startLength = 1
            If token.IsKind(SyntaxKind.CharacterLiteralToken) Then
                startLength = 2
            End If

            Return _
                position = token.Span.End AndAlso
                 (token.Span.Length = startLength OrElse
                  (token.Span.Length > startLength AndAlso Not token.ToString().EndsWith(lastChar, StringComparison.Ordinal)))
        End Function

        <Extension()>
        Public Function IsInInactiveRegion(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Contract.ThrowIfNull(syntaxTree)

            ' cases:
            ' $ is EOF

            ' #IF false Then
            '    |

            ' #IF false Then
            '    |$

            ' #IF false Then
            ' |

            ' #IF false Then
            ' |$

            If syntaxTree.FindTriviaToLeft(position, cancellationToken).Kind = SyntaxKind.DisabledTextTrivia Then
                Return True
            End If

            ' TODO : insert point at the same line as preprocessor?
            Return False
        End Function

        <Extension()>
        Public Function IsInSkippedText(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim trivia = syntaxTree.FindTriviaToLeft(position, cancellationToken)

            Return trivia.IsKind(SyntaxKind.SkippedTokensTrivia)
        End Function

        Private Function IsGlobalStatementContext(token As SyntaxToken, position As Integer) As Boolean
            If Not token.IsLastTokenOfStatement() Then
                Return False
            End If

            ' NB: Checks whether the caret is placed after a colon or an end of line.
            ' Otherwise the typed expression would still be a part of the previous statement.
            If Not token.HasTrailingTrivia OrElse token.HasAncestor(Of IncompleteMemberSyntax) Then
                Return False
            End If

            For Each trivia In token.TrailingTrivia
                If trivia.Span.Start > position Then
                    Return False
                ElseIf trivia.IsKind(SyntaxKind.ColonTrivia) Then
                    Return True
                ElseIf trivia.IsKind(SyntaxKind.EndOfLineTrivia) Then
                    Return True
                End If
            Next

            Return False
        End Function

        <Extension()>
        Public Function IsGlobalStatementContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            If Not syntaxTree.IsScript() Then
                Return False
            End If

            Dim token As SyntaxToken = syntaxTree.FindTokenOnLeftOfPosition(
                position, cancellationToken, includeDirectives:=True).GetPreviousTokenIfTouchingWord(position)

            If token.IsKind(SyntaxKind.None) Then
                Dim compilationUnit = TryCast(syntaxTree.GetRoot(cancellationToken), CompilationUnitSyntax)
                Return compilationUnit Is Nothing OrElse compilationUnit.Imports.Count = 0
            End If

            Return IsGlobalStatementContext(token, position)
        End Function

        <Extension()>
        Public Function IsRightOfDot(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            If token.Kind = SyntaxKind.None Then
                Return False
            End If

            token = token.GetPreviousTokenIfTouchingWord(position)
            Return token.Kind = SyntaxKind.DotToken
        End Function

        <Extension()>
        Public Function IsRightOfIntegerLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Return token.Kind = SyntaxKind.IntegerLiteralToken
        End Function

        <Extension()>
        Public Function IsInPreprocessorDirectiveContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim directive As DirectiveTriviaSyntax = Nothing
            Return IsInPreprocessorDirectiveContext(syntaxTree, position, cancellationToken, directive)
        End Function

        Friend Function IsInPreprocessorDirectiveContext(
                syntaxTree As SyntaxTree,
                position As Integer,
                cancellationToken As CancellationToken,
                ByRef directive As DirectiveTriviaSyntax) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            directive = token.GetAncestor(Of DirectiveTriviaSyntax)()

            ' Directives contain the EOL, so if the position is within the full span of the
            ' directive, then it is on that line, the only exception is if the directive is on the
            ' last line, the position at the end if technically not contained by the directive but
            ' its also not on a new line, so it should be considered part of the preprocessor
            ' context.
            If directive Is Nothing Then
                Return False
            End If

            Return directive.FullSpan.Contains(position) OrElse
                   directive.FullSpan.End = syntaxTree.GetRoot(cancellationToken).FullSpan.End
        End Function

        <Extension()>
        Public Function GetFirstStatementOnLine(syntaxTree As SyntaxTree, lineNumber As Integer, cancellationToken As CancellationToken) As StatementSyntax
            Dim line = syntaxTree.GetText(cancellationToken).Lines(lineNumber)
            Dim token = syntaxTree.GetRoot(cancellationToken).FindToken(line.Start)

            Dim statement = token.Parent.FirstAncestorOrSelf(Of StatementSyntax)()
            If statement IsNot Nothing AndAlso
               syntaxTree.GetText(cancellationToken).Lines.IndexOf(statement.SpanStart) = lineNumber Then

                Return statement
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetFirstEnclosingStatement(node As SyntaxNode) As StatementSyntax
            Return node.AncestorsAndSelf().Where(Function(n) TypeOf n Is StatementSyntax).OfType(Of StatementSyntax).FirstOrDefault()
        End Function
    End Module
End Namespace

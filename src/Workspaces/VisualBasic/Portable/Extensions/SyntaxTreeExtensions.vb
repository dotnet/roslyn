' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTreeExtensions
        <Extension()>
        Public Function IsEntirelyWithinStringLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)

            If token.IsKind(SyntaxKind.StringLiteralToken) Then
                Return token.SpanStart < position AndAlso position < token.Span.End OrElse AtEndOfIncompleteStringOrCharLiteral(token, position, """")
            End If

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
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            Dim directive = token.GetAncestor(Of DirectiveTriviaSyntax)()

            ' Directives contain the EOL, so if the position is within the full span of the
            ' directive, then it is on that line, the only exception is if the directive is on the
            ' last line, the position at the end if technically not contained by the directive but
            ' its also not on a new line, so it should be considered part of the preprocessor
            ' context.
            If directive Is Nothing Then
                Return False
            End If

            Return _
                      directive.FullSpan.Contains(position) OrElse
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

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Partial Friend Class ContainingStatementInfo
        Public ReadOnly IsIncomplete As Boolean
        Public ReadOnly TextSpan As TextSpan
        Public ReadOnly MatchingBlockConstruct As StatementSyntax

        Public Sub New(node As SyntaxNode)
            Me.New(node, node.Span)
        End Sub

        Public Sub New(node As SyntaxNode, span As TextSpan)
            TextSpan = span
            IsIncomplete = node.GetLastToken(includeZeroWidth:=True).IsMissing

            ' We'll only do expansion if there were no errors
            Dim statement = TryCast(node, StatementSyntax)

            If Not IsIncomplete AndAlso statement IsNot Nothing Then
                MatchingBlockConstruct = FindExpansionStatement(statement)
            End If
        End Sub

        Public Sub New(trivia As SyntaxTrivia)
            TextSpan = trivia.Span
            IsIncomplete = trivia.ContainsDiagnostics
        End Sub

        ''' <summary>
        ''' This function returns the "logical" statement that a given point is in. "Logical" in
        ''' this case means "the smallest unit the user probably thinks as a statement", or "the
        ''' thing we should format when you leave it."
        ''' </summary>
        Public Shared Function GetInfo(point As SnapshotPoint,
                                       syntaxTree As SyntaxTree,
                                       cancellationToken As CancellationToken) As ContainingStatementInfo

            Dim snapshot = point.Snapshot
            Dim pointLineNumber = snapshot.GetLineNumberFromPosition(point)

            ' Let's see if we're following a query which we are continuing
            Dim previousToken = syntaxTree.FindTokenOnLeftOfPosition(point, cancellationToken)
            If previousToken.IsLastTokenOfStatement() Then
                Dim previousRealTokenLineNumber = snapshot.GetLineNumberFromPosition(previousToken.SpanStart)

                If pointLineNumber = previousRealTokenLineNumber + 1 AndAlso
                   previousToken.GetAncestor(Of QueryClauseSyntax)() IsNot Nothing Then
                    Return New ContainingStatementInfo(previousToken.GetAncestor(Of StatementSyntax)())
                End If
            End If

            Dim trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(point)

            ' If we're at the newline, we'll want to look to the left instead
            If trivia.Kind = SyntaxKind.None Or trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                trivia = syntaxTree.FindTriviaToLeft(point, cancellationToken)
            End If

            If trivia.Kind = SyntaxKind.CommentTrivia OrElse trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                Return GetContainingStatementInfoForTrivia(trivia, snapshot, pointLineNumber)
            End If

            ' We'll keep going to the left to see if we find a LineContinuation trivia before we see more than one newline
            Dim alreadySawNewLine = False

            Do While trivia.Kind <> SyntaxKind.None
                If trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                    Dim lineNumberOfContinuation = snapshot.GetLineNumberFromPosition(trivia.SpanStart)

                    ' We can be either on the line, or the line immediately following it
                    If pointLineNumber = lineNumberOfContinuation OrElse pointLineNumber = lineNumberOfContinuation + 1 AndAlso
                       trivia.Token.GetAncestor(Of StatementSyntax)() IsNot Nothing Then
                        Return New ContainingStatementInfo(trivia.Token.GetAncestor(Of StatementSyntax)())
                    Else
                        Exit Do
                    End If
                End If

                trivia = syntaxTree.FindTriviaToLeft(trivia.SpanStart, cancellationToken)

                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    If alreadySawNewLine Then
                        Exit Do
                    Else
                        alreadySawNewLine = True
                    End If
                End If
            Loop

            Dim token = syntaxTree.GetRoot(cancellationToken).FindToken(point, findInsideTrivia:=True)

            ' If the first token is on the next line, then we're blank and so we have no statement
            If pointLineNumber <> snapshot.GetLineNumberFromPosition(token.SpanStart) Then
                Return Nothing
            End If

            Dim containingDirective = token.GetAncestor(Of DirectiveTriviaSyntax)()
            If containingDirective IsNot Nothing Then
                Return New ContainingStatementInfo(containingDirective)
            End If

            Dim containingStatement = token.GetAncestors(Of StatementSyntax) _
                                           .Where(Function(a) Not TypeOf a Is LambdaHeaderSyntax) _
                                           .FirstOrDefault()

            Dim containingTypeStatement = TryCast(containingStatement, TypeStatementSyntax)
            If containingTypeStatement IsNot Nothing Then
                Return GetContainingStatementInfoForAttributedStatement(containingTypeStatement, containingTypeStatement.AttributeLists, point)
            End If

            Dim containingMethodStatement = TryCast(containingStatement, MethodBaseSyntax)
            If containingMethodStatement IsNot Nothing Then
                Return GetContainingStatementInfoForAttributedStatement(containingMethodStatement, containingMethodStatement.AttributeLists, point)
            End If

            If containingStatement IsNot Nothing Then
                Return New ContainingStatementInfo(containingStatement)
            End If

            Return Nothing
        End Function

        Private Shared Function GetContainingStatementInfoForTrivia(trivia As SyntaxTrivia, snapshot As ITextSnapshot, pointLineNumber As Integer) As ContainingStatementInfo
            Dim triviaStatement = trivia.Token.GetAncestor(Of StatementSyntax)()

            ' If the trivia is on a different line then the actual token, we consider this
            ' comment to be on it's own statement entirely
            If triviaStatement Is Nothing OrElse snapshot.GetLineNumberFromPosition(trivia.Token.SpanStart) <> pointLineNumber Then
                Return New ContainingStatementInfo(trivia)
            Else
                ' It's on the same line as the statement, so just do the statement
                Return New ContainingStatementInfo(triviaStatement)
            End If
        End Function

        Private Shared Function FindExpansionStatement(node As StatementSyntax) As StatementSyntax
            For Each ancestor In node.Ancestors()
                Dim matchingStatements = MatchingStatementsVisitor.Instance.Visit(ancestor)

                If matchingStatements Is Nothing Then
                    Continue For
                End If

                Dim indexOfNode = matchingStatements.IndexOf(node)

                ' If we wrote the opening statement, then expand to the end
                Dim possibleExpansion As StatementSyntax = Nothing
                If indexOfNode = 0 Then
                    possibleExpansion = matchingStatements.Last()
                ElseIf indexOfNode > 0 Then
                    ' Somewhere in the middle or end, so expand to the beginning
                    possibleExpansion = matchingStatements.First()
                End If

                If possibleExpansion IsNot Nothing AndAlso Not possibleExpansion.IsMissing Then
                    Return possibleExpansion
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function GetContainingStatementInfoForAttributedStatement(node As StatementSyntax, attributes As SyntaxList(Of AttributeListSyntax), position As Integer) As ContainingStatementInfo
            If Not attributes.Any Then
                Return New ContainingStatementInfo(node)
            End If

            ' If we're inside a attribute of the statement, then we should count that as it's own
            ' statement
            Dim containingAttribute = attributes.FirstOrDefault(Function(a) a.Span.Contains(position) OrElse a.Span.End = position)

            If containingAttribute IsNot Nothing Then
                Return New ContainingStatementInfo(containingAttribute)
            End If

            ' We're outside all the attributes, so we'll return just the span that ignores the
            ' attributes
            Return New ContainingStatementInfo(node, TextSpan.FromBounds(attributes.Last.Span.End, node.Span.End))
        End Function
    End Class
End Namespace

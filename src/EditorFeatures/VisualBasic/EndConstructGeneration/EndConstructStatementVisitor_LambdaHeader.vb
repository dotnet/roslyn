' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Partial Friend Class EndConstructStatementVisitor
        Public Overrides Function VisitLambdaHeader(node As LambdaHeaderSyntax) As AbstractEndConstructResult
            Dim singleLineExpressionSyntax = TryCast(node.Parent, SingleLineLambdaExpressionSyntax)
            If singleLineExpressionSyntax IsNot Nothing Then
                Return TransformSingleLineLambda(singleLineExpressionSyntax)
            Else
                Return SpitNormalLambdaEnding(node)
            End If
        End Function

        Private Function TransformSingleLineLambda(originalNode As SingleLineLambdaExpressionSyntax) As AbstractEndConstructResult
            ' If there is newline trivia on the end of the node, we need to pull that off to stick it back on at the very end of this transformation
            Dim newLineTrivia = originalNode.GetTrailingTrivia().SkipWhile(Function(t) Not t.IsKind(SyntaxKind.EndOfLineTrivia))
            Dim node = originalNode.WithTrailingTrivia(originalNode.GetTrailingTrivia().TakeWhile(Function(t) Not t.IsKind(SyntaxKind.EndOfLineTrivia)))

            Dim tokenNextToLambda = originalNode.GetLastToken().GetNextToken()
            Dim isNextToXmlEmbeddedExpression = tokenNextToLambda.IsKind(SyntaxKind.PercentGreaterThanToken) AndAlso tokenNextToLambda.Parent.IsKind(SyntaxKind.XmlEmbeddedExpression)

            Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(originalNode.SpanStart)
            Dim indentedWhitespace = aligningWhitespace & "    "

            ' Generate the end statement since we can easily share that code
            Dim endStatementKind = If(originalNode.Kind = SyntaxKind.SingleLineSubLambdaExpression, SyntaxKind.EndSubStatement, SyntaxKind.EndFunctionStatement)
            Dim endStatement = SyntaxFactory.EndBlockStatement(endStatementKind, SyntaxFactory.Token(originalNode.SubOrFunctionHeader.DeclarationKeyword.Kind).WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(" "))) _
                        .WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(aligningWhitespace)) _
                        .WithTrailingTrivia(If(isNextToXmlEmbeddedExpression, SyntaxFactory.TriviaList(SyntaxFactory.WhitespaceTrivia(" ")), newLineTrivia))

            ' We are hitting enter after a single line. Let's transform it to a multi-line form
            If node.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                ' If we have Sub() End Sub as a lambda, we're better off just doing nothing smart
                If node.Body.IsKind(SyntaxKind.EndSubStatement) Then
                    Return Nothing
                End If

                ' Update the new header
                Dim newHeader = node.SubOrFunctionHeader

                If newHeader.ParameterList Is Nothing OrElse
                   (newHeader.ParameterList.OpenParenToken.IsMissing AndAlso newHeader.ParameterList.CloseParenToken.IsMissing) Then
                    newHeader = newHeader.WithParameterList(SyntaxFactory.ParameterList())
                End If

                newHeader = newHeader.WithTrailingTrivia(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter))

                ' Update the body with a newline
                Dim newBody = DirectCast(node.Body, StatementSyntax).WithAppendedTrailingTrivia(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter))
                Dim newBodyHasCode = False

                ' If it actually contains something, intent it too. Otherwise, we'll just let the smart indenter position
                If Not String.IsNullOrWhiteSpace(newBody.ToFullString()) Then
                    newBody = newBody.WithPrependedLeadingTrivia(SyntaxFactory.WhitespaceTrivia(indentedWhitespace))
                    newBodyHasCode = True
                End If

                Dim newExpression = SyntaxFactory.MultiLineSubLambdaExpression(
                    subOrFunctionHeader:=newHeader,
                    statements:=SyntaxFactory.SingletonList(newBody),
                    endSubOrFunctionStatement:=endStatement)

                Return New ReplaceSpanResult(originalNode.FullSpan.ToSnapshotSpan(_subjectBuffer.CurrentSnapshot),
                                             newExpression.ToFullString(),
                                             If(newBodyHasCode, CType(newExpression.Statements.First().SpanStart, Integer?), Nothing))
            Else
                If node.Body.IsMissing Then
                    If node.Body.GetTrailingTrivia().Any(Function(t) t.IsKind(SyntaxKind.SkippedTokensTrivia)) Then
                        ' If we had to skip tokens, we're probably just going to break more than we fix
                        Return Nothing
                    End If

                    ' It's still missing entirely, so just spit normally
                    Return CreateSpitLinesForLambdaHeader(node.SubOrFunctionHeader, isNextToXmlEmbeddedExpression, originalNode.SpanStart)
                End If

                Dim newHeader = node.SubOrFunctionHeader.WithTrailingTrivia(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter))

                Dim newBody = SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.ReturnKeyword).WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" ")),
                                                            DirectCast(node.Body, ExpressionSyntax)) _
                    .WithPrependedLeadingTrivia(SyntaxFactory.WhitespaceTrivia(indentedWhitespace)) _
                    .WithAppendedTrailingTrivia(SyntaxFactory.EndOfLineTrivia(_state.NewLineCharacter))

                Dim newExpression = SyntaxFactory.MultiLineSubLambdaExpression(
                subOrFunctionHeader:=newHeader,
                statements:=SyntaxFactory.SingletonList(Of StatementSyntax)(newBody),
                endSubOrFunctionStatement:=endStatement)

                ' Fish our body back out so we can figure out relative spans
                newBody = DirectCast(newExpression.Statements.First(), ReturnStatementSyntax)
                Return New ReplaceSpanResult(originalNode.FullSpan.ToSnapshotSpan(_subjectBuffer.CurrentSnapshot),
                         newExpression.ToFullString(),
                         newBody.ReturnKeyword.FullSpan.End)
            End If

            Return Nothing
        End Function

        Private Function SpitNormalLambdaEnding(node As LambdaHeaderSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of MultiLineLambdaExpressionSyntax)().Any(Function(block) block.EndSubOrFunctionStatement.IsMissing AndAlso block.IsMultiLineLambda())

            ' We have to be careful here: just because the Lambda's End isn't missing doesn't mean we shouldn't spit a
            ' End Sub / End Function. A good example is an unterminated multi-line sub in a sub, like this:
            '
            ' Sub goo()
            '    Dim x = Sub()
            ' End Sub
            '
            ' Obviously the parser has an ambiguity here, and so it chooses to parse the End Sub as being the terminator
            ' for the lambda. In this case, we'll notice that this lambda has a parent method body that uses the same
            ' Sub/Function keyword and is missing it's end construct, indicating that we should still spit.

            Dim containingMethodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If containingMethodBlock IsNot Nothing AndAlso containingMethodBlock.EndBlockStatement.IsMissing Then
                ' Is this containing method the same type (Sub/Function) as the lambda?
                If containingMethodBlock.BlockStatement.DeclarationKeyword.Kind = node.DeclarationKeyword.Kind Then
                    needsEnd = True
                End If
            End If

            If needsEnd Then
                Return CreateSpitLinesForLambdaHeader(node)
            Else
                Return Nothing
            End If
        End Function

        Private Function CreateSpitLinesForLambdaHeader(node As LambdaHeaderSyntax, Optional isNextToXmlEmbeddedExpression As Boolean = False, Optional originalNodeSpanStart? As Integer = Nothing) As AbstractEndConstructResult
            Dim spanStart As Integer = If(originalNodeSpanStart.HasValue, originalNodeSpanStart.Value, node.SpanStart)
            Dim endConstruct = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(spanStart) & "End " & node.DeclarationKeyword.ToString()

            ' We may wish to spit () at the end of if we are missing our parenthesis
            If node.ParameterList Is Nothing OrElse (node.ParameterList.OpenParenToken.IsMissing AndAlso node.ParameterList.CloseParenToken.IsMissing) Then
                Return New SpitLinesResult({"()", "", endConstruct}, startOnCurrentLine:=True)
            Else
                Return New SpitLinesResult({"", If(isNextToXmlEmbeddedExpression, endConstruct & " ", endConstruct)})
            End If
        End Function
    End Class
End Namespace

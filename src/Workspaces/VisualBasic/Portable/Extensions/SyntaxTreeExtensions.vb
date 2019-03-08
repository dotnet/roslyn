' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTreeExtensions
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
        Public Function GetSelectedFieldsAndPropertiesInSpan(root As SyntaxNode,
                                         textSpan As TextSpan, allowPartialSelection As Boolean) As ImmutableArray(Of StatementSyntax)

            Dim token = root.FindTokenOnRightOfPosition(textSpan.Start)
            Dim firstMember = token.GetAncestors(Of StatementSyntax).
                                    Where(Function(s) TypeOf s.Parent Is TypeBlockSyntax).
                                    FirstOrDefault()
            If firstMember IsNot Nothing Then
                Dim containingType = DirectCast(firstMember.Parent, TypeBlockSyntax)
                If containingType IsNot Nothing AndAlso
                   firstMember IsNot containingType.BlockStatement AndAlso
                   firstMember IsNot containingType.EndBlockStatement Then
                    Return GetFieldsAndPropertiesInSpan(textSpan, containingType, firstMember, allowPartialSelection)
                End If
            End If

            Return ImmutableArray(Of StatementSyntax).Empty
        End Function

        Private Function GetFieldsAndPropertiesInSpan(
            textSpan As TextSpan,
            containingType As TypeBlockSyntax,
            firstMember As StatementSyntax,
            allowPartialSelection As Boolean) As ImmutableArray(Of StatementSyntax)
            Dim selectedMembers = ArrayBuilder(Of StatementSyntax).GetInstance()

            Try
                Dim members = containingType.Members
                Dim fieldIndex = members.IndexOf(firstMember)
                If fieldIndex < 0 Then
                    Return ImmutableArray(Of StatementSyntax).Empty
                End If

                For i = fieldIndex To members.Count - 1
                    Dim member = members(i)
                    If IsSelectedFieldOrProperty(textSpan, member, allowPartialSelection) Then
                        selectedMembers.Add(member)
                    End If
                Next

                Return selectedMembers.ToImmutable()
            Finally
                selectedMembers.Free()
            End Try
        End Function

        Private Function IsSelectedFieldOrProperty(textSpan As TextSpan, member As StatementSyntax, allowPartialSelection As Boolean) As Boolean
            If Not member.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.PropertyStatement) Then
                Return False
            End If

            ' first, check if entire member is selected
            If textSpan.Contains(member.Span) Then
                Return True
            End If

            If Not allowPartialSelection Then
                Return False
            End If

            ' next, check if identifier is at lease partially selected
            If member.IsKind(SyntaxKind.FieldDeclaration) Then
                Dim fieldDeclaration = DirectCast(member, FieldDeclarationSyntax)
                For Each declarator In fieldDeclaration.Declarators
                    If textSpan.Contains(member.Span) Or (allowPartialSelection And textSpan.OverlapsWith(declarator.Names.Span)) Then
                        Return True
                    End If
                Next
            ElseIf member.IsKind(SyntaxKind.PropertyStatement) Then
                Return textSpan.OverlapsWith((DirectCast(member, PropertyStatementSyntax)).Identifier.Span)
            End If

            Return False
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

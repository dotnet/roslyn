' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Private Class VisualBasicTriviaResult
            Inherits TriviaResult

            Public Shared Async Function ProcessAsync(selectionResult As SelectionResult, cancellationToken As CancellationToken) As Task(Of VisualBasicTriviaResult)
                Dim preservationService = selectionResult.SemanticDocument.Document.Project.Services.GetService(Of ISyntaxTriviaService)()
                Dim root = selectionResult.SemanticDocument.Root
                Dim result = preservationService.SaveTriviaAroundSelection(root, selectionResult.FinalSpan)

                Return New VisualBasicTriviaResult(
                    Await selectionResult.SemanticDocument.WithSyntaxRootAsync(result.Root, cancellationToken).ConfigureAwait(False),
                    result)
            End Function

            Private Sub New(document As SemanticDocument, result As ITriviaSavedResult)
                MyBase.New(document, result, SyntaxKind.EndOfLineTrivia, SyntaxKind.WhitespaceTrivia)
            End Sub

            Protected Overrides Function GetAnnotationResolver(callsite As SyntaxNode, method As SyntaxNode) As AnnotationResolver
                Dim methodDefinition = TryCast(method, MethodBlockBaseSyntax)
                If callsite Is Nothing OrElse methodDefinition Is Nothing Then
                    Return Nothing
                End If

                Return Function(node, location, annotation) AnnotationResolver(node, location, annotation, callsite, methodDefinition)
            End Function

            Protected Overrides Function GetTriviaResolver(method As SyntaxNode) As TriviaResolver
                Dim methodDefinition = TryCast(method, MethodBlockBaseSyntax)
                If methodDefinition Is Nothing Then
                    Return Nothing
                End If

                Return Function(location, tokenPair, triviaMap) TriviaResolver(location, tokenPair, triviaMap, methodDefinition)
            End Function

            Private Shared Function AnnotationResolver(
                node As SyntaxNode,
                location As TriviaLocation,
                annotation As SyntaxAnnotation,
                callsite As SyntaxNode,
                method As MethodBlockBaseSyntax) As SyntaxToken

                Dim token = node.GetAnnotatedNodesAndTokens(annotation).FirstOrDefault().AsToken()
                If token.Kind <> 0 Then
                    Return token
                End If

                Select Case location
                    Case TriviaLocation.BeforeBeginningOfSpan
                        Return callsite.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
                    Case TriviaLocation.AfterEndOfSpan
                        Return callsite.GetLastToken(includeZeroWidth:=True).GetNextToken(includeZeroWidth:=True)
                    Case TriviaLocation.AfterBeginningOfSpan
                        Return method.BlockStatement.GetLastToken(includeZeroWidth:=True).GetNextToken(includeZeroWidth:=True)
                    Case TriviaLocation.BeforeEndOfSpan
                        Return method.EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
                End Select

                throw ExceptionUtilities.UnexpectedValue(location)
            End Function

            Private Function TriviaResolver(
                location As TriviaLocation,
                tokenPair As PreviousNextTokenPair,
                triviaMap As Dictionary(Of SyntaxToken, LeadingTrailingTriviaPair),
                method As MethodBlockBaseSyntax) As IEnumerable(Of SyntaxTrivia)

                ' Resolve trivia at the edge of the selection. simple case is easy to deal with, but complex cases where
                ' elastic trivia and user trivia are mixed (hybrid case) and we want to preserve some part of user coding style
                ' but not others can be dealt with here.

                ' method has no statement in them. so basically two trivia list now pointing to same thing. 
                If tokenPair.PreviousToken = method.BlockStatement.GetLastToken(includeZeroWidth:=True) AndAlso
                   tokenPair.NextToken = method.EndBlockStatement.GetFirstToken(includeZeroWidth:=True) Then
                    Return If(location = TriviaLocation.AfterBeginningOfSpan,
                              SpecializedCollections.SingletonEnumerable(Of SyntaxTrivia)(SyntaxFactory.ElasticMarker),
                              SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)())
                End If

                Dim previousTriviaPair As LeadingTrailingTriviaPair = Nothing
                Dim trailingTrivia = If(triviaMap.TryGetValue(tokenPair.PreviousToken, previousTriviaPair),
                                        previousTriviaPair.TrailingTrivia, SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)())

                Dim nextTriviaPair As LeadingTrailingTriviaPair = Nothing
                Dim leadingTrivia = If(triviaMap.TryGetValue(tokenPair.NextToken, nextTriviaPair),
                                       nextTriviaPair.LeadingTrivia, SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)())

                Dim list = trailingTrivia.Concat(leadingTrivia)

                Select Case location
                    Case TriviaLocation.BeforeBeginningOfSpan
                        Return FilterTriviaList(RemoveTrailingElasticTrivia(tokenPair.PreviousToken, list, tokenPair.NextToken))
                    Case TriviaLocation.AfterEndOfSpan
                        Return FilterTriviaList(RemoveLeadingElasticTrivia(RemoveLeadingElasticTrivia(tokenPair.PreviousToken, list, tokenPair.NextToken)))
                    Case TriviaLocation.AfterBeginningOfSpan
                        Return FilterTriviaList(RemoveLeadingElasticTrivia(tokenPair.PreviousToken, list, tokenPair.NextToken))
                    Case TriviaLocation.BeforeEndOfSpan
                        Return FilterTriviaList(RemoveTrailingElasticTrivia(tokenPair.PreviousToken, list, tokenPair.NextToken))
                End Select

                throw ExceptionUtilities.UnexpectedValue(location)
            End Function

            Private Shared Function RemoveTrailingElasticTrivia(
                token1 As SyntaxToken, list As IEnumerable(Of SyntaxTrivia), token2 As SyntaxToken) As IEnumerable(Of SyntaxTrivia)

                ' special case for skipped token trivia
                ' formatter doesn't touch tokens that have skipped tokens in-between. so, we need to take care of such case ourselves
                If list.Any(Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia) Then
                    Return RemoveElasticAfterColon(
                        token1.TrailingTrivia.Concat(list).Concat(ReplaceElasticToEndOfLine(token2.LeadingTrivia)))
                End If

                If token1.IsLastTokenOfStatement() Then
                    Return RemoveElasticAfterColon(token1.TrailingTrivia.Concat(list).Concat(token2.LeadingTrivia))
                End If

                Return token1.TrailingTrivia.Concat(list)
            End Function

            Private Shared Function RemoveLeadingElasticTrivia(
                token1 As SyntaxToken, list As IEnumerable(Of SyntaxTrivia), token2 As SyntaxToken) As IEnumerable(Of SyntaxTrivia)

                If token1.IsLastTokenOfStatement() Then
                    If SingleLineStatement(token1) Then
                        Return list.Concat(token2.LeadingTrivia)
                    End If

                    Return RemoveElasticAfterColon(token1.TrailingTrivia.Concat(list).Concat(token2.LeadingTrivia))
                End If

                Return list.Concat(token2.LeadingTrivia)
            End Function

            Private Shared Function RemoveLeadingElasticTrivia(list As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
                ' remove leading elastic trivia if it is followed by noisy trivia
                Dim trivia = list.FirstOrDefault()
                If Not trivia.IsElastic() Then
                    Return list
                End If

                For Each trivia In list.Skip(1)
                    If trivia.Kind = SyntaxKind.EndOfLineTrivia OrElse trivia.IsElastic() Then
                        Return list
                    ElseIf trivia.Kind <> SyntaxKind.EndOfLineTrivia And trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                        Return list.Skip(1)
                    End If
                Next

                Return list
            End Function

            Private Shared Function ReplaceElasticToEndOfLine(list As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
                Return list.Select(Function(t) If(t.IsElastic, SyntaxFactory.CarriageReturnLineFeed, t))
            End Function

            Private Shared Function SingleLineStatement(token As SyntaxToken) As Boolean
                ' check whether given token is the last token of a single line statement
                Dim singleLineIf = token.Parent.GetAncestor(Of SingleLineIfStatementSyntax)()
                If singleLineIf IsNot Nothing Then
                    Return True
                End If

                Dim singleLineLambda = token.Parent.GetAncestor(Of SingleLineLambdaExpressionSyntax)()
                If singleLineLambda IsNot Nothing Then
                    Return True
                End If

                Return False
            End Function

            Private Shared Function RemoveElasticAfterColon(list As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
                ' make sure we don't have elastic trivia after colon trivia
                Dim colon = False
                Dim result = New List(Of SyntaxTrivia)()

                For Each trivia In list
                    If trivia.RawKind = SyntaxKind.ColonTrivia Then
                        colon = True
                    End If

                    If colon AndAlso trivia.IsElastic() Then
                        Continue For
                    End If

                    result.Add(trivia)
                Next

                Return result
            End Function
        End Class
    End Class
End Namespace

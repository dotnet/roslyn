' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class TriviaDataFactory
        Friend Class TriviaRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _node As SyntaxNode
            Private ReadOnly _spans As SimpleIntervalTree(Of TextSpan)
            Private ReadOnly _lastToken As SyntaxToken
            Private ReadOnly _cancellationToken As CancellationToken

            Private ReadOnly _trailingTriviaMap As Dictionary(Of SyntaxToken, SyntaxTriviaList)
            Private ReadOnly _leadingTriviaMap As Dictionary(Of SyntaxToken, SyntaxTriviaList)

            Public Sub New(node As SyntaxNode, spanToFormat As SimpleIntervalTree(Of TextSpan), map As Dictionary(Of ValueTuple(Of SyntaxToken, SyntaxToken), TriviaData), cancellationToken As CancellationToken)
                Contract.ThrowIfNull(node)
                Contract.ThrowIfNull(map)

                _node = node
                _spans = spanToFormat
                _lastToken = node.GetLastToken(includeZeroWidth:=True)
                _cancellationToken = cancellationToken

                _trailingTriviaMap = New Dictionary(Of SyntaxToken, SyntaxTriviaList)()
                _leadingTriviaMap = New Dictionary(Of SyntaxToken, SyntaxTriviaList)()

                PreprocessTriviaListMap(map)
            End Sub

            Public Function Transform() As SyntaxNode
                Return Visit(_node)
            End Function

            Private Sub PreprocessTriviaListMap(map As Dictionary(Of ValueTuple(Of SyntaxToken, SyntaxToken), TriviaData))
                For Each pair In map
                    _cancellationToken.ThrowIfCancellationRequested()

                    Dim tuple = GetTrailingAndLeadingTrivia(pair)

                    If pair.Key.Item1.Kind <> 0 Then
                        _trailingTriviaMap.Add(pair.Key.Item1, tuple.Item1)
                    End If

                    If pair.Key.Item2.Kind <> 0 Then
                        _leadingTriviaMap.Add(pair.Key.Item2, tuple.Item2)
                    End If
                Next pair
            End Sub

            Private Function GetTrailingAndLeadingTrivia(pair As KeyValuePair(Of ValueTuple(Of SyntaxToken, SyntaxToken), TriviaData)) As ValueTuple(Of SyntaxTriviaList, SyntaxTriviaList)
                If pair.Key.Item1.Kind = 0 OrElse _lastToken = pair.Key.Item2 Then
                    Return ValueTuple.Create(Of SyntaxTriviaList, SyntaxTriviaList)(SyntaxTriviaList.Empty,
                                                                                    GetSyntaxTriviaList(GetTextSpan(pair.Key), pair.Value, _cancellationToken))
                End If

                Dim vbTriviaData = TryCast(pair.Value, TriviaDataWithList)
                If vbTriviaData IsNot Nothing Then
                    Dim triviaList = vbTriviaData.GetTriviaList(_cancellationToken)
                    Dim index = GetIndexForEndOfLeadingTrivia(triviaList)

                    Return ValueTuple.Create(SyntaxFactory.TriviaList(CreateTriviaListFromTo(triviaList, 0, index)), SyntaxFactory.TriviaList(CreateTriviaListFromTo(triviaList, index + 1, triviaList.Count - 1)))
                End If

                Return ValueTuple.Create(Of SyntaxTriviaList, SyntaxTriviaList)(GetSyntaxTriviaList(GetTextSpan(pair.Key), pair.Value, _cancellationToken), SyntaxTriviaList.Empty)
            End Function

            Private Function GetTextSpan(pair As ValueTuple(Of SyntaxToken, SyntaxToken)) As TextSpan
                If pair.Item1.Kind = 0 Then
                    Return TextSpan.FromBounds(_node.FullSpan.Start, pair.Item2.SpanStart)
                End If

                If pair.Item2.Kind = 0 Then
                    Return TextSpan.FromBounds(pair.Item1.Span.End, _node.FullSpan.End)
                End If

                Return TextSpan.FromBounds(pair.Item1.Span.End, pair.Item2.SpanStart)
            End Function

            Private Function CreateTriviaListFromTo(triviaList As List(Of SyntaxTrivia), startIndex As Integer, endIndex As Integer) As IEnumerable(Of SyntaxTrivia)
                If startIndex > endIndex Then
                    Return SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)()
                End If

                Dim returnTriviaList = New List(Of SyntaxTrivia)(endIndex - startIndex)
                For i As Integer = startIndex To endIndex
                    returnTriviaList.Add(triviaList(i))
                Next i

                Return returnTriviaList
            End Function

            Private Function GetIndexForEndOfLeadingTrivia(triviaList As List(Of SyntaxTrivia)) As Integer
                For i As Integer = 0 To triviaList.Count - 1
                    Dim trivia = triviaList(i)
                    If trivia.Kind = SyntaxKind.EndOfLineTrivia Or
                       trivia.Kind = SyntaxKind.ColonTrivia Then
                        Return i
                    End If
                Next i

                Return triviaList.Count - 1
            End Function

            Private Function GetSyntaxTriviaList(textSpan As TextSpan, triviaData As TriviaData, cancellationToken As CancellationToken) As SyntaxTriviaList
                Dim vbTriviaData = TryCast(triviaData, TriviaDataWithList)
                If vbTriviaData IsNot Nothing Then
                    Return SyntaxFactory.TriviaList(vbTriviaData.GetTriviaList(cancellationToken))
                End If

                ' there is no difference between ParseLeading and ParseTrailing for the given text
                Dim text = triviaData.GetTextChanges(textSpan).Single().NewText
                Return SyntaxFactory.ParseLeadingTrivia(text)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                If node Is Nothing OrElse Not Me._spans.IntersectsWith(node.FullSpan) Then
                    Return node
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                _cancellationToken.ThrowIfCancellationRequested()

                If Not Me._spans.IntersectsWith(token.FullSpan) Then
                    Return token
                End If

                Dim hasChanges = False

                ' check whether we have trivia info belongs to this token
                Dim leadingTrivia = token.LeadingTrivia
                Dim trailingTrivia = token.TrailingTrivia

                If _trailingTriviaMap.ContainsKey(token) Then
                    ' okay, we have this situation
                    ' token|trivia
                    trailingTrivia = _trailingTriviaMap(token)
                    hasChanges = True
                End If

                If _leadingTriviaMap.ContainsKey(token) Then
                    ' okay, we have this situation
                    ' trivia|token
                    leadingTrivia = _leadingTriviaMap(token)
                    hasChanges = True
                End If

                If hasChanges Then
                    Return CreateNewToken(leadingTrivia, token, trailingTrivia)
                End If

                ' we have no trivia belongs to this one
                Return token
            End Function

            Private Function CreateNewToken(leadingTrivia As SyntaxTriviaList, token As SyntaxToken, trailingTrivia As SyntaxTriviaList) As SyntaxToken
                Return token.With(leadingTrivia, trailingTrivia)
            End Function
        End Class
    End Class
End Namespace

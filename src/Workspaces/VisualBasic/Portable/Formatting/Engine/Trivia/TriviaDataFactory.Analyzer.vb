' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class Analyzer
            Public Shared Function Leading(token As SyntaxToken) As AnalysisResult
                Dim result As AnalysisResult

                Analyze(token.LeadingTrivia, result)
                Return result
            End Function

            Public Shared Function Trailing(token As SyntaxToken) As AnalysisResult
                Dim result As AnalysisResult

                Analyze(token.TrailingTrivia, result)
                Return result
            End Function

            Public Shared Function Between(token1 As SyntaxToken, token2 As SyntaxToken) As AnalysisResult
                If (Not token1.HasTrailingTrivia) AndAlso (Not token2.HasLeadingTrivia) Then
                    Return Nothing
                End If

                Dim result As AnalysisResult

                Analyze(token1.TrailingTrivia, result)
                Analyze(token2.LeadingTrivia, result)
                Return result
            End Function

            Private Shared Sub Analyze(list As SyntaxTriviaList, ByRef result As AnalysisResult)
                If list.Count = 0 Then
                    Return
                End If
                Dim previousTrivia As New SyntaxTrivia
                For Each trivia In list
                    If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                        AnalyzeWhitespacesInTrivia(trivia, result)
                    ElseIf trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                        AnalyzeLineBreak(previousTrivia, trivia, result)
                    ElseIf trivia.Kind = SyntaxKind.CommentTrivia OrElse trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                        result.HasComments = True
                    ElseIf trivia.Kind = SyntaxKind.DisabledTextTrivia OrElse trivia.Kind = SyntaxKind.SkippedTokensTrivia Then
                        result.HasSkippedOrDisabledText = True
                    ElseIf trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                        AnalyzeLineContinuation(trivia, result)
                    ElseIf trivia.Kind = SyntaxKind.ColonTrivia Then
                        result.HasColonTrivia = True
                    ElseIf trivia.Kind = SyntaxKind.ConflictMarkerTrivia Then
                        result.HasConflictMarker = True
                    Else
                        Contract.ThrowIfFalse(SyntaxFacts.IsPreprocessorDirective(trivia.Kind))

                        result.HasPreprocessor = True
                    End If
                    previousTrivia = trivia
                Next
            End Sub

            Private Shared Sub AnalyzeLineContinuation(trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                result.LineBreaks += 1

                result.HasTrailingSpace = trivia.ToFullString().Length <> 3
                result.HasLineContinuation = True
                result.HasOnlyOneSpaceBeforeLineContinuation = result.Space = 1 AndAlso result.Tab = 0

                result.HasTabAfterSpace = False
                result.Space = 0
                result.Tab = 0
            End Sub

            Private Shared Sub AnalyzeLineBreak(previousTrivia As SyntaxTrivia, trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                ' if there was any space immediately before line break, then we have trailing spaces
                If previousTrivia.Kind = SyntaxKind.WhitespaceTrivia AndAlso previousTrivia.Width > 0 Then
                    result.HasTrailingSpace = True
                    result.HasTabAfterSpace = False
                    result.Space = 0
                    result.Tab = 0
                    result.TreatAsElastic = result.TreatAsElastic Or trivia.IsElastic()
                End If

                ' reset space and tab information
                result.LineBreaks += 1

            End Sub

            Private Shared Sub AnalyzeWhitespacesInTrivia(trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                ' trivia already has text. getting text should be noop
                Debug.Assert(trivia.Kind = SyntaxKind.WhitespaceTrivia)
                Debug.Assert(trivia.Width = trivia.FullWidth)

                Dim space As Integer = 0
                Dim tab As Integer = 0
                Dim unknownWhitespace As Integer = 0

                Dim text = trivia.ToString()
                For i As Integer = 0 To trivia.Width - 1
                    If text(i) = " "c Then
                        space += 1
                    ElseIf text(i) = vbTab Then
                        If result.Space > 0 Then
                            result.HasTabAfterSpace = True
                        End If

                        tab += 1
                    Else
                        unknownWhitespace += 1
                    End If
                Next i

                ' set result
                result.Space += space
                result.Tab += tab
                result.HasUnknownWhitespace = result.HasUnknownWhitespace Or unknownWhitespace > 0
                result.TreatAsElastic = result.TreatAsElastic Or trivia.IsElastic()
            End Sub

            Friend Structure AnalysisResult
                Friend Property LineBreaks() As Integer
                Friend Property Space() As Integer
                Friend Property Tab() As Integer

                Friend Property HasTabAfterSpace() As Boolean
                Friend Property HasUnknownWhitespace() As Boolean
                Friend Property HasTrailingSpace() As Boolean
                Friend Property HasSkippedOrDisabledText() As Boolean

                Friend Property HasComments() As Boolean
                Friend Property HasPreprocessor() As Boolean
                Friend Property HasConflictMarker() As Boolean

                Friend Property HasOnlyOneSpaceBeforeLineContinuation() As Boolean
                Friend Property HasLineContinuation() As Boolean
                Friend Property HasColonTrivia() As Boolean
                Friend Property TreatAsElastic() As Boolean
            End Structure
        End Class
    End Class
End Namespace

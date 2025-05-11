' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    ''' <summary>
    ''' trivia factory.
    ''' 
    ''' it will cache some commonly used trivia to reduce memory footprint and heap allocation
    ''' </summary>
    Partial Friend Class TriviaDataFactory
        Inherits AbstractTriviaDataFactory

        Private Const s_lineContinuationCacheSize = 80

        Private ReadOnly _lineContinuations(s_lineContinuationCacheSize) As LineContinuationTrivia

        Public Sub New(treeInfo As TreeData, options As LineFormattingOptions)
            MyBase.New(treeInfo, options)
        End Sub

        Public Overrides Function CreateLeadingTrivia(token As SyntaxToken) As TriviaData
            ' no trivia
            If Not token.HasLeadingTrivia Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(Nothing, token)))
                Return GetSpaceTriviaData(space:=0)
            End If

            Dim result = Analyzer.Leading(token)
            Dim info = GetWhitespaceOnlyTriviaInfo(Nothing, token, result)
            If info IsNot Nothing Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(Nothing, token)))
                Return info
            End If

            Return New ComplexTrivia(Me.Options, Me.TreeInfo, Nothing, token)
        End Function

        Public Overrides Function CreateTrailingTrivia(token As SyntaxToken) As TriviaData
            ' no trivia
            If Not token.HasTrailingTrivia Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(token, Nothing)))
                Return GetSpaceTriviaData(space:=0)
            End If

            Dim result = Analyzer.Trailing(token)
            Dim info = GetWhitespaceOnlyTriviaInfo(token, Nothing, result)
            If info IsNot Nothing Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(token, Nothing)))
                Return info
            End If

            Return New ComplexTrivia(Me.Options, Me.TreeInfo, token, Nothing)
        End Function

        Public Overrides Function Create(token1 As SyntaxToken, token2 As SyntaxToken) As TriviaData
            ' no trivia in between
            If (Not token1.HasTrailingTrivia) AndAlso (Not token2.HasLeadingTrivia) Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(token1, token2)))
                Return GetSpaceTriviaData(space:=0)
            End If

            Dim result = Analyzer.Between(token1, token2)
            If ContainsOnlyLineContinuation(result) Then
                Dim lineContinuationTriviaData = GetLineContinuationTriviaInfo(token1, token2, result)

                If lineContinuationTriviaData IsNot Nothing Then
                    Return lineContinuationTriviaData
                End If

                Return New ComplexTrivia(Me.Options, Me.TreeInfo, token1, token2)
            End If

            Dim triviaData = GetWhitespaceOnlyTriviaInfo(token1, token2, result)
            If triviaData IsNot Nothing Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(token1, token2)))
                Return triviaData
            End If

            Return New ComplexTrivia(Me.Options, Me.TreeInfo, token1, token2)
        End Function

        Private Function GetLineContinuationTriviaInfo(token1 As SyntaxToken, token2 As SyntaxToken, result As Analyzer.AnalysisResult) As TriviaData
            If result.LineBreaks <> 1 OrElse
               result.TreatAsElastic OrElse
               Not result.HasOnlyOneSpaceBeforeLineContinuation OrElse
               Not result.HasTrailingSpace Then
                Return Nothing
            End If

            ' check whether we can cache trivia info for current indentation
            Dim lineCountAndIndentation = GetLineBreaksAndIndentation(result)

            Dim useTriviaAsItIs As Boolean = lineCountAndIndentation.Item1
            Dim lineBreaks = lineCountAndIndentation.Item2
            Dim indentation = lineCountAndIndentation.Item3

            Contract.ThrowIfFalse(lineBreaks = 1)

            Dim canUseCache = useTriviaAsItIs AndAlso
                              indentation < s_lineContinuationCacheSize

            If Not canUseCache Then
                Return Nothing
            End If

            EnsureLineContinuationTriviaInfo(indentation, Me.TreeInfo.GetTextBetween(token1, token2))
            Return Me._lineContinuations(indentation)
        End Function

        Private Sub EnsureLineContinuationTriviaInfo(indentation As Integer, originalString As String)
            Contract.ThrowIfFalse(indentation >= 0 AndAlso indentation < s_lineContinuationCacheSize)
            Debug.Assert(originalString.Substring(0, 4) = " _" & vbCrLf)

            ' set up caches
            If Me._lineContinuations(indentation) Is Nothing Then
                Dim triviaInfo = New LineContinuationTrivia(Me.Options, originalString, indentation)
                Interlocked.CompareExchange(Me._lineContinuations(indentation), triviaInfo, Nothing)
            End If
        End Sub

        Private Shared Function ContainsOnlyLineContinuation(result As Analyzer.AnalysisResult) As Boolean
            Return result.HasLineContinuation AndAlso
                   Not result.HasComments AndAlso
                   Not result.HasColonTrivia AndAlso
                   Not result.HasPreprocessor AndAlso
                   Not result.HasSkippedOrDisabledText AndAlso
                   Not result.HasUnknownWhitespace AndAlso
                   Not result.HasConflictMarker
        End Function

        Private Shared Function ContainsOnlyWhitespace(result As Analyzer.AnalysisResult) As Boolean
            Return Not result.HasComments AndAlso
                   Not result.HasColonTrivia AndAlso
                   Not result.HasPreprocessor AndAlso
                   Not result.HasSkippedOrDisabledText AndAlso
                   Not result.HasLineContinuation AndAlso
                   Not result.HasConflictMarker
        End Function

        Private Function GetWhitespaceOnlyTriviaInfo(token1 As SyntaxToken, token2 As SyntaxToken, result As Analyzer.AnalysisResult) As TriviaData
            If Not ContainsOnlyWhitespace(result) Then
                Return Nothing
            End If

            ' only whitespace in between
            Dim space As Integer = GetSpaceOnSingleLine(result)
            Contract.ThrowIfFalse(space >= -1)

            If space >= 0 Then
                Return GetSpaceTriviaData(space, result.TreatAsElastic)
            End If

            ' tab is used in a place where it is not an indentation
            If result.LineBreaks = 0 AndAlso result.Tab > 0 Then
                ' calculate actual space size from tab
                Dim spaces = CalculateSpaces(token1, token2)
                Return New ModifiedWhitespace(Me.Options, result.LineBreaks, indentation:=spaces, elastic:=result.TreatAsElastic)
            End If

            ' check whether we can cache trivia info for current indentation
            Dim lineCountAndIndentation = GetLineBreaksAndIndentation(result)

            Dim useTriviaAsItIs As Boolean = lineCountAndIndentation.Item1
            Return GetWhitespaceTriviaData(lineCountAndIndentation.Item2, lineCountAndIndentation.Item3, useTriviaAsItIs, result.TreatAsElastic)
        End Function

        Private Function CalculateSpaces(token1 As SyntaxToken, token2 As SyntaxToken) As Integer
            Dim initialColumn = If(token1.Kind = 0, 0, Me.TreeInfo.GetOriginalColumn(Me.Options.TabSize, token1) + token1.Width)
            Dim textSnippet = Me.TreeInfo.GetTextBetween(token1, token2)

            Return textSnippet.ConvertTabToSpace(Me.Options.TabSize, initialColumn, textSnippet.Length)
        End Function

        Private Function GetLineBreaksAndIndentation(result As Analyzer.AnalysisResult) As ValueTuple(Of Boolean, Integer, Integer)
            Debug.Assert(result.Tab >= 0)
            Debug.Assert(result.LineBreaks >= 0)

            Dim indentation = result.Tab * Me.Options.TabSize + result.Space
            If result.HasTrailingSpace OrElse result.HasUnknownWhitespace Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            If Not Me.Options.UseTabs Then
                If result.Tab > 0 Then
                    Return ValueTuple.Create(False, result.LineBreaks, indentation)
                End If

                Return ValueTuple.Create(True, result.LineBreaks, indentation)
            End If

            Debug.Assert(Me.Options.UseTabs)

            ' tab can only appear before space to be a valid tab for indentation
            If result.HasTabAfterSpace Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            If result.Space >= Me.Options.TabSize Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            Debug.Assert((indentation \ Options.TabSize) = result.Tab)
            Debug.Assert((indentation Mod Options.TabSize) = result.Space)

            Return ValueTuple.Create(True, result.LineBreaks, indentation)
        End Function

        Private Shared Function GetSpaceOnSingleLine(result As Analyzer.AnalysisResult) As Integer
            If result.HasTrailingSpace OrElse result.HasUnknownWhitespace OrElse result.LineBreaks > 0 OrElse result.Tab > 0 Then
                Return -1
            End If

            Return result.Space
        End Function
    End Class
End Namespace

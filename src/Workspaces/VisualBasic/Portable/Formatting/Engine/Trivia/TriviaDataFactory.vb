' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    ''' <summary>
    ''' trivia factory.
    ''' 
    ''' it will cache some commonly used trivia to reduce memory footprint and heap allocation
    ''' </summary>
    Partial Friend Class TriviaDataFactory
        Inherits AbstractTriviaDataFactory

        Private Const s_lineBreakCacheSize = 5
        Private Const s_indentationLevelCacheSize = 20
        Private Const s_lineContinuationCacheSize = 80

        Private ReadOnly _lineContinuations(s_lineContinuationCacheSize) As LineContinuationTrivia

        Public Sub New(treeInfo As TreeData, optionSet As OptionSet)
            MyBase.New(treeInfo, optionSet)
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

            Return New ComplexTrivia(Me.OptionSet, Me.TreeInfo, Nothing, token)
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

            Return New ComplexTrivia(Me.OptionSet, Me.TreeInfo, token, Nothing)
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

                Return New ComplexTrivia(Me.OptionSet, Me.TreeInfo, token1, token2)
            End If

            Dim triviaData = GetWhitespaceOnlyTriviaInfo(token1, token2, result)
            If triviaData IsNot Nothing Then
                Debug.Assert(String.IsNullOrWhiteSpace(Me.TreeInfo.GetTextBetween(token1, token2)))
                Return triviaData
            End If

            Return New ComplexTrivia(Me.OptionSet, Me.TreeInfo, token1, token2)
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
                Dim triviaInfo = New LineContinuationTrivia(Me.OptionSet, originalString, indentation)
                Interlocked.CompareExchange(Me._lineContinuations(indentation), triviaInfo, Nothing)
            End If
        End Sub

        Private Function ContainsOnlyLineContinuation(result As Analyzer.AnalysisResult) As Boolean
            Return result.HasLineContinuation AndAlso
                   Not result.HasComments AndAlso
                   Not result.HasColonTrivia AndAlso
                   Not result.HasPreprocessor AndAlso
                   Not result.HasSkippedOrDisabledText AndAlso
                   Not result.HasUnknownWhitespace
        End Function

        Private Function ContainsOnlyWhitespace(result As Analyzer.AnalysisResult) As Boolean
            If result.HasComments OrElse
               result.HasColonTrivia OrElse
               result.HasPreprocessor OrElse
               result.HasSkippedOrDisabledText OrElse
               result.HasLineContinuation Then
                Return False
            End If

            Return True
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
                Return New ModifiedWhitespace(Me.OptionSet, result.LineBreaks, Indentation:=spaces, elastic:=result.TreatAsElastic, language:=LanguageNames.VisualBasic)
            End If

            ' check whether we can cache trivia info for current indentation
            Dim lineCountAndIndentation = GetLineBreaksAndIndentation(result)

            Dim useTriviaAsItIs As Boolean = lineCountAndIndentation.Item1
            Return GetWhitespaceTriviaData(lineCountAndIndentation.Item2, lineCountAndIndentation.Item3, useTriviaAsItIs, result.TreatAsElastic)
        End Function

        Private Function CalculateSpaces(token1 As SyntaxToken, token2 As SyntaxToken) As Integer
            Dim initialColumn = If(token1.Kind = 0, 0, Me.TreeInfo.GetOriginalColumn(Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic), token1) + token1.Width)
            Dim textSnippet = Me.TreeInfo.GetTextBetween(token1, token2)

            Return textSnippet.ConvertTabToSpace(Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic), initialColumn, textSnippet.Length)
        End Function

        Private Function GetLineBreaksAndIndentation(result As Analyzer.AnalysisResult) As ValueTuple(Of Boolean, Integer, Integer)
            Debug.Assert(result.Tab >= 0)
            Debug.Assert(result.LineBreaks >= 0)

            Dim indentation = result.Tab * Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic) + result.Space
            If result.HasTrailingSpace OrElse result.HasUnknownWhitespace Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            If Not Me.OptionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.VisualBasic) Then
                If result.Tab > 0 Then
                    Return ValueTuple.Create(False, result.LineBreaks, indentation)
                End If

                Return ValueTuple.Create(True, result.LineBreaks, indentation)
            End If

            Debug.Assert(Me.OptionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.VisualBasic))

            ' tab can only appear before space to be a valid tab for indentation
            If result.HasTabAfterSpace Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            If result.Space >= Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic) Then
                Return ValueTuple.Create(False, result.LineBreaks, indentation)
            End If

            Debug.Assert((indentation \ Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic)) = result.Tab)
            Debug.Assert((indentation Mod Me.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic)) = result.Space)

            Return ValueTuple.Create(True, result.LineBreaks, indentation)
        End Function

        Private Function GetSpaceOnSingleLine(result As Analyzer.AnalysisResult) As Integer
            If result.HasTrailingSpace OrElse result.HasUnknownWhitespace OrElse result.LineBreaks > 0 OrElse result.Tab > 0 Then
                Return -1
            End If

            Return result.Space
        End Function
    End Class
End Namespace

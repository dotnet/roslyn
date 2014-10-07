Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.VisualBasic.Extensions
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaFormatter

#Region "Caches"
        Private Shared ReadOnly LineContinuationCache As SyntaxTrivia
        Private Shared ReadOnly SpaceCache() As String
        Private Shared ReadOnly Pool As TriviaListPool
#End Region

        Private ReadOnly context As FormattingContext
        Private ReadOnly token1 As SyntaxToken
        Private ReadOnly token2 As SyntaxToken

        Private ReadOnly lineBreaks As Integer
        Private ReadOnly spaces As Integer

        Shared Sub New()
            SpaceCache = New String(19) {}
            For i As Integer = 0 To 19
                SpaceCache(i) = New String(" "c, i)
            Next i

            LineContinuationCache = Syntax.LineContinuationTrivia("_" + vbCrLf)

            Pool = New TriviaListPool()
        End Sub

        Public Sub New(context As FormattingContext, token1 As SyntaxToken, token2 As SyntaxToken, lineBreaks As Integer, spaces As Integer)
            Contract.ThrowIfNull(context)
            Contract.ThrowIfFalse(lineBreaks >= 0)
            Contract.ThrowIfFalse(spaces >= 0)

            Me.context = context

            Me.token1 = token1
            Me.token2 = token2

            Me.lineBreaks = lineBreaks
            Me.spaces = spaces
        End Sub

        Public Function FormatToSyntaxTriviaList() As List(Of SyntaxTrivia)
            Dim list = TriviaListPool.Allocate()

            FormatAndAppendToSyntaxTriviaList(list)
            Return TriviaListPool.ReturnAndFree(list)
        End Function

        Public Function FormatToString() As String
            ' optimized for common case
            ' used pool to get short live heap objects
            Dim builder = StringBuilderPool.Allocate()
            Dim list = TriviaListPool.Allocate()

            FormatAndAppendToSyntaxTriviaList(list)
            For i As Integer = 0 To list.Count - 1
                builder.Append(list(i).GetFullText())
            Next i

            TriviaListPool.Free(list)
            Return StringBuilderPool.ReturnAndFree(builder)
        End Function

        Private Sub FormatAndAppendToSyntaxTriviaList(triviaList As List(Of SyntaxTrivia))
            Dim currentLineBreaks As Integer

            Dim buffer = TriviaListPool.Allocate()
            Dim triviaBuilder = ProcessTriviaOnEachLine(buffer, triviaList, currentLineBreaks)

            ' single line trivia case
            If Me.lineBreaks = 0 AndAlso currentLineBreaks = 0 Then
                Dim initialColumn = Me.context.TreeInfo.GetColumnOfToken(token1, Me.context.Options.TabSize) + token1.Width()
                triviaBuilder.CommitBetweenTokens(initialColumn, triviaList)
                Return
            End If

            ' if there was trivia left over, don't bother to add indentation. probably code is
            ' malformed anyway.
            Dim beginningOfNewLine = currentLineBreaks <> 0 OrElse IsFirstTriviaInTreeOrAfterStatementTerminator()
            If triviaBuilder.CommitLeftOver(beginningOfNewLine, triviaList, currentLineBreaks) Then
                Return
            End If

            AppendLineBreakTrivia(currentLineBreaks, triviaList)

            ' remove indentation if we are formatting "<token>\r\n .. <some noisy trivia such as line continuation>\r\n .. <statement termination token>"
            If Me.token2.Kind = SyntaxKind.StatementTerminatorToken Then
                Return
            End If

            Dim indentationString = Me.spaces.CreateIndentationString(Me.context.Options.UseTab, Me.context.Options.TabSize)
            AppendWhitespaceTrivia(indentationString, triviaList)

            TriviaListPool.Free(buffer)
        End Sub

        Private Sub AppendLineBreakTrivia(currentLineBreaks As Integer, list As List(Of SyntaxTrivia))
            If currentLineBreaks >= Me.lineBreaks Then
                Return
            End If

            ' by default, prepend extra new lines asked rather than append.
            Dim tempList = TriviaListPool.Allocate()
            For i As Integer = currentLineBreaks To Me.lineBreaks - 2
                tempList.Add(Syntax.CarriageReturnLineFeed)
            Next i

            list.InsertRange(0, tempList)
            TriviaListPool.Free(tempList)
        End Sub

        Private Function ProcessTriviaOnEachLine(buffer As List(Of SyntaxTrivia), triviaList As List(Of SyntaxTrivia), ByRef currentLineBreaks As Integer) As TriviaLineBuilder
            Dim triviaBuilder = New TriviaLineBuilder(Me.context, buffer)

            ' initialize
            currentLineBreaks = 0

            For Each currentTrivia In GetTriviaBetweenTokens()

                ' trivia list we have is between two tokens. so trailing trivia belongs to previous token will not
                ' start from new line.
                Dim beginningOfNewLine = currentLineBreaks <> 0 OrElse IsFirstTriviaInTreeOrAfterStatementTerminator()

                If currentTrivia.IsElastic OrElse
                   currentTrivia.Kind = SyntaxKind.EndOfLineTrivia OrElse
                   currentTrivia.Kind = SyntaxKind.ImplicitLineContinuationTrivia Then
                    currentLineBreaks += triviaBuilder.CommitLines(beginningOfNewLine, triviaList)

                    triviaList.Add(Syntax.CarriageReturnLineFeed)
                    currentLineBreaks += 1

                    triviaBuilder.Reset()
                    Continue For
                ElseIf currentTrivia.Kind = SyntaxKind.LineContinuationTrivia OrElse
                       currentTrivia.Kind = SyntaxKind.DocumentationComment OrElse
                       currentTrivia.Kind.IsPreprocessorDirective() OrElse
                       currentTrivia.Kind = SyntaxKind.DisabledTextTrivia Then
                    triviaBuilder.Add(currentTrivia)
                    currentLineBreaks += triviaBuilder.CommitLines(beginningOfNewLine, triviaList)

                    triviaBuilder.Reset()
                    Continue For
                End If

                triviaBuilder.Add(currentTrivia)
            Next currentTrivia

            Return triviaBuilder
        End Function

        Private Shared Sub AppendWhitespaceTrivia(whitespaceString As String, list As List(Of SyntaxTrivia))
            If String.IsNullOrEmpty(whitespaceString) Then
                Return
            End If

            list.Add(Syntax.WhitespaceTrivia(whitespaceString))
        End Sub

        Private Function IsFirstTriviaInTreeOrAfterStatementTerminator() As Boolean
            Return Me.token1.Kind = SyntaxKind.None OrElse Me.token1.Kind = SyntaxKind.StatementTerminatorToken
        End Function

        Private Shared Function GetSpaces(space As Integer) As String
            If space >= 0 AndAlso space < 20 Then
                Return SpaceCache(space)
            End If

            Return New String(" "c, space)
        End Function

        Private Function GetTriviaBetweenTokens() As IEnumerable(Of SyntaxTrivia)
            Dim list = New List(Of SyntaxTrivia)

            If Me.token1.Kind <> SyntaxKind.None AndAlso Me.token1.HasTrailingTrivia Then
                For i As Integer = 0 To Me.token1.TrailingTrivia.Count - 1
                    list.Add(Me.token1.TrailingTrivia(i))
                Next i
            End If

            If Me.token2.Kind <> SyntaxKind.None AndAlso Me.token2.HasLeadingTrivia Then
                For i As Integer = 0 To Me.token2.LeadingTrivia.Count - 1
                    list.Add(Me.token2.LeadingTrivia(i))
                Next i
            End If

            Return list
        End Function

        Public Shared Function ContainsSkippedTokensOrText(list As TriviaList) As Boolean
            For i As Integer = 0 To list.Count - 1
                Dim trivia = list(i)

                If trivia.Kind = SyntaxKind.SkippedTokens Then
                    Return True
                End If
            Next i

            Return False
        End Function

        Friend Shared Function ShouldFormatTriviaOnSingleLine(triviaList As TriviaList) As Boolean

            For i As Integer = 0 To triviaList.Count - 1
                Dim trivia = triviaList(i)

                Contract.ThrowIfTrue(trivia.Kind = SyntaxKind.EndOfLineTrivia)
                Contract.ThrowIfTrue(trivia.Kind = SyntaxKind.SkippedTokens)

                ' if it contains elastic trivia. always format
                If trivia.IsElastic Then
                    Return True
                End If

                If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                    Debug.Assert(trivia.GetText() = trivia.GetFullText())
                    Dim text = trivia.GetText()
                    If text.IndexOf(vbTab) >= 0 Then
                        Return True
                    End If
                End If

                If trivia.Kind = SyntaxKind.CommentTrivia Then
                    ' need to format if comment sit right after a token
                    Return i = 0
                End If

                If trivia.Kind = SyntaxKind.DocumentationComment Then
                    Return False
                End If

                If trivia.Kind = SyntaxKind.RegionDirective OrElse trivia.Kind = SyntaxKind.EndRegionDirective OrElse trivia.Kind.IsPreprocessorDirective() Then
                    Return False
                End If
            Next i

            Return True
        End Function

        Public Shared Function ShouldFormatTriviaOnMultipleLines(options As FormattingOptions,
                                                                 beginningOfNewLine As Boolean,
                                                                 desiredIndentation As Integer,
                                                                 list As TriviaList) As Boolean
            Return MultiLineAnalyzer.ShouldFormat(options, beginningOfNewLine, desiredIndentation, list)
        End Function
    End Class
End Namespace
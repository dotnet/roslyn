Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        ''' <summary>   
        ''' represents a general trivia between two tokens. slightly more expensive than others since it
        ''' needs to calculate stuff unlike other cases
        ''' </summary>
        Private Class ComplexTriviaData
            Inherits VisualBasicTriviaData

            Private ReadOnly privateTreeInfo As TreeData
            Private ReadOnly privateToken1 As SyntaxToken
            Private ReadOnly privateToken2 As SyntaxToken
            Private ReadOnly privateOriginalString As String
            Private ReadOnly privateTreatAsElastic As Boolean

            Public ReadOnly Property TreeInfo() As TreeData
                Get
                    Return privateTreeInfo
                End Get
            End Property

            Public ReadOnly Property Token1() As SyntaxToken
                Get
                    Return privateToken1
                End Get
            End Property

            Public ReadOnly Property Token2() As SyntaxToken
                Get
                    Return privateToken2
                End Get
            End Property

            Public ReadOnly Property OriginalString() As String
                Get
                    Return privateOriginalString
                End Get
            End Property

            Public Sub New(options As FormattingOptions, treeInfo As TreeData, token1 As SyntaxToken, token2 As SyntaxToken)
                MyBase.New(options)
                Contract.ThrowIfNull(treeInfo)

                Me.privateToken1 = token1
                Me.privateToken2 = token2

                Me.privateTreatAsElastic = HasAnyWhitespaceElasticTrivia(token1, token2)

                Me.privateTreeInfo = treeInfo
                Me.privateOriginalString = Me.TreeInfo.GetTextBetween(token1, token2)

                Dim lineBreaks As Integer
                Dim spaces As Integer
                Me.OriginalString.ProcessTextBetweenTokens(Me.TreeInfo, token1, Me.Options.TabSize, lineBreaks, spaces)

                Me.LineBreaks = lineBreaks + If(token1.Kind = SyntaxKind.StatementTerminatorToken, 1, 0)
                Me.Space = spaces
            End Sub

            Public Overrides ReadOnly Property TreatAsElastic() As Boolean
                Get
                    Return Me.privateTreatAsElastic
                End Get
            End Property

            Public Overrides ReadOnly Property IsWhitespaceOnlyTrivia() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ShouldReplaceOriginalWithNewString() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property NewString() As String
                Get
                    Return Contract.FailWithReturn(Of String)("Should never be called")
                End Get
            End Property

            Public Overrides ReadOnly Property TriviaList() As List(Of SyntaxTrivia)
                Get
                    Return Contract.FailWithReturn(Of List(Of SyntaxTrivia))("Should never be called")
                End Get
            End Property

            Public Overrides Function WithSpace(space As Integer) As TriviaData
                ' two tokens are on a singleline, we dont allow changing spaces between two tokens that contain
                ' noisy characters between them.
                If Not Me.SecondTokenIsFirstTokenOnLine Then
                    Return Me
                End If

                ' okay, two tokens are on different lines, we are basically asked to remove line breaks between them
                ' and make them to be on a single line. well, that is not allowed when there are noisy chars between them
                If Me.SecondTokenIsFirstTokenOnLine Then
                    Return Me
                End If

                Return Contract.FailWithReturn(Of TriviaData)("Can not reach here")
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer) As TriviaData
                Contract.ThrowIfFalse(line > 0)

                ' if we have elastic trivia, always let it be modified
                If Me.TreatAsElastic Then
                    Return New ModifiedComplexTriviaData(Me.Options, Me, line, indentation)
                End If

                ' two tokens are on a single line, it is always allowed to put those two tokens on a different lines
                If Not Me.SecondTokenIsFirstTokenOnLine Then
                    Return New ModifiedComplexTriviaData(Me.Options, Me, line, indentation)
                End If

                ' okay, two tokens are on different lines, now we need to see whether we can add more lines or not
                If Me.SecondTokenIsFirstTokenOnLine Then
                    ' we are asked to add more lines. sure, no problem
                    If Me.LineBreaks < line Then
                        Return New ModifiedComplexTriviaData(Me.Options, Me, line, indentation)
                    End If

                    ' we already has same number of lines, but it is asking changing indentation
                    If Me.LineBreaks = line Then
                        Return WithIndentation(indentation)
                    End If

                    ' sorry, we can't reduce lines if it contains noisy chars
                    If Me.LineBreaks > line Then
                        Return Me
                    End If
                End If

                Return Contract.FailWithReturn(Of TriviaData)("Can not reach here")
            End Function

            Public Overrides Function WithIndentation(indentation As Integer) As TriviaData
                ' if tokens are not in different line, there is nothing we can do here
                If Not Me.SecondTokenIsFirstTokenOnLine Then
                    Return Me
                End If

                ' well, we are already in a desired format, nothing to do. return as it is.
                If Me.Space = indentation Then
                    Return Me
                End If

                ' okay, indentation line has only whitespaces. luckily, it is safe for us to manipulate them.
                Return New ModifiedComplexTriviaData(Me.Options, Me, Me.LineBreaks, indentation)
            End Function

            Public Overrides Sub Format(context As FormattingContext, formattingResultApplier As Action(Of Integer, TriviaData), Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)

                Dim list As TriviaList = New TriviaList(Me.Token1.TrailingTrivia, Me.Token2.LeadingTrivia)
                Contract.ThrowIfFalse(list.Count > 0)

                ' okay, now, check whether we need or are able to format noisy tokens
                If TriviaFormatter.ContainsSkippedTokensOrText(list) Then
                    Return
                End If

                If Not ShouldFormat(context, list) Then
                    Return
                End If

                formattingResultApplier(tokenPairIndex, New FormattedComplexTriviaData(context, Me.Token1, Me.Token2, Me.LineBreaks, Me.Space, Me.OriginalString))
            End Sub

            Private Function ShouldFormat(context As FormattingContext, triviaList As TriviaList) As Boolean
                If Not Me.SecondTokenIsFirstTokenOnLine Then
                    Return TriviaFormatter.ShouldFormatTriviaOnSingleLine(triviaList)
                End If

                Debug.Assert(Me.SecondTokenIsFirstTokenOnLine)

                Dim desiredIndentation = context.GetBaseIndentation(triviaList(0).Span.Start)

                Dim beginningOfNewLine = Me.Token1.Kind = SyntaxKind.None OrElse Me.Token1.Kind = SyntaxKind.StatementTerminatorToken
                Return TriviaFormatter.ShouldFormatTriviaOnMultipleLines(context.Options, beginningOfNewLine, desiredIndentation, triviaList)
            End Function

            Private Shared Function HasAnyWhitespaceElasticTrivia(previousToken As SyntaxToken, currentToken As SyntaxToken) As Boolean
                If (Not previousToken.HasTrailingTrivia) AndAlso (Not currentToken.HasLeadingTrivia) Then
                    Return False
                End If

                Return HasAnyWhitespaceElasticTrivia(previousToken.TrailingTrivia) OrElse HasAnyWhitespaceElasticTrivia(currentToken.LeadingTrivia)
            End Function

            Private Shared Function HasAnyWhitespaceElasticTrivia(list As SyntaxTriviaList) As Boolean
                For i As Integer = 0 To list.Count - 1
                    Dim trivia = list(i)

                    If trivia.IsElastic Then
                        Return True
                    End If
                Next i

                Return False
            End Function
        End Class
    End Class
End Namespace
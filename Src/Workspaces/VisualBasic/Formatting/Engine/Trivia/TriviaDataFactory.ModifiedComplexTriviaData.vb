Imports System
Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class ModifiedComplexTriviaData
            Inherits VisualBasicTriviaData
            Private ReadOnly original As ComplexTriviaData

            Public Sub New(options As FormattingOptions, original As ComplexTriviaData, lineBreaks As Integer, space As Integer)
                MyBase.New(options)
                Contract.ThrowIfNull(original)

                Me.original = original

                ' linebreak and space can become negative during formatting. but it should be normalized to >= 0
                ' at the end.
                Me.LineBreaks = lineBreaks
                Me.Space = space
            End Sub

            Public Overrides ReadOnly Property ShouldReplaceOriginalWithNewString() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property TreatAsElastic() As Boolean
                Get
                    Return Me.original.TreatAsElastic
                End Get
            End Property

            Public Overrides ReadOnly Property IsWhitespaceOnlyTrivia() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property NewString() As String
                Get
                    Return Contract.FailWithReturn(Of String)("Should be never called")
                End Get
            End Property

            Public Overrides ReadOnly Property TriviaList() As List(Of SyntaxTrivia)
                Get
                    Return Contract.FailWithReturn(Of List(Of SyntaxTrivia))("Should be never called")
                End Get
            End Property

            Public Overrides Function WithSpace(space As Integer) As TriviaData
                Return Me.original.WithSpace(space)
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer) As TriviaData
                Return Me.original.WithLine(line, indentation)
            End Function

            Public Overrides Function WithIndentation(indentation As Integer) As TriviaData
                Return Me.original.WithIndentation(indentation)
            End Function

            Public Overrides Sub Format(context As FormattingContext, formattingResultApplier As Action(Of Integer, TriviaData), Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)
                Contract.ThrowIfFalse(Me.SecondTokenIsFirstTokenOnLine)

                Dim list = New TriviaList(Me.original.Token1.TrailingTrivia, Me.original.Token2.LeadingTrivia)
                Contract.ThrowIfFalse(list.Count > 0)

                ' okay, now, check whether we need or are able to format noisy tokens
                If TriviaFormatter.ContainsSkippedTokensOrText(list) Then
                    Return
                End If

                formattingResultApplier(tokenPairIndex, New FormattedComplexTriviaData(context, Me.original.Token1, Me.original.Token2, Me.LineBreaks, Me.Space, Me.original.OriginalString))
            End Sub
        End Class
    End Class
End Namespace
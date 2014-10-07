Imports System
Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class FormattedComplexTriviaData
            Inherits VisualBasicTriviaData

            Private ReadOnly _context As FormattingContext
            Private ReadOnly _token1 As SyntaxToken
            Private ReadOnly _token2 As SyntaxToken

            Private ReadOnly _originalString As String
            Private ReadOnly _formattedString As String
            Private ReadOnly _shouldReplace As Boolean

            Public Sub New(context As FormattingContext, token1 As SyntaxToken, token2 As SyntaxToken, lineBreaks As Integer, spaces As Integer, originalString As String)
                MyBase.New(context.Options)
                Contract.ThrowIfNull(context)
                Contract.ThrowIfNull(originalString)

                Me._context = context
                Me._token1 = token1
                Me._token2 = token2
                Me._originalString = originalString

                Me.LineBreaks = Math.Max(0, lineBreaks)
                Me.Space = Math.Max(0, spaces)

                Dim formatter = New TriviaFormatter(Me._context, Me._token1, Me._token2, Me.LineBreaks, Me.Space)
                Me._formattedString = formatter.FormatToString()

                Me._shouldReplace = Not Me._originalString.Equals(Me._formattedString)
            End Sub

            Public ReadOnly Property Token1 As SyntaxToken
                Get
                    Return Me._token1
                End Get
            End Property

            Public Overrides ReadOnly Property TreatAsElastic() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsWhitespaceOnlyTrivia() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ShouldReplaceOriginalWithNewString() As Boolean
                Get
                    Return Me._shouldReplace
                End Get
            End Property

            Public Overrides ReadOnly Property NewString() As String
                Get
                    Return Me._formattedString
                End Get
            End Property

            Public Overrides ReadOnly Property TriviaList() As List(Of SyntaxTrivia)
                Get
                    Dim formatter = New TriviaFormatter(Me._context, Me._token1, Me._token2, Me.LineBreaks, Me.Space)
                    Return formatter.FormatToSyntaxTriviaList()
                End Get
            End Property

            Public Overrides Function WithSpace(space As Integer) As TriviaData
                Return Contract.FailWithReturn(Of TriviaData)("Shouldn't be called")
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer) As TriviaData
                Return Contract.FailWithReturn(Of TriviaData)("Shouldn't be called")
            End Function

            Public Overrides Function WithIndentation(indentation As Integer) As TriviaData
                Return Contract.FailWithReturn(Of TriviaData)("Shouldn't be called")
            End Function

            Public Overrides Sub Format(context As FormattingContext, formattingResultApplier As Action(Of Integer, TriviaData), Optional tokenPairIndex As Integer = TokenPairIndexNotNeeded)
                Contract.Fail("Shouldn't be called")
            End Sub
        End Class
    End Class
End Namespace
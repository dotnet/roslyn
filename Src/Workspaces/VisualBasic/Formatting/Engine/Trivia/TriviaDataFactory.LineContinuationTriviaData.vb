Imports System
Imports System.Diagnostics
Imports System.Text
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
        Private Class LineContinuationTriviaData
            Inherits AbstractImplicitEndOfLineTriviaData

            Public Sub New(options As FormattingOptions,
                           originalString As String,
                           indentation As Integer)
                MyBase.New(options, originalString, 1, indentation, False)
            End Sub

            Protected Overrides Function CreateStringFromState() As String
                Contract.ThrowIfFalse(Me.SecondTokenIsFirstTokenOnLine)

                Dim builder = StringBuilderPool.Allocate()
                builder.Append(" "c)
                builder.Append(SyntaxFacts.GetText(SyntaxKind.LineContinuationTrivia))

                builder.Append(Me.Space.CreateIndentationString(Me.Options.UseTab, Me.Options.TabSize))
                Return StringBuilderPool.ReturnAndFree(builder)
            End Function

            Public Overrides Function WithIndentation(indentation As Integer) As TriviaData
                If Me.Space = indentation Then
                    Return Me
                End If

                Return New LineContinuationTriviaData(Me.Options, Me.originalString, indentation)
            End Function
        End Class
    End Class
End Namespace
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
        Private Class AfterStatementTerminatorTokenTriviaData
            Inherits AbstractImplicitEndOfLineTriviaData

            Public Sub New(options As FormattingOptions,
                           originalString As String,
                           lineBreaks As Integer,
                           indentation As Integer,
                           elastic As Boolean)
                MyBase.New(options, originalString, lineBreaks, indentation, elastic)
            End Sub

            Protected Overrides Function CreateStringFromState() As String
                Contract.ThrowIfFalse(Me.SecondTokenIsFirstTokenOnLine)

                Dim lineBreaks = Math.Max(0, Me.LineBreaks - 1)

                Dim builder = StringBuilderPool.Allocate()
                For i As Integer = 0 To lineBreaks - 1
                    builder.AppendLine()
                Next i

                builder.Append(Math.Max(0, Me.Space).CreateIndentationString(Me.Options.UseTab, Me.Options.TabSize))
                Return StringBuilderPool.ReturnAndFree(builder)
            End Function

            Public Overrides Function WithIndentation(indentation As Integer) As TriviaData
                If Me.Space = indentation Then
                    Return Me
                End If

                Return New AfterStatementTerminatorTokenTriviaData(Me.Options, Me.originalString, Me.LineBreaks, indentation, elastic:=False)
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer) As TriviaData
                If Me.LineBreaks = line AndAlso
                   Me.Space = indentation Then
                    Return Me
                End If

                Return New AfterStatementTerminatorTokenTriviaData(Me.Options, Me.originalString, line, indentation, elastic:=False)
            End Function
        End Class
    End Class
End Namespace
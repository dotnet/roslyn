Imports System
Imports System.Diagnostics
Imports System.Text
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private MustInherit Class AbstractImplicitEndOfLineTriviaData
            Inherits WhitespaceTriviaData

            Protected ReadOnly originalString As String
            Protected ReadOnly privateNewString As Lazy(Of String)

            Public Sub New(options As FormattingOptions,
                           originalString As String,
                           lineBreaks As Integer,
                           indentation As Integer,
                           elastic As Boolean)
                MyBase.New(options, lineBreaks, indentation, elastic)

                Me.originalString = originalString
                Me.privateNewString = New Lazy(Of String)(Function() CreateStringFromState(), isThreadSafe:=True)
            End Sub

            Protected MustOverride Function CreateStringFromState() As String

            Public Overrides Function WithSpace(space As Integer) As TriviaData
                Return Contract.FailWithReturn(Of TriviaData)("Should never happen")
            End Function

            Public Overrides Function WithLine(line As Integer, indentation As Integer) As TriviaData
                Return Contract.FailWithReturn(Of TriviaData)("Should never happen")
            End Function

            Public Overrides ReadOnly Property ShouldReplaceOriginalWithNewString As Boolean
                Get
                    Return Not Me.originalString.Equals(Me.NewString)
                End Get
            End Property

            Public Overrides ReadOnly Property NewString As String
                Get
                    Return Me.privateNewString.Value
                End Get
            End Property

            Public Overrides Sub Format(context As FormattingContext, formattingResultApplier As Action(Of Integer, TriviaData), tokenPairIndex As Integer)
                If Me.ShouldReplaceOriginalWithNewString Then
                    formattingResultApplier(tokenPairIndex, Me)
                End If
            End Sub
        End Class
    End Class
End Namespace
'INSTANT VB NOTE: This code snippet uses implicit typing. You will need to set 'Option Infer On' in the VB file or set 'Option Infer' at the project level.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class VisualBasicFormattingResult
        Private Class AggregatedResult
            Inherits AbstractAggregatedFormattingResult(Of SyntaxNode, VisualBasicFormattingResult)
            Implements IFormattingResult

            Private ReadOnly rootNode As SyntaxNode

            Public Sub New(node As SyntaxNode, results As List(Of VisualBasicFormattingResult))
                MyBase.New(results)

                Contract.ThrowIfNull(node)
                Me.rootNode = node
            End Sub

            Protected Overrides Function GetFormattedRootWorker(cancellationToken As CancellationToken) As SyntaxNode
                ' create a map
                Dim map = New Dictionary(Of TextSpan, TriviaData)()
                For Each result As VisualBasicFormattingResult In Me.FormattingResults
                    For Each change In result.GetChanges(cancellationToken)
                        map.Add(change.Item1, change.Item2)
                    Next
                Next

                Dim rewriter = New TriviaDataFactory.TriviaRewriter(Me.rootNode, map)
                Return rewriter.Transform()
            End Function
        End Class
    End Class
End Namespace
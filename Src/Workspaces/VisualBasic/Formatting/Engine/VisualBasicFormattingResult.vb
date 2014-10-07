Imports System.Collections.Generic
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting
Imports Roslyn.Services.Shared.Utilities

Namespace Roslyn.Services.VisualBasic.Formatting
    ''' <summary>
    ''' this holds onto changes made by formatting engine.
    ''' 
    ''' currently it only has an ability to apply those changes to buffer. but it could be expanded to
    ''' support other cases as well such as tree or etc.
    ''' </summary>
    Friend Class VisualBasicFormattingResult
        Inherits AbstractFormattingResult(Of SyntaxNode)
        Implements IFormattingResult

        Friend Shared Function CreateAggregatedResult(rootNode As SyntaxNode, results As List(Of VisualBasicFormattingResult)) As IFormattingResult
            Return New AggregatedResult(rootNode, results)
        End Function

        Friend Sub New(treeInfo As TreeData, myTokenStream As TokenStream)
            MyBase.New(treeInfo, myTokenStream)
        End Sub

        Private Function GetChanges(cancellationToken As CancellationToken) As IEnumerable(Of ValueTuple(Of TextSpan, TriviaData))
            Return From triviaInfo In Me.TokenStream.GetAllTriviaDataWithSpan(cancellationToken)
                   Where triviaInfo.Item2.ContainChanges
                   Select triviaInfo
        End Function

        Protected Overrides Function GetFormattedRootWorker(cancellationToken As CancellationToken) As SyntaxNode
            ' create a map
            Dim map = New Dictionary(Of TextSpan, TriviaData)()
            For Each change In GetChanges(cancellationToken)
                map.Add(change.Item1, change.Item2)
            Next

            Dim rewriter = New TriviaDataFactory.TriviaRewriter(CType(Me.TreeInfo.Root, SyntaxNode), map)
            Return rewriter.Transform()
        End Function
    End Class
End Namespace
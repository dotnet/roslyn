Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting

    Partial Class TriviaFormatter

        Private Class TriviaListPool
            ' maximum memory used by the pool is 16*20*28 (sizeof(SyntaxTrivia)) bytes
            Private Const MaxPool = 16
            Private Const Threshold = 20

            Private Shared ReadOnly triviaListPool As New ConcurrentQueue(Of List(Of SyntaxTrivia))

            Public Shared Function Allocate() As List(Of SyntaxTrivia)
                Dim result As List(Of SyntaxTrivia) = Nothing
                If triviaListPool.TryDequeue(result) Then
                    Return result
                End If

                Return New List(Of SyntaxTrivia)
            End Function

            Public Shared Function ReturnAndFree(pool As List(Of SyntaxTrivia)) As List(Of SyntaxTrivia)
                Dim result = New List(Of SyntaxTrivia)(pool)
                Free(pool)

                Return result
            End Function

            Public Shared Sub Free(pool As List(Of SyntaxTrivia))
                If triviaListPool.Count >= MaxPool OrElse
                    pool.Capacity > Threshold Then
                    Return
                End If

                pool.Clear()
                triviaListPool.Enqueue(pool)
            End Sub
        End Class
    End Class
End Namespace
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Poor man's Iterator
    ''' wraps a generator factory into an IEnumerable.
    ''' generator is a lambda with a state (machine) that will produce sequence of values when called repeatedly.
    ''' returning False from generator indicates that sequence is finished.
    ''' </summary>
    Friend Class Iterator(Of T)
        Implements IEnumerable(Of T)

        ''' <summary>
        ''' special func to create generators. the value is returned as a ByRef argument 
        ''' while result used to indicate when iterating finishes.
        ''' </summary>
        Friend Delegate Function GeneratorFunc(Of U As T)(ByRef val As U) As Boolean

        Private ReadOnly _generatorFactory As Func(Of GeneratorFunc(Of T))

        Friend Sub New(generatorFactory As Func(Of GeneratorFunc(Of T)))
            Contract.Requires(generatorFactory IsNot Nothing)
            _generatorFactory = generatorFactory
        End Sub

        Friend Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Dim generator = _generatorFactory()
            Contract.Assume(generator IsNot Nothing)

            Return New Enumerator(Of T)(generator)
        End Function

        Friend Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function

        ''' <summary>
        ''' Enumerator wrapper for a generator lambda.
        ''' </summary>
        Private Class Enumerator(Of U As T)
            Implements IEnumerator(Of U)

            Private ReadOnly _generator As GeneratorFunc(Of U)
            Private _current As U

            Friend Sub New(generator As GeneratorFunc(Of U))
                Contract.Requires(generator IsNot Nothing)
                _current = Nothing
                _generator = generator
            End Sub

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Current
                End Get
            End Property

            Friend Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                Return _generator(_current)
            End Function

            Friend Sub Reset() Implements IEnumerator.Reset
                Throw New InvalidOperationException("Reset is not supported.")
            End Sub

            Friend ReadOnly Property Current As U Implements IEnumerator(Of U).Current
                Get
                    Return _current
                End Get
            End Property

            Friend Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class ' Enumerator
    End Class
End Namespace
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' TemporaryLookupResults allows the binding process to create an uncontested pool of 
    ''' LookupResults that can be used by a single thread during binding.  LookupResults are
    ''' retrieved/returned to this pool as needed, removing the need to get them from the
    ''' global pool which may be highly contested on high-proc machines.
    ''' <para/>
    ''' TemporaryLookupResults instances can be created effectively as high up in the binding stack
    ''' as is convenient.  Ideally, they are created at any public entry-point into the binding
    ''' process and passed along to any code that needs them.
    ''' </summary>
    Friend Structure TemporaryLookupResults
        Private Shared ReadOnly s_stackPool As New ObjectPool(Of Stack(Of LookupResult))(Function() New Stack(Of LookupResult))

        Private _stack As Stack(Of LookupResult)

        ''' <summary>
        ''' Constructor parameter is unused as exists solely to bypass the public default
        ''' struct constructor.
        ''' </summary>
        Public Sub New(unused As Boolean)
            _stack = s_stackPool.Allocate()
        End Sub

        ''' <summary>
        ''' Called when you are done with the TemporaryLookupResults object.  All data it has will
        ''' be returned to a global pool to be used by the next TemporaryLookupResults instance.
        ''' </summary>
        Public Sub Dispose()
            s_stackPool.Free(_stack)
            _stack = Nothing
        End Sub

        ''' <summary>
        ''' Gets a clear <see cref="LookupResult"/> instance to use for binding.  When done
        ''' with it, call <see cref="FreeTempLookupResult(LookupResult)"/>
        ''' </summary>
        Public Function GetTempLookupResult() As LookupResult
            If _stack.Count = 0 Then
                Return New LookupResult()
            End If

            Return _stack.Pop()
        End Function

        Public Sub FreeTempLookupResult(result As LookupResult)
            result.Clear()
            _stack.Push(result)
        End Sub
    End Structure
End Namespace

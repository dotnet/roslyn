' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class LookupSymbolsInfo
        Inherits AbstractLookupSymbolsInfo(Of Symbol)

        ' TODO: tune pool size
        Private Const s_poolSize As Integer = 64
        Private Shared ReadOnly s_pool As New ObjectPool(Of LookupSymbolsInfo)(Function() New LookupSymbolsInfo(), s_poolSize)

        Private Sub New()
            MyBase.New(IdentifierComparison.Comparer)
        End Sub

        ' To implement Poolable, you need two things:
        ' 1) Expose Freeing primitive. 
        Public Sub Free()
            ' Note that poolables are not finalizable.  If one gets collected - no big deal.
            Me.Clear()
            s_pool.Free(Me)
        End Sub

        Public Shared Function GetInstance() As LookupSymbolsInfo
            Dim info As LookupSymbolsInfo = s_pool.Allocate()
            Debug.Assert(info.Count = 0)
            Return info
        End Function
    End Class
End Namespace

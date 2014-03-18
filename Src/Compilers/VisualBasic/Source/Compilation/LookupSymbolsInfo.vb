' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class LookupSymbolsInfo
        Inherits AbstractLookupSymbolsInfo(Of Symbol)

        ' TODO: tune pool size
        Private Shared ReadOnly poolSize As Integer = 64
        Private Shared ReadOnly pool As New ObjectPool(Of LookupSymbolsInfo)(Function() New LookupSymbolsInfo(), poolSize)

        Private Sub New()
            MyBase.New(IdentifierComparison.Comparer)
        End Sub

        ' To implement Poolable, you need two things:
        ' 1) Expose Freeing primitive. 
        Public Sub Free()
            ' Note that poolables are not finalizable.  If one gets collected - no big deal.
            Me.Clear()
            pool.Free(Me)
        End Sub

        Public Shared Function GetInstance() As LookupSymbolsInfo
            Dim info As LookupSymbolsInfo = pool.Allocate()
            Debug.Assert(info.Names.Count = 0)
            Return info
        End Function
    End Class
End Namespace
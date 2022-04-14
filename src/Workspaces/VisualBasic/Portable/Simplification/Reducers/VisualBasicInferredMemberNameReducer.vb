' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    ''' <summary>
    ''' Complexify makes inferred names explicit for tuple elements and anonymous type members. This
    ''' class considers which ones of those can be simplified (after the refactoring was done).
    ''' If the inferred name of the member matches, the explicit name (from Complexifiy) can be removed.
    ''' </summary>
    Partial Friend Class VisualBasicInferredMemberNameReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Public Overrides Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
            Return True
        End Function
    End Class
End Namespace

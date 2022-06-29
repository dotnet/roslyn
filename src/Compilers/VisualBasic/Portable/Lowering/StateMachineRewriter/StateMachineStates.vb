' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class StateMachineStates
        Public Const FirstIteratorFinalizeState As Integer = -3
        Public Const InitialIteratorState As Integer = 0
        Public Const FinishedStateMachine As Integer = -2
        Public Const NotStartedStateMachine As Integer = -1
        Public Const FirstUnusedState As Integer = 0
        Public Const FirstResumableIteratorState As Integer = InitialIteratorState + 1
        Public Const FirstResumableAsyncState As Integer = 0
    End Class

End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend MustInherit Class StateMachineRewriter(Of TProxy)

        Friend Enum StateMachineStates As Integer
            FinishedStateMachine = -2
            NotStartedStateMachine = -1
            FirstUnusedState = 0
        End Enum

    End Class

End Namespace

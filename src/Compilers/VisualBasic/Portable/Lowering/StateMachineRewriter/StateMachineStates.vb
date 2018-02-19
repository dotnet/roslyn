' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module StateMachineStates
        Public FinishedStateMachine As Integer = -2
        Public NotStartedStateMachine As Integer = -1
        Public FirstUnusedState As Integer = 0
    End Module

End Namespace

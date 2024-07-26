' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Allocates resumable states, i.e. states that resume execution of the state machine after await expression or yield return.
    ''' </summary>
    Friend NotInheritable Class ResumableStateMachineStateAllocator
        Private ReadOnly _slotAllocator As VariableSlotAllocator
        Private ReadOnly _increasing As Boolean
        Private ReadOnly _firstState As Integer

        ''' <summary>
        ''' The number of the next generated resumable state (i.e. state that resumes execution of the state machine after await expression or yield return).
        ''' </summary>
        Private _nextState As StateMachineState

#If DEBUG Then
        ''' <summary>
        ''' EnC support: states in this state machine that were matched to states of the previous generation state machine.
        ''' </summary>
        Private _matchedStates As BitVector = BitVector.Empty
#End If
        ''' <summary>
        ''' EnC support: number of states in this state machine that match states of the previous generation state machine.
        ''' </summary>
        Private _matchedStateCount As Integer

        Public Sub New(slotAllocator As VariableSlotAllocator, firstState As StateMachineState, increasing As Boolean)
            _increasing = increasing
            _slotAllocator = slotAllocator
            _matchedStateCount = 0
            _firstState = firstState
            _nextState = If(slotAllocator?.GetFirstUnusedStateMachineState(increasing), firstState)
        End Sub

        Public Function AllocateState(awaitOrYieldReturnSyntax As SyntaxNode) As StateMachineState
            Debug.Assert(SyntaxBindingUtilities.BindsToResumableStateMachineState(awaitOrYieldReturnSyntax))

            Dim direction = If(_increasing, +1, -1)
            Dim state As StateMachineState

            If _slotAllocator?.TryGetPreviousStateMachineState(awaitOrYieldReturnSyntax, awaitId:=Nothing, state) = True Then
#If DEBUG Then
                ' two states of the new state machine should not match the same state of the previous machine
                Debug.Assert(Not _matchedStates(state * direction))
                _matchedStates(state * direction) = True
#End If
                _matchedStateCount += 1
            Else
                state = _nextState
                _nextState = CType(_nextState + direction, StateMachineState)
            End If

            Return state
        End Function

        ''' <summary>
        ''' True if any of the states generated for any previous state machine has not been allocated in this version.
        ''' </summary>
        Public ReadOnly Property HasMissingStates As Boolean
            Get
                Return _matchedStateCount < If(_slotAllocator?.GetFirstUnusedStateMachineState(_increasing), _firstState) - _firstState
            End Get
        End Property

        Public Function GenerateThrowMissingStateDispatch(f As SyntheticBoundNodeFactory, cachedState As BoundExpression, errorCode As HotReloadExceptionCode) As BoundStatement
            Debug.Assert(f.CompilationState IsNot Nothing)
            Debug.Assert(f.CompilationState.ModuleBuilderOpt IsNot Nothing)

            If Not HasMissingStates Then
                Return Nothing
            End If

            Return f.If(
                f.Binary(
                    If(_increasing, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual),
                    f.SpecialType(SpecialType.System_Boolean),
                    cachedState,
                    f.Literal(_firstState)),
                f.Throw(
                    f.[New](
                        DirectCast(f.CompilationState.ModuleBuilderOpt.GetOrCreateHotReloadExceptionConstructorDefinition(), MethodSymbol),
                        f.StringLiteral(ConstantValue.Create(errorCode.GetExceptionMessage())),
                        f.Literal(errorCode.GetExceptionCodeValue()))))
        End Function
    End Class
End Namespace

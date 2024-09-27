// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Allocates resumable states, i.e. states that resume execution of the state machine after await expression or yield return.
    /// </summary>
    internal sealed class ResumableStateMachineStateAllocator
    {
        private readonly VariableSlotAllocator? _slotAllocator;
        private readonly bool _increasing;
        private readonly StateMachineState _firstState;

        /// <summary>
        /// The number of the next generated resumable state (i.e. state that resumes execution of the state machine after await expression or yield return).
        /// </summary>
        private StateMachineState _nextState;

#if DEBUG
        /// <summary>
        /// EnC support: states in this state machine that were matched to states of the previous generation state machine.
        /// </summary>
        private BitVector _matchedStates = BitVector.Empty;
#endif
        /// <summary>
        /// EnC support: number of states in this state machine that match states of the previous generation state machine.
        /// </summary>
        private int _matchedStateCount;

        public ResumableStateMachineStateAllocator(VariableSlotAllocator? slotAllocator, StateMachineState firstState, bool increasing)
        {
            _increasing = increasing;
            _slotAllocator = slotAllocator;
            _matchedStateCount = 0;
            _firstState = firstState;
            _nextState = slotAllocator?.GetFirstUnusedStateMachineState(increasing) ?? firstState;
        }

        public StateMachineState AllocateState(SyntaxNode awaitOrYieldReturnSyntax, AwaitDebugId awaitId)
        {
            Debug.Assert(SyntaxBindingUtilities.BindsToResumableStateMachineState(awaitOrYieldReturnSyntax));

            int direction = _increasing ? +1 : -1;

            if (_slotAllocator?.TryGetPreviousStateMachineState(awaitOrYieldReturnSyntax, awaitId, out var state) == true)
            {
#if DEBUG
                // two states of the new state machine should not match the same state of the previous machine:
                Debug.Assert(!_matchedStates[(int)state * direction]);
                _matchedStates[(int)state * direction] = true;
#endif
                _matchedStateCount++;
            }
            else
            {
                state = _nextState;
                _nextState += direction;
            }

            return state;
        }

        /// <summary>
        /// True if any of the states generated for any previous state machine has not been allocated in this version.
        /// </summary>
        public bool HasMissingStates
            => _matchedStateCount < Math.Abs((_slotAllocator?.GetFirstUnusedStateMachineState(_increasing) ?? _firstState) - _firstState);

        public BoundStatement? GenerateThrowMissingStateDispatch(SyntheticBoundNodeFactory f, BoundExpression cachedState, HotReloadExceptionCode errorCode)
        {
            Debug.Assert(f.ModuleBuilderOpt != null);

            if (!HasMissingStates)
            {
                return null;
            }

            return f.If(
                f.Binary(
                    _increasing ? BinaryOperatorKind.IntGreaterThanOrEqual : BinaryOperatorKind.IntLessThanOrEqual,
                    f.SpecialType(SpecialType.System_Boolean),
                    cachedState,
                    f.Literal(_firstState)),
                f.Throw(
                    f.New(
                        (MethodSymbol)f.ModuleBuilderOpt.GetOrCreateHotReloadExceptionConstructorDefinition(),
                        f.StringLiteral(ConstantValue.Create(errorCode.GetExceptionMessage())),
                        f.Literal(errorCode.GetExceptionCodeValue()))));
        }
    }
}

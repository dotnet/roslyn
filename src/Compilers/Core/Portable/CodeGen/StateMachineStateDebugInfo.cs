// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CodeGen;

internal readonly struct StateMachineStateDebugInfo(int syntaxOffset, AwaitDebugId awaitId, StateMachineState stateNumber)
{
    public readonly int SyntaxOffset = syntaxOffset;
    public readonly AwaitDebugId AwaitId = awaitId;
    public readonly StateMachineState StateNumber = stateNumber;
}

/// <summary>
/// Debug information maintained for each state machine.
/// Facilitates mapping of state machine states from a compilation to the previous one (or to a metadata baseline) during EnC.
/// </summary>
internal readonly struct StateMachineStatesDebugInfo
{
    public readonly ImmutableArray<StateMachineStateDebugInfo> States;

    /// <summary>
    /// The number of the first state that has not been used in any of the previous versions of the state machine,
    /// or null if we are not generating EnC delta.
    /// 
    /// For 1st generation EnC delta, this is calculated by examining the <see cref="EditAndContinueMethodDebugInformation.StateMachineStates"/> stored in the baseline metadata.
    /// For subsequent generations, the number is updated to account for newly generated states in that generation.
    /// </summary>
    public readonly StateMachineState? FirstUnusedIncreasingStateMachineState;

    /// <summary>
    /// The number of the first state that has not been used in any of the previous versions of the state machine,
    /// or null if we are not generating EnC delta, or the state machine has no decreasing states.
    /// 
    /// For 1st generation EnC delta, this is calculated by examining the <see cref="EditAndContinueMethodDebugInformation.StateMachineStates"/> stored in the baseline metadata.
    /// For subsequent generations, the number is updated to account for newly generated states in that generation.
    /// </summary>
    public readonly StateMachineState? FirstUnusedDecreasingStateMachineState;

    private StateMachineStatesDebugInfo(ImmutableArray<StateMachineStateDebugInfo> states, StateMachineState? firstUnusedIncreasingStateMachineState, StateMachineState? firstUnusedDecreasingStateMachineState)
    {
        States = states;
        FirstUnusedIncreasingStateMachineState = firstUnusedIncreasingStateMachineState;
        FirstUnusedDecreasingStateMachineState = firstUnusedDecreasingStateMachineState;
    }

    public static StateMachineStatesDebugInfo Create(VariableSlotAllocator? variableSlotAllocator, ImmutableArray<StateMachineStateDebugInfo> stateInfos)
    {
        StateMachineState? firstUnusedIncreasingStateMachineState = null, firstUnusedDecreasingStateMachineState = null;

        if (variableSlotAllocator != null)
        {
            // We start with first unused state numbers from the previous generation and update them based on states generated in the current one.
            firstUnusedIncreasingStateMachineState = variableSlotAllocator.GetFirstUnusedStateMachineState(increasing: true);
            firstUnusedDecreasingStateMachineState = variableSlotAllocator.GetFirstUnusedStateMachineState(increasing: false);

            if (!stateInfos.IsDefaultOrEmpty)
            {
                // The current method is a state machine and has some resumable/finalization states.
                // Update the first unused states based on the highest resumable (positive numbers) and lowest finalization (negative numbers) of this method.

                var maxState = stateInfos.Max(info => info.StateNumber) + 1;
                var minState = stateInfos.Min(info => info.StateNumber) - 1;

                firstUnusedIncreasingStateMachineState = (firstUnusedIncreasingStateMachineState != null) ? (StateMachineState)Math.Max((int)firstUnusedIncreasingStateMachineState.Value, (int)maxState) : maxState;

                if (minState < 0)
                {
                    firstUnusedDecreasingStateMachineState = (firstUnusedDecreasingStateMachineState != null) ? (StateMachineState)Math.Min((int)firstUnusedDecreasingStateMachineState.Value, (int)minState) : minState;
                }
            }
        }

        return new StateMachineStatesDebugInfo(
            stateInfos,
            firstUnusedIncreasingStateMachineState,
            firstUnusedDecreasingStateMachineState);
    }
}

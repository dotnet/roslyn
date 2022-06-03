// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeGen;

internal readonly struct StateMachineStateDebugInfo
{
    public readonly int SyntaxOffset;
    public readonly int StateNumber;

    public StateMachineStateDebugInfo(int syntaxOffset, int stateNumber)
    {
        SyntaxOffset = syntaxOffset;
        StateNumber = stateNumber;
    }
}

internal readonly struct StateMachineStatesDebugInfo
{
    public readonly ImmutableArray<StateMachineStateDebugInfo> States;
    public readonly int? FirstUnusedIncreasingStateMachineState;
    public readonly int? FirstUnusedDecreasingStateMachineState;

    public StateMachineStatesDebugInfo(ImmutableArray<StateMachineStateDebugInfo> states, int? firstUnusedIncreasingStateMachineState, int? firstUnusedDecreasingStateMachineState)
    {
        States = states;
        FirstUnusedIncreasingStateMachineState = firstUnusedIncreasingStateMachineState;
        FirstUnusedDecreasingStateMachineState = firstUnusedDecreasingStateMachineState;
    }

    public static StateMachineStatesDebugInfo Create(VariableSlotAllocator? variableSlotAllocator, ImmutableArray<StateMachineStateDebugInfo> stateInfos)
    {
        int? firstUnusedIncreasingStateMachineState = null, firstUnusedDecreasingStateMachineState = null;

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

                firstUnusedIncreasingStateMachineState = (firstUnusedIncreasingStateMachineState != null) ? Math.Max(firstUnusedIncreasingStateMachineState.Value, maxState) : maxState;

                if (minState < 0)
                {
                    firstUnusedDecreasingStateMachineState = (firstUnusedDecreasingStateMachineState != null) ? Math.Min(firstUnusedDecreasingStateMachineState.Value, minState) : minState;
                }
            }
        }

        return new StateMachineStatesDebugInfo(
            stateInfos,
            firstUnusedIncreasingStateMachineState,
            firstUnusedDecreasingStateMachineState);
    }
}

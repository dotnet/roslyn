// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal enum StateMachineState
{
    /// <summary>
    /// First state of an async iterator state machine that is used to resume the machine after yield return.
    /// Initial state is not used to resume state machine that yielded. State numbers decrease as the iterator makes progress.
    /// </summary>
    FirstResumableAsyncIteratorState = InitialAsyncIteratorState - 1,

    /// <summary>
    /// Initial iterator state of an async iterator.
    /// Distinct from <see cref="NotStartedOrRunningState"/> so that DisposeAsync can throw in latter case.
    /// </summary>
    InitialAsyncIteratorState = -4,

    /// <summary>
    /// First finalize state of an iterator state machine. State numbers decrease for subsequent finalize states.
    /// </summary>
    FirstIteratorFinalizeState = -4,

    FirstAsyncFinalizeState = -3,

    /// <summary>
    /// Initial state for enumerables, async-enumerables state machines.
    /// The state machine for iterators and async-iterators can never re-enter this state.
    /// </summary>
    InitialEnumerableState = -2,

    NotStartedOrRunningState = -1, // TODO2 rename to RunningState
    FirstUnusedState = 0,

    /// <summary>
    /// Finished state for iterators and async-iterators
    /// </summary>
    IteratorFinishedState = -3,

    /// <summary>
    /// Finished state for async
    /// </summary>
    AsyncFinishedState = -2,

    /// <summary>
    /// First state in async state machine that is used to resume the machine after await.
    /// State numbers increase as the async computation makes progress.
    /// </summary>
    FirstResumableAsyncState = 0,

    /// <summary>
    /// Initial iterator state of a (non-async) iterator.
    /// </summary>
    InitialIteratorState = 0,

    /// <summary>
    /// First state in iterator state machine that is used to resume the machine after yield return.
    /// Initial state is not used to resume state machine that yielded. State numbers increase as the iterator makes progress.
    /// </summary>
    FirstResumableIteratorState = InitialIteratorState + 1,
}

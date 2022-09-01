// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class StateMachineStates
    {
        internal const int FirstIteratorFinalizeState = -3;

        internal const int FinishedState = -2;
        internal const int NotStartedOrRunningState = -1;
        internal const int FirstUnusedState = 0;

        /// <summary>
        /// First state in async state machine that is used to resume the machine after await.
        /// </summary>
        internal const int FirstResumableAsyncState = 0;

        internal const int InitialIteratorState = 0;

        /// <summary>
        /// First state in iterator state machine that is used to resume the machine after yield return.
        /// Initial state is not used to resume state machine that yielded.
        /// </summary>
        internal const int FirstResumableIteratorState = InitialIteratorState + 1;

        /// <summary>
        /// Used for async-iterators to distinguish initial state from <see cref="NotStartedOrRunningState"/> so that DisposeAsync can throw in latter case.
        /// </summary>
        internal const int InitialAsyncIteratorState = -3;

        /// <summary>
        /// First state in async iterator state machine that is used to resume the machine after yield return.
        /// Initial state is not used to resume state machine that yielded.
        /// </summary>
        internal const int FirstResumableAsyncIteratorState = InitialAsyncIteratorState - 1;
    }
}

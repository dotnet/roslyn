// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class StateMachineStates
    {
        internal const int FinishedStateMachine = -2;
        internal const int NotStartedStateMachine = -1;
        internal const int FirstUnusedState = 0;

        // used for async-iterators to distinguish initial state from running state (-1) so that DisposeAsync can throw in latter case
        internal const int InitialAsyncIteratorStateMachine = -3;
    }
}

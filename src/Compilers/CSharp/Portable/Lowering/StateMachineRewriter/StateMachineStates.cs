// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

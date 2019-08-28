// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct DebuggingStateChangedEventArgs
    {
        public readonly DebuggingState Before;
        public readonly DebuggingState After;

        public DebuggingStateChangedEventArgs(DebuggingState before, DebuggingState after)
        {
            Debug.Assert(before != after);

            Before = before;
            After = after;
        }
    }
}

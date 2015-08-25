// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DebuggingStateChangedEventArgs
    {
        public DebuggingState Before { get; }
        public DebuggingState After { get; }

        public DebuggingStateChangedEventArgs(DebuggingState before, DebuggingState after)
        {
            Debug.Assert(before != after);

            this.Before = before;
            this.After = after;
        }
    }
}

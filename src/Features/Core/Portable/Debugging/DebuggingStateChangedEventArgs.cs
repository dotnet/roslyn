// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal sealed class DebuggingWorkspaceService : IDebuggingWorkspaceService
    {
        public event EventHandler<DebuggingStateChangedEventArgs> BeforeDebuggingStateChanged;

        public void OnBeforeDebuggingStateChanged(DebuggingState before, DebuggingState after)
            => BeforeDebuggingStateChanged?.Invoke(this, new DebuggingStateChangedEventArgs(before, after));
    }
}

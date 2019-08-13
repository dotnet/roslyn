// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal sealed class DebuggingWorkspaceService : IDebuggingWorkspaceService
    {
        public event EventHandler<DebuggingStateChangedEventArgs> BeforeDebuggingStateChanged;

        public IEditAndContinueService EditAndContinueServiceOpt { get; }

        internal DebuggingWorkspaceService(IEditAndContinueService editAndContinueServiceOpt)
        {
            EditAndContinueServiceOpt = editAndContinueServiceOpt;
        }

        public void OnBeforeDebuggingStateChanged(DebuggingState before, DebuggingState after)
            => BeforeDebuggingStateChanged?.Invoke(this, new DebuggingStateChangedEventArgs(before, after));
    }
}

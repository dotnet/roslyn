﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal interface IDebuggingWorkspaceService : IWorkspaceService
    {
        event EventHandler<DebuggingStateChangedEventArgs> BeforeDebuggingStateChanged;

        DebuggingState CurrentDebuggingState { get; }

        void OnBeforeDebuggingStateChanged(DebuggingState before, DebuggingState after);
    }
}

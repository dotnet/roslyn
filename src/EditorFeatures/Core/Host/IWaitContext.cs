// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    [Obsolete("You should now use UIThreadOperationStatus, which is a platform supported version of this.")]
    internal enum WaitIndicatorResult
    {
        Completed,
        Canceled,
    }

    [Obsolete("You should now use IUIThreadOperationContext, which is a platform supported version of this.")]
    internal interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }

        bool AllowCancel { get; set; }
        string Message { get; set; }

        IProgressTracker ProgressTracker { get; }
    }
}

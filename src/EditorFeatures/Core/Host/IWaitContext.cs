// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal enum WaitIndicatorResult
    {
        Completed,
        Canceled,
    }

    internal interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }

        bool AllowCancel { get; set; }
        string Message { get; set; }

        void UpdateProgress();
    }
}

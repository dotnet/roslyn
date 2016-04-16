// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    internal abstract class EditorAdornmentWaiter : AsynchronousOperationListener
    {
        public override Task CreateWaitTask()
        {
            var task = base.CreateWaitTask();
            return task.SafeContinueWith(_ =>
                {
                    Action a = () => { };
                    Dispatcher.CurrentDispatcher.Invoke(a, DispatcherPriority.ApplicationIdle);
                },
                CancellationToken.None,
                TaskScheduler.Default);
        }
    }
}
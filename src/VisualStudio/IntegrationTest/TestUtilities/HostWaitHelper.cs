// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public static class HostWaitHelper
    {
        public static void WaitForDispatchedOperationsToComplete(DispatcherPriority priority)
        {
            Action action = delegate { };
            new FrameworkElement().Dispatcher.Invoke(action, priority);
        }

        public static void PumpingWait(Task task)
        {
            PumpingWaitAll(new[] { task });
        }

        public static T PumpingWaitResult<T>(Task<T> task)
        {
            PumpingWait(task);
            return task.Result;
        }

        public static void PumpingWaitAll(IEnumerable<Task> tasks)
        {
            var smallTimeout = TimeSpan.FromMilliseconds(10);
            var taskArray = tasks.ToArray();
            var done = false;
            while (!done)
            {
                done = Task.WaitAll(taskArray, smallTimeout);
                if (!done)
                {
                    WaitForDispatchedOperationsToComplete(DispatcherPriority.ApplicationIdle);
                }
            }

            foreach (var task in tasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }
        }
    }
}
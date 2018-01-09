// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    /// <summary>
    /// Return <see cref="IAsynchronousOperationListener"/> for the given featureName
    /// 
    /// We have this abstraction so that we can have isolated listener/waiter in unit tests
    /// </summary>
    internal interface IAsynchronousOperationListenerProvider
    {
        IAsynchronousOperationListener GetListener(string featureName);
    }

    [Export(typeof(IAsynchronousOperationListenerProvider)), Shared]
    internal class AsynchronousOperationListenerProvider : IAsynchronousOperationListenerProvider
    {
        /// <summary>
        /// indicate whether asynchronous listener is enabled or not
        /// </summary>
        public static bool s_enabled = false;

        public static readonly IAsynchronousOperationListenerProvider NullProvider = new NullListenerProvider();
        public static readonly IAsynchronousOperationListener NullListener = new NullOperationListener();

        private readonly ConcurrentDictionary<string, IAsynchronousOperationListener> _listeners =
            new ConcurrentDictionary<string, IAsynchronousOperationListener>(concurrencyLevel: 2, capacity: 20);

        private bool _trackingBehavior;

        public static void Enable(bool enable)
        {
            s_enabled = enable;
        }

        public void Tracking(bool enable)
        {
            _trackingBehavior = enable;

            _listeners.Values.Cast<AsynchronousOperationListener>().Do(l => l.TrackActiveTokens = enable);
        }

        public IAsynchronousOperationListener GetListener(string featureName)
        {
            if (!s_enabled)
            {
                // if listener is not enabled. it already return null listener
                return NullListener;
            }

            if (_listeners.TryGetValue(featureName, out var listener))
            {
                return listener;
            }

            return _listeners.GetOrAdd(featureName, name => new AsynchronousOperationListener(name, _trackingBehavior));
        }

        /// <summary>
        /// Wait for all of the <see cref="IAsynchronousOperationWaiter"/> instances to finish their
        /// work.
        /// </summary>
        /// <remarks>
        /// This is a very handy method for debugging hangs in the unit test.  Set a break point in the 
        /// loop, dig into the waiters and see all of the active <see cref="IAsyncToken"/> values 
        /// representing the remaining work.
        /// </remarks>
        public void WaitAll()
        {
            var smallTimeout = TimeSpan.FromMilliseconds(10);

            Task[] tasks = null;
            while (true)
            {
                var waiters = _listeners.Values.Cast<IAsynchronousOperationWaiter>();
                tasks = waiters.Select(x => x.CreateWaitTask()).Where(t => !t.IsCompleted).ToArray();

                if (tasks.Length == 0)
                {
                    // no new pending tasks
                    break;
                }

                do
                {
                    // wait for all current tasks to be done for the time given
                    if (Task.WaitAll(tasks, smallTimeout))
                    {
                        // current set of tasks are done.
                        // see whether there are new tasks added while we were waiting
                        break;
                    }
                } while (true);
            }

            foreach (var task in tasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }
        }

        public List<AsynchronousOperationListener.DiagnosticAsyncToken> GetTokens()
        {
            return _listeners.Values.Cast<AsynchronousOperationListener>().Where(l => l.TrackActiveTokens).SelectMany(l => l.ActiveDiagnosticTokens).ToList();
        }

        private class NullOperationListener : IAsynchronousOperationListener
        {
            public IAsyncToken BeginAsyncOperation(
                string name,
                object tag = null,
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineNumber = 0) => EmptyAsyncToken.Instance;
        }

        private class NullListenerProvider : IAsynchronousOperationListenerProvider
        {
            public IAsynchronousOperationListener GetListener(string featureName) => NullListener;
        }
    }
}

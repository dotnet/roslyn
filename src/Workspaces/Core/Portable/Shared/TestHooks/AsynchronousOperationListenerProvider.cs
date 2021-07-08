// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    /// <summary>
    /// use <see cref="IAsynchronousOperationListenerProvider" /> in product code to get
    /// <see cref="IAsynchronousOperationListener" /> and use
    /// <see cref="AsynchronousOperationListenerProvider" /> in test to get waiter.
    /// </summary>
    [Shared]
    [Export(typeof(IAsynchronousOperationListenerProvider))]
    [Export(typeof(AsynchronousOperationListenerProvider))]
    internal sealed partial class AsynchronousOperationListenerProvider : IAsynchronousOperationListenerProvider
    {
        public static readonly IAsynchronousOperationListenerProvider NullProvider = new NullListenerProvider();
        public static readonly IAsynchronousOperationListener NullListener = new NullOperationListener();

        /// <summary>
        /// indicate whether asynchronous listener is enabled or not.
        /// it is tri-state since we want to retrieve this value, if never explicitly set, from environment variable
        /// and then cache it.
        /// we read value from environment variable (RoslynWaiterEnabled) because we want team, that doesn't have
        /// access to Roslyn code (InternalVisibleTo), can use this listener/waiter framework as well. 
        /// those team can enable this without using <see cref="AsynchronousOperationListenerProvider.Enable(bool)" /> API
        /// </summary>
        public static bool? s_enabled = null;

        private readonly ConcurrentDictionary<string, AsynchronousOperationListener> _singletonListeners;
        private readonly Func<string, AsynchronousOperationListener> _createCallback;

        /// <summary>
        /// indicate whether <see cref="AsynchronousOperationListener.TrackActiveTokens"/> is enabled or not
        /// it is tri-state since we want to retrieve this value, if never explicitly set, from environment variable
        /// and then cache it.
        /// we read value from environment variable (RoslynWaiterDiagnosticTokenEnabled) because we want team, that doesn't have
        /// access to Roslyn code (InternalVisibleTo), can use this listener/waiter framework as well. 
        /// those team can enable this without using <see cref="AsynchronousOperationListenerProvider.EnableDiagnosticTokens(bool)" /> API
        /// </summary>
        private bool? _enableDiagnosticTokens;

        /// <summary>
        /// Provides a default value for <see cref="_enableDiagnosticTokens"/>.
        /// </summary>
        private static bool? s_enableDiagnosticTokens;

        public static void Enable(bool enable)
            => Enable(enable, diagnostics: null);

        public static void Enable(bool enable, bool? diagnostics)
        {
            // right now, made it static so that one can enable it through reflection easy
            // but we can think of some other way
            s_enabled = enable;
            s_enableDiagnosticTokens = diagnostics;
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AsynchronousOperationListenerProvider()
        {
            _singletonListeners = new ConcurrentDictionary<string, AsynchronousOperationListener>(concurrencyLevel: 2, capacity: 20);
            _createCallback = name => new AsynchronousOperationListener(name, DiagnosticTokensEnabled);
        }

        public IAsynchronousOperationListener GetListener(string featureName)
        {
            if (!IsEnabled)
            {
                // if listener is not enabled. it always return null listener
                return NullListener;
            }

            return _singletonListeners.GetOrAdd(featureName, _createCallback);
        }

        /// <summary>
        /// Enable or disable TrackActiveTokens for test
        /// </summary>
        public void EnableDiagnosticTokens(bool enable)
        {
            _enableDiagnosticTokens = enable;
            _singletonListeners.Values.Do(l => l.TrackActiveTokens = enable);
        }

        /// <summary>
        /// Get Waiters for listeners for test
        /// </summary>
        public IAsynchronousOperationWaiter GetWaiter(string featureName)
            => (IAsynchronousOperationWaiter)GetListener(featureName);

        /// <summary>
        /// Wait for all of the <see cref="IAsynchronousOperationWaiter"/> instances to finish their
        /// work.
        /// </summary>
        /// <remarks>
        /// This is a very handy method for debugging hangs in the unit test.  Set a break point in the 
        /// loop, dig into the waiters and see all of the active <see cref="IAsyncToken"/> values 
        /// representing the remaining work.
        /// </remarks>
        public async Task WaitAllAsync(Workspace? workspace, string[]? featureNames = null, Action? eventProcessingAction = null, TimeSpan? timeout = null)
        {
            var startTime = Stopwatch.StartNew();
            var smallTimeout = TimeSpan.FromMilliseconds(10);

            RemoteHostClient? remoteHostClient = null;
            if (workspace?.Services.GetService<IRemoteHostClientProvider>() is { } remoteHostClientProvider)
            {
                remoteHostClient = await remoteHostClientProvider.TryGetRemoteHostClientAsync(CancellationToken.None).ConfigureAwait(false);
            }

            List<Task>? tasks = null;
            while (true)
            {
                var waiters = GetCandidateWaiters(featureNames);
                tasks = waiters.Select(x => x.ExpeditedWaitAsync()).Where(t => !t.IsCompleted).ToList();

                if (remoteHostClient is not null)
                {
                    var isCompleted = await remoteHostClient.TryInvokeAsync<IRemoteAsynchronousOperationListenerService, bool>(
                        (service, cancellationToken) => service.IsCompletedAsync(featureNames.ToImmutableArrayOrEmpty(), cancellationToken),
                        CancellationToken.None).ConfigureAwait(false);
                    if (isCompleted.HasValue && !isCompleted.Value)
                    {
                        tasks.Add(remoteHostClient.TryInvokeAsync<IRemoteAsynchronousOperationListenerService>(
                            (service, cancellationToken) => service.ExpeditedWaitAsync(featureNames.ToImmutableArrayOrEmpty(), cancellationToken),
                            CancellationToken.None).AsTask());
                    }
                }

                if (tasks.Count == 0)
                {
                    // no new pending tasks
                    break;
                }

                do
                {
                    // wait for all current tasks to be done for the time given
                    if (Task.WaitAll(tasks.ToArray(), smallTimeout))
                    {
                        // current set of tasks are done.
                        // see whether there are new tasks added while we were waiting
                        break;
                    }

                    // certain test requires some event queues to be processed
                    // for waiter tasks to finish such as Dispatcher queue
                    eventProcessingAction?.Invoke();

                    // in unit test where it uses fake foreground task scheduler such as StaTaskScheduler
                    // we need to yield for the scheduler to run inlined tasks. If we are not processing events, we
                    // switch to the thread pool for the continuations since the yield will only let operations at the
                    // same or higher priority to execute prior to the continuation.
                    var continueOnCapturedContext = eventProcessingAction is object;
                    await Task.Delay(smallTimeout).ConfigureAwait(continueOnCapturedContext);

                    if (startTime.Elapsed > timeout && timeout != Timeout.InfiniteTimeSpan)
                    {
                        throw new TimeoutException();
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

        public bool HasPendingWaiter(params string[] featureNames)
        {
            var waiters = GetCandidateWaiters(featureNames);
            return waiters.Any(w => w.HasPendingWork);
        }

        /// <summary>
        /// Get all saved DiagnosticAsyncToken to investigate tests failure easier
        /// </summary>
        public List<AsynchronousOperationListener.DiagnosticAsyncToken> GetTokens()
            => _singletonListeners.Values.Where(l => l.TrackActiveTokens).SelectMany(l => l.ActiveDiagnosticTokens).ToList();

        internal static bool IsEnabled
        {
            get
            {
                if (!s_enabled.HasValue)
                {
                    // if s_enabled has never been set, check environment variable to see whether it should be enabled.
                    var enabled = Environment.GetEnvironmentVariable("RoslynWaiterEnabled");
                    s_enabled = string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(enabled, "True", StringComparison.OrdinalIgnoreCase);
                }

                return s_enabled.Value;
            }
        }

        internal bool DiagnosticTokensEnabled
        {
            get
            {
                if (!_enableDiagnosticTokens.HasValue)
                {
                    if (s_enableDiagnosticTokens.HasValue)
                    {
                        _enableDiagnosticTokens = s_enableDiagnosticTokens;
                    }
                    else
                    {
                        // if _enableDiagnosticTokens has never been set, check environment variable to see whether it should be enabled.
                        var enabled = Environment.GetEnvironmentVariable("RoslynWaiterDiagnosticTokenEnabled");
                        _enableDiagnosticTokens = string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(enabled, "True", StringComparison.OrdinalIgnoreCase);
                    }
                }

                return _enableDiagnosticTokens.Value;
            }
        }

        private IEnumerable<IAsynchronousOperationWaiter> GetCandidateWaiters(string[]? featureNames)
        {
            if (featureNames == null || featureNames.Length == 0)
            {
                return _singletonListeners.Values.Cast<IAsynchronousOperationWaiter>();
            }

            return _singletonListeners.Where(kv => featureNames.Contains(kv.Key)).Select(kv => (IAsynchronousOperationWaiter)kv.Value);
        }
    }
}

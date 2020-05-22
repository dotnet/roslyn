// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal static class YieldAwaitableExtensions
    {
        public static ConfiguredYieldAwaitable ConfigureAwait(this YieldAwaitable awaitable, bool continueOnCapturedContext)
        {
            return new ConfiguredYieldAwaitable(awaitable, continueOnCapturedContext);
        }
    }

    internal readonly struct ConfiguredYieldAwaitable
    {
        private readonly YieldAwaitable _awaitable;
        private readonly bool _continueOnCapturedContext;

        public ConfiguredYieldAwaitable(YieldAwaitable awaitable, bool continueOnCapturedContext)
        {
            _awaitable = awaitable;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        public ConfiguredYieldAwaiter GetAwaiter()
            => new ConfiguredYieldAwaiter(_awaitable.GetAwaiter(), _continueOnCapturedContext);

        public readonly struct ConfiguredYieldAwaiter
            : INotifyCompletion, ICriticalNotifyCompletion
        {
            private static readonly WaitCallback s_runContinuation =
                continuation => ((Action)continuation)();

            private readonly YieldAwaitable.YieldAwaiter _awaiter;
            private readonly bool _continueOnCapturedContext;

            public ConfiguredYieldAwaiter(YieldAwaitable.YieldAwaiter awaiter, bool continueOnCapturedContext)
            {
                _awaiter = awaiter;
                _continueOnCapturedContext = continueOnCapturedContext;
            }

            public bool IsCompleted => _awaiter.IsCompleted;

            public void GetResult()
                => _awaiter.GetResult();

            public void OnCompleted(Action continuation)
            {
                if (_continueOnCapturedContext)
                {
                    _awaiter.OnCompleted(continuation);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(s_runContinuation, continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (_continueOnCapturedContext)
                {
                    _awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItem(s_runContinuation, continuation);
                }
            }
        }
    }
}

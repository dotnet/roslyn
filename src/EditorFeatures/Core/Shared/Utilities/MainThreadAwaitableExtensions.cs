// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class JoinableTaskFactoryExtensions
    {
        // Provides 'alwaysYield' support prior to https://github.com/Microsoft/vs-threading/issues/326
        public static ConfiguredMainThreadAwaitable SwitchToMainThreadAsync(this JoinableTaskFactory joinableTaskFactory, bool alwaysYield, CancellationToken cancellationToken = default)
        {
#pragma warning disable VSTHRD004 // Await SwitchToMainThreadAsync
            return new ConfiguredMainThreadAwaitable(joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken), alwaysYield);
#pragma warning restore VSTHRD004 // Await SwitchToMainThreadAsync
        }

        internal readonly struct ConfiguredMainThreadAwaitable
        {
            private readonly JoinableTaskFactory.MainThreadAwaitable _mainThreadAwaitable;
            private readonly bool _alwaysYield;

            public ConfiguredMainThreadAwaitable(JoinableTaskFactory.MainThreadAwaitable mainThreadAwaitable, bool alwaysYield)
            {
                _mainThreadAwaitable = mainThreadAwaitable;
                _alwaysYield = alwaysYield;
            }

            public ConfiguredMainThreadAwaiter GetAwaiter()
            {
                return new ConfiguredMainThreadAwaiter(_mainThreadAwaitable.GetAwaiter(), _alwaysYield);
            }
        }

        internal readonly struct ConfiguredMainThreadAwaiter : ICriticalNotifyCompletion
        {
            private readonly JoinableTaskFactory.MainThreadAwaiter _mainThreadAwaiter;
            private readonly bool _alwaysYield;

            public ConfiguredMainThreadAwaiter(JoinableTaskFactory.MainThreadAwaiter mainThreadAwaiter, bool alwaysYield)
            {
                _mainThreadAwaiter = mainThreadAwaiter;
                _alwaysYield = alwaysYield;
            }

            public bool IsCompleted
            {
                get
                {
                    if (_alwaysYield)
                    {
                        return false;
                    }

                    return _mainThreadAwaiter.IsCompleted;
                }
            }

            public void OnCompleted(Action continuation)
                => _mainThreadAwaiter.OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation)
            {
                // This should be simplified to a simple call to UnsafeOnCompleted when vs-threading is updated to a
                // version where MainThreadAwaiter implements ICriticalNotifyCompletion.
                if ((object)_mainThreadAwaiter is ICriticalNotifyCompletion criticalNotifyCompletion)
                {
                    criticalNotifyCompletion.UnsafeOnCompleted(continuation);
                }
                else
                {
                    _mainThreadAwaiter.OnCompleted(continuation);
                }
            }

            public void GetResult()
                => _mainThreadAwaiter.GetResult();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

internal sealed class AsyncEventMap
{
    private readonly SemaphoreSlim _guard = new(initialCount: 1);
    private readonly Dictionary<string, object> _eventNameToHandlerSet = [];

    public void AddAsyncEventHandler<TEventArgs>(string eventName, Func<TEventArgs, Task> asyncHandler)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            _eventNameToHandlerSet[eventName] = _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? ((AsyncEventHandlerSet<TEventArgs>)handlers).AddHandler(asyncHandler)
                : new AsyncEventHandlerSet<TEventArgs>([asyncHandler]);
        }
    }

    public void RemoveAsyncEventHandler<TEventArgs>(string eventName, Func<TEventArgs, Task> asyncHandler)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            if (_eventNameToHandlerSet.TryGetValue(eventName, out var handlers))
                _eventNameToHandlerSet[eventName] = ((AsyncEventHandlerSet<TEventArgs>)handlers).RemoveHandler(asyncHandler);
        }
    }

    public AsyncEventHandlerSet<TEventArgs> GetEventHandlerSet<TEventArgs>(string eventName)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            return _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? (AsyncEventHandlerSet<TEventArgs>)handlers
                : AsyncEventHandlerSet<TEventArgs>.Empty;
        }
    }

    public sealed class AsyncEventHandlerSet<TEventArgs>(ImmutableArray<Func<TEventArgs, Task>> asyncHandlers)
        where TEventArgs : EventArgs
    {
        private readonly ImmutableArray<Func<TEventArgs, Task>> _asyncHandlers = asyncHandlers;
        public static readonly AsyncEventHandlerSet<TEventArgs> Empty = new([]);

        public AsyncEventHandlerSet<TEventArgs> AddHandler(Func<TEventArgs, Task> asyncHandler)
            => new(_asyncHandlers.Add(asyncHandler));

        public AsyncEventHandlerSet<TEventArgs> RemoveHandler(Func<TEventArgs, Task> asyncHandler)
        {
            var newAsyncHandlers = _asyncHandlers.RemoveAll(r => r.Equals(asyncHandler));

            return newAsyncHandlers.IsEmpty ? Empty : new(newAsyncHandlers);
        }

        public bool HasHandlers => !_asyncHandlers.IsEmpty;

        public async Task RaiseEventAsync(TEventArgs arg)
        {
            foreach (var asyncHandler in _asyncHandlers)
                await asyncHandler(arg).ConfigureAwait(false);
        }
    }
}

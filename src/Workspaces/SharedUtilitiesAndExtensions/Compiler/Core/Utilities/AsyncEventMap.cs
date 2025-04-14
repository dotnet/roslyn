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

    public void AddHandler<TEventArgs>(string eventName, Action<TEventArgs> handler, bool requiresMainThread)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            _eventNameToHandlerSet[eventName] = _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? ((AsyncEventHandlerSet<TEventArgs>)handlers).AddHandler(handler, requiresMainThread)
                : new AsyncEventHandlerSet<TEventArgs>([(handler, requiresMainThread)]);
        }
    }

    public void RemoveHandler<TEventArgs>(string eventName, Action<TEventArgs> handler, bool requiresMainThread)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            if (_eventNameToHandlerSet.TryGetValue(eventName, out var handlers))
                _eventNameToHandlerSet[eventName] = ((AsyncEventHandlerSet<TEventArgs>)handlers).RemoveHandler(handler, requiresMainThread);
        }
    }

    public AsyncEventHandlerSet<TEventArgs> GetHandlerSet<TEventArgs>(string eventName)
        where TEventArgs : EventArgs
    {
        using (_guard.DisposableWait())
        {
            return _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? (AsyncEventHandlerSet<TEventArgs>)handlers
                : AsyncEventHandlerSet<TEventArgs>.Empty;
        }
    }

    public sealed class AsyncEventHandlerSet<TEventArgs>(ImmutableArray<(Action<TEventArgs>, bool)> handlers)
        where TEventArgs : EventArgs
    {
        private readonly ImmutableArray<(Action<TEventArgs> Handler, bool RequiresMainThread)> _handlers = handlers;
        public static readonly AsyncEventHandlerSet<TEventArgs> Empty = new([]);

        public AsyncEventHandlerSet<TEventArgs> AddHandler(Action<TEventArgs> handler, bool requiresMainThread)
            => new(_handlers.Add((handler, requiresMainThread)));

        public AsyncEventHandlerSet<TEventArgs> RemoveHandler(Action<TEventArgs> handler, bool requiresMainThread)
        {
            var newAsyncHandlers = _handlers.RemoveAll(arg => arg.Handler.Equals(handler) && arg.RequiresMainThread == requiresMainThread);

            return newAsyncHandlers.IsEmpty ? Empty : new(newAsyncHandlers);
        }

        public bool HasHandlers => !_handlers.IsEmpty;

        public void RaiseHandlers(TEventArgs arg, bool requiresMainThread)
        {
            foreach (var handler in _handlers)
            {
                if (handler.RequiresMainThread == requiresMainThread)
                    handler.Handler(arg);
            }
        }
    }
}

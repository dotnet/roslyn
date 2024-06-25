// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities;

internal sealed class WeakEvent<TEventArgs>
{
    private readonly object _guard = new();

    /// <summary>
    /// Each registered event handler has the lifetime of an associated owning object. This table ensures the weak
    /// references to the event handlers are not cleaned up while the owning object is still alive.
    /// </summary>
    private readonly EnumerableConditionalWeakTable<object, EventHandler<TEventArgs>> _handlers = new();

    public void AddHandler(object target, EventHandler<TEventArgs> handler)
    {
        lock (_guard)
        {
            if (_handlers.TryGetValue(target, out var existingHandler))
            {
                _handlers.AddOrUpdate(target, existingHandler + handler);
            }
            else
            {
                _handlers.Add(target, handler);
            }
        }
    }

    public void RemoveHandler(object target, EventHandler<TEventArgs> handler)
    {
        lock (_guard)
        {
            if (_handlers.TryGetValue(target, out var existingHandler))
            {
                if (existingHandler - handler is { } newHandler)
                {
                    _handlers.AddOrUpdate(target, newHandler);
                }
                else
                {
                    _handlers.Remove(target);
                }
            }
        }
    }

    public void RaiseEvent(TEventArgs e)
    {
        foreach (var (target, handler) in _handlers)
        {
            handler(target, e);
        }
    }
}

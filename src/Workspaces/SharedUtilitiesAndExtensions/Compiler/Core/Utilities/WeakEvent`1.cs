// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal delegate void WeakEventHandler<TEventArgs>(object sender, object target, TEventArgs args);

/// <summary>
/// Implements an event that can be subscribed to without keeping the subscriber alive for the lifespan of 
/// the object that declares <see cref="WeakEvent{TEventArgs}"/>.
/// 
/// Unlike handler created via <see cref="EventHandlerFactory{TArgs}.CreateWeakHandler{TTarget}(TTarget, Action{TTarget, object?, TArgs})"/> the handlers may capture state, which makes the subscribers simpler
/// and doesn't risk accidental leaks.
/// </summary>
internal readonly struct WeakEvent<TEventArgs>()
{
    /// <summary>
    /// Each registered event handler has the lifetime of an associated owning object. This table ensures the weak
    /// references to the event handlers are not cleaned up while the owning object is still alive.
    /// </summary>
    private readonly EnumerableConditionalWeakTable<object, WeakEventHandler<TEventArgs>> _handlers = new();

    public void AddHandler(object target, WeakEventHandler<TEventArgs> handler)
    {
        lock (_handlers.WriteLock)
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

    public void RemoveHandler(object target, WeakEventHandler<TEventArgs> handler)
    {
        lock (_handlers.WriteLock)
        {
            if (_handlers.TryGetValue(target, out var existingHandler))
            {
                var newHandler = existingHandler - handler;
                if (newHandler != null)
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

    public void RaiseEvent(object sender, TEventArgs e)
    {
        foreach (var (target, handler) in _handlers)
        {
            handler(sender, target, e);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities;

internal sealed class WeakEvent<TEventArgs>
{
    /// <summary>
    /// Each registered event handler has the lifetime of an associated owning object. This table ensures the weak
    /// references to the event handlers are not cleaned up while the owning object is still alive.
    /// </summary>
    /// <remarks>
    /// Access to this table is guarded by itself. For example:
    /// <code>
    /// <see langword="lock"/> (<see cref="_keepAliveTable"/>)
    /// {
    ///   // Read or write to <see cref="_keepAliveTable"/>.
    /// }
    /// </code>
    /// </remarks>
    private readonly ConditionalWeakTable<object, EventHandler<TEventArgs>> _keepAliveTable = new();

    private ImmutableList<WeakReference<EventHandler<TEventArgs>>> _weakHandlers = ImmutableList<WeakReference<EventHandler<TEventArgs>>>.Empty;

    public void AddHandler(object target, EventHandler<TEventArgs> handler)
    {
        lock (_keepAliveTable)
        {
            if (_keepAliveTable.TryGetValue(target, out var existingHandler))
            {
                // Combine the delegates and store the combination in the keep-alive table
                var newHandler = existingHandler + handler;
#if NET6_0_OR_GREATER
                _keepAliveTable.AddOrUpdate(target, newHandler);
#else
                _keepAliveTable.Remove(target);
                _keepAliveTable.Add(target, newHandler);
#endif
            }
            else
            {
                // This is the first handler for this target, so just store it in the table directly
                _keepAliveTable.Add(target, handler);
            }
        }

        ImmutableInterlocked.Update(
            ref _weakHandlers,
            static (weakHandlers, handler) =>
            {
                // Add a weak reference to the handler to the list of delegates to invoke for this event. During the
                // mutation, also remove any handlers that were collected without being removed (and are now null).
                return weakHandlers
                    .RemoveAll(WeakReferenceExtensions.IsNull)
                    .Add(new WeakReference<EventHandler<TEventArgs>>(handler));
            },
            handler);
    }

    public void RemoveHandler(object target, EventHandler<TEventArgs> handler)
    {
        lock (_keepAliveTable)
        {
            if (_keepAliveTable.TryGetValue(target, out var existingHandler))
            {
                var newHandler = existingHandler - handler;
                if (newHandler is null)
                {
                    _keepAliveTable.Remove(target);
                }
                else
                {
#if NET6_0_OR_GREATER
                    _keepAliveTable.AddOrUpdate(target, newHandler);
#else
                    _keepAliveTable.Remove(target);
                    _keepAliveTable.Add(target, newHandler);
#endif
                }
            }
        }

        ImmutableInterlocked.Update(
            ref _weakHandlers,
            static (weakHandlers, handler) =>
            {
                // Remove any references to the handler from the list of delegates to invoke for this event. During the
                // mutation, also remove any handlers that were collected without being removed (and are now null).
                var builder = weakHandlers.ToBuilder();
                for (var i = weakHandlers.Count - 1; i >= 0; i--)
                {
                    var weakHandler = weakHandlers[i];
                    if (!weakHandler.TryGetTarget(out var target))
                    {
                        // This handler was collected
                        builder.RemoveAt(i);
                        continue;
                    }

                    var updatedHandler = target - handler;
                    if (updatedHandler is null)
                    {
                        builder.RemoveAt(i);
                    }
                    else if (updatedHandler != target)
                    {
                        builder[i].SetTarget(updatedHandler);
                    }
                }

                return builder.ToImmutable();
            },
            handler);
    }

    public void RaiseEvent(object? sender, TEventArgs e)
    {
        foreach (var weakHandler in _weakHandlers)
        {
            if (!weakHandler.TryGetTarget(out var handler))
                continue;

            handler(sender, e);
        }
    }
}

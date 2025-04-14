// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

internal sealed class EventMap
{
    private readonly SemaphoreSlim _guard = new(initialCount: 1);
    private readonly Dictionary<string, EventHandlerSet> _eventNameToHandlerSet = [];

    public void AddEventHandler(string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        using (_guard.DisposableWait())
        {
            _eventNameToHandlerSet[eventName] = _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? handlers.AddHandler(handlerAndOptions)
                : EventHandlerSet.Empty.AddHandler(handlerAndOptions);
        }
    }

    public void RemoveEventHandler(string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        using (_guard.DisposableWait())
        {
            if (_eventNameToHandlerSet.TryGetValue(eventName, out var handlers))
                _eventNameToHandlerSet[eventName] = handlers.RemoveHandler(handlerAndOptions);
        }
    }

    public EventHandlerSet GetEventHandlerSet(string eventName)
    {
        using (_guard.DisposableWait())
        {
            return _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? handlers
                : EventHandlerSet.Empty;
        }
    }

    public record struct WorkspaceEventHandlerAndOptions(Action<EventArgs> Handler, WorkspaceEventOptions Options)
    {
    }

    public sealed class EventHandlerSet(ImmutableArray<Registry> registries)
    {
        public static readonly EventHandlerSet Empty = new([]);
        private readonly ImmutableArray<Registry> _registries = registries;

        public static EventHandlerSet Create(WorkspaceEventHandlerAndOptions handlerAndOptions)
            => new EventHandlerSet([new Registry(handlerAndOptions)]);

        public EventHandlerSet AddHandler(WorkspaceEventHandlerAndOptions handlerAndOptions)
            => new EventHandlerSet(_registries.Add(new Registry(handlerAndOptions)));

        public EventHandlerSet RemoveHandler(WorkspaceEventHandlerAndOptions handlerAndOptions)
        {
            var newRegistries = _registries.RemoveAll(r => r.HasHandlerAndOptions(handlerAndOptions));

            if (newRegistries == _registries)
                return this;

            // disable all registrations of this handler (so pending raise events can be squelched)
            // This does not guarantee no race condition between Raise and Remove but greatly reduces it.
            foreach (var registry in _registries.Where(r => r.HasHandlerAndOptions(handlerAndOptions)))
                registry.Unregister();

            return newRegistries.IsEmpty ? Empty : new(newRegistries);
        }

        public bool HasHandlers => _registries.Length > 0;

        public void RaiseEvent<TEventArgs>(TEventArgs arg, Func<WorkspaceEventOptions, bool> shouldRaiseEvent)
            where TEventArgs : EventArgs
        {
            foreach (var registry in _registries)
                registry.RaiseEvent(arg, shouldRaiseEvent);
        }
    }

    public sealed class Registry(WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        private WorkspaceEventHandlerAndOptions _handlerAndOptions = handlerAndOptions;
        private bool _disableHandler = false;

        public void Unregister()
            => _disableHandler = true;

        public bool HasHandlerAndOptions(WorkspaceEventHandlerAndOptions handlerAndOptions)
            => _handlerAndOptions.Equals(handlerAndOptions);

        public void RaiseEvent(EventArgs args, Func<WorkspaceEventOptions, bool> shouldRaiseEvent)
        {
            if (shouldRaiseEvent(_handlerAndOptions.Options) && !_disableHandler)
                _handlerAndOptions.Handler(args);
        }
    }
}

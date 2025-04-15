// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class WorkspaceEventMap
{
    private readonly SemaphoreSlim _guard = new(initialCount: 1);
    private readonly Dictionary<string, EventHandlerSet> _eventNameToHandlerSet = [];

    public WorkspaceEventRegistration AddEventHandler(string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        using (_guard.DisposableWait())
        {
            _eventNameToHandlerSet[eventName] = _eventNameToHandlerSet.TryGetValue(eventName, out var handlers)
                ? handlers.AddHandler(handlerAndOptions)
                : EventHandlerSet.Empty.AddHandler(handlerAndOptions);
        }

        return new WorkspaceEventRegistration(this, eventName, handlerAndOptions);
    }

    public void RemoveEventHandler(string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        using (_guard.DisposableWait())
        {
            _eventNameToHandlerSet.TryGetValue(eventName, out var originalHandlers);

            // An earlier AddEventHandler call would have created the WorkspaceEventRegistration whose
            // disposal would have called this method.
            Debug.Assert(originalHandlers != null);

            if (originalHandlers != null)
            {
                var newHandlers = originalHandlers.RemoveHandler(handlerAndOptions);
                Debug.Assert(originalHandlers != newHandlers);

                _eventNameToHandlerSet[eventName] = newHandlers;
            }
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

    public readonly record struct WorkspaceEventHandlerAndOptions(Action<EventArgs> Handler, WorkspaceEventOptions Options)
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
            var registry = _registries.FirstOrDefault(static (r, handlerAndOptions) => r.HasHandlerAndOptions(handlerAndOptions), handlerAndOptions);

            if (registry == null)
                return this;

            // disable this handler (so pending raise events can be squelched)
            // This does not guarantee no race condition between Raise and Remove but greatly reduces it.
            registry.Unregister();

            var newRegistries = _registries.Remove(registry);

            return newRegistries.IsEmpty ? Empty : new(newRegistries);
        }

        public bool HasHandlers
            => _registries.Any(static r => true);

        public bool HasMatchingOptions(Func<WorkspaceEventOptions, bool> isMatch)
            => _registries.Any(static (r, hasOptions) => r.HasMatchingOptions(hasOptions), isMatch);

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

        public bool HasMatchingOptions(Func<WorkspaceEventOptions, bool> isMatch)
            => !_disableHandler && isMatch(_handlerAndOptions.Options);

        public void RaiseEvent(EventArgs args, Func<WorkspaceEventOptions, bool> shouldRaiseEvent)
        {
            if (!_disableHandler && shouldRaiseEvent(_handlerAndOptions.Options))
                _handlerAndOptions.Handler(args);
        }
    }
}

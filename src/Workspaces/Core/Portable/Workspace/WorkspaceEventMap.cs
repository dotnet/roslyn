// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using static Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.CodeAnalysis;

internal sealed class WorkspaceEventMap
{
    private readonly object _guard = new();
    private readonly Dictionary<WorkspaceEventType, EventHandlerSet> _eventTypeToHandlerSet = [];

    public WorkspaceEventRegistration AddEventHandler(WorkspaceEventType eventType, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        lock (_guard)
        {
            _eventTypeToHandlerSet[eventType] = GetEventHandlerSet_NoLock(eventType).AddHandler(handlerAndOptions);
        }

        return new WorkspaceEventRegistration(this, eventType, handlerAndOptions);
    }

    public void RemoveEventHandler(WorkspaceEventType eventType, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        lock (_guard)
        {
            var originalHandlers = GetEventHandlerSet_NoLock(eventType);
            var newHandlers = originalHandlers.RemoveHandler(handlerAndOptions);

            // An earlier AddEventHandler call would have created the WorkspaceEventRegistration whose
            // disposal would have called this method.
            Debug.Assert(originalHandlers != newHandlers);

            _eventTypeToHandlerSet[eventType] = newHandlers;
        }
    }

    public EventHandlerSet GetEventHandlerSet(WorkspaceEventType eventType)
    {
        lock (_guard)
        {
            return GetEventHandlerSet_NoLock(eventType);
        }
    }

    private EventHandlerSet GetEventHandlerSet_NoLock(WorkspaceEventType eventType)
    {
        return _eventTypeToHandlerSet.TryGetValue(eventType, out var handlers)
            ? handlers
            : EventHandlerSet.Empty;
    }

    public readonly record struct WorkspaceEventHandlerAndOptions(Action<EventArgs> Handler, WorkspaceEventOptions Options);

    public sealed class EventHandlerSet(ImmutableArray<Registry> registries)
    {
        public static readonly EventHandlerSet Empty = new([]);
        private readonly ImmutableArray<Registry> _registries = registries;

        public static EventHandlerSet Create(WorkspaceEventHandlerAndOptions handlerAndOptions)
            => new([new Registry(handlerAndOptions)]);

        public EventHandlerSet AddHandler(WorkspaceEventHandlerAndOptions handlerAndOptions)
            => new(_registries.Add(new Registry(handlerAndOptions)));

        public EventHandlerSet RemoveHandler(WorkspaceEventHandlerAndOptions handlerAndOptions)
        {
            var registry = _registries.FirstOrDefault(static (r, handlerAndOptions) => r.HasHandlerAndOptions(handlerAndOptions), handlerAndOptions);

            Debug.Assert(registry != null, "Expected to find a registry for the handler and options.");
            if (registry == null)
                return this;

            // disable this handler (so pending raise events can be squelched)
            // This does not guarantee no race condition between Raise and Remove but greatly reduces it.
            registry.Unregister();

            var newRegistries = _registries.Remove(registry);

            return newRegistries.IsEmpty ? Empty : new(newRegistries);
        }

        public bool HasHandlers
            => !_registries.IsEmpty;

        public bool HasMatchingOptions(Func<WorkspaceEventOptions, bool> isMatch)
            => _registries.Any(static (r, hasOptions) => r.HasMatchingOptions(hasOptions), isMatch);

        public void RaiseEvent<TEventArgs>(TEventArgs arg, Func<WorkspaceEventOptions, bool> shouldRaiseEvent)
            where TEventArgs : EventArgs
        {
            foreach (var registry in _registries)
            {
                try
                {
                    registry.RaiseEvent(arg, shouldRaiseEvent);
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                    // Ensure we continue onto further items, even if one particular item fails.
                }
            }
        }
    }

    public sealed class Registry(WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        private readonly WorkspaceEventHandlerAndOptions _handlerAndOptions = handlerAndOptions;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities;

internal sealed class EventMap
{
    private readonly NonReentrantLock _guard = new();

    private readonly Dictionary<string, object> _eventNameToRegistries = [];

    public EventMap()
    {
    }

    public void AddEventHandler<TEventHandler>(string eventName, TEventHandler eventHandler)
        where TEventHandler : class
    {
        using (_guard.DisposableWait())
        {
            var registries = GetRegistries_NoLock<TEventHandler>(eventName);
            var newRegistries = registries.Add(new Registry<TEventHandler>(eventHandler));
            SetRegistries_NoLock(eventName, newRegistries);
        }
    }

    public void RemoveEventHandler<TEventHandler>(string eventName, TEventHandler eventHandler)
        where TEventHandler : class
    {
        using (_guard.DisposableWait())
        {
            var registries = GetRegistries_NoLock<TEventHandler>(eventName);

            // remove disabled registrations from list
            var newRegistries = registries.RemoveAll(r => r.HasHandler(eventHandler));

            if (newRegistries != registries)
            {
                // disable all registrations of this handler (so pending raise events can be squelched)
                // This does not guarantee no race condition between Raise and Remove but greatly reduces it.
                foreach (var registry in registries.Where(r => r.HasHandler(eventHandler)))
                {
                    registry.Unregister();
                }

                SetRegistries_NoLock(eventName, newRegistries);
            }
        }
    }

    [PerformanceSensitive(
        "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
        AllowImplicitBoxing = false)]
    public EventHandlerSet<TEventHandler> GetEventHandlers<TEventHandler>(string eventName)
        where TEventHandler : class
    {
        return new EventHandlerSet<TEventHandler>(this.GetRegistries<TEventHandler>(eventName));
    }

    private ImmutableArray<Registry<TEventHandler>> GetRegistries<TEventHandler>(string eventName)
        where TEventHandler : class
    {
        using (_guard.DisposableWait())
        {
            return GetRegistries_NoLock<TEventHandler>(eventName);
        }
    }

    private ImmutableArray<Registry<TEventHandler>> GetRegistries_NoLock<TEventHandler>(string eventName)
        where TEventHandler : class
    {
        _guard.AssertHasLock();
        if (_eventNameToRegistries.TryGetValue(eventName, out var registries))
        {
            return (ImmutableArray<Registry<TEventHandler>>)registries;
        }

        return [];
    }

    private void SetRegistries_NoLock<TEventHandler>(string eventName, ImmutableArray<Registry<TEventHandler>> registries)
        where TEventHandler : class
    {
        _guard.AssertHasLock();

        _eventNameToRegistries[eventName] = registries;
    }

    internal sealed class Registry<TEventHandler>(TEventHandler handler) : IEquatable<Registry<TEventHandler>?>
        where TEventHandler : class
    {
        private TEventHandler? _handler = handler;

        public void Unregister()
            => _handler = null;

        public void Invoke<TArg>(Action<TEventHandler, TArg> invoker, TArg arg)
        {
            var handler = _handler;
            if (handler != null)
            {
                invoker(handler, arg);
            }
        }

        public bool HasHandler(TEventHandler handler)
            => handler.Equals(_handler);

        public bool Equals(Registry<TEventHandler>? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other._handler == null && _handler == null)
            {
                return true;
            }

            if (other._handler == null || _handler == null)
            {
                return false;
            }

            return other._handler.Equals(_handler);
        }

        public override bool Equals(object? obj)
            => Equals(obj as Registry<TEventHandler>);

        public override int GetHashCode()
            => _handler == null ? 0 : _handler.GetHashCode();
    }

    internal struct EventHandlerSet<TEventHandler>
        where TEventHandler : class
    {
        private readonly ImmutableArray<Registry<TEventHandler>> _registries;

        internal EventHandlerSet(ImmutableArray<Registry<TEventHandler>> registries)
            => _registries = registries;

        public readonly bool HasHandlers
        {
            get { return _registries != null && _registries.Length > 0; }
        }

        public readonly void RaiseEvent<TArg>(Action<TEventHandler, TArg> invoker, TArg arg)
        {
            if (this.HasHandlers)
            {
                foreach (var registry in _registries)
                {
                    try
                    {
                        registry.Invoke(invoker, arg);
                    }
                    catch (Exception e) when (FatalError.ReportAndCatch(e))
                    {
                        // Catch the exception and continue as this an event handler and propagating the exception would prevent
                        // other handlers from executing
                    }
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities;

internal class EventMap
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

    internal class Registry<TEventHandler>(TEventHandler handler) : IEquatable<Registry<TEventHandler>?>
        where TEventHandler : class
    {
        private TEventHandler? _handler = handler;

        public void Unregister()
            => _handler = null;

        public void Invoke<TArg>(TArg arg, Action<TEventHandler, TArg> invoker)
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

        public readonly void RaiseEvent<TArg>(TArg arg, Action<TEventHandler, TArg> invoker)
        {
            // The try/catch here is to find additional telemetry for https://devdiv.visualstudio.com/DevDiv/_queries/query/71ee8553-7220-4b2a-98cf-20edab701fd1/.
            // We've realized there's a problem with our eventing, where if an exception is encountered while calling into subscribers to Workspace events,
            // we won't notify all of the callers. The expectation is such an exception would be thrown to the SafeStartNew in the workspace's event queue that
            // will raise that as a fatal exception, but OperationCancelledExceptions might be able to propagate through and fault the task we are using in the
            // chain. I'm choosing to use ReportWithoutCrashAndPropagate, because if our theory here is correct, it seems the first exception isn't actually
            // causing crashes, and so if it turns out this is a very common situation I don't want to make a often-benign situation fatal.
            try
            {
                if (this.HasHandlers)
                {
                    foreach (var registry in _registries)
                    {
                        registry.Invoke(invoker: invoker, arg: arg);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}

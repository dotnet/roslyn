// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Roslyn.Utilities
{
    internal class EventMap
    {
        private readonly NonReentrantLock _guard = new NonReentrantLock();

        private readonly Dictionary<string, object> _eventNameToRegistries =
            new Dictionary<string, object>();

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

            return ImmutableArray.Create<Registry<TEventHandler>>();
        }

        private void SetRegistries_NoLock<TEventHandler>(string eventName, ImmutableArray<Registry<TEventHandler>> registries)
            where TEventHandler : class
        {
            _guard.AssertHasLock();

            _eventNameToRegistries[eventName] = registries;
        }

        private class Registry<TEventHandler> : IEquatable<Registry<TEventHandler>>
            where TEventHandler : class
        {
            private TEventHandler _handler;

            public Registry(TEventHandler handler)
            {
                _handler = handler;
            }

            public void Unregister()
            {
                _handler = null;
            }

            public void Invoke(Action<TEventHandler> invoker)
            {
                var handler = _handler;
                if (handler != null)
                {
                    invoker(handler);
                }
            }

            public bool HasHandler(TEventHandler handler)
            {
                return handler.Equals(_handler);
            }

            public bool Equals(Registry<TEventHandler> other)
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

            public override bool Equals(object obj)
            {
                return Equals(obj as Registry<TEventHandler>);
            }

            public override int GetHashCode()
            {
                return _handler == null ? 0 : _handler.GetHashCode();
            }
        }

        internal struct EventHandlerSet<TEventHandler>
            where TEventHandler : class
        {
            private ImmutableArray<Registry<TEventHandler>> _registries;

            internal EventHandlerSet(object registries)
            {
                _registries = (ImmutableArray<Registry<TEventHandler>>)registries;
            }

            public bool HasHandlers
            {
                get { return _registries != null && _registries.Length > 0; }
            }

            public void RaiseEvent(Action<TEventHandler> invoker)
            {
                if (this.HasHandlers)
                {
                    foreach (var registry in _registries)
                    {
                        registry.Invoke(invoker);
                    }
                }
            }
        }
    }
}

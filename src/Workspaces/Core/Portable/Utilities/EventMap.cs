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
                    foreach (var registery in registries.Where(r => r.HasHandler(eventHandler)))
                    {
                        registery.Unregister();
                    }

                    SetRegistries_NoLock(eventName, newRegistries);
                }
            }
        }

        public bool HasEventHandlers<TEventHandler>(string eventName)
            where TEventHandler : class
        {
            return this.GetRegistries<TEventHandler>(eventName).Length > 0;
        }

        public void RaiseEvent<TEventHandler>(string eventName, Action<TEventHandler> invoker)
            where TEventHandler : class
        {
            var registries = GetRegistries<TEventHandler>(eventName);
            if (registries.Length > 0)
            {
                foreach (var registry in registries)
                {
                    registry.Invoke(invoker);
                }
            }
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

            object registries;
            if (_eventNameToRegistries.TryGetValue(eventName, out registries))
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
            private TEventHandler handler;

            public Registry(TEventHandler handler)
            {
                this.handler = handler;
            }

            public void Unregister()
            {
                this.handler = null;
            }

            public void Invoke(Action<TEventHandler> invoker)
            {
                var handler = this.handler;
                if (handler != null)
                {
                    invoker(handler);
                }
            }

            public bool HasHandler(TEventHandler handler)
            {
                return this.handler == handler;
            }

            public bool Equals(Registry<TEventHandler> other)
            {
                return other != null && other.handler == this.handler;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Registry<TEventHandler>);
            }

            public override int GetHashCode()
            {
                return this.handler.GetHashCode();
            }
        }
    }
}

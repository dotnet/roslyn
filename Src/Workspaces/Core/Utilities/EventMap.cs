// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal class EventMap
    {
        private readonly NonReentrantLock guard = new NonReentrantLock();

        private readonly Dictionary<string, object> eventNameToHandlers =
            new Dictionary<string, object>();

        public EventMap()
        {
        }

        public void AddEventHandler<TEventHandler>(string eventName, TEventHandler eventHandler)
        {
            using (this.guard.DisposableWait())
            {
                var handlers = GetEvents_NoLock<TEventHandler>(eventName);
                var newHandlers = (handlers ?? ImmutableList.Create<TEventHandler>()).Add(eventHandler);
                SetEvents_NoLock(eventName, newHandlers);
            }
        }

        public void RemoveEventHandler<TEventHandler>(string eventName, TEventHandler eventHandler)
        {
            using (this.guard.DisposableWait())
            {
                var handlers = GetEvents_NoLock<TEventHandler>(eventName);
                if (handlers != null)
                {
                    var newHandlers = handlers.Remove(eventHandler);
                    if (newHandlers != handlers)
                    {
                        SetEvents_NoLock(eventName, newHandlers);
                    }
                }
            }
        }

        public IEnumerable<TEventHandler> GetEventHandlers<TEventHandler>(string eventName)
        {
            using (this.guard.DisposableWait())
            {
                return GetEvents_NoLock<TEventHandler>(eventName);
            }
        }

        public void RaiseEvent<TEventArgs>(string eventName, object sender, TEventArgs args)
            where TEventArgs : EventArgs
        {
            var handlers = GetEventHandlers<EventHandler<TEventArgs>>(eventName);
            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    handler(sender, args);
                }
            }
        }

        private ImmutableList<TEventHandler> GetEvents_NoLock<TEventHandler>(string eventName)
        {
            this.guard.AssertHasLock();

            object handlers;
            if (this.eventNameToHandlers.TryGetValue(eventName, out handlers))
            {
                return (ImmutableList<TEventHandler>)handlers;
            }

            return null;
        }

        private void SetEvents_NoLock<TEventHandler>(string name, ImmutableList<TEventHandler> events)
        {
            this.guard.AssertHasLock();

            this.eventNameToHandlers[name] = events;
        }
    }
}
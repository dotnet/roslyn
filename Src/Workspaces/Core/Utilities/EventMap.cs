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
                var newHandlers = handlers.Add(eventHandler);
                SetEvents_NoLock(eventName, newHandlers);
            }
        }

        public void RemoveEventHandler<TEventHandler>(string eventName, TEventHandler eventHandler)
        {
            using (this.guard.DisposableWait())
            {
                var handlers = GetEvents_NoLock<TEventHandler>(eventName);
                var newHandlers = handlers.Remove(eventHandler);
                if (newHandlers != handlers)
                {
                    SetEvents_NoLock(eventName, newHandlers);
                }
            }
        }

        public ImmutableArray<TEventHandler> GetEventHandlers<TEventHandler>(string eventName)
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
            foreach (var handler in handlers)
            {
                handler(sender, args);
            }
        }

        private ImmutableArray<TEventHandler> GetEvents_NoLock<TEventHandler>(string eventName)
        {
            this.guard.AssertHasLock();

            object handlers;
            if (this.eventNameToHandlers.TryGetValue(eventName, out handlers))
            {
                return (ImmutableArray<TEventHandler>)handlers;
            }

            return ImmutableArray.Create<TEventHandler>();
        }

        private void SetEvents_NoLock<TEventHandler>(string name, ImmutableArray<TEventHandler> events)
        {
            this.guard.AssertHasLock();

            this.eventNameToHandlers[name] = events;
        }
    }
}
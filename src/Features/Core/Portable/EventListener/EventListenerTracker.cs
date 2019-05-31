// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.EventListener
{
    internal class EventListenerTracker<TService>
    {
        private readonly HashSet<string> _eventListenerInitialized;
        private readonly IEnumerable<Lazy<IEventListener, EventListenerMetadata>> _eventListeners;

        public EventListenerTracker(IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _eventListenerInitialized = new HashSet<string>();
            _eventListeners = eventListeners;
        }

        public void EnsureEventListener(Workspace workspace, TService service)
        {
            lock (_eventListenerInitialized)
            {
                if (!_eventListenerInitialized.Add(workspace.Kind))
                {
                    // already initialized
                    return;
                }

                foreach (var listener in _eventListeners.Where(l => l.Metadata.WorkspaceKinds?.Contains(workspace.Kind) ?? false)
                                                        .Select(l => l.Value)
                                                        .OfType<IEventListener<TService>>())
                {
                    listener.Listen(workspace, service);
                }
            }
        }
    }
}

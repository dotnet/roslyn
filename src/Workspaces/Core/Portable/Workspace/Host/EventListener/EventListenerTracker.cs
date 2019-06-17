// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// helper type to track whether <see cref="IEventListener"/> has been initialized.
    /// 
    /// currently, this helper only supports services whose lifetime is same as Host (ex, VS)
    /// </summary>
    /// <typeparam name="TService">TService for <see cref="IEventListener{TService}"/></typeparam>
    internal class EventListenerTracker<TService>
    {
        /// <summary>
        /// Workspace kind this event listener is initialized for
        /// </summary>
        private readonly HashSet<string> _eventListenerInitialized;
        private readonly IEnumerable<Lazy<IEventListener, EventListenerMetadata>> _eventListeners;

        public EventListenerTracker(IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _eventListenerInitialized = new HashSet<string>();
            _eventListeners = eventListeners;
        }

        public void EnsureEventListener(Workspace workspace, TService serviceOpt)
        {
            lock (_eventListenerInitialized)
            {
                if (!_eventListenerInitialized.Add(workspace.Kind))
                {
                    // already initialized
                    return;
                }

                foreach (var listener in GetListeners(workspace, _eventListeners))
                {
                    listener.Listen(workspace, serviceOpt);
                }
            }
        }

        public static IEnumerable<IEventListener<TService>> GetListeners(
            Workspace workspace, IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            return eventListeners.Where(l => l.Metadata.WorkspaceKinds.Contains(workspace.Kind))
                                 .Select(l => l.Value)
                                 .OfType<IEventListener<TService>>();
        }
    }
}

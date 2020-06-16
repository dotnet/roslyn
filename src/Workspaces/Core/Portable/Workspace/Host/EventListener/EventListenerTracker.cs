﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly ImmutableArray<Lazy<IEventListener, EventListenerMetadata>> _eventListeners;

        public EventListenerTracker(
            IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners, string kind)
        {
            _eventListenerInitialized = new HashSet<string>();
            _eventListeners = eventListeners.Where(el => el.Metadata.Service == kind).ToImmutableArray();
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
            }

            foreach (var listener in GetListeners(workspace, _eventListeners))
            {
                listener.StartListening(workspace, serviceOpt);
            }
        }

        public static IEnumerable<IEventListener<TService>> GetListeners(
            Workspace workspace, IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            return eventListeners.Where(l => l.Metadata.WorkspaceKinds.Contains(workspace.Kind))
                                 .Select(l => l.Value)
                                 .OfType<IEventListener<TService>>();
        }

        internal TestAccessor GetTestAccessor()
        {
            return new TestAccessor(this);
        }

        internal readonly struct TestAccessor
        {
            private readonly EventListenerTracker<TService> _eventListenerTracker;

            internal TestAccessor(EventListenerTracker<TService> eventListenerTracker)
                => _eventListenerTracker = eventListenerTracker;

            internal ref readonly ImmutableArray<Lazy<IEventListener, EventListenerMetadata>> EventListeners
                => ref _eventListenerTracker._eventListeners;
        }
    }
}

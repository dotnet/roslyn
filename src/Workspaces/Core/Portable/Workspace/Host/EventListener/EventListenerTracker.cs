// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// helper type to track whether <see cref="IEventListener"/> has been initialized.
/// 
/// currently, this helper only supports services whose lifetime is same as Host (ex, VS)
/// </summary>
internal sealed class EventListenerTracker(
    IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners, string kind)
{
    private readonly ImmutableArray<Lazy<IEventListener, EventListenerMetadata>> _eventListeners = [.. eventListeners.Where(el => el.Metadata.Service == kind)];

    public static IEnumerable<IEventListener> GetListeners(
        string? workspaceKind, IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
    {
        return (workspaceKind == null) ? [] : eventListeners
            .Where(l => l.Metadata.WorkspaceKinds.Contains(workspaceKind))
            .Select(l => l.Value);
    }

    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly EventListenerTracker _eventListenerTracker;

        internal TestAccessor(EventListenerTracker eventListenerTracker)
            => _eventListenerTracker = eventListenerTracker;

        internal ref readonly ImmutableArray<Lazy<IEventListener, EventListenerMetadata>> EventListeners
            => ref _eventListenerTracker._eventListeners;
    }
}

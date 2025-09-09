// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using static Microsoft.CodeAnalysis.Workspace;
using static Microsoft.CodeAnalysis.WorkspaceEventMap;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a registration for a workspace event, ensuring the event handler is properly unregistered when disposed.
/// </summary>
/// <remarks>This class is used to manage the lifecycle of an event handler associated with a workspace event. 
/// When the instance is disposed, the event handler is automatically unregistered from the event map.</remarks>
public sealed class WorkspaceEventRegistration : IDisposable
{
    private readonly WorkspaceEventType _eventType;
    private readonly WorkspaceEventHandlerAndOptions _handlerAndOptions;
    private WorkspaceEventMap? _eventMap;

    internal WorkspaceEventRegistration(WorkspaceEventMap eventMap, WorkspaceEventType eventType, WorkspaceEventHandlerAndOptions handlerAndOptions)
    {
        _eventType = eventType;
        _handlerAndOptions = handlerAndOptions;
        _eventMap = eventMap;
    }

    public void Dispose()
    {
        // Protect against simultaneous disposal from multiple threads
        var eventMap = Interlocked.Exchange(ref _eventMap, null);

        eventMap?.RemoveEventHandler(_eventType, _handlerAndOptions);
    }
}

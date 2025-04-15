// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using static Microsoft.CodeAnalysis.WorkspaceEventMap;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a registration for a workspace event, ensuring the event handler is properly unregistered when disposed.
/// </summary>
/// <remarks>This class is used to manage the lifecycle of an event handler associated with a workspace event. 
/// When the instance is disposed, the event handler is automatically unregistered from the event map.</remarks>
internal sealed class WorkspaceEventRegistration(WorkspaceEventMap eventMap, string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions) : IDisposable
{
    private readonly string _eventName = eventName;
    private readonly WorkspaceEventHandlerAndOptions _handlerAndOptions = handlerAndOptions;
    private WorkspaceEventMap? _eventMap = eventMap;

    public void Dispose()
    {
        _eventMap?.RemoveEventHandler(_eventName, _handlerAndOptions);
        _eventMap = null;
    }
}

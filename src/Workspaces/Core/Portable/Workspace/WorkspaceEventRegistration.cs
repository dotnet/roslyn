// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using static Microsoft.CodeAnalysis.EventMap;

namespace Microsoft.CodeAnalysis;

internal sealed class WorkspaceEventRegistration(EventMap eventMap, string eventName, WorkspaceEventHandlerAndOptions handlerAndOptions) : IDisposable
{
    private readonly EventMap _eventMap = eventMap;
    private readonly string _eventName = eventName;
    private readonly WorkspaceEventHandlerAndOptions _handlerAndOptions = handlerAndOptions;

    public void Dispose()
        => _eventMap.RemoveEventHandler(_eventName, _handlerAndOptions);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IUIContextService))]
internal sealed class UIContextService : IUIContextService
{
    private readonly ConcurrentDictionary<Guid, UIContext> _guidToUIContextMap = [];

    public bool IsActive(Guid contextGuid)
    {
        var uiContext = _guidToUIContextMap.GetOrAdd(contextGuid, UIContext.FromUIContextGuid);

        // Note: Getting UIContext.IsActive is free-threaded but setting is not. However, the VSTHRD10 analyzer
        // does not support reporting for property accessors differently.
        // https://github.com/microsoft/vs-threading/issues/540

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        return uiContext.IsActive;
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
    }
}

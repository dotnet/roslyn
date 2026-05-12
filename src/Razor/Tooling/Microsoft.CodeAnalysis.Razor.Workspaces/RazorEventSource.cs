// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.Razor;

[EventSource(Name = "RazorEventSource")]
internal sealed class RazorEventSource : EventSource
{
    public static readonly RazorEventSource Instance = new();

    private RazorEventSource()
    {
    }

    [Event(1, Level = EventLevel.Informational)]
    public void BackgroundDocumentGeneratorIdle() => WriteEvent(1);
}

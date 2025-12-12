// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Logging;

internal sealed class TestTelemetryLogger : TelemetryLogger
{
    public TestTelemetryLogger(bool logDelta = false)
    {
        LogDelta = logDelta;
    }

    protected override bool LogDelta { get; }

    public sealed class TestScope
    {
        public readonly TelemetryEvent EndEvent;
        public readonly LogType Type;

        public TestScope(TelemetryEvent endEvent, LogType type)
        {
            EndEvent = endEvent;
            Type = type;
        }
    }

    public List<TelemetryEvent> PostedEvents = [];
    public HashSet<TestScope> OpenedScopes = [];

    public override bool IsEnabled(FunctionId functionId)
        => true;

    protected override void PostEvent(TelemetryEvent telemetryEvent)
    {
        PostedEvents.Add(telemetryEvent);
    }

    protected override object Start(string eventName, LogType type)
    {
        var scope = new TestScope(new TelemetryEvent(eventName), type);
        OpenedScopes.Add(scope);
        return scope;
    }

    protected override void End(object scope, TelemetryResult result)
    {
        Assert.True(OpenedScopes.Remove((TestScope)scope));
    }

    protected override TelemetryEvent GetEndEvent(object scope)
        => ((TestScope)scope).EndEvent;
}

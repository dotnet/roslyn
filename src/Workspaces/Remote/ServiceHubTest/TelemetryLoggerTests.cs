// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TelemetryLoggerTests
    {
        private class TestLogger : TelemetryLogger
        {
            public TestLogger(bool logDelta = false)
            {
                LogDelta = logDelta;
            }

            protected override bool LogDelta { get; }

            public class TestScope
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

        private static IEnumerable<string> InspectProperties(TelemetryEvent @event, string? keyToIgnoreValueInspection = null)
            => @event.Properties.Select(p => $"{p.Key}={(keyToIgnoreValueInspection == p.Key ? string.Empty : InspectPropertyValue(p.Value))}");

        private static string InspectPropertyValue(object? value)
            => value switch
            {
                null => "<null>",
                TelemetryComplexProperty { Value: IEnumerable<object?> items } => $"Complex[{string.Join(",", items.Select(InspectPropertyValue))}]",
                _ => value.ToString()!
            };

        [Theory]
        [CombinatorialData]
        internal void IgnoredSeverity(LogLevel level)
        {
            var logger = new TestLogger();

            logger.Log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, LogMessage.Create("test", level));
            Assert.Equal((level < LogLevel.Information) ? 0 : 1, logger.PostedEvents.Count);
        }

        [Fact]
        public void EventWithProperties()
        {
            var logger = new TestLogger();

            logger.Log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(p =>
            {
                p.Add("test1", 1);
                p.Add("test2", new PiiValue(2));
                p.Add("test3", new object[] { 3, new PiiValue(4) });
            }));

            var postedEvent = logger.PostedEvents.Single();

            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid", postedEvent.Name);

            AssertEx.Equal(new[]
            {
                "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test1=1",
                "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test2=PII(2)",
                "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test3=Complex[3,PII(4)]",
            }, InspectProperties(postedEvent));
        }

        [Theory, CombinatorialData]
        public void LogBlockStartEnd(bool logDelta)
        {
            var logger = new TestLogger(logDelta);

            logger.LogBlockStart(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(p => p.Add("test", "start"), logLevel: LogLevel.Information), blockId: 1, CancellationToken.None);

            var scope = logger.OpenedScopes.Single();
            Assert.Equal(LogType.Trace, scope.Type);

            logger.LogBlockEnd(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(p => p.Add("test", "end")), blockId: 1, delta: 100, CancellationToken.None);

            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid", scope.EndEvent.Name);

            if (logDelta)
            {
                // We don't inspect the property value for "Delta" (time of execution) as that value will vary each time.
                AssertEx.Equal(new[]
                {
                    "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test=end",
                    "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.delta="
                }, InspectProperties(scope.EndEvent, keyToIgnoreValueInspection: "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.delta"));
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test=end"
                }, InspectProperties(scope.EndEvent));
            }
        }
    }
}

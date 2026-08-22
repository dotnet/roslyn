// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CommandLine;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class ManagedCompilerTelemetryTests
    {
        /// <summary>
        /// An <see cref="IBuildEngine5"/> that records telemetry logged via
        /// <see cref="IBuildEngine5.LogTelemetry"/>. Other members are no-ops or throw.
        /// </summary>
        private sealed class TelemetryMockEngine : IBuildEngine5
        {
            public List<(string EventName, IDictionary<string, string> Properties)> TelemetryEvents { get; } = new();

            public void LogTelemetry(string eventName, IDictionary<string, string> properties)
                => TelemetryEvents.Add((eventName, properties));

            // IBuildEngine
            public bool ContinueOnError => true;
            public int LineNumberOfTaskNode => 0;
            public int ColumnNumberOfTaskNode => 0;
            public string ProjectFileOfTaskNode => "";
            public void LogErrorEvent(BuildErrorEventArgs e) { }
            public void LogWarningEvent(BuildWarningEventArgs e) { }
            public void LogMessageEvent(BuildMessageEventArgs e) { }
            public void LogCustomEvent(CustomBuildEventArgs e) { }
            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
                => throw new NotImplementedException();

            // IBuildEngine2
            public bool IsRunningMultipleNodes => false;
            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
                => throw new NotImplementedException();
            public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
                => throw new NotImplementedException();

            // IBuildEngine3
            public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
                => throw new NotImplementedException();
            public void Yield() => throw new NotImplementedException();
            public void Reacquire() => throw new NotImplementedException();

            // IBuildEngine4
            public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
                => throw new NotImplementedException();
            public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
                => throw new NotImplementedException();
            public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
                => throw new NotImplementedException();
        }

        [Fact]
        public void ReportTelemetry_ForwardsEvents()
        {
            var engine = new TelemetryMockEngine();
            var csc = new Csc { BuildEngine = engine };

            var events = new[]
            {
                new BuildTelemetryEvent("roslyn/compilercache", new Dictionary<string, string>
                {
                    ["cachestatus"] = "hit",
                    ["language"] = "C#",
                }),
            };

            csc.ReportTelemetry(events, EmptyCompilerServerLogger.Instance);

            var reported = Assert.Single(engine.TelemetryEvents);
            Assert.Equal("roslyn/compilercache", reported.EventName);
            Assert.Equal("hit", reported.Properties["cachestatus"]);
            Assert.Equal("C#", reported.Properties["language"]);
        }

        [Fact]
        public void ReportTelemetry_NoEvents_DoesNothing()
        {
            var engine = new TelemetryMockEngine();
            var csc = new Csc { BuildEngine = engine };

            csc.ReportTelemetry(Array.Empty<BuildTelemetryEvent>(), EmptyCompilerServerLogger.Instance);

            Assert.Empty(engine.TelemetryEvents);
        }

        [Fact]
        public void ReportTelemetry_EngineWithoutTelemetrySupport_DoesNotThrow()
        {
            var engine = new MockEngine();
            var csc = new Csc { BuildEngine = engine };

            var events = new[] { new BuildTelemetryEvent("roslyn/compilercache", new Dictionary<string, string>()) };

            // MockEngine does not implement IBuildEngine5; this must be a safe no-op.
            Assert.IsNotAssignableFrom<IBuildEngine5>(engine);
            csc.ReportTelemetry(events, EmptyCompilerServerLogger.Instance);
        }
    }
}

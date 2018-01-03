// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal interface IPerformanceTrackerService : IWorkspaceService
    {
        void AddSnapshot(IEnumerable<(string analyzerId, bool builtIn, TimeSpan timeSpan)> snapshot);
        void AddSnapshot(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> snapshot);
        void GenerateReport(List<BadAnalyzerInfo> badAnalyzers);

        event EventHandler SnapshotAdded;
    }

    internal struct BadAnalyzerInfo
    {
        public readonly bool BuiltIn;
        public readonly string AnalyzerId;
        public readonly string Hash;
        public readonly double LOF;
        public readonly double Mean;
        public readonly double Stddev;

        public BadAnalyzerInfo(bool builtIn, string analyzerId, double lof_value, double mean, double stddev) : this()
        {
            BuiltIn = builtIn;
            AnalyzerId = analyzerId;
            Hash = analyzerId.GetHashCode().ToString();
            LOF = lof_value;
            Mean = mean;
            Stddev = stddev;
        }

        public string PIISafeAnalyzerId => BuiltIn ? AnalyzerId : Hash;
    }
}

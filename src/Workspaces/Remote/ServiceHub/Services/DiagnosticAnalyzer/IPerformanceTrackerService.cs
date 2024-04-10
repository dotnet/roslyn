// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal interface IPerformanceTrackerService : IWorkspaceService
    {
        void AddSnapshot(IEnumerable<AnalyzerPerformanceInfo> snapshot, int unitCount, bool forSpanAnalysis);
        void GenerateReport(List<AnalyzerInfoForPerformanceReporting> analyzerInfos, bool forSpanAnalysis);

        event EventHandler SnapshotAdded;
    }

    internal readonly struct AnalyzerInfoForPerformanceReporting
    {
        public readonly bool BuiltIn;
        public readonly string AnalyzerId;
        public readonly string AnalyzerIdHash;
        public readonly double Average;
        public readonly double AdjustedStandardDeviation;

        public AnalyzerInfoForPerformanceReporting(bool builtIn, string analyzerId, double average, double stddev) : this()
        {
            BuiltIn = builtIn;
            AnalyzerId = analyzerId;
            AnalyzerIdHash = analyzerId.GetHashCode().ToString();
            Average = average;
            AdjustedStandardDeviation = stddev;
        }

        public string PIISafeAnalyzerId => BuiltIn ? AnalyzerId : AnalyzerIdHash;
    }
}

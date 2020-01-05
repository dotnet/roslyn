// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal interface IPerformanceTrackerService : IWorkspaceService
    {
        void AddSnapshot(IEnumerable<AnalyzerPerformanceInfo> snapshot, int unitCount);
        void GenerateReport(List<ExpensiveAnalyzerInfo> badAnalyzers);

        event EventHandler SnapshotAdded;
    }

    internal struct ExpensiveAnalyzerInfo
    {
        public readonly bool BuiltIn;
        public readonly string AnalyzerId;
        public readonly string AnalyzerIdHash;
        public readonly double LocalOutlierFactor;
        public readonly double Average;
        public readonly double AdjustedStandardDeviation;

        public ExpensiveAnalyzerInfo(bool builtIn, string analyzerId, double lof_value, double average, double stddev) : this()
        {
            BuiltIn = builtIn;
            AnalyzerId = analyzerId;
            AnalyzerIdHash = analyzerId.GetHashCode().ToString();
            LocalOutlierFactor = lof_value;
            Average = average;
            AdjustedStandardDeviation = stddev;
        }

        public string PIISafeAnalyzerId => BuiltIn ? AnalyzerId : AnalyzerIdHash;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class DiagnosticArguments
    {
        public bool ReportSuppressedDiagnostics;
        public bool LogAnalyzerExecutionTime;
        public ProjectId ProjectId;
        public Checksum OptionSetChecksum;
        public Checksum[] HostAnalyzerChecksums;
        public string[] AnalyzerIds;

        public DiagnosticArguments()
        {
        }

        public DiagnosticArguments(
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            ProjectId projectId,
            Checksum optionSetChecksum,
            ImmutableArray<Checksum> hostAnalyzerChecksums,
            string[] analyzerIds)
        {
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            LogAnalyzerExecutionTime = logAnalyzerExecutionTime;

            ProjectId = projectId;

            OptionSetChecksum = optionSetChecksum;
            HostAnalyzerChecksums = hostAnalyzerChecksums.ToArray();
            AnalyzerIds = analyzerIds;
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class DiagnosticArguments
    {
        public bool ForcedAnalysis;
        public bool ReportSuppressedDiagnostics;
        public bool LogAnalyzerExecutionTime;
        public ProjectId ProjectId;
        public Checksum OptionSetChecksum;
        public string[] AnalyzerIds;

        public DiagnosticArguments()
        {
        }

        public DiagnosticArguments(
            bool forcedAnalysis,
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            ProjectId projectId,
            Checksum optionSetChecksum,
            string[] analyzerIds)
        {
            ForcedAnalysis = forcedAnalysis;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            LogAnalyzerExecutionTime = logAnalyzerExecutionTime;

            ProjectId = projectId;

            OptionSetChecksum = optionSetChecksum;
            AnalyzerIds = analyzerIds;
        }
    }
}

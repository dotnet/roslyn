// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public string[] AnalyzerIds;

        public DiagnosticArguments()
        {
        }

        public DiagnosticArguments(
            bool forcedAnalysis,
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            ProjectId projectId,
            string[] analyzerIds)
        {
            ForcedAnalysis = forcedAnalysis;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            LogAnalyzerExecutionTime = logAnalyzerExecutionTime;
            ProjectId = projectId;
            AnalyzerIds = analyzerIds;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class DiagnosticArguments
    {
        public bool IsHighPriority;
        public bool ReportSuppressedDiagnostics;
        public bool LogPerformanceInfo;
        public bool GetTelemetryInfo;
        public DocumentId? DocumentId;
        public TextSpan? DocumentSpan;
        public AnalysisKind? DocumentAnalysisKind;
        public ProjectId ProjectId;
        public string[] AnalyzerIds;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public DiagnosticArguments()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }

        public DiagnosticArguments(
            bool isHighPriority,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            DocumentId? documentId,
            TextSpan? documentSpan,
            AnalysisKind? documentAnalysisKind,
            ProjectId projectId,
            string[] analyzerIds)
        {
            IsHighPriority = isHighPriority;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            LogPerformanceInfo = logPerformanceInfo;
            GetTelemetryInfo = getTelemetryInfo;
            DocumentId = documentId;
            DocumentSpan = documentSpan;
            DocumentAnalysisKind = documentAnalysisKind;
            ProjectId = projectId;
            AnalyzerIds = analyzerIds;
        }
    }
}

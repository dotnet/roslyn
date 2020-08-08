// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class DiagnosticArguments
    {
        /// <summary>
        /// Flag indicating if this is a high priority diagnostic computation request.
        /// For example, all requests for active document are marked high priority.
        /// Additionally, diagnostic computation for explicit user commands, such as FixAll,
        /// are marked high priority.
        /// </summary>
        public bool IsHighPriority;

        /// <summary>
        /// Flag indicating if suppressed diagnostics should be returned.
        /// </summary>
        public bool ReportSuppressedDiagnostics;

        /// <summary>
        /// Flag indicating if analyzer performance info, such as analyzer execution times,
        /// should be logged as performance telemetry.
        /// </summary>
        public bool LogPerformanceInfo;

        /// <summary>
        /// Flag indicating if the analyzer telemety info, such as registered analyzer action counts
        /// and analyzer execution times, should be included in the computed result.
        /// </summary>
        public bool GetTelemetryInfo;

        /// <summary>
        /// Optional document ID, if computing diagnostics for a specific document.
        /// For example, diagnostic computation for open file analysis.
        /// </summary>
        public DocumentId? DocumentId;

        /// <summary>
        /// Optional document text span, if computing diagnostics for a specific span for a document.
        /// For example, diagnostic computation for light bulb invocation for a specific line in active document.
        /// </summary>
        public TextSpan? DocumentSpan;

        /// <summary>
        /// Optional <see cref="AnalysisKind"/>, if computing specific kind of diagnostics for a document request,
        /// i.e. <see cref="DocumentId"/> must be non-null for a non-null analysis kind.
        /// Only supported non-null values are <see cref="AnalysisKind.Syntax"/> and <see cref="AnalysisKind.Semantic"/>.
        /// </summary>
        public AnalysisKind? DocumentAnalysisKind;

        /// <summary>
        /// Project ID for the document or project for which diagnostics need to be computed.
        /// </summary>
        public ProjectId ProjectId;

        /// <summary>
        /// Array of analyzer IDs for analyzers that need to be executed for computing diagnostics.
        /// </summary>
        public string[] AnalyzerIds;

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
            Debug.Assert(documentId != null || documentSpan == null);
            Debug.Assert(documentId != null || documentAnalysisKind == null);
            Debug.Assert(documentAnalysisKind == null ||
                documentAnalysisKind == AnalysisKind.Syntax || documentAnalysisKind == AnalysisKind.Semantic);
            Debug.Assert(analyzerIds.Length > 0);

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

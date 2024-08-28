// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

// EncEditSessionInfo is populated on a background thread and then read from the UI thread
internal sealed class EditSessionTelemetry
{
    internal readonly struct Data(EditSessionTelemetry telemetry)
    {
        public readonly ImmutableArray<(ushort EditKind, ushort SyntaxKind, Guid projectId)> RudeEdits = telemetry._rudeEdits.AsImmutable();
        public readonly ImmutableArray<string> EmitErrorIds = telemetry._emitErrorIds.AsImmutable();
        public readonly ImmutableArray<Guid> ProjectsWithValidDelta = telemetry._projectsWithValidDelta.AsImmutable();
        public readonly EditAndContinueCapabilities Capabilities = telemetry._capabilities;
        public readonly bool HadCompilationErrors = telemetry._hadCompilationErrors;
        public readonly bool HadRudeEdits = telemetry._hadRudeEdits;
        public readonly bool HadValidChanges = telemetry._hadValidChanges;
        public readonly bool HadValidInsignificantChanges = telemetry._hadValidInsignificantChanges;
        public readonly bool InBreakState = telemetry._inBreakState!.Value;
        public readonly bool IsEmpty = telemetry.IsEmpty;
        public readonly bool Committed = telemetry._committed;
        public readonly TimeSpan EmitDifferenceTime = telemetry._emitDifferenceTime;
        public readonly TimeSpan AnalysisTime = telemetry._analysisTime;
    }

    private readonly object _guard = new();

    // Limit the number of reported items to limit the size of the telemetry event (max total size is 64K).
    private const int MaxReportedProjectIds = 20;

    private readonly HashSet<(ushort, ushort, Guid)> _rudeEdits = [];
    private readonly HashSet<string> _emitErrorIds = [];
    private readonly HashSet<Guid> _projectsWithValidDelta = [];

    private bool _hadCompilationErrors;
    private bool _hadRudeEdits;
    private bool _hadValidChanges;
    private bool _hadValidInsignificantChanges;
    private bool? _inBreakState;
    private bool _committed;
    private TimeSpan _emitDifferenceTime;
    private TimeSpan _analysisTime;

    private EditAndContinueCapabilities _capabilities;

    public Data GetDataAndClear()
    {
        lock (_guard)
        {
            var data = new Data(this);
            _rudeEdits.Clear();
            _emitErrorIds.Clear();
            _projectsWithValidDelta.Clear();
            _hadCompilationErrors = false;
            _hadRudeEdits = false;
            _hadValidChanges = false;
            _hadValidInsignificantChanges = false;
            _inBreakState = null;
            _capabilities = EditAndContinueCapabilities.None;
            _committed = false;
            _emitDifferenceTime = TimeSpan.Zero;
            return data;
        }
    }

    public bool IsEmpty => !(_hadCompilationErrors || _hadRudeEdits || _hadValidChanges || _hadValidInsignificantChanges);

    public void SetBreakState(bool value)
        => _inBreakState = value;

    public void LogEmitDifferenceTime(TimeSpan span)
        => _emitDifferenceTime += span;

    public void LogAnalysisTime(TimeSpan span)
        => _analysisTime += span;

    public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, Guid projectTelemetryId, ImmutableArray<string> errorsIds)
    {
        lock (_guard)
        {
            _emitErrorIds.AddRange(errorsIds);

            switch (summary)
            {
                case ProjectAnalysisSummary.NoChanges:
                    break;

                case ProjectAnalysisSummary.SyntaxErrors:
                    _hadCompilationErrors = true;
                    break;

                case ProjectAnalysisSummary.RudeEdits:
                    _hadRudeEdits = true;
                    break;

                case ProjectAnalysisSummary.ValidChanges:
                    _hadValidChanges = true;

                    if (errorsIds.IsEmpty && _projectsWithValidDelta.Count < MaxReportedProjectIds)
                    {
                        _projectsWithValidDelta.Add(projectTelemetryId);
                    }

                    break;

                case ProjectAnalysisSummary.ValidInsignificantChanges:
                    _hadValidInsignificantChanges = true;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(summary);
            }
        }
    }

    public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, Guid projectTelemetryId, ImmutableArray<Diagnostic> emitDiagnostics)
        => LogProjectAnalysisSummary(summary, projectTelemetryId, emitDiagnostics.SelectAsArray(d => d.Severity == DiagnosticSeverity.Error, d => d.Id));

    public void LogRudeEditDiagnostics(ImmutableArray<RudeEditDiagnostic> diagnostics, Guid projectTelemetryId)
    {
        lock (_guard)
        {
            foreach (var diagnostic in diagnostics)
            {
                _rudeEdits.Add(((ushort)diagnostic.Kind, diagnostic.SyntaxKind, projectTelemetryId));
            }
        }
    }

    public void LogRuntimeCapabilities(EditAndContinueCapabilities capabilities)
    {
        lock (_guard)
        {
            Debug.Assert(_capabilities == EditAndContinueCapabilities.None || _capabilities == capabilities);
            _capabilities = capabilities;
        }
    }

    public void LogCommitted()
        => _committed = true;
}

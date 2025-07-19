// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.EditAndContinue;

// EncEditSessionInfo is populated on a background thread and then read from the UI thread
internal sealed class EditSessionTelemetry
{
    internal readonly struct Data(EditSessionTelemetry telemetry)
    {
        public readonly ImmutableArray<(ushort EditKind, ushort SyntaxKind, Guid projectId)> RudeEdits = telemetry._rudeEdits.AsImmutable();
        public readonly ImmutableArray<string> EmitErrorIds = telemetry._emitErrorIds.AsImmutable();
        public readonly ImmutableArray<Guid> ProjectsWithValidDelta = telemetry._projectsWithValidDelta.AsImmutable();
        public readonly ImmutableArray<Guid> ProjectsWithUpdatedBaselines = telemetry._projectsWithUpdatedBaselines.AsImmutable();
        public readonly EditAndContinueCapabilities Capabilities = telemetry._capabilities;
        public readonly bool HasSyntaxErrors = telemetry._hadSyntaxErrors;
        public readonly bool HadBlockingRudeEdits = telemetry._hadBlockingRudeEdits;
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
    private readonly HashSet<Guid> _projectsWithUpdatedBaselines = [];

    private bool _hadSyntaxErrors;
    private bool _hadBlockingRudeEdits;
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
            _projectsWithUpdatedBaselines.Clear();
            _hadSyntaxErrors = false;
            _hadBlockingRudeEdits = false;
            _hadValidChanges = false;
            _hadValidInsignificantChanges = false;
            _inBreakState = null;
            _capabilities = EditAndContinueCapabilities.None;
            _committed = false;
            _emitDifferenceTime = TimeSpan.Zero;
            return data;
        }
    }

    public bool IsEmpty => !(_hadSyntaxErrors || _hadBlockingRudeEdits || _hadValidChanges || _hadValidInsignificantChanges);

    public void SetBreakState(bool value)
        => _inBreakState = value;

    public void LogEmitDifferenceTime(TimeSpan span)
        => _emitDifferenceTime += span;

    public void LogAnalysisTime(TimeSpan span)
        => _analysisTime += span;

    public void LogSyntaxError()
        => _hadSyntaxErrors = true;

    public void LogProjectAnalysisSummary(ProjectAnalysisSummary? summary, Guid projectTelemetryId, IEnumerable<Diagnostic> diagnostics)
    {
        lock (_guard)
        {
            var hasError = false;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    if (diagnostic.IsRudeEdit())
                    {
                        _hadBlockingRudeEdits = true;
                    }
                    else
                    {
                        _emitErrorIds.Add(diagnostic.Id);
                        hasError = true;
                    }
                }
            }

            switch (summary)
            {
                case null:
                    // report diagnostics only
                    break;

                case ProjectAnalysisSummary.NoChanges:
                    break;

                case ProjectAnalysisSummary.InvalidChanges:
                    break;

                case ProjectAnalysisSummary.ValidChanges:
                    _hadValidChanges = true;

                    if (!hasError && _projectsWithValidDelta.Count < MaxReportedProjectIds)
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

    public void LogUpdatedBaseline(Guid projectTelemetryId)
        => _projectsWithUpdatedBaselines.Add(projectTelemetryId);
}

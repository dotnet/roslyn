// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    // EncEditSessionInfo is populated on a background thread and then read from the UI thread
    internal sealed class EditSessionTelemetry
    {
        internal readonly struct Data
        {
            public readonly ImmutableArray<(ushort EditKind, ushort SyntaxKind)> RudeEdits;
            public readonly ImmutableArray<string> EmitErrorIds;
            public readonly EditAndContinueCapabilities Capabilities;
            public readonly bool HadCompilationErrors;
            public readonly bool HadRudeEdits;
            public readonly bool HadValidChanges;
            public readonly bool HadValidInsignificantChanges;
            public readonly bool InBreakState;
            public readonly bool IsEmpty;

            public Data(EditSessionTelemetry telemetry)
            {
                RudeEdits = telemetry._rudeEdits.AsImmutable();
                EmitErrorIds = telemetry._emitErrorIds.AsImmutable();
                HadCompilationErrors = telemetry._hadCompilationErrors;
                HadRudeEdits = telemetry._hadRudeEdits;
                HadValidChanges = telemetry._hadValidChanges;
                HadValidInsignificantChanges = telemetry._hadValidInsignificantChanges;
                InBreakState = telemetry._inBreakState;
                Capabilities = telemetry._capabilities;
                IsEmpty = telemetry.IsEmpty;
            }
        }

        private readonly object _guard = new();

        private readonly HashSet<(ushort, ushort)> _rudeEdits = new();
        private readonly HashSet<string> _emitErrorIds = new();

        private bool _hadCompilationErrors;
        private bool _hadRudeEdits;
        private bool _hadValidChanges;
        private bool _hadValidInsignificantChanges;
        private bool _inBreakState;

        private EditAndContinueCapabilities _capabilities;

        public Data GetDataAndClear()
        {
            lock (_guard)
            {
                var data = new Data(this);
                _rudeEdits.Clear();
                _emitErrorIds.Clear();
                _hadCompilationErrors = false;
                _hadRudeEdits = false;
                _hadValidChanges = false;
                _hadValidInsignificantChanges = false;
                _inBreakState = false;
                _capabilities = EditAndContinueCapabilities.None;
                return data;
            }
        }

        public bool IsEmpty => !(_hadCompilationErrors || _hadRudeEdits || _hadValidChanges || _hadValidInsignificantChanges);

        public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, ImmutableArray<string> errorsIds, bool inBreakState)
        {
            lock (_guard)
            {
                _emitErrorIds.AddRange(errorsIds);
                _inBreakState = inBreakState;

                switch (summary)
                {
                    case ProjectAnalysisSummary.NoChanges:
                        break;

                    case ProjectAnalysisSummary.CompilationErrors:
                        _hadCompilationErrors = true;
                        break;

                    case ProjectAnalysisSummary.RudeEdits:
                        _hadRudeEdits = true;
                        break;

                    case ProjectAnalysisSummary.ValidChanges:
                        _hadValidChanges = true;
                        break;

                    case ProjectAnalysisSummary.ValidInsignificantChanges:
                        _hadValidInsignificantChanges = true;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(summary);
                }
            }
        }

        public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, ImmutableArray<Diagnostic> emitDiagnostics, bool inBreakState)
            => LogProjectAnalysisSummary(summary, emitDiagnostics.SelectAsArray(d => d.Severity == DiagnosticSeverity.Error, d => d.Id), inBreakState);

        public void LogRudeEditDiagnostics(ImmutableArray<RudeEditDiagnostic> diagnostics)
        {
            lock (_guard)
            {
                foreach (var diagnostic in diagnostics)
                {
                    _rudeEdits.Add(((ushort)diagnostic.Kind, diagnostic.SyntaxKind));
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
    }
}

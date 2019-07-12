// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
            public readonly bool HadCompilationErrors;
            public readonly bool HadRudeEdits;
            public readonly bool HadValidChanges;
            public readonly bool HadValidInsignificantChanges;

            public Data(EditSessionTelemetry telemetry)
            {
                RudeEdits = telemetry._rudeEdits.AsImmutable();
                EmitErrorIds = telemetry._emitErrorIds.AsImmutable();
                HadCompilationErrors = telemetry._hadCompilationErrors;
                HadRudeEdits = telemetry._hadRudeEdits;
                HadValidChanges = telemetry._hadValidChanges;
                HadValidInsignificantChanges = telemetry._hadValidInsignificantChanges;
            }

            public bool IsEmpty => !(HadCompilationErrors || HadRudeEdits || HadValidChanges || HadValidInsignificantChanges);
        }

        private readonly object _guard = new object();

        private readonly HashSet<(ushort, ushort)> _rudeEdits;
        private readonly HashSet<string> _emitErrorIds;

        private bool _hadCompilationErrors;
        private bool _hadRudeEdits;
        private bool _hadValidChanges;
        private bool _hadValidInsignificantChanges;

        public EditSessionTelemetry()
        {
            _rudeEdits = new HashSet<(ushort, ushort)>();
            _emitErrorIds = new HashSet<string>();
            _hadCompilationErrors = false;
            _hadRudeEdits = false;
            _hadValidChanges = false;
            _hadValidInsignificantChanges = false;
        }

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
                return data;
            }
        }

        public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, ImmutableArray<string> errorsIds)
        {
            lock (_guard)
            {
                _emitErrorIds.AddRange(errorsIds);

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

        public void LogProjectAnalysisSummary(ProjectAnalysisSummary summary, ImmutableArray<Diagnostic> emitDiagnostics)
            => LogProjectAnalysisSummary(summary, emitDiagnostics.SelectAsArray(d => d.Severity == DiagnosticSeverity.Error, d => d.Id));

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
    }
}

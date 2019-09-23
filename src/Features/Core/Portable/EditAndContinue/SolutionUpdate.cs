// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct SolutionUpdate
    {
        public readonly SolutionUpdateStatus Summary;
        public readonly ImmutableArray<Deltas> Deltas;
        public readonly ImmutableArray<IDisposable> ModuleReaders;
        public readonly ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> EmitBaselines;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> Diagnostics;

        public SolutionUpdate(
            SolutionUpdateStatus summary,
            ImmutableArray<Deltas> deltas,
            ImmutableArray<IDisposable> moduleReaders,
            ImmutableArray<(ProjectId, EmitBaseline)> emitBaselines,
            ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnostics)
        {
            Summary = summary;
            Deltas = deltas;
            EmitBaselines = emitBaselines;
            ModuleReaders = moduleReaders;
            Diagnostics = diagnostics;
        }

        public static SolutionUpdate Blocked()
            => Blocked(ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)>.Empty);

        public static SolutionUpdate Blocked(ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnostics) => new SolutionUpdate(
            SolutionUpdateStatus.Blocked,
            ImmutableArray<Deltas>.Empty,
            ImmutableArray<IDisposable>.Empty,
            ImmutableArray<(ProjectId, EmitBaseline)>.Empty,
            diagnostics);
    }
}

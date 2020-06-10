// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class PendingSolutionUpdate
    {
        public readonly Solution Solution;
        public readonly ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> EmitBaselines;
        public readonly ImmutableArray<Deltas> Deltas;
        public readonly ImmutableArray<IDisposable> ModuleReaders;

        public PendingSolutionUpdate(
            Solution solution,
            ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> emitBaselines,
            ImmutableArray<Deltas> deltas,
            ImmutableArray<IDisposable> moduleReaders)
        {
            Solution = solution;
            EmitBaselines = emitBaselines;
            Deltas = deltas;
            ModuleReaders = moduleReaders;
        }
    }
}

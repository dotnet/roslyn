// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        public readonly ImmutableArray<Document> ChangedDocuments;

        public PendingSolutionUpdate(
            Solution solution,
            ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> emitBaselines,
            ImmutableArray<Deltas> deltas,
            ImmutableArray<IDisposable> moduleReaders,
            ImmutableArray<Document> changedDocuments)
        {
            Solution = solution;
            EmitBaselines = emitBaselines;
            Deltas = deltas;
            ModuleReaders = moduleReaders;
            ChangedDocuments = changedDocuments;
        }
    }
}

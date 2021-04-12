﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct SolutionUpdate
    {
        public readonly ManagedModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions;
        public readonly ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> EmitBaselines;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> Diagnostics;
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> DocumentsWithRudeEdits;

        public SolutionUpdate(
            ManagedModuleUpdates moduleUpdates,
            ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
            ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> emitBaselines,
            ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnostics,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits)
        {
            ModuleUpdates = moduleUpdates;
            NonRemappableRegions = nonRemappableRegions;
            EmitBaselines = emitBaselines;
            Diagnostics = diagnostics;
            DocumentsWithRudeEdits = documentsWithRudeEdits;
        }

        public static SolutionUpdate Blocked(
            ImmutableArray<(ProjectId, ImmutableArray<Diagnostic>)> diagnostics,
            ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic>)> documentsWithRudeEdits)
            => new(
                new(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty),
                ImmutableArray<(Guid, ImmutableArray<(ManagedModuleMethodId, NonRemappableRegion)>)>.Empty,
                ImmutableArray<(ProjectId, EmitBaseline)>.Empty,
                diagnostics,
                documentsWithRudeEdits);
    }
}

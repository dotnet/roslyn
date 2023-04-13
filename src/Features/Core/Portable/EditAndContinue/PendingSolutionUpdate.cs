// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class PendingSolutionUpdate
    {
        public readonly Solution Solution;
        public readonly ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> EmitBaselines;
        public readonly ImmutableArray<VisualStudio.Debugger.Contracts.HotReload.ManagedHotReloadUpdate> Deltas;
        public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> Regions)> NonRemappableRegions;

        public PendingSolutionUpdate(
            Solution solution,
            ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> emitBaselines,
            ImmutableArray<VisualStudio.Debugger.Contracts.HotReload.ManagedHotReloadUpdate> deltas,
            ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions)
        {
            Solution = solution;
            EmitBaselines = emitBaselines;
            Deltas = deltas;
            NonRemappableRegions = nonRemappableRegions;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal abstract class PendingUpdate
    {
        public readonly ImmutableArray<ProjectBaseline> ProjectBaselines;
        public readonly ImmutableArray<ManagedHotReloadUpdate> Deltas;

        public PendingUpdate(
            ImmutableArray<ProjectBaseline> projectBaselines,
            ImmutableArray<ManagedHotReloadUpdate> deltas)
        {
            ProjectBaselines = projectBaselines;
            Deltas = deltas;
        }
    }

    internal sealed class PendingSolutionUpdate : PendingUpdate
    {
        public readonly Solution Solution;
        public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> Regions)> NonRemappableRegions;

        public PendingSolutionUpdate(
            Solution solution,
            ImmutableArray<ProjectBaseline> projectBaselines,
            ImmutableArray<ManagedHotReloadUpdate> deltas,
            ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions)
            : base(projectBaselines, deltas)
        {
            Solution = solution;
            NonRemappableRegions = nonRemappableRegions;
        }
    }
}

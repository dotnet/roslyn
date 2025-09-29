// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class PendingUpdate(
    ImmutableArray<ProjectBaseline> projectBaselines,
    ImmutableArray<ManagedHotReloadUpdate> deltas)
{
    public readonly ImmutableArray<ProjectBaseline> ProjectBaselines = projectBaselines;
    public readonly ImmutableArray<ManagedHotReloadUpdate> Deltas = deltas;
}

internal sealed class PendingSolutionUpdate(
    Solution solution,
    ImmutableDictionary<ProjectId, Guid> staleProjects,
    ImmutableArray<ProjectId> projectsToRebuild,
    ImmutableArray<ProjectBaseline> projectBaselines,
    ImmutableArray<ManagedHotReloadUpdate> deltas,
    ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions) : PendingUpdate(projectBaselines, deltas)
{
    public readonly Solution Solution = solution;
    public readonly ImmutableDictionary<ProjectId, Guid> StaleProjects = staleProjects;
    public readonly ImmutableArray<ProjectId> ProjectsToRebuild = projectsToRebuild;
    public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> Regions)> NonRemappableRegions = nonRemappableRegions;
}

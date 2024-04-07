// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a 'cone' of projects that is being sync'ed between the local and remote hosts.  A project cone starts
/// with a <see cref="RootProjectId"/>, and contains both it and all dependent projects within <see cref="_projectIds"/>.
/// </summary>
internal sealed class ProjectCone : IEquatable<ProjectCone>
{
    public readonly ProjectId RootProjectId;
    private readonly FrozenSet<ProjectId> _projectIds;

    public ProjectCone(ProjectId rootProjectId, FrozenSet<ProjectId> projectIds)
    {
        Contract.ThrowIfFalse(projectIds.Contains(rootProjectId));
        RootProjectId = rootProjectId;
        _projectIds = projectIds;
    }

    public bool Contains(ProjectId projectId)
        => _projectIds.Contains(projectId);

    public override bool Equals(object? obj)
        => obj is ProjectCone cone && Equals(cone);

    public bool Equals(ProjectCone? other)
        => other is not null && this.RootProjectId == other.RootProjectId && this._projectIds.SetEquals(other._projectIds);

    public override int GetHashCode()
        => throw new NotImplementedException();
}

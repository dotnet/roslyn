// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public sealed class ProjectReference : IEquatable<ProjectReference>
{
    private readonly bool _embedInteropTypes;

    public ProjectReference(ProjectId projectId, ImmutableArray<string> aliases = default, bool embedInteropTypes = false)
    {
        Contract.ThrowIfNull(projectId);

        ProjectId = projectId;
        Aliases = aliases.NullToEmpty();
        _embedInteropTypes = embedInteropTypes;
    }

    public ProjectId ProjectId { get; }

    /// <summary>
    /// Aliases for the reference. Empty if the reference has no aliases.
    /// </summary>
    public ImmutableArray<string> Aliases { get; }

    /// <summary>
    /// True if interop types defined in the referenced project should be embedded into the referencing project.
    /// </summary>
    public bool EmbedInteropTypes => _embedInteropTypes;

    public override bool Equals(object obj)
        => this.Equals(obj as ProjectReference);

    public bool Equals(ProjectReference reference)
    {
        if (ReferenceEquals(this, reference))
        {
            return true;
        }

        return reference is object &&
               ProjectId == reference.ProjectId &&
               Aliases.SequenceEqual(reference.Aliases) &&
               _embedInteropTypes == reference._embedInteropTypes;
    }

    public static bool operator ==(ProjectReference left, ProjectReference right)
        => EqualityComparer<ProjectReference>.Default.Equals(left, right);

    public static bool operator !=(ProjectReference left, ProjectReference right)
        => !(left == right);

    public override int GetHashCode()
        => Hash.CombineValues(Aliases, Hash.Combine(ProjectId, _embedInteropTypes.GetHashCode()));

    private string GetDebuggerDisplay()
        => ProjectId.ToString();
}

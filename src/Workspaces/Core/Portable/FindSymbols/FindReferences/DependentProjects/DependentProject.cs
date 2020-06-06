// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.DependentProjects
{
    internal readonly struct DependentProject : IEquatable<DependentProject>
    {
        public readonly ProjectId ProjectId;
        public readonly bool HasInternalsAccess;

        public DependentProject(ProjectId dependentProjectId, bool hasInternalsAccess)
        {
            this.ProjectId = dependentProjectId;
            this.HasInternalsAccess = hasInternalsAccess;
        }

        public override bool Equals(object? obj)
            => obj is DependentProject project && this.Equals(project);

        public override int GetHashCode()
            => Hash.Combine(HasInternalsAccess, ProjectId.GetHashCode());

        public bool Equals(DependentProject other)
            => HasInternalsAccess == other.HasInternalsAccess && ProjectId.Equals(other.ProjectId);
    }
}

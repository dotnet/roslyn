// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.DependentProjects
{
    /// <summary>
    /// The name of the assembly, and optionally also the project, that a symbol came from. Used in the <see
    /// cref="DependentProjectsFinder"/> to quickly find which set of projects in a solution should searched when
    /// looking at symbols from that same location.
    /// </summary>
    internal readonly struct DefinitionProject : IEquatable<DefinitionProject>
    {
        private readonly ProjectId? _sourceProjectId;
        private readonly string _assemblyName;

        public DefinitionProject(ProjectId? sourceProjectId, string assemblyName)
        {
            _sourceProjectId = sourceProjectId;
            _assemblyName = assemblyName;
        }

        public override bool Equals(object? obj)
            => obj is DefinitionProject project && Equals(project);

        public bool Equals(DefinitionProject other)
            => EqualityComparer<ProjectId?>.Default.Equals(_sourceProjectId, other._sourceProjectId) &&
               _assemblyName == other._assemblyName;

        public override int GetHashCode()
            => Hash.Combine(_sourceProjectId, _assemblyName.GetHashCode());
    }
}

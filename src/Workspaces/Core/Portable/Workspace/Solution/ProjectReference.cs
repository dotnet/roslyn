// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public sealed class ProjectReference : IEquatable<ProjectReference>
    {
        private readonly ProjectId _projectId;
        private readonly ImmutableArray<string> _aliases;
        private readonly bool _embedInteropTypes;

        public ProjectReference(ProjectId projectId, ImmutableArray<string> aliases = default, bool embedInteropTypes = false)
        {
            Contract.ThrowIfNull(projectId);

            _projectId = projectId;
            _aliases = aliases.NullToEmpty();
            _embedInteropTypes = embedInteropTypes;
        }

        public ProjectId ProjectId => _projectId;

        /// <summary>
        /// Aliases for the reference. Empty if the reference has no aliases.
        /// </summary>
        public ImmutableArray<string> Aliases => _aliases;

        /// <summary>
        /// True if interop types defined in the referenced project should be embedded into the referencing project.
        /// </summary>
        public bool EmbedInteropTypes => _embedInteropTypes;

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ProjectReference);
        }

        public bool Equals(ProjectReference reference)
        {
            if (ReferenceEquals(this, reference))
            {
                return true;
            }

            return !ReferenceEquals(reference, null) &&
                   _projectId == reference._projectId &&
                   _aliases.SequenceEqual(reference._aliases) &&
                   _embedInteropTypes == reference._embedInteropTypes;
        }

        public static bool operator ==(ProjectReference left, ProjectReference right)
        {
            return EqualityComparer<ProjectReference>.Default.Equals(left, right);
        }

        public static bool operator !=(ProjectReference left, ProjectReference right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Hash.CombineValues(_aliases, Hash.Combine(_projectId, _embedInteropTypes.GetHashCode()));
        }

        private string GetDebuggerDisplay()
        {
            return _projectId.ToString();
        }
    }
}

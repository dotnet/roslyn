// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public sealed class ProjectReference : IEquatable<ProjectReference>
    {
        private readonly ProjectId projectId;
        private readonly ImmutableArray<string> aliases;
        private readonly bool embedInteropTypes;

        public ProjectReference(ProjectId projectId, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false)
        {
            Contract.ThrowIfNull(projectId);
            this.projectId = projectId;
            this.aliases = aliases;
            this.embedInteropTypes = embedInteropTypes;
        }

        public ProjectId ProjectId { get { return projectId; } }
        public ImmutableArray<string> Aliases { get { return aliases; } }
        public bool EmbedInteropTypes { get { return embedInteropTypes; } }

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
                   this.ProjectId == reference.ProjectId &&
                   this.Aliases.NullToEmpty().SequenceEqual(reference.Aliases.NullToEmpty()) &&
                   this.EmbedInteropTypes == reference.EmbedInteropTypes;
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
            return Hash.CombineValues(aliases, Hash.Combine(projectId, embedInteropTypes.GetHashCode()));
        }

        private string GetDebuggerDisplay()
        {
            return this.projectId.ToString();
        }
    }
}

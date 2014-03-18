// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ProjectReference : IEquatable<ProjectReference>
    {
        private readonly ProjectId projectId;
        private readonly string alias;
        private readonly bool embedInteropTypes;

        public ProjectReference(ProjectId projectId, string alias = null, bool embedInteropTypes = false)
        {
            Contract.ThrowIfNull(projectId);
            this.projectId = projectId;
            this.alias = alias;
            this.embedInteropTypes = embedInteropTypes;
        }

        public ProjectId ProjectId { get { return projectId; } }
        public string Alias { get { return alias; } }
        public bool EmbedInteropTypes { get { return embedInteropTypes; } }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ProjectReference);
        }

        public bool Equals(ProjectReference reference)
        {
            return !ReferenceEquals(reference, null) &&
                   this.ProjectId == reference.ProjectId &&
                   this.Alias == reference.Alias &&
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
            return Hash.Combine(alias, Hash.Combine(projectId, embedInteropTypes.GetHashCode()));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return this.projectId.ToString();
            }
        }
    }
}

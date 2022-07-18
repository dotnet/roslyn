// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal readonly struct DiagnosticBucket
    {
        /// <summary>
        /// The identity of bucket group. 
        /// </summary>
        public readonly object Id;

        /// <summary>
        /// <see cref="Workspace"/> this bucket is associated with.
        /// </summary>
        public readonly Workspace Workspace;

        /// <summary>
        /// <see cref="ProjectId"/> this bucket is associated with, or <see langword="null"/>.
        /// </summary>
        public readonly ProjectId? ProjectId;

        /// <summary>
        /// <see cref="DocumentId"/> this bucket is associated with, or <see langword="null"/>.
        /// </summary>
        public readonly DocumentId? DocumentId;

        public DiagnosticBucket(object id, Workspace workspace, ProjectId? projectId, DocumentId? documentId)
        {
            Id = id;
            Workspace = workspace;
            ProjectId = projectId;
            DocumentId = documentId;
        }
    }
}

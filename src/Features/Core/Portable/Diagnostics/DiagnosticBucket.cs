// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal readonly struct DiagnosticBucket(object id, Workspace workspace, ProjectId? projectId, DocumentId? documentId)
    {
        /// <summary>
        /// The identity of bucket group. 
        /// </summary>
        public readonly object Id = id;

        /// <summary>
        /// <see cref="Workspace"/> this bucket is associated with.
        /// </summary>
        public readonly Workspace Workspace = workspace;

        /// <summary>
        /// <see cref="ProjectId"/> this bucket is associated with, or <see langword="null"/>.
        /// </summary>
        public readonly ProjectId? ProjectId = projectId;

        /// <summary>
        /// <see cref="DocumentId"/> this bucket is associated with, or <see langword="null"/>.
        /// </summary>
        public readonly DocumentId? DocumentId = documentId;
    }
}

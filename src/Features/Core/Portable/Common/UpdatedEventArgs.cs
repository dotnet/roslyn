// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Common
{
    internal class UpdatedEventArgs(object id, Workspace workspace, ProjectId? projectId, DocumentId? documentId) : EventArgs
    {
        /// <summary>
        /// The identity of update group. 
        /// </summary>
        public object Id { get; } = id;

        /// <summary>
        /// <see cref="Workspace"/> this update is associated with.
        /// </summary>
        public Workspace Workspace { get; } = workspace;

        /// <summary>
        /// <see cref="ProjectId"/> this update is associated with, or <see langword="null"/>.
        /// </summary>
        public ProjectId? ProjectId { get; } = projectId;

        /// <summary>
        /// <see cref="DocumentId"/> this update is associated with, or <see langword="null"/>.
        /// </summary>
        public DocumentId? DocumentId { get; } = documentId;
    }
}

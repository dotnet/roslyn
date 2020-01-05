// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Common
{
    internal class UpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The identity of update group. 
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// <see cref="Workspace"/> this update is associated with.
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        /// <see cref="ProjectId"/> this update is associated with, or <see langword="null"/>.
        /// </summary>
        public ProjectId? ProjectId { get; }

        /// <summary>
        /// <see cref="DocumentId"/> this update is associated with, or <see langword="null"/>.
        /// </summary>
        public DocumentId? DocumentId { get; }

        public UpdatedEventArgs(object id, Workspace workspace, ProjectId? projectId, DocumentId? documentId)
        {
            Id = id;
            Workspace = workspace;
            ProjectId = projectId;
            DocumentId = documentId;
        }
    }
}

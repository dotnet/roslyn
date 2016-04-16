// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Workspace this update is associated with
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        /// projectId this update is associated with
        /// </summary>
        public ProjectId ProjectId { get; }

        /// <summary>
        /// documentId this update is associated with
        /// </summary>
        public DocumentId DocumentId { get; }

        public UpdatedEventArgs(object id, Workspace workspace, ProjectId projectId, DocumentId documentId)
        {
            this.Id = id;
            this.Workspace = workspace;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
        }
    }
}

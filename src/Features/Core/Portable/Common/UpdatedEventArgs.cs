// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Common
{
    internal class UpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The identity of update group. 
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Optional name of the build tool. 
        /// </summary>
        public string BuildTool { get; }

        /// <summary>
        /// Workspace this update is associated with
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        /// Id of the project this update is associated with.
        /// </summary>
        public ProjectId ProjectId { get; }

        /// <summary>
        /// Id of the document this update is associated with.
        /// </summary>
        public DocumentId DocumentId { get; }

        public UpdatedEventArgs(object id, Workspace workspace, ProjectId projectId, DocumentId documentId, string buildTool)
        {
            Debug.Assert(id != null);
            Debug.Assert(workspace != null);

            Id = id;
            Workspace = workspace;
            ProjectId = projectId;
            DocumentId = documentId;
            BuildTool = buildTool;
        }
    }
}

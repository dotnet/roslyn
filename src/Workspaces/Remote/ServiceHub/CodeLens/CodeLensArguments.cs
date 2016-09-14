// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class CodeLensArguments
    {
        public Guid ProjectIdGuid;
        public string ProjectIdDebugName;
        public Guid DocumentIdGuid;
        public string DocumentIdDebugName;

        public CodeLensArguments()
        {
        }

        public CodeLensArguments(DocumentId documentId)
        {
            ProjectIdGuid = documentId.ProjectId.Id;
            ProjectIdDebugName = documentId.ProjectId.DebugName;
            DocumentIdGuid = documentId.Id;
            DocumentIdDebugName = documentId.DebugName;
        }

        public DocumentId GetDocumentId()
            =>
            DocumentId.CreateFromSerialized(ProjectId.CreateFromSerialized(ProjectIdGuid, ProjectIdDebugName),
                DocumentIdGuid, DocumentIdDebugName);
    }
}
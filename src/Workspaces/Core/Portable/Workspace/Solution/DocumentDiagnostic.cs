// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    public class DocumentDiagnostic : WorkspaceDiagnostic
    {
        public DocumentId DocumentId { get; }

        public DocumentDiagnostic(WorkspaceDiagnosticKind kind, string message, DocumentId documentId)
            : base(kind, message)
        {
            this.DocumentId = documentId;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

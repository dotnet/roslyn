// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal struct ProjectOrDocumentId
{
    public object Id { get; }

    public ProjectOrDocumentId(ProjectId projectId)
    {
        Id = projectId;
    }

    public ProjectOrDocumentId(DocumentId documentId)
    {
        Id = documentId;
    }
}

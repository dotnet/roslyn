// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Wrapper around project and document ids for convenience in caching diagnostic results and
/// use in the <see cref="IDiagnosticSource"/>
/// </summary>
internal readonly record struct ProjectOrDocumentId
{
    /// <summary>
    /// Non-null if this represents a documentId.  Used for equality comparisons.
    /// </summary>
    private readonly DocumentId? _documentId;

    /// <summary>
    /// Non-null if this represents a projectId.  Used for equality comparisons.
    /// </summary>
    private readonly ProjectId? _projectId;

    public ProjectOrDocumentId(ProjectId projectId)
    {
        _projectId = projectId;
        _documentId = null;
    }

    public ProjectOrDocumentId(DocumentId documentId)
    {
        _documentId = documentId;
        _projectId = null;
    }
}

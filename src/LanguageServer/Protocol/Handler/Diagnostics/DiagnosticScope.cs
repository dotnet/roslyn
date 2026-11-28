// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// What an <see cref="IDiagnosticSource"/> reports diagnostics for: a project, a document, or a file by its path. Used to
/// identify the source when caching diagnostic results.
/// </summary>
internal readonly record struct DiagnosticScope
{
    /// <summary>
    /// Non-null if this represents a documentId.  Used for equality comparisons.
    /// </summary>
    private readonly DocumentId? _documentId;

    /// <summary>
    /// Non-null if this represents a projectId.  Used for equality comparisons.
    /// </summary>
    private readonly ProjectId? _projectId;

    /// <summary>
    /// Non-null if this represents a file by its path.  Used for equality comparisons.
    /// </summary>
    private readonly string? _filePath;

    public DiagnosticScope(ProjectId projectId)
    {
        _projectId = projectId;
        _documentId = null;
        _filePath = null;
    }

    public DiagnosticScope(DocumentId documentId)
    {
        _documentId = documentId;
        _projectId = null;
        _filePath = null;
    }

    /// <summary>
    /// Creates an id for a file that a source reports diagnostics for, whether or not the file is a project or document in the
    /// workspace.
    /// </summary>
    public DiagnosticScope(string filePath)
    {
        _filePath = filePath;
        _documentId = null;
        _projectId = null;
    }

    public string? FilePath => _filePath;
}

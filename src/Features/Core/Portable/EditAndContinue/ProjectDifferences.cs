// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Differences between documents of old and new projects.
/// </summary>
internal sealed class ProjectDifferences() : IDisposable
{
    public readonly ArrayBuilder<Document> ChangedOrAddedDocuments = ArrayBuilder<Document>.GetInstance();
    public readonly ArrayBuilder<Document> DeletedDocuments = ArrayBuilder<Document>.GetInstance();

    /// <summary>
    /// Projects differ in compilation options, parse options, or other project attributes.
    /// </summary>
    public bool HasSettingChange { get; set; }

    /// <summary>
    /// Projects differ in project or metadata references.
    /// </summary>
    public bool HasReferenceChange { get; set; }

    public void Dispose()
    {
        ChangedOrAddedDocuments.Free();
        DeletedDocuments.Free();
    }

    public bool HasDocumentChanges
        => !ChangedOrAddedDocuments.IsEmpty || !DeletedDocuments.IsEmpty;

    public bool Any()
        => HasDocumentChanges || HasSettingChange || HasReferenceChange;

    public bool IsEmpty
        => !Any();

    public void Clear()
    {
        ChangedOrAddedDocuments.Clear();
        DeletedDocuments.Clear();
    }

    public void Log(TraceLog log, Project newProject)
    {
        if (HasDocumentChanges)
        {
            log.Write($"Found {ChangedOrAddedDocuments.Count} potentially changed, {DeletedDocuments.Count} deleted document(s) in project {newProject.GetLogDisplay()}");
        }

        if (HasReferenceChange)
        {
            log.Write($"References of project {newProject.GetLogDisplay()} changed");
        }

        if (HasSettingChange)
        {
            log.Write($"Settings of project {newProject.GetLogDisplay()} changed");
        }
    }
}

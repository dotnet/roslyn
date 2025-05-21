// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Differences between documents of old and new projects.
/// </summary>
internal readonly struct ProjectDifferences() : IDisposable
{
    public readonly ArrayBuilder<Document> ChangedOrAddedDocuments = ArrayBuilder<Document>.GetInstance();
    public readonly ArrayBuilder<Document> DeletedDocuments = ArrayBuilder<Document>.GetInstance();
    public readonly ArrayBuilder<MetadataResourceInfo> ChangedOrAddedResources = ArrayBuilder<MetadataResourceInfo>.GetInstance();
    public readonly ArrayBuilder<MetadataResourceInfo> DeletedResources = ArrayBuilder<MetadataResourceInfo>.GetInstance();

    public void Dispose()
    {
        ChangedOrAddedDocuments.Free();
        DeletedDocuments.Free();
        ChangedOrAddedResources.Free();
        DeletedResources.Free();
    }

    public bool HasResourceChanges
        => !ChangedOrAddedResources.IsEmpty || !DeletedResources.IsEmpty;

    public bool HasDocumentChanges
        => !ChangedOrAddedDocuments.IsEmpty || !DeletedDocuments.IsEmpty;

    public bool Any()
        => HasDocumentChanges || HasResourceChanges;

    public bool IsEmpty
        => !Any();

    public void Clear()
    {
        ChangedOrAddedDocuments.Clear();
        DeletedDocuments.Clear();
        ChangedOrAddedResources.Clear();
        DeletedResources.Clear();
    }
}

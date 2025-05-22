// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Differences between documents of old and new projects.
/// </summary>
internal readonly struct ProjectDocumentDifferences() : IDisposable
{
    public readonly ArrayBuilder<Document> ChangedOrAdded = ArrayBuilder<Document>.GetInstance();
    public readonly ArrayBuilder<Document> Deleted = ArrayBuilder<Document>.GetInstance();

    public void Dispose()
    {
        ChangedOrAdded.Free();
        Deleted.Free();
    }

    public bool IsEmpty
        => ChangedOrAdded.IsEmpty && Deleted.IsEmpty;

    public bool Any()
        => !IsEmpty;

    public void Clear()
    {
        ChangedOrAdded.Clear();
        Deleted.Clear();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ResxSelection;

/// <summary>
/// Contains information about a .resx file and its contents.
/// </summary>
internal sealed record ResxFileInfo
{
    public string FilePath { get; }
    public string RelativePathFromDocument { get; }
    public ImmutableArray<ResxEntry> ExistingEntries { get; }
    public string? Namespace { get; }
    public DateTime LastModified { get; }
    
    public ResxFileInfo(
        string filePath, 
        string relativePathFromDocument,
        ImmutableArray<ResxEntry> existingEntries,
        string? nameSpace = null,
        DateTime lastModified = default)
    {
        FilePath = filePath;
        RelativePathFromDocument = relativePathFromDocument;
        ExistingEntries = existingEntries;
        Namespace = nameSpace;
        LastModified = lastModified == default ? DateTime.UtcNow : lastModified;
    }
}

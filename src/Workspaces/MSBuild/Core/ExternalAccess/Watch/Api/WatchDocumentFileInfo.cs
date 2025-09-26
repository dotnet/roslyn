// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

/// <summary>
/// Represents a source file that is part of a project file.
/// </summary>
internal readonly struct WatchDocumentFileInfo
{
    internal DocumentFileInfo UnderlyingObject { get; }

    internal WatchDocumentFileInfo(DocumentFileInfo underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchDocumentFileInfo(string filePath, string logicalPath, bool isLinked, bool isGenerated, ImmutableArray<string> folders)
        : this(new(filePath, logicalPath, isLinked, isGenerated, folders))
    {
    }

    /// <summary>
    /// The absolute path to the document file on disk.
    /// </summary>
    public string FilePath => UnderlyingObject.FilePath;

    /// <summary>
    /// A fictional path to the document, relative to the project.
    /// The document may not actually exist at this location, and is used
    /// to represent linked documents. This includes the file name.
    /// </summary>
    public string LogicalPath => UnderlyingObject.LogicalPath;

    /// <summary>
    /// True if the document has a logical path that differs from its 
    /// absolute file path.
    /// </summary>
    public bool IsLinked => UnderlyingObject.IsLinked;

    /// <summary>
    /// True if the file was generated during build.
    /// </summary>
    public bool IsGenerated => UnderlyingObject.IsGenerated;

    /// <summary>
    /// Containing folders of the document relative to the containing project root path.
    /// </summary>
    public ImmutableArray<string> Folders => UnderlyingObject.Folders;
}

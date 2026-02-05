// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Represents a source file that is part of a project file.
/// </summary>
[DataContract]
internal sealed class DocumentFileInfo(string filePath, string logicalPath, bool isLinked, bool isGenerated, string[] folders)
{
    /// <summary>
    /// The absolute path to the document file on disk.
    /// </summary>
    [DataMember(Order = 0)]
    public string FilePath { get; } = filePath;

    /// <summary>
    /// A fictional path to the document, relative to the project.
    /// The document may not actually exist at this location, and is used
    /// to represent linked documents. This includes the file name.
    /// </summary>
    [DataMember(Order = 1)]
    public string LogicalPath { get; } = logicalPath;

    /// <summary>
    /// True if the document has a logical path that differs from its 
    /// absolute file path.
    /// </summary>
    [DataMember(Order = 2)]
    public bool IsLinked { get; } = isLinked;

    /// <summary>
    /// True if the file was generated during build.
    /// </summary>
    [DataMember(Order = 3)]
    public bool IsGenerated { get; } = isGenerated;

    /// <summary>
    /// Containing folders of the document relative to the containing project root path.
    /// </summary>
    [DataMember(Order = 4)]
    public string[] Folders { get; } = folders;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a source file that is part of a project file.
    /// </summary>
    internal sealed class DocumentFileInfo(string filePath, string logicalPath, bool isLinked, bool isGenerated, SourceCodeKind sourceCodeKind, ImmutableArray<string> folders)
    {
        /// <summary>
        /// The absolute path to the document file on disk.
        /// </summary>
        public string FilePath { get; } = filePath;

        /// <summary>
        /// A fictional path to the document, relative to the project.
        /// The document may not actually exist at this location, and is used
        /// to represent linked documents. This includes the file name.
        /// </summary>
        public string LogicalPath { get; } = logicalPath;

        /// <summary>
        /// True if the document has a logical path that differs from its 
        /// absolute file path.
        /// </summary>
        public bool IsLinked { get; } = isLinked;

        /// <summary>
        /// True if the file was generated during build.
        /// </summary>
        public bool IsGenerated { get; } = isGenerated;

        /// <summary>
        /// The <see cref="SourceCodeKind"/> of this document.
        /// </summary>
        public SourceCodeKind SourceCodeKind { get; } = sourceCodeKind;

        /// <summary>
        /// Containing folders of the document relative to the containing project root path.
        /// </summary>
        public ImmutableArray<string> Folders { get; } = folders;
    }
}

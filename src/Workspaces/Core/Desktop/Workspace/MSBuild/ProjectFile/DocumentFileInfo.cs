// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a source file that is part of a project file.
    /// </summary>
    internal sealed class DocumentFileInfo
    {
        /// <summary>
        /// The absolute path to the document file on disk.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// A fictional path to the document, relative to the project.
        /// The document may not actually exist at this location, and is used
        /// to represent linked documents. This includes the file name.
        /// </summary>
        public string LogicalPath { get; }

        /// <summary>
        /// True if the document has a logical path that differs from its 
        /// absolute file path.
        /// </summary>
        public bool IsLinked { get; }

        /// <summary>
        /// True if the file was generated during build.
        /// </summary>
        public bool IsGenerated { get; }

        public DocumentFileInfo(string filePath, string logicalPath, bool isLinked, bool isGenerated)
        {
            this.FilePath = filePath;
            this.LogicalPath = logicalPath;
            this.IsLinked = isLinked;
            this.IsGenerated = isGenerated;
        }
    }
}

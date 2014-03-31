// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to XML files specified in the source.
    /// </summary>
    public class XmlFileResolver : XmlReferenceResolver
    {
        public static readonly XmlFileResolver Default = new XmlFileResolver(baseDirectory: null);

        private readonly string baseDirectory;

        public XmlFileResolver(string baseDirectory)
        {
            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            this.baseDirectory = baseDirectory;
        }

        public string BaseDirectory
        {
            get { return baseDirectory; }
        }

        /// <summary>
        /// Resolves XML document file path.
        /// </summary>
        /// <param name="path">
        /// Value of the "file" attribute of an &lt;include&gt; documentation comment element.
        /// </param>
        /// <param name="baseFilePath">
        /// Path of the source file (<see cref="SyntaxTree.FilePath"/>) or XML document that contains the <paramref name="path"/>.
        /// If not null used as a base path of <paramref name="path"/>, if <paramref name="path"/> is relative.
        /// If <paramref name="baseFilePath"/> is relative <see cref="BaseDirectory"/> is used as the base path of <paramref name="baseFilePath"/>.
        /// </param>
        /// <returns>Normalized XML document file path or null if not found.</returns>
        public override string ResolveReference(string path, string baseFilePath)
        {
            // Dev11: first look relative to the directory containing the file with the <include> element (baseFilepath)
            // and then look look in the base directory (i.e. current working directory of the compiler).

            string resolvedPath;

            if (baseFilePath != null)
            {
                resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, baseDirectory);
                Debug.Assert(resolvedPath == null || PathUtilities.IsAbsolute(resolvedPath));
                if (FileExists(resolvedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
                }
            }

            if (baseDirectory != null)
            {
                resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                Debug.Assert(resolvedPath == null || PathUtilities.IsAbsolute(resolvedPath));
                if (FileExists(resolvedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
                }
            }

            return null;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(resolvedPath, "resolvedPath");
            return FileUtilities.OpenRead(resolvedPath);
        }

        protected virtual bool FileExists(string resolvedPath)
        {
            return File.Exists(resolvedPath);
        }
    }
}

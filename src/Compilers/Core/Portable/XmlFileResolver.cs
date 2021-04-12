// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to XML files specified in the source.
    /// </summary>
    public class XmlFileResolver : XmlReferenceResolver
    {
        public static XmlFileResolver Default { get; } = new XmlFileResolver(baseDirectory: null);

        private readonly string? _baseDirectory;

        public XmlFileResolver(string? baseDirectory)
        {
            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, nameof(baseDirectory));
            }

            _baseDirectory = baseDirectory;
        }

        public string? BaseDirectory
        {
            get { return _baseDirectory; }
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
        public override string? ResolveReference(string path, string? baseFilePath)
        {
            // Dev11: first look relative to the directory containing the file with the <include> element (baseFilepath)
            // and then look in the base directory (i.e. current working directory of the compiler).

            string? resolvedPath;

            if (baseFilePath != null)
            {
                resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, _baseDirectory);
                Debug.Assert(resolvedPath == null || PathUtilities.IsAbsolute(resolvedPath));
                if (FileExists(resolvedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
                }
            }

            if (_baseDirectory != null)
            {
                resolvedPath = FileUtilities.ResolveRelativePath(path, _baseDirectory);
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
            CompilerPathUtilities.RequireAbsolutePath(resolvedPath, nameof(resolvedPath));
            return FileUtilities.OpenRead(resolvedPath);
        }

        protected virtual bool FileExists([NotNullWhen(true)] string? resolvedPath)
        {
            return File.Exists(resolvedPath);
        }

        public override bool Equals(object? obj)
        {
            // Explicitly check that we're not comparing against a derived type
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (XmlFileResolver)obj;
            return string.Equals(_baseDirectory, other._baseDirectory, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return _baseDirectory != null ? StringComparer.Ordinal.GetHashCode(_baseDirectory) : 0;
        }
    }
}

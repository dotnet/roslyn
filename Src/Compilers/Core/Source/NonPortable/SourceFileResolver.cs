// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to source files specified in source code.
    /// </summary>
    public class SourceFileResolver : SourceReferenceResolver
    {
        public static readonly SourceFileResolver Default = new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        private readonly string baseDirectory;
        private readonly ImmutableArray<string> searchPaths;

        public SourceFileResolver(IEnumerable<string> searchPaths, string baseDirectory)
            : this(ImmutableArray.CreateRange(searchPaths), baseDirectory)
        {
        }

        public SourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            this.baseDirectory = baseDirectory;
            this.searchPaths = searchPaths;
        }

        public string BaseDirectory
        {
            get { return baseDirectory; }
        }

        public ImmutableArray<string> SearchPaths
        {
            get { return this.searchPaths; }
        }

        public override string NormalizePath(string path, string baseFilePath)
        {
            return FileUtilities.NormalizeRelativePath(path, baseFilePath, baseDirectory);
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, baseDirectory, searchPaths, FileExists);

            if (!FileExists(resolvedPath))
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
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

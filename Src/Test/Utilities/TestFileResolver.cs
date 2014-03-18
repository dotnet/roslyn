// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// File resolver that doesn't resolve any relative paths. 
    /// Tests should subclass and specify only as much context as necessary to test specific scenarios.
    /// </summary>
    internal abstract class TestFileResolver : FileResolver
    {
        public TestFileResolver()
            : base(assemblySearchPaths: ImmutableArray.Create<string>(),
                   baseDirectory: null)
        {
        }
    }

    internal class MappingReferenceResolver : TestFileResolver
    {
        private readonly Dictionary<string, string> assemblyNames;
        private readonly Dictionary<string, string> files;

        public MappingReferenceResolver(Dictionary<string, string> assemblyNames = null, Dictionary<string, string> files = null)
        {
            this.assemblyNames = assemblyNames;
            this.files = files;
        }

        public override string ResolveAssemblyName(string assemblyName)
        {
            string result;
            return assemblyNames != null && assemblyNames.TryGetValue(assemblyName, out result) ? result : null;
        }

        public override string ResolveMetadataFile(string path, string basePath)
        {
            if (PathUtilities.IsAbsolute(path))
            {
                return path;
            }

            string result;
            return files != null && files.TryGetValue(path, out result) ? result : null;
        }
    }

    public class VirtualizedFileResolver : FileResolver
    {
        private readonly Dictionary<string, string> assemblyNames;
        private readonly HashSet<string> existingFullPaths;

        public VirtualizedFileResolver(
            Dictionary<string, string> assemblyNames = null, 
            IEnumerable<string> existingFullPaths = null, 
            string baseDirectory = null,
            ImmutableArray<string> assemblySearchPaths = default(ImmutableArray<string>))
            : base(assemblySearchPaths.NullToEmpty(), baseDirectory)
        {
            this.assemblyNames = assemblyNames;
            this.existingFullPaths = new HashSet<string>(existingFullPaths, StringComparer.OrdinalIgnoreCase);
        }

        public override string ResolveAssemblyName(string assemblyName)
        {
            string result;
            return assemblyNames != null && assemblyNames.TryGetValue(assemblyName, out result) ? result : null;
        }

        internal override bool FileExists(string fullPath)
        {
            return fullPath != null && existingFullPaths != null && existingFullPaths.Contains(FileUtilities.NormalizeAbsolutePath(fullPath));
        }
    }
}

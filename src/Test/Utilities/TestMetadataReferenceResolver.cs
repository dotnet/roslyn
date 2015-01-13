// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// File resolver that doesn't resolve any relative paths. 
    /// Tests should subclass and specify only as much context as necessary to test specific scenarios.
    /// </summary>
    internal abstract class TestMetadataReferenceResolver : MetadataFileReferenceResolver
    {
        public TestMetadataReferenceResolver()
            : base(searchPaths: ImmutableArray.Create<string>(),
                   baseDirectory: null)
        {
        }
    }

    internal class MappingReferenceResolver : TestMetadataReferenceResolver
    {
        private readonly Dictionary<string, string> assemblyNames;
        private readonly Dictionary<string, string> files;

        public MappingReferenceResolver(Dictionary<string, string> assemblyNames = null, Dictionary<string, string> files = null)
        {
            this.assemblyNames = assemblyNames;
            this.files = files;
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            if (PathUtilities.IsFilePath(reference))
            {
                if (PathUtilities.IsAbsolute(reference))
                {
                    return reference;
                }

                string result;
                return files != null && files.TryGetValue(reference, out result) ? result : null;
            }
            else
            {
                string result;
                return assemblyNames != null && assemblyNames.TryGetValue(reference, out result) ? result : null;
            }
        }
    }

    internal class VirtualizedFileReferenceResolver : MetadataFileReferenceResolver
    {
        private readonly HashSet<string> existingFullPaths;

        public VirtualizedFileReferenceResolver(
            IEnumerable<string> existingFullPaths = null, 
            string baseDirectory = null,
            ImmutableArray<string> searchPaths = default(ImmutableArray<string>))
            : base(searchPaths.NullToEmpty(), baseDirectory)
        {
            this.existingFullPaths = new HashSet<string>(existingFullPaths, StringComparer.OrdinalIgnoreCase);
        }

        protected override bool FileExists(string fullPath)
        {
            return fullPath != null && existingFullPaths != null && existingFullPaths.Contains(FileUtilities.NormalizeAbsolutePath(fullPath));
        }
    }
}

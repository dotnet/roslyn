// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public override string ResolveReference(string reference, string baseFilePath)
        {
            return null;
        }

        public override ImmutableArray<string> SearchPaths
        {
            get { return ImmutableArray<string>.Empty; }
        }

        public override string BaseDirectory
        {
            get { return null; }
        }

        internal override MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            throw new NotImplementedException();
        }

        internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
        {
            throw new NotImplementedException();
        }
    }

    internal class MappingReferenceResolver : TestMetadataReferenceResolver
    {
        private readonly Dictionary<string, string> _assemblyNames;
        private readonly Dictionary<string, string> _files;

        public MappingReferenceResolver(Dictionary<string, string> assemblyNames = null, Dictionary<string, string> files = null)
        {
            _assemblyNames = assemblyNames;
            _files = files;
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
                return _files != null && _files.TryGetValue(reference, out result) ? result : null;
            }
            else
            {
                string result;
                return _assemblyNames != null && _assemblyNames.TryGetValue(reference, out result) ? result : null;
            }
        }
    }

    internal sealed class VirtualizedFileReferenceResolver : MetadataFileReferenceResolver
    {
        private readonly RelativePathReferenceResolver _resolver;
        private readonly HashSet<string> _existingFullPaths;

        public VirtualizedFileReferenceResolver(
            IEnumerable<string> existingFullPaths = null,
            string baseDirectory = null,
            ImmutableArray<string> searchPaths = default(ImmutableArray<string>))
        {
            _resolver = new RelativePathReferenceResolver(searchPaths.NullToEmpty(), baseDirectory);
            _existingFullPaths = new HashSet<string>(existingFullPaths, StringComparer.OrdinalIgnoreCase);
        }

        public override ImmutableArray<string> SearchPaths
        {
            get { return _resolver.SearchPaths; }
        }

        public override string BaseDirectory
        {
            get { return _resolver.BaseDirectory; }
        }

        internal override MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            throw new NotImplementedException();
        }

        internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
        {
            throw new NotImplementedException();
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            var fullPath = _resolver.ResolveReference(reference, baseFilePath);
            if (fullPath != null && _existingFullPaths != null && _existingFullPaths.Contains(FileUtilities.NormalizeAbsolutePath(fullPath)))
            {
                return fullPath;
            }
            return null;
        }
    }
}

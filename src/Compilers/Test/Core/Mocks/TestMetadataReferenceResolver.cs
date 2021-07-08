// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal class TestMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly RelativePathResolver _pathResolver;
        private readonly Dictionary<string, PortableExecutableReference> _assemblyNames;
        private readonly Dictionary<string, PortableExecutableReference> _files;

        public TestMetadataReferenceResolver(
            RelativePathResolver pathResolver = null,
            Dictionary<string, PortableExecutableReference> assemblyNames = null,
            Dictionary<string, PortableExecutableReference> files = null)
        {
            _pathResolver = pathResolver;
            _assemblyNames = assemblyNames ?? new Dictionary<string, PortableExecutableReference>();
            _files = files ?? new Dictionary<string, PortableExecutableReference>();
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            Dictionary<string, PortableExecutableReference> map;

            if (PathUtilities.IsFilePath(reference))
            {
                if (_pathResolver != null)
                {
                    reference = _pathResolver.ResolvePath(reference, baseFilePath);
                    if (reference == null)
                    {
                        return ImmutableArray<PortableExecutableReference>.Empty;
                    }
                }

                map = _files;
            }
            else
            {
                map = _assemblyNames;
            }

            return map.TryGetValue(reference, out var result) ? ImmutableArray.Create(result) : ImmutableArray<PortableExecutableReference>.Empty;
        }

        public override bool Equals(object other) => true;
        public override int GetHashCode() => 1;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal sealed class DesktopMetadataReferenceResolver : MetadataFileReferenceResolver
    {
        private readonly MetadataFileReferenceResolver _pathResolver;
        private readonly NuGetPackageResolver _packageResolver;
        private readonly GacFileResolver _gacFileResolver;

        internal DesktopMetadataReferenceResolver(
            MetadataFileReferenceResolver pathResolver,
            NuGetPackageResolver packageResolver,
            GacFileResolver gacFileResolver)
        {
            _pathResolver = pathResolver;
            _packageResolver = packageResolver;
            _gacFileResolver = gacFileResolver;
        }

        public override ImmutableArray<string> SearchPaths
        {
            get { return _pathResolver.SearchPaths; }
        }

        public override string BaseDirectory
        {
            get { return _pathResolver.BaseDirectory; }
        }

        internal override MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            return new DesktopMetadataReferenceResolver(_pathResolver.WithSearchPaths(searchPaths), _packageResolver, _gacFileResolver);
        }

        internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
        {
            return new DesktopMetadataReferenceResolver(_pathResolver.WithBaseDirectory(baseDirectory), _packageResolver, _gacFileResolver);
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            if (PathUtilities.IsFilePath(reference))
            {
                return _pathResolver.ResolveReference(reference, baseFilePath);
            }

            if (_packageResolver != null)
            {
                string path = _packageResolver.ResolveNuGetPackage(reference);
                if (path != null && PortableShim.File.Exists(path))
                {
                    return path;
                }
            }

            if (_gacFileResolver != null)
            {
                return _gacFileResolver.ResolveReference(reference);
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            var other = obj as DesktopMetadataReferenceResolver;
            return (other != null) &&
                object.Equals(_pathResolver, other._pathResolver) &&
                object.Equals(_packageResolver, other._packageResolver) &&
                object.Equals(_gacFileResolver, other._gacFileResolver);
        }

        public override int GetHashCode()
        {
            int result = _pathResolver.GetHashCode();
            if (_packageResolver != null)
            {
                result = Hash.Combine(result, _packageResolver.GetHashCode());
            }
            if (_gacFileResolver != null)
            {
                result = Hash.Combine(result, _gacFileResolver.GetHashCode());
            }
            return result;
        }
    }
}

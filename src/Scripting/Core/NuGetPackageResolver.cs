// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal sealed class NuGetPackageResolver : MetadataReferenceResolver
    {
        private readonly MetadataFileReferenceProvider _provider;

        internal NuGetPackageResolver(MetadataFileReferenceProvider provider)
        {
            _provider = provider;
        }

        internal string ResolveNuGetPackage(string reference)
        {
            if (PathUtilities.IsFilePath(reference))
            {
                return null;
            }

            var assemblyName = GetPackageAssemblyName(reference);
            if (assemblyName == null)
            {
                return null;
            }

            // Expecting {package}{version}\lib\{arch}\{package}.dll.
            var resolvedPath = PathUtilities.CombineAbsoluteAndRelativePaths(reference, "lib");
            if (!PortableShim.Directory.Exists(resolvedPath))
            {
                return null;
            }

            // We're not validating the architecture currently
            // so fail if there's not exactly one architecture.
            resolvedPath = PortableShim.Directory.EnumerateDirectories(resolvedPath, "*", PortableShim.SearchOption.TopDirectoryOnly).SingleOrDefault();
            if (resolvedPath == null)
            {
                return null;
            }

            return PathUtilities.CombineAbsoluteAndRelativePaths(resolvedPath, assemblyName);
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            var path = ResolveNuGetPackage(reference);
            var metadata = (path == null) ? null : _provider.GetReference(path, properties);
            return (metadata == null) ?
                ImmutableArray<PortableExecutableReference>.Empty :
                ImmutableArray.Create(metadata);
        }

        public override bool Equals(object obj)
        {
            var other = obj as NuGetPackageResolver;
            return (other != null) && object.Equals(_provider, other._provider);
        }

        public override int GetHashCode()
        {
            return _provider.GetHashCode();
        }

        private static string GetPackageAssemblyName(string reference)
        {
            // Assembly name is <id/> in .nuspec file.
            // For now, simply strip off any version #, etc.
            var name = PathUtilities.GetFileName(reference);
            int offset = 0;
            while ((offset = name.IndexOf('.', offset)) >= 0)
            {
                if ((offset < name.Length - 1) && char.IsDigit(name[offset + 1]))
                {
                    return name.Substring(0, offset) + ".dll";
                }
                offset += 1;
            }
            return null;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Resolves metadata references for scripts.
    /// </summary>
    /// <remarks>
    /// Operates on runtime metadata artifacts.
    /// </remarks>
    internal sealed class RuntimeMetadataReferenceResolver : MetadataReferenceResolver, IEquatable<RuntimeMetadataReferenceResolver>
    {
        public static readonly RuntimeMetadataReferenceResolver Default = new RuntimeMetadataReferenceResolver();

        internal readonly RelativePathResolver PathResolver;
        internal readonly NuGetPackageResolver PackageResolver;
        internal readonly GacFileResolver GacFileResolver;
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _fileReferenceProvider;

        internal RuntimeMetadataReferenceResolver(
            ImmutableArray<string> searchPaths = default(ImmutableArray<string>),
            string baseDirectory = null)
            : this(new RelativePathResolver(searchPaths.NullToEmpty(), baseDirectory),
                   null,
                   GacFileResolver.Default)
        {
        }

        internal RuntimeMetadataReferenceResolver(
            RelativePathResolver pathResolver,
            NuGetPackageResolver packageResolver,
            GacFileResolver gacFileResolver,
            Func<string, MetadataReferenceProperties, PortableExecutableReference> fileReferenceProvider = null)
        {
            PathResolver = pathResolver;
            PackageResolver = packageResolver;
            GacFileResolver = gacFileResolver;
            _fileReferenceProvider = fileReferenceProvider ?? 
                new Func<string, MetadataReferenceProperties, PortableExecutableReference>((path, properties) => MetadataReference.CreateFromFile(path, properties));
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            if (PathResolver != null && PathUtilities.IsFilePath(reference))
            {
                var resolvedPath = PathResolver.ResolvePath(reference, baseFilePath);
                if (resolvedPath == null)
                {
                    return ImmutableArray<PortableExecutableReference>.Empty;
                }

                return ImmutableArray.Create(_fileReferenceProvider(resolvedPath, properties));
            }

            if (PackageResolver != null)
            {
                var paths = PackageResolver.ResolveNuGetPackage(reference);
                if (!paths.IsDefaultOrEmpty)
                {
                    return paths.SelectAsArray(path => _fileReferenceProvider(path, properties));
                }
            }

            if (GacFileResolver != null)
            {
                var path = GacFileResolver.Resolve(reference);
                if (path != null)
                {
                    return ImmutableArray.Create(_fileReferenceProvider(path, properties));
                }
            }

            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(PathResolver, Hash.Combine(PackageResolver, Hash.Combine(GacFileResolver, 0)));
        }

        public bool Equals(RuntimeMetadataReferenceResolver other)
        {
            return ReferenceEquals(this, other) ||
                other != null &&
                Equals(PathResolver, other.PathResolver) &&
                Equals(PackageResolver, other.PackageResolver) &&
                Equals(GacFileResolver, other.GacFileResolver);
        }

        public override bool Equals(object other) => Equals(other as RuntimeMetadataReferenceResolver);
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        // Ideally we'd use properties with no aliases, but currently that's not possible since empty aliases mean {global}.
        private static readonly MetadataReferenceProperties ResolvedMissingAssemblyReferenceProperties = MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("<implicit>"));

        public static readonly RuntimeMetadataReferenceResolver Default = new RuntimeMetadataReferenceResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        internal readonly RelativePathResolver PathResolver;
        internal readonly NuGetPackageResolver PackageResolver;
        internal readonly GacFileResolver GacFileResolver;
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _fileReferenceProvider;

        // TODO: Look for .winmd, but only if the identity has content WindowsRuntime (https://github.com/dotnet/roslyn/issues/6483)
        // The extensions are in order in which the CLR loader looks for assembly files.
        internal static ImmutableArray<string> AssemblyExtensions = ImmutableArray.Create(".dll", ".exe");

        internal RuntimeMetadataReferenceResolver(
            ImmutableArray<string> searchPaths,
            string baseDirectory)
            : this(new RelativePathResolver(searchPaths, baseDirectory), null, GacFileResolver.IsAvailable ? new GacFileResolver() : null)
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

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            // look in the GAC:
            if (GacFileResolver != null && referenceIdentity.IsStrongName)
            {
                var path = GacFileResolver.Resolve(referenceIdentity.GetDisplayName());
                if (path != null)
                {
                    return CreateResolvedMissingReference(path);
                }
            }

            // look in the directory of the requesting definition:
            var definitionPath = (definition as PortableExecutableReference)?.FilePath;
            if (definitionPath != null)
            {
                var pathWithoutExtension = PathUtilities.CombinePathsUnchecked(PathUtilities.GetDirectoryName(definitionPath), referenceIdentity.Name);
                foreach (var extension in AssemblyExtensions)
                {
                    var fullPath = pathWithoutExtension + extension;
                    if (File.Exists(fullPath))
                    {
                        return CreateResolvedMissingReference(fullPath);
                    }
                }
            }

            return null;
        }

        private PortableExecutableReference CreateResolvedMissingReference(string fullPath)
        {
            return _fileReferenceProvider(fullPath, ResolvedMissingAssemblyReferenceProperties);
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            string packageName;
            string packageVersion;
            if (NuGetPackageResolver.TryParsePackageReference(reference, out packageName, out packageVersion))
            {
                if (PackageResolver != null)
                {
                    var paths = PackageResolver.ResolveNuGetPackage(packageName, packageVersion);
                    Debug.Assert(!paths.IsDefault);
                    return paths.SelectAsArray(path => _fileReferenceProvider(path, properties));
                }
            }
            else if (PathUtilities.IsFilePath(reference))
            {
                if (PathResolver != null)
                {
                    var resolvedPath = PathResolver.ResolvePath(reference, baseFilePath);
                    if (resolvedPath != null)
                    {
                        return ImmutableArray.Create(_fileReferenceProvider(resolvedPath, properties));
                    }
                }
            }
            else if (GacFileResolver != null)
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

        internal RuntimeMetadataReferenceResolver WithRelativePathResolver(RelativePathResolver resolver)
        {
            return Equals(resolver, PathResolver) ? this :
                new RuntimeMetadataReferenceResolver(resolver, PackageResolver, GacFileResolver, _fileReferenceProvider);
        }
    }
}

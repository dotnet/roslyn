// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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
        private static readonly MetadataReferenceProperties s_resolvedMissingAssemblyReferenceProperties =
            MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("<implicit>"));

        internal static string GetDesktopFrameworkDirectory() => GacFileResolver.IsAvailable ?
            PathUtilities.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName) : null;

        // file name to path:
        private static ImmutableDictionary<string, string> _lazyTrustedPlatformAssemblies;

        public static readonly RuntimeMetadataReferenceResolver Default =
            new RuntimeMetadataReferenceResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        internal readonly RelativePathResolver PathResolver;
        internal readonly NuGetPackageResolver PackageResolver;
        internal readonly GacFileResolver GacFileResolver;
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _fileReferenceProvider;
        private readonly bool _useCoreResolver;

        // TODO: Look for .winmd, but only if the identity has content WindowsRuntime (https://github.com/dotnet/roslyn/issues/6483)
        // The extensions are in order in which the CLR loader looks for assembly files.
        internal static ImmutableArray<string> AssemblyExtensions = ImmutableArray.Create(".dll", ".exe");

        internal RuntimeMetadataReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            : this(pathResolver: new RelativePathResolver(searchPaths, baseDirectory),
                   packageResolver: null,
                   gacFileResolver: GacFileResolver.IsAvailable ? new GacFileResolver() : null,
                   useCoreResolver: !GacFileResolver.IsAvailable,
                   fileReferenceProvider: null)
        {
        }

        internal RuntimeMetadataReferenceResolver(
            RelativePathResolver pathResolver,
            NuGetPackageResolver packageResolver,
            GacFileResolver gacFileResolver,
            bool useCoreResolver,
            Func<string, MetadataReferenceProperties, PortableExecutableReference> fileReferenceProvider = null)
        {
            PathResolver = pathResolver;
            PackageResolver = packageResolver;
            GacFileResolver = gacFileResolver;
            _useCoreResolver = useCoreResolver;
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

            // look into a directory containing CorLib:
            if (_useCoreResolver)
            {
                var result = ResolveTrustedPlatformAssemblyCore(referenceIdentity.Name, s_resolvedMissingAssemblyReferenceProperties);
                if (result != null)
                {
                    return result;
                }
            }

            // look in the directory of the requesting definition:
            string definitionPath = (definition as PortableExecutableReference)?.FilePath;
            if (definitionPath != null)
            {
                string pathWithoutExtension = PathUtilities.CombinePathsUnchecked(PathUtilities.GetDirectoryName(definitionPath), referenceIdentity.Name);
                foreach (string extension in AssemblyExtensions)
                {
                    string fullPath = pathWithoutExtension + extension;
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
            return _fileReferenceProvider(fullPath, s_resolvedMissingAssemblyReferenceProperties);
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            if (NuGetPackageResolver.TryParsePackageReference(reference, out string packageName, out string packageVersion))
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
                    string resolvedPath = PathResolver.ResolvePath(reference, baseFilePath);
                    if (resolvedPath != null)
                    {
                        return ImmutableArray.Create(_fileReferenceProvider(resolvedPath, properties));
                    }
                }
            }
            else
            {
                if (GacFileResolver != null)
                {
                    string path = GacFileResolver.Resolve(reference);
                    if (path != null)
                    {
                        return ImmutableArray.Create(_fileReferenceProvider(path, properties));
                    }
                }

                if (_useCoreResolver && AssemblyIdentity.TryParseDisplayName(reference, out var identity, out var identityParts))
                {
                    var result = ResolveTrustedPlatformAssemblyCore(identity.Name, properties);
                    if (result != null)
                    {
                        return ImmutableArray.Create(result);
                    }
                }
            }

            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        private PortableExecutableReference ResolveTrustedPlatformAssemblyCore(string name, MetadataReferenceProperties properties)
        {
            if (_lazyTrustedPlatformAssemblies == null)
            {
                _lazyTrustedPlatformAssemblies = GetTrustedPlatformAssemblyMap();
            }

            if (_lazyTrustedPlatformAssemblies.TryGetValue(name, out string path) && File.Exists(path))
            {
                return MetadataReference.CreateFromFile(path, properties);
            }

            return null;
        }

        private static ImmutableDictionary<string, string> GetTrustedPlatformAssemblyMap()
        {
            var set = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            if (CoreClrShim.AppContext.GetData?.Invoke("TRUSTED_PLATFORM_ASSEMBLIES") is string paths)
            {
                foreach (var path in paths.Split(Path.PathSeparator))
                {
                    if (PathUtilities.GetExtension(path) == ".dll")
                    {
                        string fileName = PathUtilities.GetFileName(path, includeExtension: false);
                        if (fileName.EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = fileName.Substring(0, fileName.Length - ".ni".Length);
                        }

                        // last one wins:
                        set[fileName] = path;
                    }
                }
            }

            return set.ToImmutable();
        }

        public override int GetHashCode()
        {
            return Hash.Combine(PathResolver,
                   Hash.Combine(PackageResolver,
                   Hash.Combine(GacFileResolver,
                   Hash.Combine(_useCoreResolver, 0))));
        }

        public bool Equals(RuntimeMetadataReferenceResolver other)
        {
            return ReferenceEquals(this, other) ||
                other != null &&
                Equals(PathResolver, other.PathResolver) &&
                Equals(PackageResolver, other.PackageResolver) &&
                Equals(GacFileResolver, other.GacFileResolver) &&
                _useCoreResolver == other._useCoreResolver;
        }

        public override bool Equals(object other) => Equals(other as RuntimeMetadataReferenceResolver);

        internal RuntimeMetadataReferenceResolver WithRelativePathResolver(RelativePathResolver resolver)
        {
            return Equals(resolver, PathResolver) ? this :
                new RuntimeMetadataReferenceResolver(resolver, PackageResolver, GacFileResolver, _useCoreResolver, _fileReferenceProvider);
        }
    }
}

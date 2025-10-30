// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
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

        internal static string? GetDesktopFrameworkDirectory() => GacFileResolver.IsAvailable ?
            PathUtilities.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName) : null;

        // file name to path:
        internal ImmutableDictionary<string, string> TrustedPlatformAssemblies;

        internal readonly RelativePathResolver PathResolver;
        internal readonly NuGetPackageResolver? PackageResolver;
        internal readonly GacFileResolver? GacFileResolver;
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _createFromFileFunc;

        // TODO: Look for .winmd, but only if the identity has content WindowsRuntime (https://github.com/dotnet/roslyn/issues/6483)
        // The extensions are in order in which the CLR loader looks for assembly files.
        internal static ImmutableArray<string> AssemblyExtensions = ImmutableArray.Create(".dll", ".exe");

        private static readonly char[] s_directorySeparators = [PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar];

        /// <summary>
        /// Creates a resolver that uses the current platform settings (GAC, platform assembly list).
        /// </summary>
        internal static RuntimeMetadataReferenceResolver CreateCurrentPlatformResolver(
            ImmutableArray<string> searchPaths = default,
            string? baseDirectory = null,
            Func<string, MetadataReferenceProperties, PortableExecutableReference>? createFromFileFunc = null)
        {
            return new RuntimeMetadataReferenceResolver(
                searchPaths,
                baseDirectory,
                packageResolver: null,
                gacFileResolver: GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                GetTrustedPlatformAssemblyPaths(),
                createFromFileFunc);
        }

        internal RuntimeMetadataReferenceResolver(
            ImmutableArray<string> searchPaths = default,
            string? baseDirectory = null,
            NuGetPackageResolver? packageResolver = null,
            GacFileResolver? gacFileResolver = null,
            ImmutableArray<string> platformAssemblyPaths = default,
            Func<string, MetadataReferenceProperties, PortableExecutableReference>? createFromFileFunc = null)
            : this(new RelativePathResolver(searchPaths.NullToEmpty(), baseDirectory),
                   packageResolver,
                   gacFileResolver,
                   GetTrustedPlatformAssemblies(platformAssemblyPaths.NullToEmpty()),
                   createFromFileFunc)
        {
        }

        internal RuntimeMetadataReferenceResolver(
            RelativePathResolver pathResolver,
            NuGetPackageResolver? packageResolver,
            GacFileResolver? gacFileResolver,
            ImmutableDictionary<string, string> trustedPlatformAssemblies,
            Func<string, MetadataReferenceProperties, PortableExecutableReference>? createFromfileFunc = null)
        {
            PathResolver = pathResolver;
            PackageResolver = packageResolver;
            GacFileResolver = gacFileResolver;
            _createFromFileFunc = createFromfileFunc ?? ((path, properties) => Script.CreateFromFile(path, PEStreamOptions.PrefetchEntireImage, properties));
            TrustedPlatformAssemblies = trustedPlatformAssemblies;
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
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

            // check platform assemblies:
            if (!TrustedPlatformAssemblies.IsEmpty)
            {
                var result = ResolveTrustedPlatformAssembly(referenceIdentity.Name, s_resolvedMissingAssemblyReferenceProperties);
                if (result != null)
                {
                    return result;
                }
            }

            // look in the directory of the requesting definition:
            var definitionDirectory = PathUtilities.GetDirectoryName((definition as PortableExecutableReference)?.FilePath);
            if (definitionDirectory != null)
            {
                string pathWithoutExtension = PathUtilities.CombinePathsUnchecked(definitionDirectory, referenceIdentity.Name);
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

        private PortableExecutableReference CreateFromFile(string filePath, MetadataReferenceProperties properties) =>
            _createFromFileFunc(filePath, properties);

        private PortableExecutableReference CreateResolvedMissingReference(string fullPath) =>
            _createFromFileFunc(fullPath, s_resolvedMissingAssemblyReferenceProperties);

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            if (NuGetPackageResolver.TryParsePackageReference(reference, out string packageName, out string packageVersion))
            {
                if (PackageResolver != null)
                {
                    var paths = PackageResolver.ResolveNuGetPackage(packageName, packageVersion);
                    Debug.Assert(!paths.IsDefault);
                    return paths.SelectAsArray(path => CreateFromFile(path, properties));
                }
            }
            else if (PathUtilities.IsFilePath(reference))
            {
                if (!TrustedPlatformAssemblies.IsEmpty && reference.IndexOfAny(s_directorySeparators) < 0)
                {
                    var result = ResolveTrustedPlatformAssembly(PathUtilities.GetFileName(reference, includeExtension: false), properties);
                    if (result != null)
                    {
                        return ImmutableArray.Create(result);
                    }
                }

                if (PathResolver != null)
                {
                    string? resolvedPath = PathResolver.ResolvePath(reference, baseFilePath);
                    if (resolvedPath != null)
                    {
                        return ImmutableArray.Create(CreateFromFile(resolvedPath, properties));
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
                        return ImmutableArray.Create(CreateFromFile(path, properties));
                    }
                }

                if (!TrustedPlatformAssemblies.IsEmpty && AssemblyIdentity.TryParseDisplayName(reference, out var identity, out var identityParts))
                {
                    var result = ResolveTrustedPlatformAssembly(identity.Name, properties);
                    if (result != null)
                    {
                        return ImmutableArray.Create(result);
                    }
                }
            }

            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        private PortableExecutableReference? ResolveTrustedPlatformAssembly(string name, MetadataReferenceProperties properties)
            => TrustedPlatformAssemblies.TryGetValue(name, out var path) && File.Exists(path) ? CreateFromFile(path, properties) : null;

        internal static ImmutableArray<string> GetTrustedPlatformAssemblyPaths()
            => ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator)).ToImmutableArrayOrEmpty();

        internal static ImmutableDictionary<string, string> GetTrustedPlatformAssemblies(ImmutableArray<string> paths)
        {
            if (paths.IsEmpty)
            {
                return ImmutableDictionary<string, string>.Empty;
            }

            var set = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                if (PathUtilities.GetExtension(path) == ".dll")
                {
                    string fileName = PathUtilities.GetFileName(path, includeExtension: false);
                    if (fileName.EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = fileName[..^".ni".Length];
                    }

                    // last one wins:
                    set[fileName] = path;
                }
            }

            return set.ToImmutable();
        }

        public override int GetHashCode()
        {
            return Hash.Combine(PathResolver,
                   Hash.Combine(PackageResolver,
                   Hash.Combine(GacFileResolver,
                   RuntimeHelpers.GetHashCode(TrustedPlatformAssemblies))));
        }

        public bool Equals(RuntimeMetadataReferenceResolver? other)
        {
            return ReferenceEquals(this, other) ||
                other != null &&
                Equals(PathResolver, other.PathResolver) &&
                Equals(PackageResolver, other.PackageResolver) &&
                Equals(GacFileResolver, other.GacFileResolver) &&
                ReferenceEquals(TrustedPlatformAssemblies, other.TrustedPlatformAssemblies);
        }

        public override bool Equals(object? other) => Equals(other as RuntimeMetadataReferenceResolver);

        internal RuntimeMetadataReferenceResolver WithRelativePathResolver(RelativePathResolver resolver)
        {
            return Equals(resolver, PathResolver) ? this :
                new RuntimeMetadataReferenceResolver(resolver, PackageResolver, GacFileResolver, TrustedPlatformAssemblies, _createFromFileFunc);
        }
    }
}

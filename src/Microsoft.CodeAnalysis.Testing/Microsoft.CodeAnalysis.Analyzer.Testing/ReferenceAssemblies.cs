// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

#if NET46 || NET472 || NETSTANDARD || NETCOREAPP3_1
using NuGet.Packaging.Signing;
#endif

namespace Microsoft.CodeAnalysis.Testing
{
    public sealed partial class ReferenceAssemblies
    {
        private const string ReferenceAssembliesPackageVersion = "1.0.0";

        private static readonly FileSystemSemaphore Semaphore = new FileSystemSemaphore(Path.Combine(Path.GetTempPath(), "test-packages", ".lock"));

        private static ImmutableDictionary<NuGet.Packaging.Core.PackageIdentity, string> s_packageToInstalledLocation
            = ImmutableDictionary.Create<NuGet.Packaging.Core.PackageIdentity, string>(PackageIdentityComparer.Default);

        private static ImmutableHashSet<NuGet.Packaging.Core.PackageIdentity> s_emptyPackages
            = ImmutableHashSet.Create<NuGet.Packaging.Core.PackageIdentity>(PackageIdentityComparer.Default);

        private readonly Dictionary<string, ImmutableArray<MetadataReference>> _references
            = new Dictionary<string, ImmutableArray<MetadataReference>>();

        public ReferenceAssemblies(string targetFramework)
        {
            TargetFramework = targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));
            AssemblyIdentityComparer = AssemblyIdentityComparer.Default;
            ReferenceAssemblyPath = null;
            Assemblies = ImmutableArray<string>.Empty;
            FacadeAssemblies = ImmutableArray<string>.Empty;
            LanguageSpecificAssemblies = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
            Packages = ImmutableArray<PackageIdentity>.Empty;
        }

        public ReferenceAssemblies(string targetFramework, PackageIdentity? referenceAssemblyPackage, string referenceAssemblyPath)
        {
            TargetFramework = targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));
            AssemblyIdentityComparer = AssemblyIdentityComparer.Default;
            ReferenceAssemblyPackage = referenceAssemblyPackage ?? throw new ArgumentNullException(nameof(referenceAssemblyPackage));
            ReferenceAssemblyPath = referenceAssemblyPath;
            Assemblies = ImmutableArray<string>.Empty;
            FacadeAssemblies = ImmutableArray<string>.Empty;
            LanguageSpecificAssemblies = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
            Packages = ImmutableArray<PackageIdentity>.Empty;
        }

        private ReferenceAssemblies(
            string targetFramework,
            AssemblyIdentityComparer assemblyIdentityComparer,
            PackageIdentity? referenceAssemblyPackage,
            string? referenceAssemblyPath,
            ImmutableArray<string> assemblies,
            ImmutableArray<string> facadeAssemblies,
            ImmutableDictionary<string, ImmutableArray<string>> languageSpecificAssemblies,
            ImmutableArray<PackageIdentity> packages,
            string? nugetConfigFilePath)
        {
            TargetFramework = targetFramework;
            AssemblyIdentityComparer = assemblyIdentityComparer;
            ReferenceAssemblyPackage = referenceAssemblyPackage;
            ReferenceAssemblyPath = referenceAssemblyPath;
            Assemblies = assemblies.IsDefault ? ImmutableArray<string>.Empty : assemblies;
            FacadeAssemblies = facadeAssemblies.IsDefault ? ImmutableArray<string>.Empty : facadeAssemblies;
            LanguageSpecificAssemblies = languageSpecificAssemblies;
            Packages = packages.IsDefault ? ImmutableArray<PackageIdentity>.Empty : packages;
            NuGetConfigFilePath = nugetConfigFilePath;
        }

        public static ReferenceAssemblies Default
        {
            get
            {
#if NETSTANDARD1_5
                return NetStandard.NetStandard15;
#elif NETSTANDARD2_0
                return NetStandard.NetStandard20;
#elif NET452
                return NetFramework.Net452.Default;
#elif NET46
                return NetFramework.Net46.Default;
#elif NET472
                return NetFramework.Net472.Default;
#elif NETCOREAPP3_1
                return NetCore.NetCoreApp31;
#endif
            }
        }

        public string TargetFramework { get; }

        public AssemblyIdentityComparer AssemblyIdentityComparer { get; }

        public PackageIdentity? ReferenceAssemblyPackage { get; }

        public string? ReferenceAssemblyPath { get; }

        public ImmutableArray<string> Assemblies { get; }

        public ImmutableArray<string> FacadeAssemblies { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> LanguageSpecificAssemblies { get; }

        public ImmutableArray<PackageIdentity> Packages { get; }

        public string? NuGetConfigFilePath { get; }

        public ReferenceAssemblies WithAssemblyIdentityComparer(AssemblyIdentityComparer assemblyIdentityComparer)
            => new ReferenceAssemblies(TargetFramework, assemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, Assemblies, FacadeAssemblies, LanguageSpecificAssemblies, Packages, NuGetConfigFilePath);

        public ReferenceAssemblies WithAssemblies(ImmutableArray<string> assemblies)
            => new ReferenceAssemblies(TargetFramework, AssemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, assemblies, FacadeAssemblies, LanguageSpecificAssemblies, Packages, NuGetConfigFilePath);

        public ReferenceAssemblies WithFacadeAssemblies(ImmutableArray<string> facadeAssemblies)
            => new ReferenceAssemblies(TargetFramework, AssemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, Assemblies, facadeAssemblies, LanguageSpecificAssemblies, Packages, NuGetConfigFilePath);

        public ReferenceAssemblies AddAssemblies(ImmutableArray<string> assemblies)
            => WithAssemblies(Assemblies.AddRange(assemblies));

        public ReferenceAssemblies AddFacadeAssemblies(ImmutableArray<string> facadeAssemblies)
            => WithFacadeAssemblies(FacadeAssemblies.AddRange(facadeAssemblies));

        public ReferenceAssemblies WithLanguageSpecificAssemblies(ImmutableDictionary<string, ImmutableArray<string>> languageSpecificAssemblies)
            => new ReferenceAssemblies(TargetFramework, AssemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, Assemblies, FacadeAssemblies, languageSpecificAssemblies, Packages, NuGetConfigFilePath);

        public ReferenceAssemblies WithLanguageSpecificAssemblies(string language, ImmutableArray<string> assemblies)
            => WithLanguageSpecificAssemblies(LanguageSpecificAssemblies.SetItem(language, assemblies));

        public ReferenceAssemblies AddLanguageSpecificAssemblies(string language, ImmutableArray<string> assemblies)
        {
            if (!LanguageSpecificAssemblies.TryGetValue(language, out var existing))
            {
                existing = ImmutableArray<string>.Empty;
            }

            return WithLanguageSpecificAssemblies(language, existing.AddRange(assemblies));
        }

        public ReferenceAssemblies WithPackages(ImmutableArray<PackageIdentity> packages)
            => new ReferenceAssemblies(TargetFramework, AssemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, Assemblies, FacadeAssemblies, LanguageSpecificAssemblies, packages, NuGetConfigFilePath);

        public ReferenceAssemblies AddPackages(ImmutableArray<PackageIdentity> packages)
            => WithPackages(Packages.AddRange(packages));

        public ReferenceAssemblies WithNuGetConfigFilePath(string nugetConfigFilePath)
            => new ReferenceAssemblies(TargetFramework, AssemblyIdentityComparer, ReferenceAssemblyPackage, ReferenceAssemblyPath, Assemblies, FacadeAssemblies, LanguageSpecificAssemblies, Packages, nugetConfigFilePath);

        public async Task<ImmutableArray<MetadataReference>> ResolveAsync(string? language, CancellationToken cancellationToken)
        {
            if (language is object)
            {
                if (LanguageSpecificAssemblies.IsEmpty
                    || !LanguageSpecificAssemblies.TryGetValue(language, out var languageSpecificAssemblies)
                    || languageSpecificAssemblies.IsEmpty)
                {
                    return await ResolveAsync(null, cancellationToken);
                }
            }

            language ??= string.Empty;
            lock (_references)
            {
                if (_references.TryGetValue(language, out var references))
                {
                    return references;
                }
            }

            using (var releaser = await Semaphore.WaitAsync(cancellationToken))
            {
                lock (_references)
                {
                    if (_references.TryGetValue(language, out var references))
                    {
                        return references;
                    }
                }

                var computedReferences = await ResolveCoreAsync(language, cancellationToken);
                lock (_references)
                {
                    _references.Add(language, computedReferences);
                }

                return computedReferences;
            }
        }

        /// <seealso href="https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries"/>
        private async Task<ImmutableArray<MetadataReference>> ResolveCoreAsync(string language, CancellationToken cancellationToken)
        {
            var settings = string.IsNullOrEmpty(NuGetConfigFilePath) ? Settings.LoadDefaultSettings(root: null) : Settings.LoadSpecificSettings(root: null, NuGetConfigFilePath);
            var sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());
            var targetFramework = NuGetFramework.ParseFolder(TargetFramework);
            var logger = NullLogger.Instance;

            using (var cacheContext = new SourceCacheContext())
            {
                var temporaryPackagesFolder = Path.Combine(Path.GetTempPath(), "test-packages");
                Directory.CreateDirectory(temporaryPackagesFolder);

                var repositories = sourceRepositoryProvider.GetRepositories().ToImmutableArray();
                repositories = repositories.Insert(0, new SourceRepository(new PackageSource(temporaryPackagesFolder, "test-packages"), Repository.Provider.GetCoreV3(), FeedType.FileSystemPackagesConfig));
                repositories = repositories.Insert(0, sourceRepositoryProvider.CreateRepository(new PackageSource(new Uri(SettingsUtility.GetGlobalPackagesFolder(settings)).AbsoluteUri, "global"), FeedType.FileSystemV3));
                var dependencies = ImmutableDictionary.CreateBuilder<NuGet.Packaging.Core.PackageIdentity, SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                if (ReferenceAssemblyPackage is object)
                {
                    await GetPackageDependenciesAsync(ReferenceAssemblyPackage.ToNuGetIdentity(), targetFramework, repositories, cacheContext, logger, dependencies, cancellationToken);
                }

                foreach (var packageIdentity in Packages)
                {
                    await GetPackageDependenciesAsync(packageIdentity.ToNuGetIdentity(), targetFramework, repositories, cacheContext, logger, dependencies, cancellationToken);
                }

                var availablePackages = dependencies.ToImmutable();

                var packagesToInstall = new List<NuGet.Packaging.Core.PackageIdentity>();
                if (ReferenceAssemblyPackage is object)
                {
                    packagesToInstall.Add(ReferenceAssemblyPackage.ToNuGetIdentity()!);
                }

                if (!Packages.IsEmpty)
                {
                    var targetIds = new List<string>(Packages.Select(package => package.Id));
                    var preferredVersions = new List<NuGet.Packaging.Core.PackageIdentity>(Packages.Select(package => package.ToNuGetIdentity()));
                    if (ReferenceAssemblyPackage is object)
                    {
                        // Make sure to include the implicit reference assembly package
                        if (!targetIds.Contains(ReferenceAssemblyPackage.Id))
                        {
                            targetIds.Insert(0, ReferenceAssemblyPackage.Id);
                        }

                        if (!preferredVersions.Any(preferred => preferred.Id == ReferenceAssemblyPackage.Id))
                        {
                            preferredVersions.Add(ReferenceAssemblyPackage.ToNuGetIdentity());
                        }
                    }

                    var resolverContext = new PackageResolverContext(
                        DependencyBehavior.Lowest,
                        targetIds,
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<PackageReference>(),
                        preferredVersions,
                        availablePackages.Values,
                        repositories.Select(repository => repository.PackageSource),
                        logger);
                    var resolver = new PackageResolver();

                    packagesToInstall.AddRange(resolver.Resolve(resolverContext, cancellationToken));
                }

                var globalPathResolver = new PackagePathResolver(SettingsUtility.GetGlobalPackagesFolder(settings));
                var localPathResolver = new PackagePathResolver(temporaryPackagesFolder);
#if NET452
                var packageExtractionContext = new PackageExtractionContext(logger)
                {
                    PackageSaveMode = PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode = XmlDocFileSaveMode.None,
                };
#elif NET46 || NET472 || NETSTANDARD2_0 || NETCOREAPP3_1
                var packageExtractionContext = new PackageExtractionContext(
                    PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode.None,
                    ClientPolicyContext.GetClientPolicy(settings, logger),
                    logger);
#elif NETSTANDARD1_5
                var packageExtractionContext = new PackageExtractionContext(
                    PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode.None,
                    logger,
                    new PackageSignatureVerifier(
                        SignatureVerificationProviderFactory.GetSignatureVerificationProviders(),
                        SignedPackageVerifierSettings.Default));
#else
#error The current target framework is not supported.
#endif

                var frameworkReducer = new FrameworkReducer();

                var frameworkAssemblies = new HashSet<string>();
                frameworkAssemblies.UnionWith(Assemblies);
                if (LanguageSpecificAssemblies.TryGetValue(language, out var languageSpecificAssemblies))
                {
                    frameworkAssemblies.UnionWith(languageSpecificAssemblies);
                }

                var resolvedAssemblies = new HashSet<string>();
                foreach (var packageToInstall in packagesToInstall)
                {
                    if (s_emptyPackages.Contains(packageToInstall))
                    {
                        continue;
                    }

                    PackageReaderBase packageReader;
                    var installedPath = GetInstalledPath(localPathResolver, globalPathResolver, packageToInstall);
                    if (installedPath is null)
                    {
                        var downloadResource = await availablePackages[packageToInstall].Source.GetResourceAsync<DownloadResource>(cancellationToken);
                        var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                            packageToInstall,
                            new PackageDownloadContext(cacheContext),
                            SettingsUtility.GetGlobalPackagesFolder(settings),
                            logger,
                            cancellationToken);

                        if (!PackageIdentityComparer.Default.Equals(packageToInstall, ReferenceAssemblyPackage?.ToNuGetIdentity())
                            && !downloadResult.PackageReader.GetItems(PackagingConstants.Folders.Lib).Any()
                            && !downloadResult.PackageReader.GetItems(PackagingConstants.Folders.Ref).Any())
                        {
                            // This package has no compile time impact
                            ImmutableInterlocked.Update(ref s_emptyPackages, (emptyPackages, package) => emptyPackages.Add(package), packageToInstall);
                            continue;
                        }

                        await PackageExtractor.ExtractPackageAsync(
#if !NET452 && !NETSTANDARD1_5
#pragma warning disable SA1114 // Parameter list should follow declaration
                            downloadResult.PackageSource,
#pragma warning restore SA1114 // Parameter list should follow declaration
#endif
                            downloadResult.PackageStream,
                            localPathResolver,
                            packageExtractionContext,
                            cancellationToken);

                        installedPath = GetInstalledPath(localPathResolver, globalPathResolver, packageToInstall);
                        packageReader = downloadResult.PackageReader;
                    }
                    else
                    {
                        packageReader = new PackageFolderReader(installedPath);
                    }

                    if (installedPath is null)
                    {
                        continue;
                    }

                    var libItems = await packageReader.GetLibItemsAsync(cancellationToken);
                    var nearestLib = frameworkReducer.GetNearest(targetFramework, libItems.Select(x => x.TargetFramework));
                    var frameworkItems = await packageReader.GetFrameworkItemsAsync(cancellationToken);
                    var nearestFramework = frameworkReducer.GetNearest(targetFramework, frameworkItems.Select(x => x.TargetFramework));
                    var refItems = await packageReader.GetItemsAsync(PackagingConstants.Folders.Ref, cancellationToken);
                    var nearestRef = frameworkReducer.GetNearest(targetFramework, refItems.Select(x => x.TargetFramework));
                    if (nearestRef is object)
                    {
                        var nearestRefItems = refItems.Single(x => x.TargetFramework == nearestRef);
                        foreach (var item in nearestRefItems.Items)
                        {
                            if (!string.Equals(Path.GetExtension(item), ".dll", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(Path.GetExtension(item), ".exe", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(Path.GetExtension(item), ".winmd", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            resolvedAssemblies.Add(Path.Combine(installedPath, item));
                        }
                    }
                    else if (nearestLib is object)
                    {
                        var nearestLibItems = libItems.Single(x => x.TargetFramework == nearestLib);
                        foreach (var item in nearestLibItems.Items)
                        {
                            if (!string.Equals(Path.GetExtension(item), ".dll", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(Path.GetExtension(item), ".exe", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(Path.GetExtension(item), ".winmd", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            resolvedAssemblies.Add(Path.Combine(installedPath, item));
                        }
                    }

                    // Include framework references except for package based frameworks
                    if (!targetFramework.IsPackageBased && nearestFramework is object)
                    {
                        var nearestFrameworkItems = frameworkItems.Single(x => x.TargetFramework == nearestFramework);
                        frameworkAssemblies.UnionWith(nearestFrameworkItems.Items);
                    }
                }

                var referenceAssemblyInstalledPath = ReferenceAssemblyPackage is object
                    ? GetInstalledPath(localPathResolver, globalPathResolver, ReferenceAssemblyPackage.ToNuGetIdentity())
                    : null;
                Debug.Assert(ReferenceAssemblyPackage is null || referenceAssemblyInstalledPath is object, $"Assertion failed: {nameof(ReferenceAssemblyPackage)} is null || {nameof(referenceAssemblyInstalledPath)} is object");
                Debug.Assert(ReferenceAssemblyPackage is null || ReferenceAssemblyPath is object, $"Assertion failed: {nameof(ReferenceAssemblyPackage)} is null || {nameof(ReferenceAssemblyPath)} is object");

                foreach (var assembly in frameworkAssemblies)
                {
                    if (ReferenceAssemblyPackage is null)
                    {
                        throw new InvalidOperationException($"Cannot resolve assembly '{assembly}' without a reference assembly package");
                    }

                    if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".dll")))
                    {
                        resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".dll")));
                    }
                    else if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".exe")))
                    {
                        resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".exe")));
                    }
                    else if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".winmd")))
                    {
                        resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".winmd")));
                    }
                }

                // Prefer assemblies from the reference assembly package to ones otherwise provided
                if (ReferenceAssemblyPackage is object)
                {
                    var referenceAssemblies = new HashSet<string>(resolvedAssemblies.Where(resolved => resolved.StartsWith(referenceAssemblyInstalledPath!)));

                    // Suppression due to https://github.com/dotnet/roslyn/issues/44735
                    var referenceAssemblyNames = new HashSet<string>(referenceAssemblies.Select((Func<string, string>)Path.GetFileNameWithoutExtension!));
                    resolvedAssemblies.RemoveWhere(resolved => referenceAssemblyNames.Contains(Path.GetFileNameWithoutExtension(resolved)) && !referenceAssemblies.Contains(resolved));
                }

                // Add the facade assemblies
                if (ReferenceAssemblyPackage is object)
                {
                    var facadesPath = Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, "Facades");
                    if (Directory.Exists(facadesPath))
                    {
                        foreach (var path in Directory.GetFiles(facadesPath, "*.dll").Concat(Directory.GetFiles(facadesPath, "*.exe")).Concat(Directory.GetFiles(facadesPath, "*.winmd")))
                        {
                            resolvedAssemblies.RemoveWhere(existingAssembly => Path.GetFileNameWithoutExtension(existingAssembly) == Path.GetFileNameWithoutExtension(path));
                            resolvedAssemblies.Add(Path.GetFullPath(path));
                        }
                    }

                    foreach (var assembly in FacadeAssemblies)
                    {
                        if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".dll")))
                        {
                            resolvedAssemblies.RemoveWhere(existingAssembly => Path.GetFileNameWithoutExtension(existingAssembly) == assembly);
                            resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".dll")));
                        }
                        else if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".exe")))
                        {
                            resolvedAssemblies.RemoveWhere(existingAssembly => Path.GetFileNameWithoutExtension(existingAssembly) == assembly);
                            resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".exe")));
                        }
                        else if (File.Exists(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".winmd")))
                        {
                            resolvedAssemblies.RemoveWhere(existingAssembly => Path.GetFileNameWithoutExtension(existingAssembly) == assembly);
                            resolvedAssemblies.Add(Path.GetFullPath(Path.Combine(referenceAssemblyInstalledPath!, ReferenceAssemblyPath!, assembly + ".winmd")));
                        }
                    }
                }
                else
                {
                    if (!FacadeAssemblies.IsEmpty)
                    {
                        throw new InvalidOperationException($"Cannot resolve facade assemblies without a reference assembly package");
                    }
                }

                return resolvedAssemblies.Select(MetadataReferences.CreateReferenceFromFile).ToImmutableArray();

                static string? GetInstalledPath(PackagePathResolver localPathResolver, PackagePathResolver globalPathResolver, NuGet.Packaging.Core.PackageIdentity packageIdentity)
                {
                    string? installedPath = s_packageToInstalledLocation.GetValueOrDefault(packageIdentity);
                    if (installedPath is null)
                    {
                        installedPath = GetInstalledPath(localPathResolver, packageIdentity)
                            ?? GetInstalledPath(globalPathResolver, packageIdentity);
                        if (installedPath is object)
                        {
                            installedPath = ImmutableInterlocked.GetOrAdd(ref s_packageToInstalledLocation, packageIdentity, installedPath);
                        }
                    }

                    return installedPath;

                    static string? GetInstalledPath(PackagePathResolver resolver, NuGet.Packaging.Core.PackageIdentity id)
                    {
                        try
                        {
                            return resolver.GetInstalledPath(id);
                        }
                        catch (PathTooLongException)
                        {
                            return null;
                        }
                    }
                }
            }
        }

        private static async Task GetPackageDependenciesAsync(
            NuGet.Packaging.Core.PackageIdentity packageIdentity,
            NuGetFramework targetFramework,
            ImmutableArray<SourceRepository> repositories,
            SourceCacheContext cacheContext,
            ILogger logger,
            ImmutableDictionary<NuGet.Packaging.Core.PackageIdentity, SourcePackageDependencyInfo>.Builder dependencies,
            CancellationToken cancellationToken)
        {
            if (dependencies.ContainsKey(packageIdentity))
            {
                return;
            }

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>(cancellationToken);
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    packageIdentity,
                    targetFramework,
#if !NET452
                    cacheContext,
#endif
                    logger,
                    cancellationToken);
                if (dependencyInfo is null)
                {
                    continue;
                }

                dependencyInfo = new SourcePackageDependencyInfo(new NuGet.Packaging.Core.PackageIdentity(dependencyInfo.Id, dependencyInfo.Version), FilterDependencies(dependencyInfo.Dependencies), dependencyInfo.Listed, dependencyInfo.Source, dependencyInfo.DownloadUri, dependencyInfo.PackageHash);
                dependencies.Add(packageIdentity, dependencyInfo);
                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    await GetPackageDependenciesAsync(new NuGet.Packaging.Core.PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion), targetFramework, repositories, cacheContext, logger, dependencies, cancellationToken);
                }

                break;
            }

            static IEnumerable<PackageDependency> FilterDependencies(IEnumerable<PackageDependency> dependencies)
            {
                return dependencies.Where(dependency => !dependency.Exclude.Contains("Compile"));
            }
        }

        public static class NetFramework
        {
            public static class Net20
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net20",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net20",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v2.0"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Data", "System.Xml"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Drawing", "System.Windows.Forms"));
            }

            public static class Net40
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net40",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net40",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.0"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net45
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net45",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net45",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.5"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net451
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net451",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net451",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.5.1"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net452
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net452",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net452",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.5.2"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net46
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net46",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net46",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.6"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net461
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net461",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net461",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.6.1"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net462
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net462",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net462",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.6.2"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net47
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net47",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net47",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.7"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net471
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net471",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net471",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.7.1"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net472
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net472",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net472",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.7.2"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }

            public static class Net48
            {
                public static ReferenceAssemblies Default { get; }
                    = new ReferenceAssemblies(
                        "net48",
                        new PackageIdentity(
                            "Microsoft.NETFramework.ReferenceAssemblies.net48",
                            ReferenceAssembliesPackageVersion),
                        Path.Combine("build", ".NETFramework", "v4.8"))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .AddAssemblies(ImmutableArray.Create("mscorlib", "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Net.Http", "System.Xml", "System.Xml.Linq"))
                    .AddLanguageSpecificAssemblies(LanguageNames.CSharp, ImmutableArray.Create("Microsoft.CSharp"))
                    .AddLanguageSpecificAssemblies(LanguageNames.VisualBasic, ImmutableArray.Create("Microsoft.VisualBasic"));

                public static ReferenceAssemblies WindowsForms { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("System.Deployment", "System.Drawing", "System.Windows.Forms"));

                public static ReferenceAssemblies Wpf { get; }
                    = Default.AddAssemblies(ImmutableArray.Create("PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase"));
            }
        }

        public static class NetCore
        {
            public static ReferenceAssemblies NetCoreApp10 { get; }
                = new ReferenceAssemblies("netcoreapp1.0")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.NETCore.App", "1.0.16")));

            public static ReferenceAssemblies NetCoreApp11 { get; }
                = new ReferenceAssemblies("netcoreapp1.1")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.NETCore.App", "1.1.13")));

            public static ReferenceAssemblies NetCoreApp20 { get; }
                = new ReferenceAssemblies("netcoreapp2.0")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.NETCore.App", "2.0.9")));

            public static ReferenceAssemblies NetCoreApp21 { get; }
                = new ReferenceAssemblies("netcoreapp2.1")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.NETCore.App", "2.1.13")));

            public static ReferenceAssemblies NetCoreApp30 { get; }
                = new ReferenceAssemblies(
                    "netcoreapp3.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "3.0.0"),
                    Path.Combine("ref", "netcoreapp3.0"));

            public static ReferenceAssemblies NetCoreApp31 { get; }
                = new ReferenceAssemblies(
                    "netcoreapp3.1",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "3.1.0"),
                    Path.Combine("ref", "netcoreapp3.1"));
        }

        public static class Net
        {
            private static readonly Lazy<ReferenceAssemblies> _lazyNet50 =
                new Lazy<ReferenceAssemblies>(() =>
                {
                    if (!NuGetFramework.Parse("net5.0").IsPackageBased)
                    {
                        // The NuGet version provided at runtime does not recognize the 'net5.0' target framework
                        throw new NotSupportedException("The 'net5.0' target framework is not supported by this version of NuGet.");
                    }

                    return new ReferenceAssemblies(
                        "net5.0",
                        new PackageIdentity(
                            "Microsoft.NETCore.App.Ref",
                            "5.0.0"),
                        Path.Combine("ref", "net5.0"));
                });

            public static ReferenceAssemblies Net50 => _lazyNet50.Value;
        }

        public static class NetStandard
        {
            public static ReferenceAssemblies NetStandard10 { get; }
                = new ReferenceAssemblies("netstandard1.0")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard11 { get; }
                = new ReferenceAssemblies("netstandard1.1")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard12 { get; }
                = new ReferenceAssemblies("netstandard1.2")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard13 { get; }
                = new ReferenceAssemblies("netstandard1.3")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard14 { get; }
                = new ReferenceAssemblies("netstandard1.4")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard15 { get; }
                = new ReferenceAssemblies("netstandard1.5")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard16 { get; }
                = new ReferenceAssemblies("netstandard1.6")
                .AddPackages(ImmutableArray.Create(new PackageIdentity("NETStandard.Library", "1.6.1")));

            public static ReferenceAssemblies NetStandard20 { get; }
                = new ReferenceAssemblies(
                    "netstandard2.0",
                    new PackageIdentity(
                        "NETStandard.Library",
                        "2.0.3"),
                    Path.Combine("build", "netstandard2.0", "ref"))
                .AddAssemblies(ImmutableArray.Create("netstandard"))
                .AddFacadeAssemblies(ImmutableArray.Create(
                    "Microsoft.Win32.Primitives",
                    "System.AppContext",
                    "System.Collections.Concurrent",
                    "System.Collections",
                    "System.Collections.NonGeneric",
                    "System.Collections.Specialized",
                    "System.ComponentModel",
                    "System.ComponentModel.EventBasedAsync",
                    "System.ComponentModel.Primitives",
                    "System.ComponentModel.TypeConverter",
                    "System.Console",
                    "System.Data.Common",
                    "System.Diagnostics.Contracts",
                    "System.Diagnostics.Debug",
                    "System.Diagnostics.FileVersionInfo",
                    "System.Diagnostics.Process",
                    "System.Diagnostics.StackTrace",
                    "System.Diagnostics.TextWriterTraceListener",
                    "System.Diagnostics.Tools",
                    "System.Diagnostics.TraceSource",
                    "System.Diagnostics.Tracing",
                    "System.Drawing.Primitives",
                    "System.Dynamic.Runtime",
                    "System.Globalization.Calendars",
                    "System.Globalization",
                    "System.Globalization.Extensions",
                    "System.IO.Compression",
                    "System.IO.Compression.ZipFile",
                    "System.IO",
                    "System.IO.FileSystem",
                    "System.IO.FileSystem.DriveInfo",
                    "System.IO.FileSystem.Primitives",
                    "System.IO.FileSystem.Watcher",
                    "System.IO.IsolatedStorage",
                    "System.IO.MemoryMappedFiles",
                    "System.IO.Pipes",
                    "System.IO.UnmanagedMemoryStream",
                    "System.Linq",
                    "System.Linq.Expressions",
                    "System.Linq.Parallel",
                    "System.Linq.Queryable",
                    "System.Net.Http",
                    "System.Net.NameResolution",
                    "System.Net.NetworkInformation",
                    "System.Net.Ping",
                    "System.Net.Primitives",
                    "System.Net.Requests",
                    "System.Net.Security",
                    "System.Net.Sockets",
                    "System.Net.WebHeaderCollection",
                    "System.Net.WebSockets.Client",
                    "System.Net.WebSockets",
                    "System.ObjectModel",
                    "System.Reflection",
                    "System.Reflection.Extensions",
                    "System.Reflection.Primitives",
                    "System.Resources.Reader",
                    "System.Resources.ResourceManager",
                    "System.Resources.Writer",
                    "System.Runtime.CompilerServices.VisualC",
                    "System.Runtime",
                    "System.Runtime.Extensions",
                    "System.Runtime.Handles",
                    "System.Runtime.InteropServices",
                    "System.Runtime.InteropServices.RuntimeInformation",
                    "System.Runtime.Numerics",
                    "System.Runtime.Serialization.Formatters",
                    "System.Runtime.Serialization.Json",
                    "System.Runtime.Serialization.Primitives",
                    "System.Runtime.Serialization.Xml",
                    "System.Security.Claims",
                    "System.Security.Cryptography.Algorithms",
                    "System.Security.Cryptography.Csp",
                    "System.Security.Cryptography.Encoding",
                    "System.Security.Cryptography.Primitives",
                    "System.Security.Cryptography.X509Certificates",
                    "System.Security.Principal",
                    "System.Security.SecureString",
                    "System.Text.Encoding",
                    "System.Text.Encoding.Extensions",
                    "System.Text.RegularExpressions",
                    "System.Threading",
                    "System.Threading.Overlapped",
                    "System.Threading.Tasks",
                    "System.Threading.Tasks.Parallel",
                    "System.Threading.Thread",
                    "System.Threading.ThreadPool",
                    "System.Threading.Timer",
                    "System.ValueTuple",
                    "System.Xml.ReaderWriter",
                    "System.Xml.XDocument",
                    "System.Xml.XmlDocument",
                    "System.Xml.XmlSerializer",
                    "System.Xml.XPath",
                    "System.Xml.XPath.XDocument",
                    "mscorlib",
                    "System.ComponentModel.Composition",
                    "System.Core",
                    "System",
                    "System.Data",
                    "System.Drawing",
                    "System.IO.Compression.FileSystem",
                    "System.Net",
                    "System.Numerics",
                    "System.Runtime.Serialization",
                    "System.ServiceModel.Web",
                    "System.Transactions",
                    "System.Web",
                    "System.Windows",
                    "System.Xml",
                    "System.Xml.Linq",
                    "System.Xml.Serialization"));

            public static ReferenceAssemblies NetStandard21 { get; }
                = new ReferenceAssemblies(
                    "netstandard2.1",
                    new PackageIdentity(
                        "NETStandard.Library.Ref",
                        "2.1.0"),
                    Path.Combine("ref", "netstandard2.1"));
        }

        internal static class TestAccessor
        {
            public static bool IsPackageBased(string targetFramework)
            {
                var framework = NuGetFramework.ParseFolder(targetFramework);
                return framework.IsPackageBased;
            }
        }
    }
}

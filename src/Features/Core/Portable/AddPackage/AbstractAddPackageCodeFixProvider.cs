// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal abstract partial class AbstractAddPackageCodeFixProvider : CodeFixProvider
    {
        private readonly IPackageInstallerService _packageInstallerService;
        private readonly ISymbolSearchService _symbolSearchService;

        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddPackageCodeFixProvider(
            IPackageInstallerService packageInstallerService,
            ISymbolSearchService symbolSearchService)
        {
            _packageInstallerService = packageInstallerService;
            _symbolSearchService = symbolSearchService;
        }

        protected abstract bool IncludePrerelease { get; }

        public abstract override FixAllProvider GetFixAllProvider();

        protected async Task<ImmutableArray<CodeAction>> GetAddPackagesCodeActionsAsync(
            CodeFixContext context, ISet<string> assemblyNames)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var workspaceServices = document.Project.Solution.Workspace.Services;

            var symbolSearchService = _symbolSearchService ?? workspaceServices.GetService<ISymbolSearchService>();
            var installerService = _packageInstallerService ?? workspaceServices.GetService<IPackageInstallerService>();

            var language = document.Project.Language;

            var options = workspaceServices.Workspace.Options;
            var searchNugetPackages = options.GetOption(
                SymbolSearchOptions.SuggestForTypesInNuGetPackages, language);

            var codeActions = ArrayBuilder<CodeAction>.GetInstance();
            if (symbolSearchService != null &&
                installerService != null &&
                searchNugetPackages &&
                installerService.IsEnabled(document.Project.Id))
            {
                var packageSources = installerService.TryGetPackageSources();

                foreach (var packageSource in packageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sortedPackages = await FindMatchingPackagesAsync(
                        packageSource, symbolSearchService,
                        assemblyNames, cancellationToken).ConfigureAwait(false);

                    foreach (var package in sortedPackages)
                    {
                        codeActions.Add(new InstallPackageParentCodeAction(
                            installerService, packageSource.Source,
                            package.PackageName, IncludePrerelease, document));
                    }
                }
            }

            return codeActions.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<PackageWithAssemblyResult>> FindMatchingPackagesAsync(
            PackageSource source,
            ISymbolSearchService searchService,
            ISet<string> assemblyNames,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new HashSet<PackageWithAssemblyResult>();

            foreach (var assemblyName in assemblyNames)
            {
                var packagesWithAssembly = await searchService.FindPackagesWithAssemblyAsync(
                    source.Name, assemblyName, cancellationToken).ConfigureAwait(false);

                result.AddRange(packagesWithAssembly);
            }

            // Ensure the packages are sorted by rank.
            var sortedPackages = result.ToImmutableArray().Sort();

            return sortedPackages;
        }
    }
}

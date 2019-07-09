// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override abstract FixAllProvider GetFixAllProvider();

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
                foreach (var packageSource in installerService.GetPackageSources())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sortedPackages = await FindMatchingPackagesAsync(
                        packageSource, symbolSearchService,
                        installerService, assemblyNames, cancellationToken).ConfigureAwait(false);

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

        private async Task<ImmutableArray<PackageWithAssemblyResult>> FindMatchingPackagesAsync(
            PackageSource source,
            ISymbolSearchService searchService,
            IPackageInstallerService installerService,
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

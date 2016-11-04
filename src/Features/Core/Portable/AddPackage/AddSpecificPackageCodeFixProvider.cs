// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal abstract partial class AddSpecificPackageCodeFixProvider : CodeFixProvider
    {
        private readonly IPackageInstallerService _packageInstallerService;

        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AddSpecificPackageCodeFixProvider(
            IPackageInstallerService packageInstallerService = null)
        {
            _packageInstallerService = packageInstallerService;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var packageInfo = GetPackageResult(context.Diagnostics[0].Id);

            var addPackageCodeActions = GetAddPackagesCodeActions(context, packageInfo);
            context.RegisterFixes(addPackageCodeActions, context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected abstract PackageInfo GetPackageResult(string diagnosticId);

        private ImmutableArray<CodeAction> GetAddPackagesCodeActions(
            CodeFixContext context, PackageInfo packageResult)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var workspaceServices = document.Project.Solution.Workspace.Services;

            var installerService = _packageInstallerService ?? workspaceServices.GetService<IPackageInstallerService>();

            var language = document.Project.Language;

            var options = workspaceServices.Workspace.Options;
            var searchNugetPackages = options.GetOption(
                SymbolSearchOptions.SuggestForTypesInNuGetPackages, language);

            var codeActions = ArrayBuilder<CodeAction>.GetInstance();
            if (installerService != null &&
                searchNugetPackages &&
                installerService.IsEnabled)
            {
                foreach (var packageSource in installerService.PackageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    codeActions.Add(new InstallPackageParentCodeAction(
                        installerService, packageResult, document));
                }
            }

            return codeActions.ToImmutableAndFree();
        }

        private async Task<ImmutableArray<PackageWithAssemblyInfo>> FindMatchingPackagesAsync(
            PackageSource source, 
            ISymbolSearchService searchService, 
            IPackageInstallerService installerService, 
            ISet<AssemblyIdentity> uniqueIdentities, 
            ArrayBuilder<CodeAction> builder, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new HashSet<PackageWithAssemblyInfo>();

            foreach (var identity in uniqueIdentities)
            {
                var packagesWithAssembly = await searchService.FindPackagesWithAssemblyAsync(
                    source, identity.Name, cancellationToken).ConfigureAwait(false);

                result.AddRange(packagesWithAssembly);
            }

            // Ensure the packages are sorted by rank.
            var sortedPackages = result.ToImmutableArray().Sort();

            return sortedPackages;
        }
    }
}
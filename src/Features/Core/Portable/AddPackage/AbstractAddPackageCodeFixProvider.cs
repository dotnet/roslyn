// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage;

/// <summary>
/// Values for parameters can be provided (during testing) for mocking purposes.
/// </summary> 
internal abstract partial class AbstractAddPackageCodeFixProvider : CodeFixProvider
{
    protected abstract bool IncludePrerelease { get; }

    public abstract override FixAllProvider? GetFixAllProvider();

    protected async Task<ImmutableArray<CodeAction>> GetAddPackagesCodeActionsAsync(
        CodeFixContext context, ISet<string> assemblyNames)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var workspaceServices = document.Project.Solution.Services;

        if (workspaceServices.GetService<ISymbolSearchService>() is not { } symbolSearchService ||
            workspaceServices.GetService<IPackageInstallerService>() is not { } installerService ||
            !installerService.IsEnabled(document.Project.Id))
        {
            return [];
        }

        var options = await document.GetSymbolSearchOptionsAsync(cancellationToken).ConfigureAwait(false);
        if (!options.SearchNuGetPackages)
        {
            return [];
        }

        var codeActions = ArrayBuilder<CodeAction>.GetInstance();
        var packageSources = PackageSourceHelper.GetPackageSources(installerService.TryGetPackageSources());

        foreach (var (name, source) in packageSources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sortedPackages = await FindMatchingPackagesAsync(
                name, symbolSearchService,
                assemblyNames, cancellationToken).ConfigureAwait(false);

            foreach (var package in sortedPackages)
            {
                codeActions.Add(new InstallPackageParentCodeAction(
                    installerService, source,
                    package.PackageName, IncludePrerelease, document));
            }
        }

        return codeActions.ToImmutableAndFree();
    }

    private static async Task<ImmutableArray<PackageWithAssemblyResult>> FindMatchingPackagesAsync(
        string sourceName,
        ISymbolSearchService searchService,
        ISet<string> assemblyNames,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new HashSet<PackageWithAssemblyResult>();

        foreach (var assemblyName in assemblyNames)
        {
            var packagesWithAssembly = await searchService.FindPackagesWithAssemblyAsync(
                sourceName, assemblyName, cancellationToken).ConfigureAwait(false);

            result.AddRange(packagesWithAssembly);
        }

        // Ensure the packages are sorted by rank.
        var sortedPackages = result.ToImmutableArray().Sort();

        return sortedPackages;
    }
}

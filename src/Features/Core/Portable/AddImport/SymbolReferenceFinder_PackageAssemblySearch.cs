// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class SymbolReferenceFinder
        {
            internal async Task FindNugetOrReferenceAssemblyReferencesAsync(
                ArrayBuilder<Reference> allReferences, CancellationToken cancellationToken)
            {
                if (allReferences.Count > 0)
                {
                    // Only do this if none of the project or metadata searches produced 
                    // any results. We always consider source and local metadata to be 
                    // better than any NuGet/assembly-reference results.
                    return;
                }

                if (!_owner.CanAddImportForType(_diagnostic, _node, out var nameNode))
                {
                    return;
                }

                CalculateContext(nameNode, _syntaxFacts, out var name, out var arity, out var inAttributeContext, out var hasIncompleteParentMember);

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken: cancellationToken))
                {
                    return;
                }

                await FindNugetOrReferenceAssemblyTypeReferencesAsync(
                    allReferences, nameNode, name, arity, inAttributeContext, cancellationToken).ConfigureAwait(false);
            }

            private async Task FindNugetOrReferenceAssemblyTypeReferencesAsync(
                ArrayBuilder<Reference> allReferences, TSimpleNameSyntax nameNode,
                string name, int arity, bool inAttributeContext,
                CancellationToken cancellationToken)
            {
                if (arity == 0 && inAttributeContext)
                {
                    await FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                        allReferences, nameNode, name + AttributeSuffix, arity,
                        isAttributeSearch: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                await FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                    allReferences, nameNode, name, arity,
                    isAttributeSearch: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                ArrayBuilder<Reference> allReferences, TSimpleNameSyntax nameNode,
                string name, int arity, bool isAttributeSearch, CancellationToken cancellationToken)
            {
                var workspaceServices = _document.Project.Solution.Workspace.Services;

                var symbolSearchService = _owner._symbolSearchService ?? workspaceServices.GetService<ISymbolSearchService>();
                var installerService = _owner._packageInstallerService ?? workspaceServices.GetService<IPackageInstallerService>();

                var language = _document.Project.Language;

                var options = workspaceServices.Workspace.Options;
                var searchReferenceAssemblies = options.GetOption(
                    SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, language);
                var searchNugetPackages = options.GetOption(
                    SymbolSearchOptions.SuggestForTypesInNuGetPackages, language);

                if (symbolSearchService != null &&
                    searchReferenceAssemblies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindReferenceAssemblyTypeReferencesAsync(
                        symbolSearchService, allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }

                if (symbolSearchService != null &&
                    installerService != null &&
                    searchNugetPackages && 
                    installerService.IsEnabled)
                {
                    foreach (var packageSource in installerService.PackageSources)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await FindNugetTypeReferencesAsync(
                            packageSource, symbolSearchService, installerService, allReferences,
                            nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private async Task FindReferenceAssemblyTypeReferencesAsync(
                ISymbolSearchService searchService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await searchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, cancellationToken).ConfigureAwait(false);
                if (results.IsDefault)
                {
                    return;
                }

                var project = _document.Project;
                var projectId = project.Id;
                var workspace = project.Solution.Workspace;

                foreach (var result in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await HandleReferenceAssemblyReferenceAsync(
                        allReferences, nameNode, project,
                        isAttributeSearch, result, weight: allReferences.Count,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task FindNugetTypeReferencesAsync(
                PackageSource source,
                ISymbolSearchService searchService,
                IPackageInstallerService installerService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await searchService.FindPackagesWithTypeAsync(
                    source.Name, name, arity, cancellationToken).ConfigureAwait(false);
                if (results.IsDefault)
                {
                    return;
                }

                var project = _document.Project;
                var projectId = project.Id;
                var workspace = project.Solution.Workspace;

                foreach (var result in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await HandleNugetReferenceAsync(
                        source.Source, installerService, allReferences, nameNode,
                        project, isAttributeSearch, result, 
                        weight: allReferences.Count).ConfigureAwait(false);
                }
            }

            private async Task HandleReferenceAssemblyReferenceAsync(
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                Project project,
                bool isAttributeSearch,
                ReferenceAssemblyWithTypeResult result,
                int weight,
                CancellationToken cancellationToken)
            {
                foreach (var reference in project.MetadataReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assemblySymbol?.Name == result.AssemblyName)
                    {
                        // Project already has a reference to an assembly with this name.
                        return;
                    }
                }

                var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
                allReferences.Add(new AssemblyReference(
                    _owner, new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames, weight), result));
            }

            private Task HandleNugetReferenceAsync(
                string source,
                IPackageInstallerService installerService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                Project project,
                bool isAttributeSearch,
                PackageWithTypeResult result,
                int weight)
            {
                if (!installerService.IsInstalled(project.Solution.Workspace, project.Id, result.PackageName))
                {
                    var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
                    allReferences.Add(new PackageReference(_owner, installerService,
                        new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames, weight), 
                        source, result.PackageName, result.Version));
                }

                return SpecializedTasks.EmptyTask;
            }

            private static string GetDesiredName(bool isAttributeSearch, string typeName)
            {
                var desiredName = typeName;
                if (isAttributeSearch)
                {
                    desiredName = desiredName.GetWithoutAttributeSuffix(isCaseSensitive: false);
                }

                return desiredName;
            }
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
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

                if (!_owner.CanAddImportForType(_diagnosticId, _node, out var nameNode))
                {
                    return;
                }

                CalculateContext(
                    nameNode, _syntaxFacts,
                    out var name, out var arity, out var inAttributeContext, out _, out _);

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
                if (_searchReferenceAssemblies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindReferenceAssemblyTypeReferencesAsync(
                        allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }

                foreach (var packageSource in _packageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindNugetTypeReferencesAsync(
                        packageSource, allReferences,
                        nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task FindReferenceAssemblyTypeReferencesAsync(
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await _symbolSearchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, cancellationToken).ConfigureAwait(false);
                if (results == null)
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
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await _symbolSearchService.FindPackagesWithTypeAsync(
                    source.Source, name, arity, cancellationToken).ConfigureAwait(false);
                if (results == null)
                {
                    return;
                }

                var project = _document.Project;
                var projectId = project.Id;
                var workspace = project.Solution.Workspace;

                foreach (var result in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    HandleNugetReference(
                        source.Source, allReferences, nameNode,
                        project, isAttributeSearch, result,
                        weight: allReferences.Count);
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
                    _owner, new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames.ToReadOnlyList(), weight), result));
            }

            private void HandleNugetReference(
                string source,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                Project project,
                bool isAttributeSearch,
                PackageWithTypeResult result,
                int weight)
            {
                var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
                allReferences.Add(new PackageReference(_owner,
                    new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames.ToReadOnlyList(), weight),
                    source, result.PackageName, result.Version));
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

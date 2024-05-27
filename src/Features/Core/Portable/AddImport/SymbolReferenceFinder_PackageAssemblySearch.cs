// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private partial class SymbolReferenceFinder
    {
        internal async Task FindNugetOrReferenceAssemblyReferencesAsync(
            ConcurrentQueue<Reference> allReferences, CancellationToken cancellationToken)
        {
            // Only do this if none of the project or metadata searches produced 
            // any results. We always consider source and local metadata to be 
            // better than any NuGet/assembly-reference results.
            if (allReferences.Count > 0)
                return;

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
            ConcurrentQueue<Reference> allReferences, TSimpleNameSyntax nameNode,
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
            ConcurrentQueue<Reference> allReferences, TSimpleNameSyntax nameNode,
            string name, int arity, bool isAttributeSearch, CancellationToken cancellationToken)
        {
            if (_options.SearchOptions.SearchReferenceAssemblies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await FindReferenceAssemblyTypeReferencesAsync(
                    allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
            }

            var packageSources = PackageSourceHelper.GetPackageSources(_packageSources);
            foreach (var (sourceName, sourceUrl) in packageSources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await FindNugetTypeReferencesAsync(
                    sourceName, sourceUrl, allReferences,
                    nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FindReferenceAssemblyTypeReferencesAsync(
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            string name,
            int arity,
            bool isAttributeSearch,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await _symbolSearchService.FindReferenceAssembliesWithTypeAsync(
                name, arity, cancellationToken).ConfigureAwait(false);

            var project = _document.Project;

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
            string sourceName,
            string sourceUrl,
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            string name,
            int arity,
            bool isAttributeSearch,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await _symbolSearchService.FindPackagesWithTypeAsync(
                sourceName, name, arity, cancellationToken).ConfigureAwait(false);

            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                HandleNugetReference(
                    sourceUrl, allReferences, nameNode,
                    isAttributeSearch, result,
                    weight: allReferences.Count);
            }
        }

        private async Task HandleReferenceAssemblyReferenceAsync(
            ConcurrentQueue<Reference> allReferences,
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
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol?.Name == result.AssemblyName)
                {
                    // Project already has a reference to an assembly with this name.
                    return;
                }
            }

            var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
            allReferences.Enqueue(new AssemblyReference(
                _owner, new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames.ToReadOnlyList(), weight), result));
        }

        private void HandleNugetReference(
            string source,
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            bool isAttributeSearch,
            PackageWithTypeResult result,
            int weight)
        {
            var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
            allReferences.Enqueue(new PackageReference(_owner,
                new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames.ToReadOnlyList(), weight),
                source, result.PackageName, result.Version));
        }

        private static string? GetDesiredName(bool isAttributeSearch, string typeName)
            => isAttributeSearch ? typeName.GetWithoutAttributeSuffix(isCaseSensitive: false) : typeName;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private sealed partial class SymbolReferenceFinder
    {
        internal async Task FindNugetOrReferenceAssemblyReferencesAsync(
            ConcurrentQueue<Reference> allReferences, CancellationToken cancellationToken)
        {
            // Only do this if none of the project or metadata searches produced any results. We always consider source
            // and local metadata to be better than any NuGet/assembly-reference results.
            if (!allReferences.IsEmpty)
                return;

            TSimpleNameSyntax? nameNode;
            if (_isWithinImport)
            {
                nameNode = _node as TSimpleNameSyntax;
            }
            else if (!_owner.CanAddImportForType(_diagnosticId, _node, out nameNode))
            {
                return;
            }

            if (nameNode is null)
                return;

            if (ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken))
                return;

            CalculateContext(
                nameNode, _syntaxFacts,
                out var name, out var arity, out var inAttributeContext,
                out _, out _);

            if (arity == 0 && inAttributeContext)
                await FindWorkerAsync(name + AttributeSuffix, arity, isAttributeSearch: true).ConfigureAwait(false);

            await FindWorkerAsync(name, arity, isAttributeSearch: false).ConfigureAwait(false);

            return;

            async Task FindWorkerAsync(
                string name,
                int arity,
                bool isAttributeSearch)
            {
                if (_options.SearchOptions.SearchReferenceAssemblies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindReferenceAssemblyReferencesAsync(
                        allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }

                var packageSources = PackageSourceHelper.GetPackageSources(_packageSources);
                foreach (var (sourceName, sourceUrl) in packageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindNugetReferencesAsync(
                        sourceName, sourceUrl, allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task FindReferenceAssemblyReferencesAsync(
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            string name,
            int arity,
            bool isAttributeSearch,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await _symbolSearchService.FindReferenceAssembliesAsync(
                name, arity, _isWithinImport, cancellationToken).ConfigureAwait(false);

            var project = _document.Project;

            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IncludeResult(nameNode, result.ContainingNamespaceNames))
                    continue;

                await HandleReferenceAssemblyReferenceAsync(
                    allReferences, nameNode, project,
                    isAttributeSearch, result, weight: allReferences.Count,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private bool IncludeResult(TSimpleNameSyntax nameNode, ImmutableArray<string> namespaceNames)
        {
            // Outside of a using/import we did a search based on a simple leftmost name.  The index only provides
            // results for namespaces that contain that name.  So we don't need to do anything special here and we can
            // include that result.
            if (!_isWithinImport)
                return true;

            // Inside of a using/import we do a search on the part of the using that doesn't bind (like 'Json' in `using
            // Newtonsoft.Json;`).  But this may find results in other potential namespaces (like `Goobar.Json`).  So we
            // need to make sure the namespace the index found matches the full name in the using/import.
            var syntaxFacts = this._document.GetRequiredLanguageService<ISyntaxFactsService>();
            var rootNode = syntaxFacts.IsRightOfQualifiedName(nameNode)
                ? nameNode.GetRequiredParent()
                : nameNode;

            return NamespaceMatches(rootNode, namespaceNames.AsSpan());

            bool NamespaceMatches(SyntaxNode rootNode, ReadOnlySpan<string> namespaceNames)
            {
                // We have a part of the name tree, but no more namespace names to match.  This is definitely not a match.
                if (namespaceNames.Length == 0)
                    return false;

                if (syntaxFacts.IsIdentifierName(rootNode))
                {
                    // If we're on a single identifier, then we have to have a single namespace name that matches it.
                    return namespaceNames is [var name] && name == syntaxFacts.GetIdentifierOfIdentifierName(rootNode).ValueText;
                }
                else if (syntaxFacts.IsQualifiedName(rootNode))
                {
                    // If we have a qualified name (like A.B.C) then we recurse down the left side (A.B) and the right (C),
                    // passing in the corresponding parts of the namespace-name to match against.

                    syntaxFacts.GetPartsOfQualifiedName(rootNode, out var left, out _, out var right);
                    return NamespaceMatches(left, namespaceNames[0..^1]) &&
                           NamespaceMatches(right, namespaceNames[^1..]);
                }
                else
                {
                    // Anything else is a mismatch.
                    return false;
                }
            }
        }

        private async Task FindNugetReferencesAsync(
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
            var results = await _symbolSearchService.FindPackagesAsync(
                sourceName, name, arity, _isWithinImport, cancellationToken).ConfigureAwait(false);

            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IncludeResult(nameNode, result.ContainingNamespaceNames))
                    continue;

                allReferences.Enqueue(new PackageReference(
                    _owner,
                    new SearchResult(
                        desiredName: GetDesiredName(_isWithinImport, isAttributeSearch, result.TypeName),
                        nameNode,
                        result.ContainingNamespaceNames.ToReadOnlyList(), weight: allReferences.Count),
                    sourceUrl, result.PackageName, result.Version, _isWithinImport));
            }
        }

        private async Task HandleReferenceAssemblyReferenceAsync(
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            Project project,
            bool isAttributeSearch,
            ReferenceAssemblyResult result,
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

            var desiredName = GetDesiredName(_isWithinImport, isAttributeSearch, result.TypeName);
            allReferences.Enqueue(new AssemblyReference(
                _owner,
                new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames.ToReadOnlyList(), weight),
                result,
                _isWithinImport));
        }

        private static string? GetDesiredName(bool isWithinImport, bool isAttributeSearch, string typeName)
            => isWithinImport ? null : isAttributeSearch ? typeName.GetWithoutAttributeSuffix(isCaseSensitive: false) : typeName;
    }
}

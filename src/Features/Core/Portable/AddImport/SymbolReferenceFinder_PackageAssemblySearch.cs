// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
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
            ConcurrentQueue<Reference> allReferences, bool exact, CancellationToken cancellationToken)
        {
            var options = this.Options.SearchOptions;
            Contract.ThrowIfFalse(options.SearchNuGetPackages || options.SearchReferenceAssemblies);

            // We only support searching NuGet in an exact manner currently. 
            if (!exact)
            {
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, Reason='NuGet/reference assembly search only supports exact search'");
                return;
            }

            // Only do this if none of the project or metadata searches produced any results. We always consider source
            // and local metadata to be better than any NuGet/assembly-reference results.
            if (!allReferences.IsEmpty)
            {
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, ExistingReferenceCount={allReferences.Count}, Reason='Existing source or metadata references found'");
                return;
            }

            if (!_owner.CanAddImportForTypeOrNamespace(_diagnosticId, _node, out var nameNode))
            {
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, DiagnosticId='{_diagnosticId}', Node='{_node.GetType().FullName}', Reason='CanAddImportForTypeOrNamespace returned false'");
                return;
            }

            if (ExpressionBinds(nameNode, checkForExtensionMembers: false, cancellationToken))
            {
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, DiagnosticId='{_diagnosticId}', NameNode='{nameNode.GetFirstToken().ValueText}', Reason='Expression binds'");
                return;
            }

            var (typeQuery, namespaceQuery, inAttributeContext) = GetSearchQueries(nameNode);
            if (typeQuery.IsDefault && namespaceQuery.IsDefault)
            {
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, DiagnosticId='{_diagnosticId}', NameNode='{nameNode.GetFirstToken().ValueText}', Reason='No type or namespace query'");
                return;
            }

            AddImportTrace.LogMessage($"AddImport PackageAssemblySearchStart: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, DiagnosticId='{_diagnosticId}', NameNode='{nameNode.GetFirstToken().ValueText}', TypeQuery='{FormatTypeQuery(typeQuery)}', NamespaceQuery='{FormatNamespaceQuery(namespaceQuery)}', InAttributeContext={inAttributeContext}, SearchNuGetPackages={Options.SearchOptions.SearchNuGetPackages}, SearchReferenceAssemblies={Options.SearchOptions.SearchReferenceAssemblies}");

            if (inAttributeContext && typeQuery.Arity == 0)
                await FindWorkerAsync(new(typeQuery.Name + AttributeSuffix, typeQuery.Arity), namespaceQuery, isAttributeSearch: true).ConfigureAwait(false);

            await FindWorkerAsync(typeQuery, namespaceQuery, isAttributeSearch: false).ConfigureAwait(false);
            AddImportTrace.LogMessage($"AddImport PackageAssemblySearchComplete: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Exact={exact}, DiagnosticId='{_diagnosticId}', TotalReferenceCount={allReferences.Count}");

            return;

            async Task FindWorkerAsync(
                TypeQuery typeQuery,
                NamespaceQuery namespaceQuery,
                bool isAttributeSearch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchWorkerStart: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', IsAttributeSearch={isAttributeSearch}, TypeQuery='{FormatTypeQuery(typeQuery)}', NamespaceQuery='{FormatNamespaceQuery(namespaceQuery)}', ExistingReferenceCount={allReferences.Count}");
                await FindReferenceAssemblyReferencesAsync(
                    allReferences, nameNode, typeQuery, namespaceQuery, isAttributeSearch, cancellationToken).ConfigureAwait(false);

                var packageSources = PackageSourceHelper.GetPackageSources(_packageSources).ToArray();
                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchWorkerPackages: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', IsAttributeSearch={isAttributeSearch}, PackageSourceCount={packageSources.Length}");
                foreach (var (sourceName, sourceUrl) in packageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindNugetReferencesAsync(
                        sourceName, sourceUrl, allReferences, nameNode, typeQuery, namespaceQuery, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }

                AddImportTrace.LogMessage($"AddImport PackageAssemblySearchWorkerComplete: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', IsAttributeSearch={isAttributeSearch}, ReferenceCount={allReferences.Count}");
            }
        }

        private static string FormatTypeQuery(TypeQuery typeQuery)
            => typeQuery.IsDefault ? "<default>" : $"{typeQuery.Name}`{typeQuery.Arity}";

        private static string FormatNamespaceQuery(NamespaceQuery namespaceQuery)
            => namespaceQuery.IsDefault ? "<default>" : string.Join(".", namespaceQuery.Names);

        private (TypeQuery typeQuery, NamespaceQuery namespaceQuery, bool inAttributeContext) GetSearchQueries(TSimpleNameSyntax nameNode)
        {
            if (_isWithinImport)
            {
                // Inside of a using/import we do a search on the part of the using that doesn't bind (like 'Json' in `using
                // Newtonsoft.Json;`).  But this may find results in other potential namespaces (like `Goobar.Json`).  So we
                // need to make sure the namespace the index found matches the full name in the using/import.
                var current = (SyntaxNode)nameNode;
                while (_syntaxFacts.IsQualifiedName(current.Parent))
                    current = current.Parent;

                using var _1 = ArrayBuilder<string>.GetInstance(out var result);

                if (!TryAddNames(result, current))
                    return default;

                return (TypeQuery.Default, result.ToImmutableAndClear(), inAttributeContext: false);
            }
            else
            {
                CalculateContext(
                    nameNode, _syntaxFacts,
                    out var name, out var arity, out var inAttributeContext,
                    out _, out _);

                return (new(name, arity), NamespaceQuery.Default, inAttributeContext);
            }

            bool TryAddNames(ArrayBuilder<string> result, SyntaxNode rootNode)
            {
                if (_syntaxFacts.IsIdentifierName(rootNode))
                {
                    // If we're on a single identifier, then we have to have a single namespace name that matches it.
                    result.Add(_syntaxFacts.GetIdentifierOfIdentifierName(rootNode).ValueText);
                    return true;
                }
                else if (_syntaxFacts.IsQualifiedName(rootNode))
                {
                    // If we have a qualified name (like A.B.C) then we recurse down the left side (A.B) and the right (C),
                    // passing in the corresponding parts of the namespace-name to match against.

                    _syntaxFacts.GetPartsOfQualifiedName(rootNode, out var left, out _, out var right);
                    return TryAddNames(result, left) && TryAddNames(result, right);
                }
                else
                {
                    // Anything else is a mismatch.
                    return false;
                }
            }
        }

        private async Task FindReferenceAssemblyReferencesAsync(
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            TypeQuery typeQuery,
            NamespaceQuery namespaceQuery,
            bool isAttributeSearch,
            CancellationToken cancellationToken)
        {
            if (!this.Options.SearchOptions.SearchReferenceAssemblies)
            {
                AddImportTrace.LogMessage($"AddImport ReferenceAssemblySearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', Reason='Search option disabled'");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var results = await _symbolSearchService.FindReferenceAssembliesAsync(
                typeQuery, namespaceQuery, cancellationToken).ConfigureAwait(false);
            AddImportTrace.LogMessage($"AddImport ReferenceAssemblySearchResults: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', ResultCount={results.Length}, IsAttributeSearch={isAttributeSearch}, TypeQuery='{FormatTypeQuery(typeQuery)}', NamespaceQuery='{FormatNamespaceQuery(namespaceQuery)}'");

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

        private async Task FindNugetReferencesAsync(
            string sourceName,
            string sourceUrl,
            ConcurrentQueue<Reference> allReferences,
            TSimpleNameSyntax nameNode,
            TypeQuery typeQuery,
            NamespaceQuery namespaceQuery,
            bool isAttributeSearch,
            CancellationToken cancellationToken)
        {
            if (!this.Options.SearchOptions.SearchNuGetPackages)
            {
                AddImportTrace.LogMessage($"AddImport NuGetSearchSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', SourceName='{sourceName}', Reason='Search option disabled'");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var results = await _symbolSearchService.FindPackagesAsync(
                sourceName, typeQuery, namespaceQuery, cancellationToken).ConfigureAwait(false);
            AddImportTrace.LogMessage($"AddImport NuGetSearchResults: Document='{_document.FilePath ?? _document.Name}', Project='{_document.Project.Name}', SourceName='{sourceName}', ResultCount={results.Length}, IsAttributeSearch={isAttributeSearch}, TypeQuery='{FormatTypeQuery(typeQuery)}', NamespaceQuery='{FormatNamespaceQuery(namespaceQuery)}', Results=[{string.Join("; ", results.Select(static (result, index) => $"{index}: Package='{result.PackageName}', Version='{result.Version}', Type='{result.TypeName}', Namespace='{string.Join(".", result.ContainingNamespaceNames)}'"))}]");

            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                    AddImportTrace.LogMessage($"AddImport ReferenceAssemblyResultSkipped: Document='{_document.FilePath ?? _document.Name}', Project='{project.Name}', AssemblyName='{result.AssemblyName}', Reason='Project already references assembly'");
                    return;
                }
            }

            var desiredName = GetDesiredName(_isWithinImport, isAttributeSearch, result.TypeName);
            AddImportTrace.LogMessage($"AddImport ReferenceAssemblyResultAdded: Document='{_document.FilePath ?? _document.Name}', Project='{project.Name}', AssemblyName='{result.AssemblyName}', TypeName='{result.TypeName}', Namespace='{string.Join(".", result.ContainingNamespaceNames)}', DesiredName='{desiredName ?? "<null>"}', IsAttributeSearch={isAttributeSearch}, Weight={weight}");
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

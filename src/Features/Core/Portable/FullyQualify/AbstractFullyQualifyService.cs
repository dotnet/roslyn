// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify;

internal abstract partial class AbstractFullyQualifyService<TSimpleNameSyntax> : IFullyQualifyService
    where TSimpleNameSyntax : SyntaxNode
{
    private const int MaxResults = 3;

    private const int NamespaceWithNoErrorsWeight = 0;
    private const int TypeWeight = 1;
    private const int NamespaceWithErrorsWeight = 2;

    protected abstract bool CanFullyQualify(SyntaxNode node, [NotNullWhen(true)] out TSimpleNameSyntax? simpleName);
    protected abstract Task<SyntaxNode> ReplaceNodeAsync(TSimpleNameSyntax simpleName, string containerName, bool resultingSymbolIsType, CancellationToken cancellationToken);

    public async Task<FullyQualifyFixData?> GetFixDataAsync(
        Document document, TextSpan span, bool hideAdvancedMembers, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var result = await client.TryInvokeAsync<IRemoteFullyQualifyService, FullyQualifyFixData?>(
                document.Project,
                (service, solutionChecksum, cancellationToken) => service.GetFixDataAsync(solutionChecksum, document.Id, span, hideAdvancedMembers, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
                return null;

            return result.Value;
        }

        return await GetFixDataInCurrentProcessAsync(document, span, hideAdvancedMembers, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FullyQualifyFixData?> GetFixDataInCurrentProcessAsync(
        Document document, TextSpan span, bool hideAdvancedMembers, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        using (Logger.LogBlock(FunctionId.Refactoring_FullyQualify, cancellationToken))
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindToken(span.Start).GetAncestors<SyntaxNode>().First(n => n.Span.Contains(span));

            // Has to be a simple identifier or generic name.
            if (node == null || !CanFullyQualify(node, out var simpleName))
                return null;

            var ignoreCase = !syntaxFacts.IsCaseSensitive;
            syntaxFacts.GetNameAndArityOfSimpleName(simpleName, out var name, out _);
            var inAttributeContext = syntaxFacts.IsAttributeName(simpleName);

            var matchingTypes = await FindAsync(name, ignoreCase, SymbolFilter.Type).ConfigureAwait(false);
            var matchingAttributeTypes = inAttributeContext ? await FindAsync(name + nameof(Attribute), ignoreCase, SymbolFilter.Type).ConfigureAwait(false) : ImmutableArray<ISymbol>.Empty;
            var matchingNamespaces = inAttributeContext ? ImmutableArray<ISymbol>.Empty : await FindAsync(name, ignoreCase, SymbolFilter.Namespace).ConfigureAwait(false);

            if (matchingTypes.IsEmpty && matchingAttributeTypes.IsEmpty && matchingNamespaces.IsEmpty)
                return null;

            // We found some matches for the name alone.  Do some more checks to see if those matches are applicable in this location.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var matchingTypeSearchResults = GetTypeSearchResults(semanticModel, simpleName, hideAdvancedMembers, matchingTypes.Concat(matchingAttributeTypes));
            var matchingNamespaceSearchResults = GetNamespaceSearchResults(semanticModel, simpleName, matchingNamespaces);
            if (matchingTypeSearchResults.IsEmpty && matchingNamespaceSearchResults.IsEmpty)
                return null;

            var matchingTypeContainers = FilterAndSort(GetContainers(matchingTypeSearchResults, semanticModel.Compilation));
            var matchingNamespaceContainers = FilterAndSort(GetContainers(matchingNamespaceSearchResults, semanticModel.Compilation));

            var proposedContainers = matchingTypeContainers
                .Concat(matchingNamespaceContainers)
                .Distinct()
                .Take(MaxResults)
                .ToImmutableArray();

            using var _1 = ArrayBuilder<FullyQualifyIndividualFixData>.GetInstance(out var fixes);
            await AddFixesAsync(document, semanticModel, simpleName, name, proposedContainers, fixes, cancellationToken).ConfigureAwait(false);

            if (fixes.Count == 0)
                return null;

            return new FullyQualifyFixData(name, fixes.ToImmutable());
        }

        async Task<ImmutableArray<ISymbol>> FindAsync(string name, bool ignoreCase, SymbolFilter filter)
        {
            using var query = SearchQuery.Create(name, ignoreCase);
            return await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project, query, filter, cancellationToken).ConfigureAwait(false);
        }

        ImmutableArray<SymbolResult> GetTypeSearchResults(
            SemanticModel semanticModel,
            TSimpleNameSyntax simpleName,
            bool hideAdvancedMembers,
            ImmutableArray<ISymbol> matchingTypes)
        {
            var editorBrowserInfo = new EditorBrowsableInfo(semanticModel.Compilation);

            var looksGeneric = syntaxFacts.LooksGeneric(simpleName);

            var inAttributeContext = syntaxFacts.IsAttributeName(simpleName);
            syntaxFacts.GetNameAndArityOfSimpleName(simpleName, out var name, out var arity);

            var validSymbols = matchingTypes
                .OfType<INamedTypeSymbol>()
                .Where(s =>
                    IsValidNamedTypeSearchResult(semanticModel, arity, inAttributeContext, looksGeneric, s) &&
                    s.IsEditorBrowsable(hideAdvancedMembers, semanticModel.Compilation, editorBrowserInfo))
                .ToImmutableArray();

            // Check what the current node binds to.  If it binds to any symbols, but with
            // the wrong arity, then we don't want to suggest fully qualifying to the same
            // type that we're already binding to.  That won't address the WrongArity problem.
            var currentSymbolInfo = semanticModel.GetSymbolInfo(simpleName, cancellationToken);
            if (currentSymbolInfo.CandidateReason == CandidateReason.WrongArity)
            {
                validSymbols = validSymbols.WhereAsArray(
                    s => !currentSymbolInfo.CandidateSymbols.Contains(s));
            }

            return validSymbols.SelectAsArray(s => new SymbolResult(s, weight: TypeWeight));
        }

        ImmutableArray<SymbolResult> GetNamespaceSearchResults(
            SemanticModel semanticModel,
            TSimpleNameSyntax simpleName,
            ImmutableArray<ISymbol> symbols)
        {
            // There might be multiple namespaces that this name will resolve successfully in.
            // Some of them may be 'better' results than others.  For example, say you have
            //  Y.Z   and Y exists in both X1 and X2
            // We'll want to order them such that we prefer the namespace that will correctly
            // bind Z off of Y as well.

            string? rightName = null;
            var isAttributeName = false;
            if (syntaxFacts.IsLeftSideOfDot(simpleName))
            {
                var rightSide = syntaxFacts.GetRightSideOfDot(simpleName.Parent);
                Contract.ThrowIfNull(rightSide);

                syntaxFacts.GetNameAndArityOfSimpleName(rightSide, out rightName, out _);
                isAttributeName = syntaxFacts.IsAttributeName(rightSide);
            }

            return symbols
                .OfType<INamespaceSymbol>()
                .Where(n => !n.IsGlobalNamespace && HasAccessibleTypes(n, semanticModel, cancellationToken))
                .Select(n => new SymbolResult(n,
                    BindsWithoutErrors(n, rightName, isAttributeName) ? NamespaceWithNoErrorsWeight : NamespaceWithErrorsWeight))
                .ToImmutableArray();
        }
    }

    private async Task AddFixesAsync(
        Document document,
        SemanticModel semanticModel,
        TSimpleNameSyntax simpleName,
        string name,
        ImmutableArray<SymbolResult> proposedContainers,
        ArrayBuilder<FullyQualifyIndividualFixData> fixes,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var ignoreCase = !syntaxFacts.IsCaseSensitive;

        foreach (var symbolResult in proposedContainers)
        {
            var container = symbolResult.Symbol;
            Contract.ThrowIfNull(symbolResult.OriginalSymbol);
            var containerName = container.ToMinimalDisplayString(semanticModel, simpleName.SpanStart);

            // Actual member name might differ by case.
            string memberName;
            if (ignoreCase)
            {
                var member = container.GetMembers(name).FirstOrDefault();
                memberName = member != null ? member.Name : name;
            }
            else
            {
                memberName = name;
            }

            var title = $"{containerName}.{memberName}";
            var textChanges = await ProcessNodeAsync(document, simpleName, containerName, symbolResult.OriginalSymbol, cancellationToken).ConfigureAwait(false);
            fixes.Add(new FullyQualifyIndividualFixData(title, textChanges.ToImmutableArray()));
        }
    }

    private async Task<IEnumerable<TextChange>> ProcessNodeAsync(Document document, TSimpleNameSyntax simpleName, string containerName, INamespaceOrTypeSymbol originalSymbol, CancellationToken cancellationToken)
    {
        var newRoot = await ReplaceNodeAsync(simpleName, containerName, originalSymbol.IsType, cancellationToken).ConfigureAwait(false);
        var newDocument = document.WithSyntaxRoot(newRoot);
        var cleanedDocument = await CodeAction.CleanupDocumentAsync(
            newDocument, CodeCleanupOptions.GetDefault(document.Project.Services), cancellationToken).ConfigureAwait(false);

        return await cleanedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidNamedTypeSearchResult(
        SemanticModel semanticModel, int arity, bool inAttributeContext,
        bool looksGeneric, INamedTypeSymbol searchResult)
    {
        if (arity != 0 && searchResult.GetArity() != arity)
        {
            // If the user supplied type arguments, then the search result has to match the 
            // number provided.
            return false;
        }

        if (looksGeneric && searchResult.TypeArguments.Length == 0)
        {
            return false;
        }

        if (!searchResult.IsAccessibleWithin(semanticModel.Compilation.Assembly))
        {
            // Search result has to be accessible from our current location.
            return false;
        }

        if (inAttributeContext && !searchResult.IsAttribute())
        {
            // If we need an attribute, we have to have found an attribute.
            return false;
        }

        if (!HasValidContainer(searchResult))
        {
            // Named type we find must be in a namespace, or a non-generic type.
            return false;
        }

        return true;
    }

    private static bool HasValidContainer(ISymbol symbol)
        => symbol.ContainingSymbol is INamespaceSymbol or INamedTypeSymbol { IsGenericType: false };

    private bool BindsWithoutErrors(INamespaceSymbol ns, string? rightName, bool isAttributeName)
    {
        // If there was no name on the right, then this binds without any problems.
        if (rightName == null)
            return true;

        // Otherwise, see if the namespace we will bind this contains a member with the same
        // name as the name on the right.
        var types = ns.GetMembers(rightName);
        if (types.Any())
            return true;

        if (!isAttributeName)
        {
            return false;
        }

        return BindsWithoutErrors(ns, rightName + nameof(Attribute), isAttributeName: false);
    }

    private static bool HasAccessibleTypes(INamespaceSymbol @namespace, SemanticModel model, CancellationToken cancellationToken)
        => Enumerable.Any(@namespace.GetAllTypes(cancellationToken), t => t.IsAccessibleWithin(model.Compilation.Assembly));

    private static IEnumerable<SymbolResult> GetContainers(
        ImmutableArray<SymbolResult> symbols, Compilation compilation)
    {
        foreach (var symbolResult in symbols)
        {
            var containingSymbol = symbolResult.Symbol.ContainingSymbol as INamespaceOrTypeSymbol;
            if (containingSymbol is INamespaceSymbol namespaceSymbol)
            {
                containingSymbol = compilation.GetCompilationNamespace(namespaceSymbol);
            }

            if (containingSymbol != null)
            {
                yield return symbolResult.WithSymbol(containingSymbol);
            }
        }
    }

    private static IEnumerable<SymbolResult> FilterAndSort(IEnumerable<SymbolResult> symbols)
        => symbols.Distinct()
           .Where(n => n.Symbol is INamedTypeSymbol or INamespaceSymbol { IsGlobalNamespace: false })
           .Order();
}

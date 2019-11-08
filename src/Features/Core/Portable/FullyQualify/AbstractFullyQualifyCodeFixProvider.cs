// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify
{
    internal abstract partial class AbstractFullyQualifyCodeFixProvider : CodeFixProvider
    {
        private const int MaxResults = 3;

        private const int NamespaceWithNoErrorsWeight = 0;
        private const int TypeWeight = 1;
        private const int NamespaceWithErrorsWeight = 2;

        protected AbstractFullyQualifyCodeFixProvider()
        {
        }

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix
            // https://github.com/dotnet/roslyn/issues/34465
            return null;
        }

        protected abstract bool IgnoreCase { get; }
        protected abstract bool CanFullyQualify(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract Task<SyntaxNode> ReplaceNodeAsync(SyntaxNode node, string containerName, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;

            var project = document.Project;
            var diagnostic = diagnostics.First();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindToken(span.Start).GetAncestors<SyntaxNode>().First(n => n.Span.Contains(span));

            using (Logger.LogBlock(FunctionId.Refactoring_FullyQualify, cancellationToken))
            {
                // Has to be a simple identifier or generic name.
                if (node == null || !CanFullyQualify(diagnostic, ref node))
                {
                    return;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var matchingTypes = await GetMatchingTypesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);
                var matchingNamespaces = await GetMatchingNamespacesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);

                if (matchingTypes.IsEmpty && matchingNamespaces.IsEmpty)
                {
                    return;
                }

                var matchingTypeContainers = FilterAndSort(GetContainers(matchingTypes, semanticModel.Compilation));
                var matchingNamespaceContainers = FilterAndSort(GetContainers(matchingNamespaces, semanticModel.Compilation));

                var proposedContainers =
                    matchingTypeContainers.Concat(matchingNamespaceContainers)
                                          .Distinct()
                                          .Take(MaxResults);

                var displayService = project.LanguageServices.GetService<ISymbolDisplayService>();
                var codeActions = CreateActions(document, node, semanticModel, proposedContainers, displayService).ToImmutableArray();

                if (codeActions.Length > 1)
                {
                    // Wrap the spell checking actions into a single top level suggestion
                    // so as to not clutter the list.
                    context.RegisterCodeFix(new GroupingCodeAction(
                        string.Format(FeaturesResources.Fully_qualify_0, GetNodeName(document, node)),
                        codeActions), context.Diagnostics);
                }
                else
                {
                    context.RegisterFixes(codeActions, context.Diagnostics);
                }
            }
        }

        private IEnumerable<CodeAction> CreateActions(
            Document document, SyntaxNode node, SemanticModel semanticModel,
            IEnumerable<INamespaceOrTypeSymbol> proposedContainers,
            ISymbolDisplayService displayService)
        {
            foreach (var container in proposedContainers)
            {
                var containerName = displayService.ToMinimalDisplayString(semanticModel, node.SpanStart, container);

                var name = GetNodeName(document, node);

                // Actual member name might differ by case.
                string memberName;
                if (IgnoreCase)
                {
                    var member = container.GetMembers(name).FirstOrDefault();
                    memberName = member != null ? member.Name : name;
                }
                else
                {
                    memberName = name;
                }

                var codeAction = new MyCodeAction(
                    $"{containerName}.{memberName}",
                    c => ProcessNode(document, node, containerName, c));

                yield return codeAction;
            }
        }

        private static string GetNodeName(Document document, SyntaxNode node)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            syntaxFacts.GetNameAndArityOfSimpleName(node, out var name, out var arity);
            return name;
        }

        private async Task<Document> ProcessNode(Document document, SyntaxNode node, string containerName, CancellationToken cancellationToken)
        {
            var newRoot = await ReplaceNodeAsync(node, containerName, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<ImmutableArray<SymbolResult>> GetMatchingTypesAsync(
            Project project, SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var syntaxFacts = project.LanguageServices.GetService<ISyntaxFactsService>();

            syntaxFacts.GetNameAndArityOfSimpleName(node, out var name, out var arity);
            var looksGeneric = syntaxFacts.LooksGeneric(node);

            var symbolAndProjectIds = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, IgnoreCase),
                SymbolFilter.Type, cancellationToken).ConfigureAwait(false);
            var symbols = symbolAndProjectIds.SelectAsArray(t => t.Symbol);

            // also lookup type symbols with the "Attribute" suffix.
            var inAttributeContext = syntaxFacts.IsAttributeName(node);
            if (inAttributeContext)
            {
                var attributeSymbolAndProjectIds = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                    project, SearchQuery.Create(name + "Attribute", IgnoreCase),
                    SymbolFilter.Type, cancellationToken).ConfigureAwait(false);
                symbols = symbols.Concat(attributeSymbolAndProjectIds.SelectAsArray(t => t.Symbol));
            }

            var validSymbols = symbols
                .OfType<INamedTypeSymbol>()
                .Where(s => IsValidNamedTypeSearchResult(semanticModel, arity, inAttributeContext, looksGeneric, s))
                .ToImmutableArray();

            // Check what the current node binds to.  If it binds to any symbols, but with
            // the wrong arity, then we don't want to suggest fully qualifying to the same
            // type that we're already binding to.  That won't address the WrongArity problem.
            var currentSymbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            if (currentSymbolInfo.CandidateReason == CandidateReason.WrongArity)
            {
                validSymbols = validSymbols.WhereAsArray(
                    s => !currentSymbolInfo.CandidateSymbols.Contains(s));
            }

            return validSymbols.SelectAsArray(s => new SymbolResult(s, weight: TypeWeight));
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
        {
            var container = symbol.ContainingSymbol;
            return container is INamespaceSymbol ||
                   (container is INamedTypeSymbol { IsGenericType: false } parentType);
        }

        private async Task<ImmutableArray<SymbolResult>> GetMatchingNamespacesAsync(
            Project project,
            SemanticModel semanticModel,
            SyntaxNode simpleName,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFacts.IsAttributeName(simpleName))
            {
                return ImmutableArray<SymbolResult>.Empty;
            }

            syntaxFacts.GetNameAndArityOfSimpleName(simpleName, out var name, out var arityUnused);
            if (cancellationToken.IsCancellationRequested)
            {
                return ImmutableArray<SymbolResult>.Empty;
            }

            var symbolAndProjectIds = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, IgnoreCase),
                SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

            var symbols = symbolAndProjectIds.SelectAsArray(t => t.Symbol);

            // There might be multiple namespaces that this name will resolve successfully in.
            // Some of them may be 'better' results than others.  For example, say you have
            //  Y.Z   and Y exists in both X1 and X2
            // We'll want to order them such that we prefer the namespace that will correctly
            // bind Z off of Y as well.

            string rightName = null;
            var isAttributeName = false;
            if (syntaxFacts.IsLeftSideOfDot(simpleName))
            {
                var rightSide = syntaxFacts.GetRightSideOfDot(simpleName.Parent);
                syntaxFacts.GetNameAndArityOfSimpleName(rightSide, out rightName, out arityUnused);
                isAttributeName = syntaxFacts.IsAttributeName(rightSide);
            }

            var namespaces = symbols
                .OfType<INamespaceSymbol>()
                .Where(n => !n.IsGlobalNamespace && HasAccessibleTypes(n, semanticModel, cancellationToken))
                .Select(n => new SymbolResult(n,
                    BindsWithoutErrors(n, rightName, isAttributeName) ? NamespaceWithNoErrorsWeight : NamespaceWithErrorsWeight));

            return namespaces.ToImmutableArray();
        }

        private bool BindsWithoutErrors(INamespaceSymbol ns, string rightName, bool isAttributeName)
        {
            // If there was no name on the right, then this binds without any problems.
            if (rightName == null)
            {
                return true;
            }

            // Otherwise, see if the namespace we will bind this contains a member with the same
            // name as the name on the right.
            var types = ns.GetMembers(rightName);
            if (types.Any())
            {
                return true;
            }

            if (!isAttributeName)
            {
                return false;
            }

            return BindsWithoutErrors(ns, rightName + "Attribute", isAttributeName: false);
        }

        private bool HasAccessibleTypes(INamespaceSymbol @namespace, SemanticModel model, CancellationToken cancellationToken)
        {
            return Enumerable.Any(@namespace.GetAllTypes(cancellationToken), t => t.IsAccessibleWithin(model.Compilation.Assembly));
        }

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

        private IEnumerable<INamespaceOrTypeSymbol> FilterAndSort(IEnumerable<SymbolResult> symbols)
        {
            symbols ??= SpecializedCollections.EmptyList<SymbolResult>();
            symbols = symbols.Distinct()
                             .Where(n => n.Symbol is INamedTypeSymbol || !((INamespaceSymbol)n.Symbol).IsGlobalNamespace)
                             .Order();
            return symbols.Select(n => n.Symbol).ToList();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }

        private class GroupingCodeAction : CodeAction.CodeActionWithNestedActions
        {
            public GroupingCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions, isInlinable: true)
            {
            }
        }
    }
}

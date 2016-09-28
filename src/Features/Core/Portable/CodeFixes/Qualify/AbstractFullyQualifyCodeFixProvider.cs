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

                var matchingTypes = await this.GetMatchingTypesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);
                var matchingNamespaces = await this.GetMatchingNamespacesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);

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

                foreach (var container in proposedContainers)
                {
                    var containerName = displayService.ToMinimalDisplayString(semanticModel, node.SpanStart, container);

                    var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                    string name;
                    int arity;
                    syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

                    // Actual member name might differ by case.
                    string memberName;
                    if (this.IgnoreCase)
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

                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }
        }

        private async Task<Document> ProcessNode(Document document, SyntaxNode node, string containerName, CancellationToken cancellationToken)
        {
            var newRoot = await this.ReplaceNodeAsync(node, containerName, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<ImmutableArray<SymbolResult>> GetMatchingTypesAsync(
            Project project, SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            // Can't be on the right hand side of binary expression (like 'dot').
            cancellationToken.ThrowIfCancellationRequested();
            string name;
            int arity;
            var syntaxFacts = project.LanguageServices.GetService<ISyntaxFactsService>();
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

            var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false);

            // also lookup type symbols with the "Attribute" suffix.
            var inAttributeContext = syntaxFacts.IsAttributeName(node);
            if (inAttributeContext)
            {
                symbols = symbols.Concat(
                    await SymbolFinder.FindDeclarationsAsync(project, name + "Attribute", this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false));
            }

            var accessibleTypeSymbols = symbols
                .OfType<INamedTypeSymbol>()
                .Where(s => (arity == 0 || s.GetArity() == arity)
                    && s.IsAccessibleWithin(semanticModel.Compilation.Assembly)
                    && (!inAttributeContext || s.IsAttribute())
                    && HasValidContainer(s))
                .ToImmutableArray();

            return accessibleTypeSymbols.SelectAsArray(s => new SymbolResult(s, weight: TypeWeight));
        }

        private static bool HasValidContainer(ISymbol symbol)
        {
            var container = symbol.ContainingSymbol;
            return container is INamespaceSymbol ||
                (container is INamedTypeSymbol && !((INamedTypeSymbol)container).IsGenericType);
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

            string name;
            int arityUnused;
            syntaxFacts.GetNameAndArityOfSimpleName(simpleName, out name, out arityUnused);
            if (cancellationToken.IsCancellationRequested)
            {
                return ImmutableArray<SymbolResult>.Empty;
            }

            var symbols = await SymbolFinder.FindDeclarationsAsync(
                project, name, this.IgnoreCase, SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

            // There might be multiple namespaces that this name will resolve successfully in.
            // Some of them may be 'better' results than others.  For example, say you have
            //  Y.Z   and Y exists in both X1 and X2
            // We'll want to order them such that we prefer the namespace that will correctly
            // bind Z off of Y as well.

            string rightName = null;
            bool isAttributeName = false;
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
            IEnumerable<SymbolResult> symbols, Compilation compilation)
        {
            foreach (var symbolResult in symbols)
            {
                var containingSymbol = symbolResult.Symbol.ContainingSymbol as INamespaceOrTypeSymbol;
                if (containingSymbol is INamespaceSymbol)
                {
                    containingSymbol = compilation.GetCompilationNamespace((INamespaceSymbol)containingSymbol);
                }

                if (containingSymbol != null)
                {
                    yield return symbolResult.WithSymbol(containingSymbol);
                }
            }
        }

        private IEnumerable<INamespaceOrTypeSymbol> FilterAndSort(IEnumerable<SymbolResult> symbols)
        {
            symbols = symbols ?? SpecializedCollections.EmptyList<SymbolResult>();
            symbols = symbols.Distinct()
                             .Where(n => n.Symbol is INamedTypeSymbol || !((INamespaceSymbol)n.Symbol).IsGlobalNamespace)
                             .Order();
            return symbols.Select(n => n.Symbol).ToList();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}

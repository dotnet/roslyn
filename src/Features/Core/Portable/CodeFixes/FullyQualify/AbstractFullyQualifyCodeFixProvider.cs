// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        protected AbstractFullyQualifyCodeFixProvider()
        {
        }

        protected abstract bool IgnoreCase { get; }
        protected abstract bool CanFullyQualify(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract SyntaxNode ReplaceNode(SyntaxNode node, string containerName, CancellationToken cancellationToken);

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
                if (node != null && CanFullyQualify(diagnostic, ref node))
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    var matchingTypes = await this.GetMatchingTypesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);
                    var matchingNamespaces = await this.GetMatchingNamespacesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);

                    if (matchingTypes != null || matchingNamespaces != null)
                    {
                        matchingTypes = matchingTypes ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
                        matchingNamespaces = matchingNamespaces ?? SpecializedCollections.EmptyEnumerable<ISymbol>();

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
            }
        }

        private Task<Document> ProcessNode(Document document, SyntaxNode node, string containerName, CancellationToken cancellationToken)
        {
            var newRoot = this.ReplaceNode(node, containerName, cancellationToken);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        internal async Task<IEnumerable<ISymbol>> GetMatchingTypesAsync(
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
                .ToList();

            return accessibleTypeSymbols;
        }

        private static bool HasValidContainer(ISymbol symbol)
        {
            var container = symbol.ContainingSymbol;
            return container is INamespaceSymbol ||
                (container is INamedTypeSymbol && !((INamedTypeSymbol)container).IsGenericType);
        }

        internal async Task<IEnumerable<ISymbol>> GetMatchingNamespacesAsync(
            Project project,
            SemanticModel semanticModel,
            SyntaxNode simpleName,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFacts.IsAttributeName(simpleName))
            {
                return null;
            }

            string name;
            int arity;
            syntaxFacts.GetNameAndArityOfSimpleName(simpleName, out name, out arity);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

            var namespaces = symbols
                .OfType<INamespaceSymbol>()
                .Where(n => !n.IsGlobalNamespace &&
                            HasAccessibleTypes(n, semanticModel, cancellationToken));

            return namespaces;
        }

        private bool HasAccessibleTypes(INamespaceSymbol @namespace, SemanticModel model, CancellationToken cancellationToken)
        {
            return Enumerable.Any(@namespace.GetAllTypes(cancellationToken), t => t.IsAccessibleWithin(model.Compilation.Assembly));
        }

        private static IEnumerable<INamespaceOrTypeSymbol> GetContainers(IEnumerable<ISymbol> symbols, Compilation compilation)
        {
            foreach (var symbol in symbols)
            {
                var containingSymbol = symbol.ContainingSymbol as INamespaceOrTypeSymbol;
                if (containingSymbol is INamespaceSymbol)
                {
                    containingSymbol = compilation.GetCompilationNamespace((INamespaceSymbol)containingSymbol);
                }

                if (containingSymbol != null)
                {
                    yield return containingSymbol;
                }
            }
        }

        private IEnumerable<INamespaceOrTypeSymbol> FilterAndSort(IEnumerable<INamespaceOrTypeSymbol> symbols)
        {
            symbols = symbols ?? SpecializedCollections.EmptyList<INamespaceOrTypeSymbol>();
            symbols = symbols.Distinct()
                             .Where(n => n is INamedTypeSymbol || !((INamespaceSymbol)n).IsGlobalNamespace)
                             .OrderBy(this.Compare);
            return symbols;
        }

        private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>> s_symbolToNameMap =
            new ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>>();
        private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>>.CreateValueCallback s_getNameParts = GetNameParts;

        private static IList<string> GetNameParts(INamespaceOrTypeSymbol symbol)
        {
            return symbol.ToNameDisplayString().Split('.');
        }

        private int Compare(INamespaceOrTypeSymbol n1, INamespaceOrTypeSymbol n2)
        {
            Contract.Requires(n1 is INamespaceSymbol || !((INamedTypeSymbol)n1).IsGenericType);
            Contract.Requires(n2 is INamespaceSymbol || !((INamedTypeSymbol)n2).IsGenericType);

            if (n1 is INamedTypeSymbol && n2 is INamespaceSymbol)
            {
                return -1;
            }
            else if (n1 is INamespaceSymbol && n2 is INamedTypeSymbol)
            {
                return 1;
            }

            var names1 = s_symbolToNameMap.GetValue(n1, GetNameParts);
            var names2 = s_symbolToNameMap.GetValue(n2, GetNameParts);

            for (var i = 0; i < Math.Min(names1.Count, names2.Count); i++)
            {
                var comp = names1[i].CompareTo(names2[i]);
                if (comp != 0)
                {
                    return comp;
                }
            }

            return names1.Count - names2.Count;
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

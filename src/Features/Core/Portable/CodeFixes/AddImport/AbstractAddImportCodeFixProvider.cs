// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    using System.Collections.Immutable;
    using SymbolReference = ValueTuple<INamespaceOrTypeSymbol, MetadataReference>;

    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IgnoreCase { get; }

        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool CanAddImportForMethod(Diagnostic diagnostic, ISyntaxFactsService syntaxFacts, ref SyntaxNode node);
        protected abstract bool CanAddImportForNamespace(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract bool CanAddImportForQuery(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract bool CanAddImportForType(Diagnostic diagnostic, ref SyntaxNode node);

        protected abstract ISet<INamespaceSymbol> GetNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetQueryClauseInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract string GetDescription(INamespaceOrTypeSymbol symbol, SemanticModel semanticModel, SyntaxNode root);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, bool specialCaseSystem, CancellationToken cancellationToken);
        protected abstract bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsViableField(IFieldSymbol field, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsViableProperty(IPropertySymbol property, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;

            var project = document.Project;
            var diagnostic = diagnostics.First();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ancestors = root.FindToken(span.Start, findInsideTrivia: true).GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return;
            }

            var node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            if (node == null)
            {
                return;
            }

            var placeSystemNamespaceFirst = document.Project.Solution.Workspace.Options.GetOption(
                OrganizerOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (this.CanAddImport(node, cancellationToken))
                    {
                        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var getter = new ProposedImportGetter(this, document, semanticModel, diagnostic, node, cancellationToken);
                        var proposedImports = await getter.DoAsync().ConfigureAwait(false);

                        if (proposedImports?.Count > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            foreach (var reference in proposedImports)
                            {
                                var import = reference.Item1;
                                var description = this.GetDescription(import, semanticModel, node);
                                if (description != null)
                                {
                                    var action = new MyCodeAction(description, c =>
                                        this.AddImportAsync(node, import, document, placeSystemNamespaceFirst, c));
                                    context.RegisterCodeFix(action, diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsViableExtensionMethod(
            ITypeSymbol typeSymbol,
            IMethodSymbol method)
        {
            return typeSymbol != null && method.ReduceExtensionMethod(typeSymbol) != null;
        }

        private static bool ArityAccessibilityAndAttributeContextAreCorrect(
            SemanticModel semanticModel,
            ITypeSymbol symbol,
            int arity,
            bool inAttributeContext,
            bool hasIncompleteParentMember)
        {
            return (arity == 0 || symbol.GetArity() == arity || hasIncompleteParentMember)
                   && symbol.IsAccessibleWithin(semanticModel.Compilation.Assembly)
                   && (!inAttributeContext || symbol.IsAttribute());
        }

        private static void CalculateContext(SyntaxNode node, ISyntaxFactsService syntaxFacts, out string name, out int arity, out bool inAttributeContext, out bool hasIncompleteParentMember)
        {
            // Has to be a simple identifier or generic name.
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

            inAttributeContext = syntaxFacts.IsAttributeName(node);
            hasIncompleteParentMember = syntaxFacts.HasIncompleteParentMember(node);
        }

        private static bool NotGlobalNamespace(SymbolReference reference)
        {
            var symbol = reference.Item1;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.Item1 != null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }

        private class ProposedImportGetter
        {
            private readonly CancellationToken cancellationToken;
            private readonly Diagnostic diagnostic;
            private readonly Project project;
            private readonly Document document;
            private readonly SemanticModel semanticModel;

            private SyntaxNode node;

            private INamedTypeSymbol containingType;
            private ISymbol containingTypeOrAssembly;
            private ISet<INamespaceSymbol> namespacesInScope;
            private ISyntaxFactsService syntaxFacts;
            private readonly AbstractAddImportCodeFixProvider owner;

            public ProposedImportGetter(
                AbstractAddImportCodeFixProvider owner,
                Document document, SemanticModel semanticModel, Diagnostic diagnostic, SyntaxNode node, CancellationToken cancellationToken)
            {
                this.owner = owner;
                this.document = document;
                this.semanticModel = semanticModel;
                this.diagnostic = diagnostic;
                this.node = node;
                this.cancellationToken = cancellationToken;

                this.project = document.Project;
            }

            internal async Task<List<SymbolReference>> DoAsync()
            {
                this.containingType = semanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                this.containingTypeOrAssembly = containingType ?? (ISymbol)semanticModel.Compilation.Assembly;
                this.namespacesInScope = owner.GetNamespacesInScope(semanticModel, node, cancellationToken);
                this.syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                var project = document.Project;
                var viableMetadataReferences = await GetViableMetadataReferencesAsync().ConfigureAwait(false);

                var matchingTypesNamespaces = await this.GetNamespacesForMatchingTypesAsync().ConfigureAwait(false);
                var matchingTypes = await this.GetMatchingTypesAsync().ConfigureAwait(false);
                var matchingNamespaces = await this.GetNamespacesForMatchingNamespacesAsync().ConfigureAwait(false);
                var matchingExtensionMethodsNamespaces = await this.GetNamespacesForMatchingExtensionMethodsAsync().ConfigureAwait(false);
                var matchingFieldsAndPropertiesAsync = await this.GetNamespacesForMatchingFieldsAndPropertiesAsync().ConfigureAwait(false);
                var queryPatternsNamespaces = await this.GetNamespacesForQueryPatternsAsync().ConfigureAwait(false);

                if (matchingTypesNamespaces == null &&
                    matchingNamespaces == null &&
                    matchingExtensionMethodsNamespaces == null &&
                    matchingFieldsAndPropertiesAsync == null &&
                    queryPatternsNamespaces == null &&
                    matchingTypes == null)
                {
                    return null;
                }

                matchingTypesNamespaces = matchingTypesNamespaces ?? SpecializedCollections.EmptyList<SymbolReference>();
                matchingNamespaces = matchingNamespaces ?? SpecializedCollections.EmptyList<SymbolReference>();
                matchingExtensionMethodsNamespaces = matchingExtensionMethodsNamespaces ?? SpecializedCollections.EmptyList<SymbolReference>();
                matchingFieldsAndPropertiesAsync = matchingFieldsAndPropertiesAsync ?? SpecializedCollections.EmptyList<SymbolReference>();
                queryPatternsNamespaces = queryPatternsNamespaces ?? SpecializedCollections.EmptyList<SymbolReference>();
                matchingTypes = matchingTypes ?? SpecializedCollections.EmptyList<SymbolReference>();

                var proposedImports =
                    matchingTypesNamespaces
                                .Concat(matchingNamespaces)
                                .Concat(matchingExtensionMethodsNamespaces)
                                .Concat(matchingFieldsAndPropertiesAsync)
                                .Concat(queryPatternsNamespaces)
                                .Concat(matchingTypes)
                                .Distinct()
                                .Where(NotNull)
                                .Where(NotGlobalNamespace)
                                .OrderBy(CompareReferences)
                                .Take(8)
                                .ToList();

                return proposedImports;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingTypesAsync()
            {
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetNamespacesForMatchingTypesAsync(arity, inAttributeContext, hasIncompleteParentMember, symbols);
            }

            private async Task<IList<SymbolReference>> GetMatchingTypesAsync()
            {
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetMatchingTypes(name, arity, inAttributeContext, symbols, hasIncompleteParentMember);
            }

            private async Task<IEnumerable<ITypeSymbol>> GetTypeSymbols(
                string name,
                bool inAttributeContext)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (ExpressionBinds(checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, owner.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix.
                if (inAttributeContext)
                {
                    symbols = symbols.Concat(
                        await SymbolFinder.FindDeclarationsAsync(project, name + "Attribute", owner.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false));
                }

                return symbols.OfType<ITypeSymbol>();
            }

            protected bool ExpressionBinds(bool checkForExtensionMethods)
            {
                // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
                // For extension methods, however, we will continue to search if there exists any better matched method.
                cancellationToken.ThrowIfCancellationRequested();
                var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
                {
                    return true;
                }

                return symbolInfo.Symbol != null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync()
            {
                if (!owner.CanAddImportForNamespace(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

                if (ExpressionBinds(checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project, name, owner.IgnoreCase, SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

                return GetProposedNamespaces(
                    symbols.OfType<INamespaceSymbol>().Select(n => n.ContainingNamespace));
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingExtensionMethodsAsync()
            {
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

                var extensionMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var symbols = await GetSymbolsAsync().ConfigureAwait(false);
                if (symbols != null)
                {
                    extensionMethods = FilterForExtensionMethods(expression, symbols);
                }

                var addMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var methodSymbols = await GetAddMethodsAsync(expression).ConfigureAwait(false);
                if (methodSymbols != null)
                {
                    addMethods = GetProposedNamespaces(
                    methodSymbols.Select(s => s.ContainingNamespace));
                }

                return extensionMethods.Concat(addMethods).ToList();
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingFieldsAndPropertiesAsync()
            {
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

                var symbols = await GetSymbolsAsync().ConfigureAwait(false);

                if (symbols != null)
                {
                    return FilterForFieldsAndProperties(expression, symbols);
                }

                return null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync()
            {
                if (!owner.CanAddImportForQuery(diagnostic, ref node))
                {
                    return null;
                }

                ITypeSymbol type = owner.GetQueryClauseInfo(semanticModel, node, cancellationToken);
                if (type == null)
                {
                    return null;
                }

                // find extension methods named "Select"
                var symbols = await SymbolFinder.FindDeclarationsAsync(project, "Select", owner.IgnoreCase, SymbolFilter.Member, cancellationToken).ConfigureAwait(false);

                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(s => s.IsExtensionMethod && owner.IsViableExtensionMethod(type, s))
                    .ToList();

                return GetProposedNamespaces(
                    extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private Task<ImmutableArray<MetadataReference>> GetViableMetadataReferencesAsync()
            {
                return SpecializedTasks.EmptyImmutableArray<MetadataReference>();
            }

            private int CompareReferences(SymbolReference r1, SymbolReference r2)
            {
                // Always prefer references to our own project.
                if (r1.Item2 == null && r2.Item2 != null)
                {
                    return -1;
                }

                if (r1.Item2 != null && r2.Item2 == null)
                {
                    return 1;
                }

                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(r1.Item1, r2.Item1);
            }
            private List<SymbolReference> GetMatchingTypes(string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                    semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedTypes(name, accessibleTypeSymbols);
            }

            private List<SymbolReference> GetNamespacesForMatchingTypesAsync(int arity, bool inAttributeContext, bool hasIncompleteParentMember, IEnumerable<ITypeSymbol> symbols)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => s.ContainingSymbol is INamespaceSymbol
                                && ArityAccessibilityAndAttributeContextAreCorrect(
                                    semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedNamespaces(accessibleTypeSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetProposedNamespaces(IEnumerable<INamespaceSymbol> namespaces)
            {
                // We only want to offer to add a using if we don't already have one.
                return
                    namespaces.Where(n => !n.IsGlobalNamespace)
                              .Select(n => semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                              .Where(n => n != null && !namespacesInScope.Contains(n))
                              .Select(n => new SymbolReference(n, null))
                              .ToList();
            }

            private List<SymbolReference> GetProposedTypes(string name, List<ITypeSymbol> accessibleTypeSymbols)
            {
                List<SymbolReference> result = null;
                if (accessibleTypeSymbols != null)
                {
                    foreach (var typeSymbol in accessibleTypeSymbols)
                    {
                        if (typeSymbol?.ContainingType != null)
                        {
                            result = result ?? new List<SymbolReference>();
                            result.Add(new SymbolReference(typeSymbol.ContainingType, null));
                        }
                    }
                }

                return result;
            }

            private Task<IEnumerable<ISymbol>> GetSymbolsAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();

                // See if the name binds.  If it does, there's nothing further we need to do.
                if (ExpressionBinds(checkForExtensionMethods: true))
                {
                    return SpecializedTasks.EmptyEnumerable<ISymbol>();
                }

                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
                if (name == null)
                {
                    return SpecializedTasks.EmptyEnumerable<ISymbol>();
                }

                return SymbolFinder.FindDeclarationsAsync(project, name, owner.IgnoreCase, SymbolFilter.Member, cancellationToken);
            }

            private IEnumerable<SymbolReference> FilterForExtensionMethods(SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(method => method.IsExtensionMethod &&
                                     method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                     owner.IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private IList<SymbolReference> FilterForFieldsAndProperties(SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var propertySymbols = symbols
                    .OfType<IPropertySymbol>()
                    .Where(property => property.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                       owner.IsViableProperty(property, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                var fieldSymbols = symbols
                    .OfType<IFieldSymbol>()
                    .Where(field => field.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                    owner.IsViableField(field, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    propertySymbols.Select(s => s.ContainingNamespace).Concat(fieldSymbols.Select(s => s.ContainingNamespace)));
            }

            private async Task<IEnumerable<IMethodSymbol>> GetAddMethodsAsync(SyntaxNode expression)
            {
                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
                if (name != null)
                {
                    return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
                }

                if (owner.IsAddMethodContext(node, semanticModel))
                {
                    var symbols = await SymbolFinder.FindDeclarationsAsync(project, "Add", owner.IgnoreCase, SymbolFilter.Member, cancellationToken).ConfigureAwait(false);
                    return symbols
                        .OfType<IMethodSymbol>()
                        .Where(method => method.IsExtensionMethod &&
                                         method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                         owner.IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken));
                }

                return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
            }
        }
    }
}
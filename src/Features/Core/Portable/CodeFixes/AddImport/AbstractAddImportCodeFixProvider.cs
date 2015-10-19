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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
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
        protected abstract IEnumerable<ITypeSymbol> GetProposedTypes(string name, List<ITypeSymbol> accessibleTypeSymbols, SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope);
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

            var placeSystemNamespaceFirst = document.Project.Solution.Workspace.Options.GetOption(Microsoft.CodeAnalysis.Shared.Options.OrganizerOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (this.CanAddImport(node, cancellationToken))
                    {
                        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var containingType = semanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                        var containingTypeOrAssembly = containingType ?? (ISymbol)semanticModel.Compilation.Assembly;
                        var namespacesInScope = this.GetNamespacesInScope(semanticModel, node, cancellationToken);
                        var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                        var matchingTypesNamespaces = await this.GetNamespacesForMatchingTypesAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, cancellationToken).ConfigureAwait(false);
                        var matchingTypes = await this.GetMatchingTypesAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, cancellationToken).ConfigureAwait(false);
                        var matchingNamespaces = await this.GetNamespacesForMatchingNamespacesAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, cancellationToken).ConfigureAwait(false);
                        var matchingExtensionMethodsNamespaces = await this.GetNamespacesForMatchingExtensionMethodsAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, cancellationToken).ConfigureAwait(false);
                        var matchingFieldsAndPropertiesAsync = await this.GetNamespacesForMatchingFieldsAndPropertiesAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, cancellationToken).ConfigureAwait(false);
                        var queryPatternsNamespaces = await this.GetNamespacesForQueryPatternsAsync(project, diagnostic, node, semanticModel, namespacesInScope, cancellationToken).ConfigureAwait(false);

                        if (matchingTypesNamespaces != null || matchingNamespaces != null || matchingExtensionMethodsNamespaces != null || matchingFieldsAndPropertiesAsync != null || queryPatternsNamespaces != null || matchingTypes != null)
                        {
                            matchingTypesNamespaces = matchingTypesNamespaces ?? SpecializedCollections.EmptyList<INamespaceSymbol>();
                            matchingNamespaces = matchingNamespaces ?? SpecializedCollections.EmptyList<INamespaceSymbol>();
                            matchingExtensionMethodsNamespaces = matchingExtensionMethodsNamespaces ?? SpecializedCollections.EmptyList<INamespaceSymbol>();
                            matchingFieldsAndPropertiesAsync = matchingFieldsAndPropertiesAsync ?? SpecializedCollections.EmptyList<INamespaceSymbol>();
                            queryPatternsNamespaces = queryPatternsNamespaces ?? SpecializedCollections.EmptyList<INamespaceSymbol>();
                            matchingTypes = matchingTypes ?? SpecializedCollections.EmptyList<ITypeSymbol>();

                            var proposedImports =
                                matchingTypesNamespaces.Cast<INamespaceOrTypeSymbol>()
                                           .Concat(matchingNamespaces.Cast<INamespaceOrTypeSymbol>())
                                           .Concat(matchingExtensionMethodsNamespaces.Cast<INamespaceOrTypeSymbol>())
                                           .Concat(matchingFieldsAndPropertiesAsync.Cast<INamespaceOrTypeSymbol>())
                                           .Concat(queryPatternsNamespaces.Cast<INamespaceOrTypeSymbol>())
                                           .Concat(matchingTypes.Cast<INamespaceOrTypeSymbol>())
                                           .Distinct()
                                           .Where(NotNull)
                                           .Where(NotGlobalNamespace)
                                           .OrderBy(INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols)
                                           .Take(8)
                                           .ToList();

                            if (proposedImports.Count > 0)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                foreach (var import in proposedImports)
                                {
                                    var description = this.GetDescription(import, semanticModel, node);
                                    if (description != null)
                                    {
                                        var action = new MyCodeAction(description, (c) =>
                                            this.AddImportAsync(node, import, document, placeSystemNamespaceFirst, cancellationToken));
                                        context.RegisterCodeFix(action, diagnostic);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<INamespaceSymbol>> GetNamespacesForMatchingTypesAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForType(diagnostic, ref node))
            {
                return null;
            }

            string name;
            int arity;
            bool inAttributeContext, hasIncompleteParentMember;
            CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

            var symbols = await GetTypeSymbols(project, node, semanticModel, name, inAttributeContext, cancellationToken).ConfigureAwait(false);
            if (symbols == null)
            {
                return null;
            }

            return GetNamespacesForMatchingTypesAsync(semanticModel, namespacesInScope, arity, inAttributeContext, hasIncompleteParentMember, symbols);
        }

        private IEnumerable<INamespaceSymbol> GetNamespacesForMatchingTypesAsync(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, int arity, bool inAttributeContext, bool hasIncompleteParentMember, IEnumerable<ITypeSymbol> symbols)
        {
            var accessibleTypeSymbols = symbols
                .Where(s => s.ContainingSymbol is INamespaceSymbol
                            && ArityAccessibilityAndAttributeContextAreCorrect(
                                semanticModel, s, arity,
                                inAttributeContext, hasIncompleteParentMember))
                .ToList();

            return GetProposedNamespaces(
                accessibleTypeSymbols.Select(s => s.ContainingNamespace),
                semanticModel,
                namespacesInScope);
        }

        private async Task<IEnumerable<ITypeSymbol>> GetMatchingTypesAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForType(diagnostic, ref node))
            {
                return null;
            }

            string name;
            int arity;
            bool inAttributeContext, hasIncompleteParentMember;
            CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

            var symbols = await GetTypeSymbols(project, node, semanticModel, name, inAttributeContext, cancellationToken).ConfigureAwait(false);
            if (symbols == null)
            {
                return null;
            }

            return GetMatchingTypes(semanticModel, namespacesInScope, name, arity, inAttributeContext, symbols, hasIncompleteParentMember);
        }

        private async Task<IEnumerable<INamespaceSymbol>> GetNamespacesForMatchingNamespacesAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForNamespace(diagnostic, ref node))
            {
                return null;
            }

            string name;
            int arity;
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

            if (ExpressionBinds(node, semanticModel, cancellationToken))
            {
                return null;
            }

            var symbols = await SymbolFinder.FindDeclarationsAsync(
                project, name, this.IgnoreCase, SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

            return GetProposedNamespaces(
                symbols.OfType<INamespaceSymbol>().Select(n => n.ContainingNamespace),
                semanticModel,
                namespacesInScope);
        }

        private async Task<IEnumerable<INamespaceSymbol>> GetNamespacesForMatchingExtensionMethodsAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
            {
                return null;
            }

            var expression = node.Parent;

            var extensionMethods = SpecializedCollections.EmptyEnumerable<INamespaceSymbol>();
            var symbols = await GetSymbolsAsync(project, node, semanticModel, syntaxFacts, cancellationToken).ConfigureAwait(false);
            if (symbols != null)
            {
                extensionMethods = FilterForExtensionMethods(semanticModel, namespacesInScope, syntaxFacts, expression, symbols, cancellationToken);
            }

            var addMethods = SpecializedCollections.EmptyEnumerable<INamespaceSymbol>();
            var methodSymbols = await GetAddMethodsAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, expression, cancellationToken).ConfigureAwait(false);
            if (methodSymbols != null)
            {
                addMethods = GetProposedNamespaces(
                methodSymbols.Select(s => s.ContainingNamespace),
                semanticModel,
                namespacesInScope);
            }

            return extensionMethods.Concat(addMethods);
        }

        private async Task<IEnumerable<INamespaceSymbol>> GetNamespacesForMatchingFieldsAndPropertiesAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
            {
                return null;
            }

            var expression = node.Parent;

            var symbols = await GetSymbolsAsync(project, node, semanticModel, syntaxFacts, cancellationToken).ConfigureAwait(false);

            if (symbols != null)
            {
                return FilterForFieldsAndProperties(semanticModel, namespacesInScope, syntaxFacts, expression, symbols, cancellationToken);
            }

            return null;
        }

        private IEnumerable<INamespaceSymbol> FilterForFieldsAndProperties(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, ISyntaxFactsService syntaxFacts, SyntaxNode expression, IEnumerable<ISymbol> symbols, CancellationToken cancellationToken)
        {
            var propertySymbols = symbols
                .OfType<IPropertySymbol>()
                .Where(property => property.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                   IsViableProperty(property, expression, semanticModel, syntaxFacts, cancellationToken))
                .ToList();

            var fieldSymbols = symbols
                .OfType<IFieldSymbol>()
                .Where(field => field.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                IsViableField(field, expression, semanticModel, syntaxFacts, cancellationToken))
                .ToList();

            return GetProposedNamespaces(
                propertySymbols.Select(s => s.ContainingNamespace).Concat(fieldSymbols.Select(s => s.ContainingNamespace)),
                semanticModel,
                namespacesInScope);
        }

        private Task<IEnumerable<ISymbol>> GetSymbolsAsync(
            Project project,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // See if the name binds.  If it does, there's nothing further we need to do.
            if (ExpressionBinds(node, semanticModel, cancellationToken, checkForExtensionMethods: true))
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

            return SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Member, cancellationToken);
        }

        private async Task<IEnumerable<IMethodSymbol>> GetAddMethodsAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            string name;
            int arity;
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
            if (name != null)
            {
                return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
            }

            if (IsAddMethodContext(node, semanticModel))
            {
                var symbols = await SymbolFinder.FindDeclarationsAsync(project, "Add", this.IgnoreCase, SymbolFilter.Member, cancellationToken).ConfigureAwait(false);
                return symbols
                    .OfType<IMethodSymbol>()
                    .Where(method => method.IsExtensionMethod &&
                                     method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                     IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken));
            }

            return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
        }

        private IEnumerable<INamespaceSymbol> FilterForExtensionMethods(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, ISyntaxFactsService syntaxFacts, SyntaxNode expression, IEnumerable<ISymbol> symbols, CancellationToken cancellationToken)
        {
            var extensionMethodSymbols = symbols
                .OfType<IMethodSymbol>()
                .Where(method => method.IsExtensionMethod &&
                                 method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                 IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken))
                .ToList();

            return GetProposedNamespaces(
                extensionMethodSymbols.Select(s => s.ContainingNamespace),
                semanticModel,
                namespacesInScope);
        }

        private async Task<IEnumerable<INamespaceSymbol>> GetNamespacesForQueryPatternsAsync(
            Project project,
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope,
            CancellationToken cancellationToken)
        {
            if (!this.CanAddImportForQuery(diagnostic, ref node))
            {
                return null;
            }

            ITypeSymbol type = this.GetQueryClauseInfo(semanticModel, node, cancellationToken);
            if (type == null)
            {
                return null;
            }

            // find extension methods named "Select"
            var symbols = await SymbolFinder.FindDeclarationsAsync(project, "Select", this.IgnoreCase, SymbolFilter.Member, cancellationToken).ConfigureAwait(false);

            var extensionMethodSymbols = symbols
                .OfType<IMethodSymbol>()
                .Where(s => s.IsExtensionMethod && IsViableExtensionMethod(type, s))
                .ToList();

            return GetProposedNamespaces(
                extensionMethodSymbols.Select(s => s.ContainingNamespace),
                semanticModel,
                namespacesInScope);
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

        private async Task<IEnumerable<ITypeSymbol>> GetTypeSymbols(
            Project project,
            SyntaxNode node,
            SemanticModel semanticModel,
            string name,
            bool inAttributeContext,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (ExpressionBinds(node, semanticModel, cancellationToken))
            {
                return null;
            }

            var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false);

            // also lookup type symbols with the "Attribute" suffix.
            if (inAttributeContext)
            {
                symbols = symbols.Concat(
                    await SymbolFinder.FindDeclarationsAsync(project, name + "Attribute", this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false));
            }

            return symbols.OfType<ITypeSymbol>();
        }

        private IEnumerable<ITypeSymbol> GetMatchingTypes(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
        {
            var accessibleTypeSymbols = symbols
                .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                semanticModel, s, arity,
                                inAttributeContext, hasIncompleteParentMember))
                .ToList();

            return GetProposedTypes(
                        name,
                        accessibleTypeSymbols,
                        semanticModel,
                        namespacesInScope);
        }

        private static void CalculateContext(SyntaxNode node, ISyntaxFactsService syntaxFacts, out string name, out int arity, out bool inAttributeContext, out bool hasIncompleteParentMember)
        {
            // Has to be a simple identifier or generic name.
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

            inAttributeContext = syntaxFacts.IsAttributeName(node);
            hasIncompleteParentMember = syntaxFacts.HasIncompleteParentMember(node);
        }

        protected bool ExpressionBinds(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken, bool checkForExtensionMethods = false)
        {
            // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
            // For extension methods, however, we will continue to search if there exists any better matched method.
            cancellationToken.ThrowIfCancellationRequested();
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
            {
                return true;
            }

            return symbolInfo.Symbol != null;
        }

        protected IEnumerable<INamespaceSymbol> GetProposedNamespaces(
            IEnumerable<INamespaceSymbol> namespaces,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope)
        {
            // We only want to offer to add a using if we don't already have one.
            return
                namespaces.Where(n => !n.IsGlobalNamespace)
                          .Select(n => semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                          .Where(n => n != null && !namespacesInScope.Contains(n));
        }

        private static bool NotGlobalNamespace(INamespaceOrTypeSymbol symbol)
        {
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(INamespaceOrTypeSymbol symbol)
        {
            return symbol != null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}

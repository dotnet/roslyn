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

                        if (matchingTypesNamespaces != null ||
                            matchingNamespaces != null ||
                            matchingExtensionMethodsNamespaces != null ||
                            matchingFieldsAndPropertiesAsync != null ||
                            queryPatternsNamespaces != null ||
                            matchingTypes != null)
                        {
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

                            if (proposedImports.Count > 0)
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

        private async Task<IList<SymbolReference>> GetNamespacesForMatchingTypesAsync(
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

        private List<SymbolReference> GetNamespacesForMatchingTypesAsync(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, int arity, bool inAttributeContext, bool hasIncompleteParentMember, IEnumerable<ITypeSymbol> symbols)
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

        private async Task<IList<SymbolReference>> GetMatchingTypesAsync(
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

        private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync(
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

        private async Task<IList<SymbolReference>> GetNamespacesForMatchingExtensionMethodsAsync(
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

            var extensionMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
            var symbols = await GetSymbolsAsync(project, node, semanticModel, syntaxFacts, cancellationToken).ConfigureAwait(false);
            if (symbols != null)
            {
                extensionMethods = FilterForExtensionMethods(semanticModel, namespacesInScope, syntaxFacts, expression, symbols, cancellationToken);
            }

            var addMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
            var methodSymbols = await GetAddMethodsAsync(project, diagnostic, node, semanticModel, namespacesInScope, syntaxFacts, expression, cancellationToken).ConfigureAwait(false);
            if (methodSymbols != null)
            {
                addMethods = GetProposedNamespaces(
                methodSymbols.Select(s => s.ContainingNamespace),
                semanticModel,
                namespacesInScope);
            }

            return extensionMethods.Concat(addMethods).ToList();
        }

        private async Task<IList<SymbolReference>> GetNamespacesForMatchingFieldsAndPropertiesAsync(
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

        private IList<SymbolReference> FilterForFieldsAndProperties(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, ISyntaxFactsService syntaxFacts, SyntaxNode expression, IEnumerable<ISymbol> symbols, CancellationToken cancellationToken)
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

        private IEnumerable<SymbolReference> FilterForExtensionMethods(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, ISyntaxFactsService syntaxFacts, SyntaxNode expression, IEnumerable<ISymbol> symbols, CancellationToken cancellationToken)
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

        private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync(
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

        private List<SymbolReference> GetMatchingTypes(SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope, string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
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
        protected List<SymbolReference> GetProposedTypes(string name, List<ITypeSymbol> accessibleTypeSymbols, SemanticModel semanticModel, ISet<INamespaceSymbol> namespacesInScope)
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

        protected List<SymbolReference> GetProposedNamespaces(
            IEnumerable<INamespaceSymbol> namespaces,
            SemanticModel semanticModel,
            ISet<INamespaceSymbol> namespacesInScope)
        {
            // We only want to offer to add a using if we don't already have one.
            return
                namespaces.Where(n => !n.IsGlobalNamespace)
                          .Select(n => semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                          .Where(n => n != null && !namespacesInScope.Contains(n))
                          .Select(n => new SymbolReference(n, null))
                          .ToList();
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
    }
}

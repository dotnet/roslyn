// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    internal abstract class ImportAdderService : ILanguageService
    {
        public enum Strategy
        {
            AddImportsFromSyntaxes,
            AddImportsFromSymbolAnnotations,
        }

        public async Task<Document> AddImportsAsync(
            Document document,
            IEnumerable<TextSpan> spans,
            Strategy strategy,
            OptionSet? options,
            CancellationToken cancellationToken)
        {
            options ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
            var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>(new TextSpanIntervalIntrospector(), spans);

            var nodes = root.DescendantNodesAndSelf().Where(
                n => spansTree.HasIntervalThatOverlapsWith(n.FullSpan.Start, n.FullSpan.Length));

            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            if (strategy == Strategy.AddImportsFromSymbolAnnotations)
                return await AddImportDirectivesFromSymbolAnnotationsAsync(document, nodes, addImportsService, generator, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

            if (strategy == Strategy.AddImportsFromSyntaxes)
                return await AddImportDirectivesFromSyntaxesAsync(document, nodes, addImportsService, generator, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

            throw ExceptionUtilities.UnexpectedValue(strategy);
        }

        protected abstract INamespaceSymbol? GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model);

        private SyntaxNode MakeSafeToAddNamespaces(
            SyntaxNode root,
            IEnumerable<INamespaceSymbol> namespaceSymbols,
            SemanticModel model,
            Workspace workspace,
            CancellationToken cancellationToken)
        {
            var namespaceMembers = namespaceSymbols.SelectMany(x => x.GetMembers());
            var extensionMethods =
                namespaceMembers.OfType<INamedTypeSymbol>().Where(t => t.MightContainExtensionMethods)
                .SelectMany(x => x.GetMembers().OfType<IMethodSymbol>().Where(x => x.IsExtensionMethod));

            return MakeSafeToAddNamespaces(root, namespaceMembers, extensionMethods, model, workspace, cancellationToken);
        }

        /// <summary>
        /// Fully qualifies parts of the document that may change meaning if namespaces are added, 
        /// and marks them with <see cref="Simplifier.Annotation"/> so they can be reduced later.
        /// </summary>
        protected abstract SyntaxNode MakeSafeToAddNamespaces(
            SyntaxNode root,
            IEnumerable<INamespaceOrTypeSymbol> namespaceMembers,
            IEnumerable<IMethodSymbol> extensionMethods,
            SemanticModel model,
            Workspace workspace,
            CancellationToken cancellationToken);

        private SyntaxNode GenerateNamespaceImportDeclaration(INamespaceSymbol namespaceSymbol, SyntaxGenerator generator)
        {
            // We add Simplifier.Annotation so that the import can be removed if it turns out to be unnecessary.
            // This can happen for a number of reasons (we replace the type with var, inbuilt type, alias, etc.)
            return generator
                .NamespaceImportDeclaration(namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat))
                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
        }

        private async Task<Document> AddImportDirectivesFromSyntaxesAsync(
            Document document,
            IEnumerable<SyntaxNode> syntaxNodes,
            IAddImportsService addImportsService,
            SyntaxGenerator generator,
            bool placeSystemNamespaceFirst,
            CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var importsToAdd);
            using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToSimplify);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodesWithExplicitNamespaces = syntaxNodes
                .Select(n => (syntaxnode: n, namespaceSymbol: GetExplicitNamespaceSymbol(n, model)))
                .Where(x => x.namespaceSymbol != null);

            var addedSymbols = new HashSet<INamespaceSymbol>();
            foreach (var (node, namespaceSymbol) in nodesWithExplicitNamespaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                nodesToSimplify.Add(node);

                if (addedSymbols.Contains(namespaceSymbol))
                    continue;

                var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);
                if (addImportsService.HasExistingImport(model.Compilation, root, node, namespaceSyntax, generator))
                    continue;

                if (IsInsideNamespace(node, namespaceSymbol, model, cancellationToken))
                    continue;

                addedSymbols.Add(namespaceSymbol);
                importsToAdd.Add(namespaceSyntax);
            }

            if (nodesToSimplify.Count == 0)
                return document;

            var annotation = new SyntaxAnnotation();

            root = root.ReplaceNodes(
                nodesToSimplify,
                (o, r) => r.WithAdditionalAnnotations(Simplifier.Annotation, annotation));

            var first = root.DescendantNodesAndSelf().First(x => x.HasAnnotation(annotation));
            var last = root.DescendantNodesAndSelf().Last(x => x.HasAnnotation(annotation));

            var context = first.GetCommonRoot(last);

            root = addImportsService.AddImports(model.Compilation, root, context, importsToAdd, generator, placeSystemNamespaceFirst, cancellationToken);

            return document.WithSyntaxRoot(root);

        }

        private async Task<Document> AddImportDirectivesFromSymbolAnnotationsAsync(
            Document document,
            IEnumerable<SyntaxNode> syntaxNodes,
            IAddImportsService addImportsService,
            SyntaxGenerator generator,
            bool placeSystemNamespaceFirst,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var importsToAdd);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode? first = null, last = null;
            var annotatedNodes = syntaxNodes.Where(x => x.HasAnnotations(SymbolAnnotation.Kind));
            var addedSymbols = new HashSet<INamespaceSymbol>();

            foreach (var annotatedNode in annotatedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (annotatedNode.GetAnnotations(DoNotAddImportsAnnotation.Kind).Any())
                    continue;

                var annotations = annotatedNode.GetAnnotations(SymbolAnnotation.Kind);
                foreach (var annotation in annotations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var namedType in SymbolAnnotation.GetSymbols(annotation, model.Compilation).OfType<INamedTypeSymbol>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (namedType.OriginalDefinition.IsSpecialType() || namedType.IsNullable() || namedType.IsTupleType)
                            continue;

                        var namespaceSymbol = namedType.ContainingNamespace;
                        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
                            continue;

                        first ??= annotatedNode;
                        last = annotatedNode;

                        if (addedSymbols.Contains(namespaceSymbol))
                            continue;

                        var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);
                        if (addImportsService.HasExistingImport(model.Compilation, root, annotatedNode, namespaceSyntax, generator))
                            continue;

                        if (IsInsideNamespace(annotatedNode, namespaceSymbol, model, cancellationToken))
                            continue;

                        addedSymbols.Add(namespaceSymbol);
                        importsToAdd.Add(namespaceSyntax);
                    }
                }
            }

            if (importsToAdd.Count == 0)
                return document;

            var context = first == null || last == null ? null : first.GetCommonRoot(last);

            {
                // Mark the context with an annotation. 
                // This will allow us to find it after we have called MakeSafeToAddNamespaces.
                var annotation = new SyntaxAnnotation();
                RoslynDebug.Assert(context is object);
                document = document.WithSyntaxRoot(root.ReplaceNode(context, context.WithAdditionalAnnotations(annotation)));
                root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Make Safe to add namespaces
                document = document.WithSyntaxRoot(
                   MakeSafeToAddNamespaces(root, addedSymbols, model, document.Project.Solution.Workspace, cancellationToken));
                root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Find the context. It might be null if we have removed the context in the process of complexifying the tree.
                context = root.DescendantNodesAndSelf().FirstOrDefault(x => x.HasAnnotation(annotation)) ?? root;
            }

            root = addImportsService.AddImports(model.Compilation, root, context, importsToAdd, generator, placeSystemNamespaceFirst, cancellationToken);
            return document.WithSyntaxRoot(root);
        }

        /// <summary>
        /// Checks if the namespace declaration <paramref name="node"/> is contained inside,
        /// or any of its ancestor namespaces are the same as <paramref name="symbol"/>
        /// </summary>
        private bool IsInsideNamespace(SyntaxNode node, INamespaceSymbol symbol, SemanticModel model, CancellationToken cancellationToken)
        {
            var containedNamespace = model.GetEnclosingNamespace(node.SpanStart, cancellationToken);

            while (containedNamespace != null)
            {
                if (containedNamespace.Equals(symbol))
                    return true;
                containedNamespace = containedNamespace.ContainingNamespace;
            }

            return false;
        }
    }
}

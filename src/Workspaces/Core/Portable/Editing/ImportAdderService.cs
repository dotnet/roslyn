// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
            bool safe,
            OptionSet? options,
            CancellationToken cancellationToken)
        {
            options ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(model);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportsService = document.GetLanguageService<IAddImportsService>();
            var generator = SyntaxGenerator.GetGenerator(document);

            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan>(TextSpanIntervalIntrospector.Instance, spans);

            var nodes = root.DescendantNodesAndSelf().Where(IsInSpan);
            var (importDirectivesToAdd, namespaceSymbols, context) = strategy switch
            {
                Strategy.AddImportsFromSymbolAnnotations
                    => GetImportDirectivesFromAnnotatedNodesAsync(nodes, root, model, addImportsService, generator, cancellationToken),
                Strategy.AddImportsFromSyntaxes
                    => GetImportDirectivesFromSyntaxesAsync(nodes, ref root, model, addImportsService, generator, cancellationToken),
                _ => throw new InvalidEnumArgumentException(nameof(strategy), (int)strategy, typeof(Strategy)),
            };

            if (importDirectivesToAdd.Length == 0)
            {
                return document.WithSyntaxRoot(root); //keep any added simplifier annotations
            }

            if (safe)
            {
                // Mark the context with an annotation. 
                // This will allow us to find it after we have called MakeSafeToAddNamespaces.
                var annotation = new SyntaxAnnotation();
                document = document.WithSyntaxRoot(root.ReplaceNode(context, context.WithAdditionalAnnotations(annotation)));
                root = (await document.GetSyntaxRootAsync().ConfigureAwait(false))!;

                model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(model);

                // Make Safe to add namespaces
                document = document.WithSyntaxRoot(
                   MakeSafeToAddNamespaces(root, namespaceSymbols, model, document.Project.Solution.Workspace, cancellationToken));
                root = (await document.GetSyntaxRootAsync().ConfigureAwait(false))!;

                model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(model);

                // Find the context. It might be null if we have removed the context in the process of complexifying the tree.
                context = root.DescendantNodesAndSelf().FirstOrDefault(x => x.HasAnnotation(annotation)) ?? root;
            }

            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            root = addImportsService.AddImports(model.Compilation, root, context, importDirectivesToAdd, placeSystemNamespaceFirst, cancellationToken);

            return document.WithSyntaxRoot(root);

            bool IsInSpan(SyntaxNode node) =>
                spansTree.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root">ref as we add simplifier annotations to nodes with explicit namespaces</param>
        /// <returns></returns>
        private (ImmutableArray<SyntaxNode> imports, IEnumerable<INamespaceSymbol> namespaceSymbols, SyntaxNode? context) GetImportDirectivesFromSyntaxesAsync(
                IEnumerable<SyntaxNode> syntaxNodes,
                ref SyntaxNode root,
                SemanticModel model,
                IAddImportsService addImportsService,
                SyntaxGenerator generator,
                CancellationToken cancellationToken
            )
        {
            var importsToAdd = ArrayBuilder<SyntaxNode>.GetInstance();

            var nodesWithExplicitNamespaces = syntaxNodes
                .Select(n => (syntaxnode: n, namespaceSymbol: GetExplicitNamespaceSymbol(n, model)))
                .Where(x => x.namespaceSymbol != null);

            var nodesToSimplify = ArrayBuilder<SyntaxNode>.GetInstance();

            var addedSymbols = new HashSet<INamespaceSymbol>();

            foreach (var (node, namespaceSymbol) in nodesWithExplicitNamespaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                nodesToSimplify.Add(node);

                if (addedSymbols.Contains(namespaceSymbol))
                {
                    continue;
                }

                var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);

                if (addImportsService.HasExistingImport(model.Compilation, root, node, namespaceSyntax))
                {
                    continue;
                }

                if (IsInsideNamespace(node, namespaceSymbol, model, cancellationToken))
                {
                    continue;
                }

                addedSymbols.Add(namespaceSymbol);
                importsToAdd.Add(namespaceSyntax);
            }

            if (nodesToSimplify.Count == 0)
            {
                nodesToSimplify.Free();
                return (importsToAdd.ToImmutableAndFree(), addedSymbols, null);
            }

            var annotation = new SyntaxAnnotation();

            root = root.ReplaceNodes(
                    nodesToSimplify,
                    (o, r) => r.WithAdditionalAnnotations(Simplifier.Annotation, annotation));

            var first = root.DescendantNodesAndSelf().First(x => x.HasAnnotation(annotation));
            var last = root.DescendantNodesAndSelf().Last(x => x.HasAnnotation(annotation));

            nodesToSimplify.Free();
            return (importsToAdd.ToImmutableAndFree(), addedSymbols, first.GetCommonRoot(last));
        }

        private (ImmutableArray<SyntaxNode> imports, IEnumerable<INamespaceSymbol> namespaceSymbols, SyntaxNode? context) GetImportDirectivesFromAnnotatedNodesAsync(
            IEnumerable<SyntaxNode> syntaxNodes,
            SyntaxNode root,
            SemanticModel model,
            IAddImportsService addImportsService,
            SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            SyntaxNode? first = null;
            SyntaxNode? last = null;
            var importsToAdd = ArrayBuilder<SyntaxNode>.GetInstance();

            var annotatedNodes = syntaxNodes.Where(x => x.HasAnnotations(SymbolAnnotation.Kind));
            var addedSymbols = new HashSet<INamespaceSymbol>();
            foreach (var annotatedNode in annotatedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (annotatedNode.GetAnnotations(DoNotAddImportsAnnotation.Kind).Any())
                {
                    continue;
                }

                var annotations = annotatedNode.GetAnnotations(SymbolAnnotation.Kind);
                foreach (var annotation in annotations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var namedType in SymbolAnnotation.GetSymbols(annotation, model.Compilation).OfType<INamedTypeSymbol>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (namedType.OriginalDefinition.IsSpecialType() || namedType.IsNullable())
                        {
                            continue;
                        }

                        var namespaceSymbol = namedType.ContainingNamespace;
                        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
                        {
                            continue;
                        }

                        first ??= annotatedNode;
                        last = annotatedNode;

                        if (addedSymbols.Contains(namespaceSymbol))
                        {
                            continue;
                        }

                        var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);

                        if (addImportsService.HasExistingImport(model.Compilation, root, annotatedNode, namespaceSyntax))
                        {
                            continue;
                        }

                        if (IsInsideNamespace(annotatedNode, namespaceSymbol, model, cancellationToken))
                        {
                            continue;
                        }

                        addedSymbols.Add(namespaceSymbol);
                        importsToAdd.Add(namespaceSyntax);
                    }
                }
            }

            // we don't add simplifier annotations here, 
            // since whatever added the symbol annotation probably also added simplifier annotations,
            // and if not they probably didn't for a reason

            return (importsToAdd.ToImmutableAndFree(), addedSymbols, first?.GetCommonRoot(last));
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

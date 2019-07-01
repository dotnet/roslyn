// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
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
            OptionSet options,
            CancellationToken cancellationToken)
        {
            options = options ?? await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportsService = document.GetLanguageService<IAddImportsService>();
            var generator = SyntaxGenerator.GetGenerator(document);

            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan>(TextSpanIntervalIntrospector.Instance, spans);

            bool isInSpan(SyntaxNode node) =>
                spansTree.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length);

            var nodes = root.DescendantNodesAndSelf().Where(isInSpan);
            var (importDirectivesToAdd, context) = strategy switch
            {
                Strategy.AddImportsFromSymbolAnnotations
                    => GetImportDirectivesFromAnnotatedNodesAsync(nodes, root, model, addImportsService, generator, cancellationToken),
                Strategy.AddImportsFromSyntaxes
                    => GetImportDirectivesFromSyntaxesAsync(nodes, ref root, model, addImportsService, generator, cancellationToken),
                _ => throw new InvalidEnumArgumentException(nameof(strategy), (int)strategy, typeof(Strategy)),
            };

            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            root = addImportsService.AddImports(model.Compilation, root, context, importDirectivesToAdd, placeSystemNamespaceFirst);

            return document.WithSyntaxRoot(root);
        }

        protected abstract INamespaceSymbol GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model);

        /// <summary>
        /// Gets the namespace <paramref name="node"/> is contained in, or null otherwise
        /// </summary>
        protected abstract INamespaceSymbol GetContainedNamespace(SyntaxNode node, SemanticModel model);

        private SyntaxNode GenerateNamespaceImportDeclaration(INamespaceSymbol namespaceSymbol, SyntaxGenerator generator)
        {
            return generator
                .NamespaceImportDeclaration(namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat))
                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
        }

        private (IEnumerable<SyntaxNode> imports, SyntaxNode context) GetImportDirectivesFromSyntaxesAsync(
                IEnumerable<SyntaxNode> syntaxNodes,
                ref SyntaxNode root,
                SemanticModel model,
                IAddImportsService addImportsService,
                SyntaxGenerator generator,
                CancellationToken cancellationToken
            )
        {
            var importsToAdd = new List<SyntaxNode>();

            var nodesWithExplicitNamespaces = syntaxNodes
                .Select(n => (syntaxnode: n, namespaceSymbol: GetExplicitNamespaceSymbol(n, model)))
                .Where(x => x.namespaceSymbol != null);

            var nodesToSimplify = new List<SyntaxNode>();

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

                if (IsInsideNamespace(node, namespaceSymbol, model))
                {
                    continue;
                }

                addedSymbols.Add(namespaceSymbol);
                importsToAdd.Add(namespaceSyntax);
            }

            if (nodesToSimplify.Count is 0)
            {
                return (importsToAdd, null);
            }

            var annotation = new SyntaxAnnotation();

            root = root.ReplaceNodes(
                    nodesToSimplify,
                    (o, r) => r.WithAdditionalAnnotations(Simplifier.Annotation, annotation));

            var first = root.DescendantNodesAndSelf().First(x => x.HasAnnotation(annotation));
            var last = root.DescendantNodesAndSelf().Last(x => x.HasAnnotation(annotation));

            return (importsToAdd, first.GetCommonRoot(last));
        }

        private (IEnumerable<SyntaxNode> imports, SyntaxNode context) GetImportDirectivesFromAnnotatedNodesAsync(
            IEnumerable<SyntaxNode> syntaxNodes,
            SyntaxNode root,
            SemanticModel model,
            IAddImportsService addImportsService,
            SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            SyntaxNode first = null;
            SyntaxNode last = null;
            var importsToAdd = new List<SyntaxNode>();

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
                        if (IsBuiltIn(namedType))
                        {
                            continue;
                        }

                        var namespaceSymbol = namedType.ContainingNamespace;
                        if (namespaceSymbol?.IsGlobalNamespace != false)
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

                        if(IsInsideNamespace(annotatedNode, namespaceSymbol, model))
                        {
                            continue;
                        }

                        addedSymbols.Add(namespaceSymbol);
                        importsToAdd.Add(namespaceSyntax);
                    }
                }
            }

            return (importsToAdd, first?.GetCommonRoot(last));
        }

        private bool IsBuiltIn(INamedTypeSymbol type)
        {
            switch (type.OriginalDefinition.SpecialType)
            {
                case SpecialType.System_Object:
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_Nullable_T:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the namespace declaration <paramref name="node"/> is contained inside,
        /// or any of its ancestor namespaces are the same as <paramref name="symbol"/>
        /// </summary>
        private bool IsInsideNamespace(SyntaxNode node, INamespaceSymbol symbol, SemanticModel model)
        {
            var containedNamespace = GetContainedNamespace(node, model);

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

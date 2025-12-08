// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing;

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
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        // Create a simple interval tree for simplification spans.
        var spansTree = new TextSpanMutableIntervalTree(spans);

        // Only dive deeper into nodes that actually overlap with the span we care about.  And also only include
        // those child nodes that themselves overlap with the span.  i.e. if we have:
        //
        //                Parent
        //       /                    \
        //      A  [|   B     C   |]   D
        //
        // We'll dive under the parent because it overlaps with the span.  But we only want to include (and dive
        // into) B and C not A and D.
        if (strategy == Strategy.AddImportsFromSymbolAnnotations)
        {
            var nodes = root.DescendantNodesAndSelf(n => OverlapsWithSpan(n) && n.ContainsAnnotations).Where(OverlapsWithSpan);
            var annotatedNodes = nodes.Where(x => x.HasAnnotations(SymbolAnnotation.Kind));

            return await AddImportDirectivesFromSymbolAnnotationsAsync(document, annotatedNodes, addImportsService, generator, options, cancellationToken).ConfigureAwait(false);
        }

        if (strategy == Strategy.AddImportsFromSyntaxes)
        {
            var nodes = root.DescendantNodesAndSelf(OverlapsWithSpan).Where(OverlapsWithSpan);

            return await AddImportDirectivesFromSyntaxesAsync(document, nodes, addImportsService, generator, options, cancellationToken).ConfigureAwait(false);
        }

        throw ExceptionUtilities.UnexpectedValue(strategy);

        bool OverlapsWithSpan(SyntaxNode n) => spansTree.HasIntervalThatOverlapsWith(n.FullSpan.Start, n.FullSpan.Length);
    }

    protected abstract INamespaceSymbol? GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model);

    private async Task<ISet<INamespaceSymbol>> GetSafeToAddImportsAsync(
        ImmutableArray<INamespaceSymbol> namespaceSymbols,
        SyntaxNode container,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        using var _ = PooledHashSet<INamespaceSymbol>.GetInstance(out var conflicts);
        await AddPotentiallyConflictingImportsAsync(
            model, container, namespaceSymbols, conflicts, cancellationToken).ConfigureAwait(false);
        return namespaceSymbols.Except(conflicts).ToSet();
    }

    /// <summary>
    /// Looks at the contents of the document for top level identifiers (or existing extension method calls), and
    /// blocks off imports that could potentially bring in a name that would conflict with them.
    /// <paramref name="container"/> is the node that the import will be added to.  This will either be the
    /// compilation-unit node, or one of the namespace-blocks in the file.
    /// </summary>
    protected abstract Task AddPotentiallyConflictingImportsAsync(
        SemanticModel model,
        SyntaxNode container,
        ImmutableArray<INamespaceSymbol> namespaceSymbols,
        HashSet<INamespaceSymbol> conflicts,
        CancellationToken cancellationToken);

    private static SyntaxNode GenerateNamespaceImportDeclaration(INamespaceSymbol namespaceSymbol, SyntaxGenerator generator)
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
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var importsToAdd);
        using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToSimplify);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var nodesWithExplicitNamespaces = syntaxNodes
            .Select(n => (syntaxnode: n, namespaceSymbol: GetExplicitNamespaceSymbol(n, model)));

        var addedSymbols = new HashSet<INamespaceSymbol>();
        foreach (var (node, namespaceSymbol) in nodesWithExplicitNamespaces)
        {
            if (namespaceSymbol is null)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            nodesToSimplify.Add(node);

            if (addedSymbols.Contains(namespaceSymbol))
                continue;

            var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);
            if (addImportsService.HasExistingImport(model, root, node, namespaceSyntax, generator, cancellationToken))
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

        root = addImportsService.AddImports(
            model, root, context, importsToAdd, generator, options, cancellationToken);

        return document.WithSyntaxRoot(root);
    }

    private async Task<Document> AddImportDirectivesFromSymbolAnnotationsAsync(
        Document document,
        IEnumerable<SyntaxNode> annotatedNodes,
        IAddImportsService addImportsService,
        SyntaxGenerator generator,
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = PooledDictionary<INamespaceSymbol, SyntaxNode>.GetInstance(out var importToSyntax);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

#if !WORKSPACE
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
#else
        var model = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
#endif

        SyntaxNode? first = null, last = null;

        foreach (var annotatedNode in annotatedNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (annotatedNode.HasAnnotations(DoNotAddImportsAnnotation.Kind))
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

                    if (importToSyntax.ContainsKey(namespaceSymbol))
                        continue;

                    var namespaceSyntax = GenerateNamespaceImportDeclaration(namespaceSymbol, generator);
                    if (addImportsService.HasExistingImport(model, root, annotatedNode, namespaceSyntax, generator, cancellationToken))
                        continue;

                    if (IsInsideNamespace(annotatedNode, namespaceSymbol, model, cancellationToken))
                        continue;

                    importToSyntax[namespaceSymbol] = namespaceSyntax;
                }
            }
        }

        if (first == null || last == null || importToSyntax.Count == 0)
            return document;

        var context = first.GetCommonRoot(last);

        // Find the namespace/compilation-unit we'll be adding all these imports to.
        var importContainer = addImportsService.GetImportContainer(root, context, importToSyntax.First().Value, options);

        // Now remove any imports we think can cause conflicts in that container.
        var safeImportsToAdd = await GetSafeToAddImportsAsync(
            [.. importToSyntax.Keys],
            importContainer,
            model,
            cancellationToken).ConfigureAwait(false);

        var importsToAdd = importToSyntax.SelectAsArray(
            predicate: kvp => safeImportsToAdd.Contains(kvp.Key),
            selector: kvp => kvp.Value);
        if (importsToAdd.Length == 0)
            return document;

        root = addImportsService.AddImports(model, root, context, importsToAdd, generator, options, cancellationToken);
        return document.WithSyntaxRoot(root);
    }

    /// <summary>
    /// Checks if the namespace declaration <paramref name="node"/> is contained inside,
    /// or any of its ancestor namespaces are the same as <paramref name="symbol"/>
    /// </summary>
    private static bool IsInsideNamespace(SyntaxNode node, INamespaceSymbol symbol, SemanticModel model, CancellationToken cancellationToken)
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

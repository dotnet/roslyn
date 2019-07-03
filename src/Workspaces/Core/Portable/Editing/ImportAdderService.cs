// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
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
        public async Task<Document> AddImportsAsync(
            Document document, IEnumerable<TextSpan> spans,
            OptionSet options, CancellationToken cancellationToken)
        {
            options ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan>(TextSpanIntervalIntrospector.Instance, spans);

            bool isInSpan(SyntaxNodeOrToken nodeOrToken) =>
                spansTree.HasIntervalThatOverlapsWith(nodeOrToken.FullSpan.Start, nodeOrToken.FullSpan.Length);

            var nodesWithExplicitNamespaces = root.DescendantNodesAndSelf().Where(n => isInSpan(n) && GetExplicitNamespaceSymbol(n, model) != null).ToList();

            var namespacesToAdd = new HashSet<INamespaceSymbol>();
            namespacesToAdd.AddRange(nodesWithExplicitNamespaces.Select(
                n => GetExplicitNamespaceSymbol(n, model)));

            var generator = SyntaxGenerator.GetGenerator(document);
            var imports = namespacesToAdd.Select(ns => generator.NamespaceImportDeclaration(ns.ToDisplayString()).WithAdditionalAnnotations(Simplifier.Annotation))
                                         .ToArray();

            // annotate these nodes so they get simplified later
            var newRoot = root.ReplaceNodes(
                nodesWithExplicitNamespaces,
                (o, r) => r.WithAdditionalAnnotations(Simplifier.Annotation));

            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
            var addImportsService = document.GetLanguageService<IAddImportsService>();
            var finalRoot = addImportsService.AddImports(
                model.Compilation, newRoot, newRoot, imports, placeSystemNamespaceFirst);

            return document.WithSyntaxRoot(finalRoot);
        }

        protected abstract INamespaceSymbol GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model);
    }
}

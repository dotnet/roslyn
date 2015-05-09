// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    internal abstract class ImportAdderService : ILanguageService
    {
        public async Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options, CancellationToken cancellationToken)
        {
            options = options ?? document.Project.Solution.Workspace.Options;

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = model.SyntaxTree.GetRoot();

            // Create a simple interval tree for simplification spans.
            var spansTree = new SimpleIntervalTree<TextSpan>(TextSpanIntervalIntrospector.Instance, spans);

            Func<SyntaxNodeOrToken, bool> isInSpan = (nodeOrToken) =>
                spansTree.GetOverlappingIntervals(nodeOrToken.FullSpan.Start, nodeOrToken.FullSpan.Length).Any();

            var nodesWithExplicitNamespaces = root.DescendantNodesAndSelf().Where(n => isInSpan(n) && GetExplicitNamespaceSymbol(n, model) != null).ToList();

            var namespacesToAdd = new HashSet<INamespaceSymbol>();
            namespacesToAdd.AddRange(nodesWithExplicitNamespaces.Select(n => GetExplicitNamespaceSymbol(n, model)));

            // annotate these nodes so they get simplified later
            var newRoot = root.ReplaceNodes(nodesWithExplicitNamespaces, (o, r) => r.WithAdditionalAnnotations(Simplifier.Annotation));
            var newDoc = document.WithSyntaxRoot(newRoot);
            var newModel = await newDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            newRoot = this.AddNamespaceImports(newDoc, newModel, options, namespacesToAdd);
            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode AddNamespaceImports(
            Document document,
            SemanticModel model,
            OptionSet options,
            IEnumerable<INamespaceSymbol> namespaces)
        {
            var existingNamespaces = new HashSet<INamespaceSymbol>();
            this.GetExistingImportedNamespaces(document, model, existingNamespaces);

            var namespacesToAdd = new HashSet<INamespaceSymbol>(namespaces);
            namespacesToAdd.RemoveAll(existingNamespaces);

            var root = model.SyntaxTree.GetRoot();
            if (namespacesToAdd.Count == 0)
            {
                return root;
            }

            var gen = SyntaxGenerator.GetGenerator(document);

            var newRoot = root;
            foreach (var import in namespacesToAdd.Select(ns => gen.NamespaceImportDeclaration(ns.ToDisplayString()).WithAdditionalAnnotations(Simplifier.Annotation)))
            {
                newRoot = this.InsertNamespaceImport(newRoot, gen, import, options);
            }

            return newRoot;
        }

        protected virtual void GetExistingImportedNamespaces(Document document, SemanticModel model, HashSet<INamespaceSymbol> namespaces)
        {
            // only consider top level imports
            var gen = SyntaxGenerator.GetGenerator(document);
            var root = model.SyntaxTree.GetRoot();
            var imports = gen.GetNamespaceImports(root);

            var symbols = imports.Select(imp => GetImportedNamespaceSymbol(imp, model))
                       .OfType<INamespaceSymbol>()
                       .Select(ns => model.Compilation.GetCompilationNamespace(ns))
                       .ToList();

            namespaces.AddRange(symbols);
        }

        protected abstract INamespaceSymbol GetImportedNamespaceSymbol(SyntaxNode import, SemanticModel model);
        protected abstract INamespaceSymbol GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model);
        protected abstract SyntaxNode InsertNamespaceImport(SyntaxNode root, SyntaxGenerator gen, SyntaxNode import, OptionSet options);
    }
}

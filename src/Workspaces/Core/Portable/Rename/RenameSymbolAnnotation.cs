// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Provides a way to produce and consume annotations for rename
    /// that contain the previous symbol serialized as a <see cref="SymbolKey" />.
    /// This annotations is used by <see cref="Workspace.TryApplyChanges(Solution)" /> 
    /// in some cases to notify the workspace host of refactorings.
    /// </summary>
    internal static class RenameSymbolAnnotation
    {
        public const string RenameSymbolKind = nameof(RenameSymbolAnnotation);

        public static SyntaxAnnotation Create(ISymbol oldSymbol)
            => new SyntaxAnnotation(RenameSymbolKind, SerializeData(oldSymbol));

        public static async Task<ImmutableDictionary<ISymbol, ISymbol>> GatherChangedSymbolsInDocumentsAsync(
            IEnumerable<DocumentId> changedDocumentIds,
            Solution newSolution, Solution oldSolution,
            CancellationToken cancellationToken)
        {
            var changedSymbols = ImmutableDictionary.CreateBuilder<ISymbol, ISymbol>();

            foreach (var changedDocId in changedDocumentIds)
            {
                var newDocument = newSolution.GetDocument(changedDocId);

                // Documents without syntax tree won't have the annotations attached
                if (newDocument is null || !(newDocument.SupportsSyntaxTree && newDocument.SupportsSemanticModel))
                {
                    continue;
                }

                var syntaxRoot = newDocument.GetSyntaxRootSynchronously(cancellationToken);
                var symbolRenameNodes = syntaxRoot.GetAnnotatedNodes(RenameSymbolKind);

                foreach (var node in symbolRenameNodes)
                {
                    foreach (var annotation in node.GetAnnotations(RenameSymbolKind))
                    {
                        var oldDocument = oldSolution.GetDocument(changedDocId);
                        if (oldDocument is null)
                        {
                            continue;
                        }

                        var newSemanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var currentSemanticModel = await oldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        var oldSymbol = ResolveSymbol(annotation, currentSemanticModel.Compilation);
                        var newSymbol = newSemanticModel.GetDeclaredSymbol(node);

                        changedSymbols.Add(oldSymbol, newSymbol);
                    }
                }
            }

            return changedSymbols.ToImmutable();
        }

        internal static ISymbol ResolveSymbol(SyntaxAnnotation annotation, Compilation oldCompilation)
        {
            if (annotation.Kind != RenameSymbolKind)
            {
                throw new InvalidOperationException($"'{annotation}' is not of kind {RenameSymbolKind}");
            }

            if (string.IsNullOrEmpty(annotation.Data))
            {
                throw new InvalidOperationException($"'{annotation}' has no data");
            }

            var oldSymbolKey = SymbolKey.ResolveString(annotation.Data, oldCompilation);

            return oldSymbolKey.Symbol;
        }

        private static string SerializeData(ISymbol oldSymbol)
            => oldSymbol.GetSymbolKey().ToString();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

#nullable enable 

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Provides a way to produce and consume annotations for rename
    /// that contain the previous symbol serialized as a <see cref="SymbolKey" />.
    /// This annotations is used by <see cref="Workspace.TryApplyChanges(Solution)" /> 
    /// in some cases to notify the workspace host of refactorings.
    /// </summary>
    /// <remarks>
    /// This annotation is applied to the declaring syntax of a symbol that has been renamed. 
    /// When Workspace.TryApplyChanges happens in Visual Studio, we raise rename events for that symbol.
    /// </remarks>
    internal static class RenameSymbolAnnotation
    {
        public const string RenameSymbolKind = nameof(RenameSymbolAnnotation);

        public static bool ShouldAnnotateSymbol(ISymbol symbol)
            => symbol.DeclaringSyntaxReferences.Any();

        public static SyntaxAnnotation? Create(ISymbol oldSymbol)
        {
            return ShouldAnnotateSymbol(oldSymbol)
                ? new SyntaxAnnotation(RenameSymbolKind, SerializeData(oldSymbol))
                : null;
        }

        public static TNode WithRenameSymbolAnnotation<TNode>(this TNode node, SemanticModel semanticModel)
            where TNode : SyntaxNode
            => node.WithAdditionalAnnotations(Create(semanticModel.GetDeclaredSymbol(node)));

        public static async Task<ImmutableDictionary<ISymbol, ISymbol>> GatherChangedSymbolsInDocumentsAsync(
            IEnumerable<DocumentId> changedDocumentIds,
            Solution newSolution, Solution oldSolution,
            CancellationToken cancellationToken)
        {
            var changedSymbols = ImmutableDictionary.CreateBuilder<ISymbol, ISymbol>();

            foreach (var changedDocId in changedDocumentIds)
            {
                var newDocument = newSolution.GetRequiredDocument(changedDocId);

                // Documents without syntax tree won't have the annotations attached
                if (!newDocument.SupportsSyntaxTree)
                {
                    continue;
                }

                var syntaxRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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

                        var newSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var oldSemanticModel = await oldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        var oldSymbol = ResolveSymbol(annotation, oldSemanticModel.Compilation);
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

        private static string SerializeData(ISymbol symbol)
            => symbol.GetSymbolKey().ToString();
    }
}

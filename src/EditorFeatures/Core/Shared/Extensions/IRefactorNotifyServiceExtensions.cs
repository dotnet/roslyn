// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IRefactorNotifyServiceExtensions
    {
        /// <summary>
        /// Calls all IRefactorNotifyService implementations TryOnBeforeGlobalSymbolRenamed, and if it succeds calls
        /// TryOnAfterGlobalSymbolRenamed. All calls are made on the UI thread, the <see cref="ForegroundThreadAffinitizedObject"/>
        /// is used to ensure this behavior. 
        /// </summary>
        public static void TryNotifyChangesSynchronously(
            this IEnumerable<Lazy<IRefactorNotifyService>> refactorNotifyServices,
            Workspace workspace,
            Solution newSolution,
            Solution oldSolution,
            IThreadingContext threadContext,
            CancellationToken cancellationToken = default)
        {
            // TryOn{Before, After}GlobalSymbolRenamed requires calls from the foreground thread. Check that the
            // caller is calling from the right thread before doing any work.
            threadContext.ThrowIfNotOnUIThread();

            try
            {
                var refactorNotifyTask = refactorNotifyServices.TryNotifyChangesAsync(workspace, newSolution, oldSolution, threadContext, cancellationToken);
                refactorNotifyTask.Wait(cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // No reason to fail because notify fails, but we want to track failure to see if there's something we're doing wrong. This results
                // in a potentially bad user experience, but not complete broken and not worth crashing. 
            }
        }

        private static async Task TryNotifyChangesAsync(
            this IEnumerable<Lazy<IRefactorNotifyService>> refactorNotifyServices,
            Workspace workspace,
            Solution newSolution,
            Solution oldSolution,
            IThreadingContext threadContext,
            CancellationToken cancellationToken)
        {
            var projectChanges = newSolution.GetChanges(oldSolution).GetProjectChanges().ToImmutableArray();
            var changedDocumentIds = projectChanges.SelectMany(pd => pd.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true)).ToImmutableArray();
            var _ = PooledDictionary<ISymbol, ISymbol>.GetInstance(out var changedSymbols);

            foreach (var documentId in changedDocumentIds)
            {
                var newDocument = newSolution.GetRequiredDocument(documentId);
                var oldDocument = oldSolution.GetDocument(documentId);

                if (oldDocument is null)
                {
                    continue;
                }

                var newSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var oldSemanticModel = await oldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var renamedNodes = await GatherAnnotatedNodesAsync(newDocument, cancellationToken).ConfigureAwait(false);

                foreach (var node in renamedNodes)
                {
                    foreach (var annotation in node.GetAnnotations(RenameSymbolAnnotation.RenameSymbolKind))
                    {
                        var oldSymbol = annotation.ResolveSymbol(oldSemanticModel.Compilation);
                        Contract.ThrowIfNull(oldSymbol);

                        var newSymbol = newSemanticModel.GetDeclaredSymbol(node, cancellationToken);
                        Contract.ThrowIfNull(newSymbol);

                        changedSymbols.Add(oldSymbol, newSymbol);
                    }
                }
            }

            // TryOn{Before, After}GlobalSymbolRenamed requires calls from the foreground thread. 
            await threadContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (var (oldSymbol, newSymbol) in changedSymbols)
            {
                foreach (var refactorNotifyService in refactorNotifyServices)
                {
                    if (refactorNotifyService.Value.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocumentIds, oldSymbol, newSymbol.Name, false))
                    {
                        refactorNotifyService.Value.TryOnAfterGlobalSymbolRenamed(workspace, changedDocumentIds, oldSymbol, newSymbol.Name, false);
                    }
                }
            }
        }

        private static async Task<ImmutableArray<SyntaxNode>> GatherAnnotatedNodesAsync(Document document, CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
            {
                return ImmutableArray.Create<SyntaxNode>();
            }

            var changedSymbols = ImmutableArray.CreateBuilder<SyntaxNode>();

            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var symbolRenameNodes = syntaxRoot.GetAnnotatedNodes(RenameSymbolAnnotation.RenameSymbolKind);

            changedSymbols.AddRange(symbolRenameNodes);

            return changedSymbols.ToImmutable();
        }
    }
}

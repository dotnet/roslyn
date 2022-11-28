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
            try
            {
                var refactorNotifyTask = refactorNotifyServices.TryNotifyChangesAsync(workspace, newSolution, oldSolution, threadContext, cancellationToken);
                refactorNotifyTask.Wait(cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                // No reason to fail because notify fails, but we want to track failure to see if there's something we're doing wrong. This results
                // in a potentially bad user experience, but not complete broken and not worth crashing. 
            }
        }

        public static bool TryOnBeforeGlobalSymbolRenamed(
            this IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            Workspace workspace,
            IEnumerable<DocumentId> changedDocuments,
            ISymbol symbol,
            string newName,
            bool throwOnFailure)
        {
            foreach (var refactorNotifyService in refactorNotifyServices)
            {
                if (!refactorNotifyService.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryOnAfterGlobalSymbolRenamed(
            this IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            Workspace workspace,
            IEnumerable<DocumentId> changedDocuments,
            ISymbol symbol,
            string newName,
            bool throwOnFailure)
        {
            foreach (var refactorNotifyService in refactorNotifyServices)
            {
                if (!refactorNotifyService.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task TryNotifyChangesAsync(
            this IEnumerable<Lazy<IRefactorNotifyService>> refactorNotifyServices,
            Workspace workspace,
            Solution newSolution,
            Solution oldSolution,
            IThreadingContext threadContext,
            CancellationToken cancellationToken)
        {
            var renameSymbolNotificationService = workspace.Services.GetService<IRenameSymbolNotificationService>();
            if (renameSymbolNotificationService is null)
            {
                return;
            }

            var projectChanges = newSolution.GetChanges(oldSolution).GetProjectChanges().ToImmutableArray();
            var changedDocumentIds = projectChanges.SelectMany(pd => pd.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true)).ToImmutableArray();

            var changedSymbols = await renameSymbolNotificationService.GetChangedSymbolsAsync(newSolution, oldSolution, cancellationToken).ConfigureAwait(false);
            if (changedSymbols.IsEmpty)
            {
                return;
            }

            // TryOn{Before, After}GlobalSymbolRenamed requires calls from the foreground thread. 
            if (threadContext.HasMainThread)
            {
                await threadContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }

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
    }
}

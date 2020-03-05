// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IRefactorNotifyServiceExtensions
    {
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
    }
}

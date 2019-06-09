// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

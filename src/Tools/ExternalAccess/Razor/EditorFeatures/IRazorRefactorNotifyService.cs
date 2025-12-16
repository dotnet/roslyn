// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

/// <inheritdoc cref="IRefactorNotifyService" />
internal interface IRazorRefactorNotifyService
{
    /// <inheritdoc cref="IRefactorNotifyService.TryOnBeforeGlobalSymbolRenamed" />
    bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure);

    /// <inheritdoc cref="IRefactorNotifyService.TryOnAfterGlobalSymbolRenamed" />
    bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure);
}

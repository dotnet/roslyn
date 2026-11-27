// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rename;

/// <summary>
/// Allows editors to listen to refactoring events and take appropriate action. For example, 
/// when VS knows about a symbol rename, it asks the Xaml language service to update xaml files
/// </summary>
internal interface IRefactorNotifyService
{
    /// <summary>
    /// Notifies any interested parties that a rename action is about to happen. 
    /// Implementers can request the rename action be cancelled, in which case they should 
    /// return false or throw an exception, depending on the throwOnFailure argument. Callers 
    /// should honor cancellation requests by not applying the rename and not calling 
    /// <see cref="TryOnAfterGlobalSymbolRenamed"/>.
    /// </summary>
    /// <returns>True if the rename should proceed.</returns>
    bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure);

    /// <summary>
    /// Notifies any interested parties that a symbol rename has been applied to the 
    /// workspace. This should only be called if <see cref="TryOnBeforeGlobalSymbolRenamed"/> was
    /// called and returned true before the symbol rename was applied to the workspace. 
    /// In the event of a failure to rename, implementers should return false or throw an
    /// exception, depending on the throwOnFailure argument.
    /// </summary>
    /// <returns>True if the rename was successful.</returns>
    bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure);
}

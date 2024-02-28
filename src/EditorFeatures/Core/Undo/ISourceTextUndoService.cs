// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Undo;

/// <summary>
/// A service that allows consumers to register undo transactions for a supplied
/// <see cref="SourceText"/> with a supplied description. The description is the
/// display string by which the IDE's undo stack UI will subsequently refer to the transaction.
/// </summary>
internal interface ISourceTextUndoService : IWorkspaceService
{
    /// <summary>
    /// Registers undo transaction for the supplied <see cref="SourceText"/>.
    /// </summary>
    /// <param name="sourceText">The <see cref="SourceText"/> for which undo transaction is being registered.</param>
    /// <param name="description">The display string by which the IDE's undo stack UI will subsequently refer to the transaction.</param>
    ISourceTextUndoTransaction RegisterUndoTransaction(SourceText sourceText, string description);

    /// <summary>
    /// Starts previously registered undo transaction for the supplied <see cref="ITextSnapshot"/> (if any).
    /// </summary>
    /// <param name="snapshot">The <see cref="ITextSnapshot"/> for the <see cref="SourceText"/> for undo transaction being started.</param>
    /// <remarks>
    /// This method will handle the translation from <see cref="ITextSnapshot"/> to <see cref="SourceText"/>
    /// and update the IDE's undo stack UI with the transaction's previously registered description string.
    /// </remarks>
    bool BeginUndoTransaction(ITextSnapshot snapshot);

    /// <summary>
    /// Completes and deletes the supplied undo transaction.
    /// </summary>
    /// <param name="transaction">The undo transaction that is being ended.</param>
    bool EndUndoTransaction(ISourceTextUndoTransaction transaction);
}

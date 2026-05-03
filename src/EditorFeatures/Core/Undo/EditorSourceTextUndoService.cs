// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Undo;

[ExportWorkspaceService(typeof(ISourceTextUndoService), ServiceLayer.Editor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorSourceTextUndoService(ITextUndoHistoryRegistry undoHistoryRegistry) : ISourceTextUndoService
{
    private readonly Dictionary<SourceText, SourceTextUndoTransaction> _transactions = [];

    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;

    public ISourceTextUndoTransaction RegisterUndoTransaction(SourceText sourceText, string description)
    {
        if (sourceText != null && !string.IsNullOrWhiteSpace(description))
        {
            var transaction = new SourceTextUndoTransaction(this, sourceText, description);
            _transactions.Add(sourceText, transaction);
            return transaction;
        }

        return null;
    }

    public bool BeginUndoTransaction(ITextSnapshot snapshot)
    {
        var sourceText = snapshot?.AsText();
        if (sourceText != null)
        {
            _transactions.TryGetValue(sourceText, out var transaction);
            if (transaction != null)
            {
                return transaction.Begin(_undoHistoryRegistry?.GetHistory(snapshot.TextBuffer));
            }
        }

        return false;
    }

    public bool EndUndoTransaction(ISourceTextUndoTransaction transaction)
    {
        if (transaction != null && _transactions.ContainsKey(transaction.SourceText))
        {
            _transactions.Remove(transaction.SourceText);
            return true;
        }

        return false;
    }

    private sealed class SourceTextUndoTransaction(ISourceTextUndoService service, SourceText sourceText, string description) : ISourceTextUndoTransaction
    {
        private readonly ISourceTextUndoService _service = service;
        public SourceText SourceText { get; } = sourceText;
        public string Description { get; } = description;

        private ITextUndoTransaction _transaction;

        internal bool Begin(ITextUndoHistory undoHistory)
        {
            if (undoHistory != null)
            {
                _transaction = new HACK_TextUndoTransactionThatRollsBackProperly(undoHistory.CreateTransaction(Description));
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _transaction?.Complete();

            _service.EndUndoTransaction(this);
        }
    }
}

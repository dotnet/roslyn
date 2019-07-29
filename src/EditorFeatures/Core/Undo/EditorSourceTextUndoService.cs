// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    [ExportWorkspaceService(typeof(ISourceTextUndoService), ServiceLayer.Editor), Shared]
    internal sealed class EditorSourceTextUndoService : ISourceTextUndoService
    {
        private readonly Dictionary<SourceText, SourceTextUndoTransaction> _transactions = new Dictionary<SourceText, SourceTextUndoTransaction>();

        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;

        [ImportingConstructor]
        public EditorSourceTextUndoService(ITextUndoHistoryRegistry undoHistoryRegistry)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
        }

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

        private sealed class SourceTextUndoTransaction : ISourceTextUndoTransaction
        {
            private readonly ISourceTextUndoService _service;
            public SourceText SourceText { get; }
            public string Description { get; }

            private ITextUndoTransaction _transaction;

            public SourceTextUndoTransaction(ISourceTextUndoService service, SourceText sourceText, string description)
            {
                _service = service;
                SourceText = sourceText;
                Description = description;
            }

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
                if (_transaction != null)
                {
                    _transaction.Complete();
                }

                _service.EndUndoTransaction(this);
            }
        }
    }
}

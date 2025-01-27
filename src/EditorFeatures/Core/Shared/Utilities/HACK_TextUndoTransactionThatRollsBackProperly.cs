// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// An implementation of <see cref="ITextUndoTransaction" /> that wraps another
/// <see cref="ITextUndoTransaction" />. Some undo implementations (notably the VS implementation)
/// violate the specified contract for Cancel(), which states that cancelling an active transaction
/// should undo the primitives that we already added. This works around that problem; calling Cancel()
/// on this forwards the cancellation to the inner transaction, and if it failed to roll back we
/// do it ourselves.
/// </summary>
internal sealed class HACK_TextUndoTransactionThatRollsBackProperly(ITextUndoTransaction innerTransaction) : ITextUndoTransaction
{
    private readonly ITextUndoTransaction _innerTransaction = innerTransaction;
    private readonly RollbackDetectingUndoPrimitive _undoPrimitive = new();

    private bool _transactionOpen = true;

    public bool CanRedo => _innerTransaction.CanRedo;

    public bool CanUndo => _innerTransaction.CanUndo;

    public string Description
    {
        get
        {
            return _innerTransaction.Description;
        }

        set
        {
            _innerTransaction.Description = value;
        }
    }

    public ITextUndoHistory History => _innerTransaction.History;

    public IMergeTextUndoTransactionPolicy MergePolicy
    {
        get
        {
            return _innerTransaction.MergePolicy;
        }

        set
        {
            _innerTransaction.MergePolicy = value;
        }
    }

    public ITextUndoTransaction Parent => _innerTransaction.Parent;

    public UndoTransactionState State => _innerTransaction.State;

    public IList<ITextUndoPrimitive> UndoPrimitives => _innerTransaction.UndoPrimitives;

    public void AddUndo(ITextUndoPrimitive undo)
        => _innerTransaction.AddUndo(undo);

    public void Cancel()
    {
        var transactionWasOpen = _transactionOpen;
        _transactionOpen = false;

        // First, add an undo primitive so we can detect whether or not undo gets called
        if (transactionWasOpen)
        {
            _innerTransaction.AddUndo(_undoPrimitive);
        }

        _innerTransaction.Cancel();

        if (transactionWasOpen && !_undoPrimitive.UndoCalled)
        {
            // Undo each of the primitives in reverse order to clean up. This is slimy.
            foreach (var primitive in _innerTransaction.UndoPrimitives.Reverse())
            {
                primitive.Undo();
            }
        }
    }

    public void Complete()
    {
        _transactionOpen = false;
        _innerTransaction.Complete();
    }

    public void Dispose()
    {
        if (_transactionOpen)
        {
            // Call our cancel method first to ensure we handle it properly
            Cancel();
        }

        _innerTransaction.Dispose();
    }

    public void Do()
        => _innerTransaction.Do();

    public void Undo()
        => _innerTransaction.Undo();

    private class RollbackDetectingUndoPrimitive : ITextUndoPrimitive
    {
        internal bool UndoCalled = false;

        public bool CanRedo => true;

        public bool CanUndo => true;

        public ITextUndoTransaction? Parent { get; set; }

        public bool CanMerge(ITextUndoPrimitive older)
            => false;

        public void Do()
        {
        }

        public ITextUndoPrimitive Merge(ITextUndoPrimitive older)
            => throw new NotSupportedException();

        public void Undo()
            => UndoCalled = true;
    }
}

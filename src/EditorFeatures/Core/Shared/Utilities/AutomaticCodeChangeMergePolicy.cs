// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// a merge policy that should be used for any automatic code changes that could happen in sequences so that
/// all those steps are shown to users as one undo transaction rather than multiple ones
/// </summary>
internal sealed class AutomaticCodeChangeMergePolicy : IMergeTextUndoTransactionPolicy
{
    public static readonly AutomaticCodeChangeMergePolicy Instance = new();

    public bool CanMerge(ITextUndoTransaction newerTransaction, ITextUndoTransaction olderTransaction)
    {
        // We want to merge with any other transaction of our policy type
        return true;
    }

    public void PerformTransactionMerge(ITextUndoTransaction existingTransaction, ITextUndoTransaction newTransaction)
    {
        // Add all of our commit primitives into the existing transaction
        foreach (var primitive in newTransaction.UndoPrimitives)
        {
            existingTransaction.UndoPrimitives.Add(primitive);
        }
    }

    public bool TestCompatiblePolicy(IMergeTextUndoTransactionPolicy other)
    {
        // We are compatible with any other merging policy
        return other is AutomaticCodeChangeMergePolicy;
    }
}

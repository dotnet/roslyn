// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// a merge policy that should be used for any automatic code changes that could happen in sequences so that
    /// all those steps are shown to users as one undo transaction rather than multiple ones
    /// </summary>
    internal class AutomaticCodeChangeMergePolicy : IMergeTextUndoTransactionPolicy
    {
        public static readonly AutomaticCodeChangeMergePolicy Instance = new AutomaticCodeChangeMergePolicy();

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
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal enum CompletionFilterReason
    {
        Insertion,
        Deletion,
        NonInsertionOrDeletion,
#if false
        // If necessary, we could add additional filter reasons.  For example, for the below items.
        // However, we have no need for them currently.  That somewhat makes sense.  We only want
        // to really customize our filtering behavior depending on if a user was typing/deleting
        // in the buffer.

        Snippets,
        ItemFiltersChanged,
        CaretPositionChanged,
        Invoke,
        InvokeAndCommitIfUnique
#endif
    }

    internal static class CompletionTriggerExtensions
    {
        public static CompletionFilterReason GetFilterReason(this CompletionTrigger trigger)
            => trigger.Kind.GetFilterReason();
    }

    internal static class CompletionTriggerKindExtensions
    {
        public static CompletionFilterReason GetFilterReason(this CompletionTriggerKind kind)
        {
            switch (kind)
            {
                case CompletionTriggerKind.Insertion:
                    return CompletionFilterReason.Insertion;
                case CompletionTriggerKind.Deletion:
                    return CompletionFilterReason.Deletion;
                case CompletionTriggerKind.Snippets:
                case CompletionTriggerKind.Invoke: 
                case CompletionTriggerKind.InvokeAndCommitIfUnique:
                    return CompletionFilterReason.NonInsertionOrDeletion;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static bool ShouldFilterAgainstUserText(this CompletionTriggerKind kind)
            => kind != CompletionTriggerKind.Invoke && kind != CompletionTriggerKind.Deletion;
    }
}
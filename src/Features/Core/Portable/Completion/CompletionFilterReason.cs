// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal enum CompletionFilterReason
    {
        Insertion,
        Deletion,
        Snippets,
        ItemFiltersChanged,
        CaretPositionChanged,
        Invoke,
        InvokeAndCommitIfUnique
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
                case CompletionTriggerKind.Insertion: return CompletionFilterReason.Insertion;
                case CompletionTriggerKind.Deletion: return CompletionFilterReason.Deletion;
                case CompletionTriggerKind.Snippets: return CompletionFilterReason.Snippets;
                case CompletionTriggerKind.Invoke: return CompletionFilterReason.Invoke;
                case CompletionTriggerKind.InvokeAndCommitIfUnique: return CompletionFilterReason.InvokeAndCommitIfUnique;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static bool ShouldFilterAgainstUserText(this CompletionTriggerKind kind)
            => kind != CompletionTriggerKind.Invoke && kind != CompletionTriggerKind.Deletion;
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion;

internal enum CompletionFilterReason
{
    Insertion,
    Deletion,
    CaretPositionChanged,
    Other,

#if false
    // If necessary, we could add additional filter reasons.  For example, for the below items.
    // However, we have no need for them currently.  That somewhat makes sense.  We only want
    // to really customize our filtering behavior depending on if a user was typing/deleting
    // in the buffer.
    Snippets,
    ItemFiltersChanged,
    Invoke,
    InvokeAndCommitIfUnique
#endif
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotUtilities
{
    public static ImmutableArray<TextChange> NormalizeCopilotTextChanges(IEnumerable<TextChange> textChanges)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
        foreach (var textChange in textChanges)
            builder.Add(textChange);

        // Ensure everything is sorted.
        builder.Sort(static (c1, c2) => c1.Span.Start - c2.Span.Start);

        // Now, go through and make sure no edit overlaps another.
        for (int i = 1, n = builder.Count; i < n; i++)
        {
            var lastEdit = builder[i - 1];
            var currentEdit = builder[i];

            if (lastEdit.Span.OverlapsWith(currentEdit.Span))
                return default;
        }

        // Things look good.  Can process these sorted edits.
        return builder.ToImmutableAndClear();
    }
}

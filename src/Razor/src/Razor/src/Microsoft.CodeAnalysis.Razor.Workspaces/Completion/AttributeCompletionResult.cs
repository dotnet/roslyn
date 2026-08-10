// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class AttributeCompletionResult
{
    public IReadOnlyDictionary<string, IEnumerable<BoundAttributeDescriptor>> Completions { get; }

    private AttributeCompletionResult(IReadOnlyDictionary<string, IEnumerable<BoundAttributeDescriptor>> completions)
    {
        Completions = completions;
    }

    internal static AttributeCompletionResult Create(Dictionary<string, HashSet<BoundAttributeDescriptor>> completions)
    {
        if (completions is null)
        {
            throw new ArgumentNullException(nameof(completions));
        }

        var readonlyCompletions = new Dictionary<string, IEnumerable<BoundAttributeDescriptor>>(
            capacity: completions.Count,
            comparer: completions.Comparer);

        foreach (var (key, value) in completions)
        {
            readonlyCompletions.Add(key, value);
        }

        return new AttributeCompletionResult(readonlyCompletions);
    }
}

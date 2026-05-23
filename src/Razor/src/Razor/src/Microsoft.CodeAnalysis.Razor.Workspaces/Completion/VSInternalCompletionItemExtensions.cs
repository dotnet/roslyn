// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class VSInternalCompletionItemExtensions
{
    public const string ResultIdKey = "_resultId";

    public static bool TryGetCompletionListResultIds(this VSInternalCompletionItem completion, out ImmutableArray<int> resultIds)
    {
        if (!CompletionListMerger.TrySplit(completion.Data, out var splitData))
        {
            resultIds = default;
            return false;
        }

        using var ids = new PooledArrayBuilder<int>();
        for (var i = 0; i < splitData.Length; i++)
        {
            var data = splitData[i];
            if (data.TryGetProperty(ResultIdKey, out var resultIdElement) &&
                resultIdElement.TryGetInt32(out var resultId))
            {
                ids.Add(resultId);
            }
        }

        if (ids.Count > 0)
        {
            resultIds = ids.ToImmutable();
            return true;
        }

        resultIds = default;
        return false;
    }

    public static void UseCommitCharactersFrom(
        this VSInternalCompletionItem completionItem,
        RazorCompletionItem razorCompletionItem,
        VSInternalClientCapabilities clientCapabilities)
    {
        var razorCommitCharacters = razorCompletionItem.CommitCharacters;
        if (razorCommitCharacters.IsEmpty)
        {
            return;
        }

        var supportsVSExtensions = clientCapabilities?.SupportsVisualStudioExtensions ?? false;
        if (supportsVSExtensions)
        {
            completionItem.VsCommitCharacters = RazorCommitCharacter.ToVsCommitCharacters(razorCommitCharacters);
        }
        else
        {
            completionItem.CommitCharacters = RazorCommitCharacter.ToStringCommitCharacters(razorCommitCharacters);
        }
    }
}

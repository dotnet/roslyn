// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractLoadDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
{
    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
         filterCharacterRules: [],
         commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())],
         enterKeyRule: EnterKeyRule.Never,
         selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    private static ImmutableArray<char> GetCommitCharacters()
    {
        using var builder = TemporaryArray<char>.Empty;
        builder.Add('"');
        if (PathUtilities.IsUnixLikePlatform)
        {
            builder.Add('/');
        }
        else
        {
            builder.Add('/');
            builder.Add('\\');
        }

        return builder.ToImmutableAndClear();
    }

    protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
    {
        var helper = GetFileSystemCompletionHelper(context.Document, Glyph.CSharpFile, [".csx"], s_rules);
        context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
    }
}

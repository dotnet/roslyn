// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal abstract class AbstractLoadDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
    {
        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
             filterCharacterRules: ImmutableArray<CharacterSetModificationRule>.Empty,
             commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())),
             enterKeyRule: EnterKeyRule.Never,
             selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

        private static ImmutableArray<char> GetCommitCharacters()
        {
            using var builder = ArrayBuilder<char>.GetInstance();
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

            return builder.ToImmutable();
        }

        protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
        {
            var helper = GetFileSystemCompletionHelper(context.Document, Glyph.CSharpFile, ImmutableArray.Create(".csx"), s_rules);
            context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
        }
    }
}

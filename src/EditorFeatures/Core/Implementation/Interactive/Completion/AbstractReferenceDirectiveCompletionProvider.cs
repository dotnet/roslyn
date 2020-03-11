﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal abstract class AbstractReferenceDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
    {
        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
            filterCharacterRules: ImmutableArray<CharacterSetModificationRule>.Empty,
            commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())),
            enterKeyRule: EnterKeyRule.Never,
            selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

        private static readonly char[] s_pathIndicators = new char[] { '/', '\\', ':' };

        private static ImmutableArray<char> GetCommitCharacters()
        {
            using var builderDisposer = ArrayBuilder<char>.GetInstance(out var builder);

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

            if (GacFileResolver.IsAvailable)
            {
                builder.Add(',');
            }

            return builder.ToImmutable();
        }

        protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
        {
            if (GacFileResolver.IsAvailable && pathThroughLastSlash.IndexOfAny(s_pathIndicators) < 0)
            {
                var gacHelper = new GlobalAssemblyCacheCompletionHelper(s_rules);
                context.AddItems(await gacHelper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
            }

            if (pathThroughLastSlash.IndexOf(',') < 0)
            {
                var helper = GetFileSystemCompletionHelper(context.Document, Glyph.Assembly, ImmutableArray.Create(".dll", ".exe"), s_rules);
                context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
            }
        }
    }
}

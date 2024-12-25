// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractReferenceDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
{
    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
        filterCharacterRules: [],
        commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())],
        enterKeyRule: EnterKeyRule.Never,
        selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    private static readonly char[] s_pathIndicators = ['/', '\\', ':'];

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

        return builder.ToImmutableAndClear();
    }

    protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
    {
        var resolver = context.Document.Project.CompilationOptions.MetadataReferenceResolver as RuntimeMetadataReferenceResolver;
        if (resolver != null && pathThroughLastSlash.IndexOfAny(s_pathIndicators) < 0)
        {
            foreach (var (name, path) in resolver.TrustedPlatformAssemblies)
            {
                context.AddItem(CommonCompletionItem.Create(name, displayTextSuffix: "", glyph: Glyph.Assembly, rules: s_rules));
                context.AddItem(CommonCompletionItem.Create(PathUtilities.GetFileName(path, includeExtension: true), displayTextSuffix: "", glyph: Glyph.Assembly, rules: s_rules));
            }

            if (resolver.GacFileResolver is object)
            {
                var gacHelper = new GlobalAssemblyCacheCompletionHelper(s_rules);
                context.AddItems(await gacHelper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
            }
        }

        if (pathThroughLastSlash.IndexOf(',') < 0)
        {
            var helper = GetFileSystemCompletionHelper(context.Document, Glyph.Assembly, RuntimeMetadataReferenceResolver.AssemblyExtensions, s_rules);
            context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
        }
    }
}

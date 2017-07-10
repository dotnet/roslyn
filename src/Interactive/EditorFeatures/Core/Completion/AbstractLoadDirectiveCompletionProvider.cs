// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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
            var builder = ArrayBuilder<char>.GetInstance();
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

            return builder.ToImmutableAndFree();
        }

        private FileSystemCompletionHelper GetFileSystemCompletionHelper(SourceText text, Document document)
        {
            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolver.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - MiscFilesWorkspace for loose .csx files
            var searchPaths = (document.Project.CompilationOptions.SourceReferenceResolver as SourceFileResolver)?.SearchPaths ?? ImmutableArray<string>.Empty;

            return new FileSystemCompletionHelper(
                Glyph.OpenFolder,
                Glyph.CSharpFile,
                searchPaths: searchPaths,
                baseDirectoryOpt: GetBaseDirectory(text, document),
                allowableExtensions: ImmutableArray.Create(".csx"),
                itemRules: s_rules);
        }

        protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
        {
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var helper = GetFileSystemCompletionHelper(text, context.Document);
            context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
        }
    }
}

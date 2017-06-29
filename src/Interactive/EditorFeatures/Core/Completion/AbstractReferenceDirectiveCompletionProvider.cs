// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
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

            if (GacFileResolver.IsAvailable)
            {
                builder.Add(',');
            }

            return builder.ToImmutableAndFree();
        }

        private FileSystemCompletionHelper GetFileSystemCompletionHelper(SourceText text, Document document)
        {
            var referenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver;
            
            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolvers.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - MiscFilesWorkspace for loose .csx files
            ImmutableArray<string> searchPaths;

            if (referenceResolver is RuntimeMetadataReferenceResolver rtResolver)
            {
                searchPaths = rtResolver.PathResolver.SearchPaths;
            }
            else if (referenceResolver is WorkspaceMetadataFileReferenceResolver workspaceResolver)
            {
                searchPaths = workspaceResolver.PathResolver.SearchPaths;
            }
            else
            {
                searchPaths = ImmutableArray<string>.Empty;
            }

            return new FileSystemCompletionHelper(
                Glyph.OpenFolder,
                Glyph.Assembly,
                searchPaths: searchPaths,
                baseDirectoryOpt: GetBaseDirectory(text, document),
                allowableExtensions: ImmutableArray.Create(".dll", ".exe"),
                itemRules: s_rules);
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
                var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
                var fileSystemHelper = GetFileSystemCompletionHelper(text, context.Document);
                context.AddItems(await fileSystemHelper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
            }
        }
    }
}

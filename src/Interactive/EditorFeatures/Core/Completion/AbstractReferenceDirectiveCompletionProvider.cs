// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal abstract partial class AbstractReferenceDirectiveCompletionProvider : CompletionListProvider
    {
        protected abstract bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken);

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return PathCompletionUtilities.IsTriggerCharacter(text, characterPosition);
        }

        private TextSpan GetTextChangeSpan(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetTextChangeSpan(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }

        private static ICurrentWorkingDirectoryDiscoveryService GetFileSystemDiscoveryService(ITextSnapshot textSnapshot)
        {
            return CurrentWorkingDirectoryDiscoveryService.GetService(textSnapshot);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // first try to get the #r string literal token.  If we couldn't, then we're not in a #r
            // reference directive and we immediately bail.
            SyntaxToken stringLiteral;
            if (!TryGetStringLiteralToken(tree, position, out stringLiteral, cancellationToken))
            {
                return;
            }

            var textChangeSpan = this.GetTextChangeSpan(stringLiteral, position);

            var gacHelper = new GlobalAssemblyCacheCompletionHelper(this, textChangeSpan, ItemRules.Instance);
            var text = await document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot == null)
            {
                // Passing null to GetFileSystemDiscoveryService raises an exception.
                // Instead, return here since there is no longer snapshot for this document.
                return;
            }

            var referenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver;

            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolvers.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - ScriptWorkspace for loose .csx files (we don't have such workspace today)
            ImmutableArray<string> searchPaths;

            RuntimeMetadataReferenceResolver rtResolver;
            WorkspaceMetadataFileReferenceResolver workspaceResolver;

            if ((rtResolver = referenceResolver as RuntimeMetadataReferenceResolver) != null)
            {
                searchPaths = rtResolver.PathResolver.SearchPaths;
            }
            else if ((workspaceResolver = referenceResolver as WorkspaceMetadataFileReferenceResolver) != null)
            {
                searchPaths = workspaceResolver.PathResolver.SearchPaths;
            }
            else
            {
                return;
            }

            var fileSystemHelper = new FileSystemCompletionHelper(
                this, textChangeSpan,
                GetFileSystemDiscoveryService(snapshot),
                Glyph.OpenFolder,
                Glyph.Assembly,
                searchPaths: searchPaths,
                allowableExtensions: new[] { ".dll", ".exe" },
                exclude: path => path.Contains(","),
                itemRules: ItemRules.Instance);

            var pathThroughLastSlash = GetPathThroughLastSlash(stringLiteral, position);

            var documentPath = document.Project.IsSubmission ? null : document.FilePath;
            context.AddItems(gacHelper.GetItems(pathThroughLastSlash, documentPath));
            context.AddItems(fileSystemHelper.GetItems(pathThroughLastSlash, documentPath));
        }

        private static string GetPathThroughLastSlash(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetPathThroughLastSlash(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }
    }
}

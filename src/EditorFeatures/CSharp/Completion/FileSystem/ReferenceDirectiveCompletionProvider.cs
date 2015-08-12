// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    // TODO(cyrusn): Use a predefined name here.
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    internal partial class ReferenceDirectiveCompletionProvider : AbstractCompletionProvider
    {
        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return PathCompletionUtilities.IsTriggerCharacter(text, characterPosition);
        }

        private ICurrentWorkingDirectoryDiscoveryService GetFileSystemDiscoveryService(ITextSnapshot textSnapshot)
        {
            return CurrentWorkingDirectoryDiscoveryService.GetService(textSnapshot);
        }

        private TextSpan GetTextChangeSpan(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetTextChangeSpan(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }

        private string GetPathThroughLastSlash(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetPathThroughLastSlash(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTrigger trigger, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // first try to get the #r string literal token.  If we couldn't, then we're not in a #r
            // reference directive and we immediately bail.
            SyntaxToken stringLiteral;
            if (!TryGetStringLiteralToken(tree, position, out stringLiteral, cancellationToken))
            {
                return null;
            }

            var documentPath = document.Project.IsSubmission ? null : document.FilePath;
            var textChangeSpan = this.GetTextChangeSpan(stringLiteral, position);

            var gacHelper = new GlobalAssemblyCacheCompletionHelper(this, textChangeSpan, itemRules: ItemRules.Instance);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot == null)
            {
                // Passing null to GetFileSystemDiscoveryService raises an exception.
                // Instead, return here since there is no longer snapshot for this document.
                return null;
            }

            var assemblyReferenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver as AssemblyReferenceResolver;
            if (assemblyReferenceResolver == null)
            {
                return null;
            }

            var metadataFileResolver = assemblyReferenceResolver.PathResolver as MetadataFileReferenceResolver;
            if (metadataFileResolver == null)
            {
                return null;
            }

            var fileSystemHelper = new FileSystemCompletionHelper(
                this, textChangeSpan,
                GetFileSystemDiscoveryService(snapshot),
                Glyph.OpenFolder,
                Glyph.Assembly,
                searchPaths: metadataFileResolver.SearchPaths,
                allowableExtensions: new[] { ".dll", ".exe" },
                exclude: path => path.Contains(","),
                itemRules: ItemRules.Instance);

            var pathThroughLastSlash = this.GetPathThroughLastSlash(stringLiteral, position);
            return gacHelper.GetItems(pathThroughLastSlash, documentPath).Concat(
                fileSystemHelper.GetItems(pathThroughLastSlash, documentPath));
        }

        private bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        {
            if (tree.IsEntirelyWithinStringLiteral(position, cancellationToken))
            {
                var token = tree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
                if (token.Kind() == SyntaxKind.EndOfDirectiveToken || token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
                }

                if (token.Kind() == SyntaxKind.StringLiteralToken &&
                    token.Parent.Kind() == SyntaxKind.ReferenceDirectiveTrivia)
                {
                    stringLiteral = token;
                    return true;
                }
            }

            stringLiteral = default(SyntaxToken);
            return false;
        }
    }
}

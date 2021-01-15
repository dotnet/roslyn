// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceGeneratedDocumentState : DocumentState
    {
        public string HintName { get; }
        public ISourceGenerator SourceGenerator { get; }

        public static SourceGeneratedDocumentState Create(
            SourceGeneratedDocumentState? previousState,
            string hintName,
            SourceText generatedSourceText,
            SyntaxTree generatedSyntaxTree,
            DocumentId documentId,
            ISourceGenerator sourceGenerator,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            CancellationToken cancellationToken)
        {
            var tree = generatedSyntaxTree;
            var options = tree.Options;
            var filePath = tree.FilePath;

            bool sourceChanged;
            ValueSource<TextAndVersion> textSource;

            // We can reuse the previous TextAndVersion if the checksum from the text matches.
            if (previousState is null
                || !previousState.TryGetTextAndVersion(out var textAndVersion)
                || Checksum.From(textAndVersion.Text.GetChecksum()) != Checksum.From(generatedSourceText.GetChecksum()))
            {
                sourceChanged = true;
                textAndVersion = TextAndVersion.Create(generatedSourceText, VersionStamp.Create());
                textSource = new ConstantValueSource<TextAndVersion>(textAndVersion);
            }
            else
            {
                sourceChanged = false;
                textSource = previousState.TextAndVersionSource;
            }

            // We can reuse the previous tree if the options haven't changed
            bool treeChanged;
            if (!sourceChanged && Equals(previousState!.ParseOptions, generatedSyntaxTree.Options))
            {
                treeChanged = false;
                tree = previousState.SyntaxTree.WithFilePath(tree.FilePath);
            }
            else
            {
                treeChanged = true;
            }

            if (treeChanged)
            {
                var root = tree.GetRoot(cancellationToken);
                Contract.ThrowIfNull(languageServices.SyntaxTreeFactory, "We should not have a generated syntax tree for a language that doesn't support trees.");

                if (languageServices.SyntaxTreeFactory.CanCreateRecoverableTree(root))
                {
                    // We will only create recoverable text if we can create a recoverable tree; if we created a
                    // recoverable text but not a new tree, it would mean tree.GetText() could still potentially return
                    // the non-recoverable text, but asking the document directly for it's text would give a recoverable
                    // text with a different object identity. We only need to create a recoverable text if the source
                    // changed; otherwise, the reused text source would be recoverable.
                    if (sourceChanged)
                    {
                        textSource = CreateRecoverableText(textAndVersion, solutionServices);
                    }

                    tree = languageServices.SyntaxTreeFactory.CreateRecoverableTree(
                        documentId.ProjectId,
                        filePath: tree.FilePath,
                        options,
                        textSource,
                        generatedSourceText.Encoding,
                        root);
                }
            }

            var treeAndVersion = TreeAndVersion.Create(tree, textAndVersion.Version);

            return new SourceGeneratedDocumentState(
                languageServices,
                solutionServices,
                documentServiceProvider: null,
                new DocumentInfo.DocumentAttributes(
                    documentId,
                    name: hintName,
                    folders: SpecializedCollections.EmptyReadOnlyList<string>(),
                    options.Kind,
                    filePath: filePath,
                    isGenerated: true,
                    designTimeOnly: false),
                options,
                sourceText: null, // don't strongly hold the text
                textSource,
                treeAndVersion,
                sourceGenerator,
                hintName);
        }

        private SourceGeneratedDocumentState(
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions? options,
            SourceText? sourceText,
            ValueSource<TextAndVersion> textSource,
            TreeAndVersion treeAndVersion,
            ISourceGenerator sourceGenerator,
            string hintName)
            : base(languageServices, solutionServices, documentServiceProvider, attributes, options, sourceText, textSource, new ConstantValueSource<TreeAndVersion>(treeAndVersion))
        {
            SourceGenerator = sourceGenerator;
            HintName = hintName;
        }

        /// <summary>
        /// Equivalent to calling <see cref="DocumentState.GetSyntaxTree(CancellationToken)"/>, but avoids the implicit requirement of a cancellation token since
        /// we can always get the tree right away.
        /// </summary>
        /// <remarks>
        /// We won't expose this through <see cref="SourceGeneratedDocument"/> in case the implementation changes.
        /// </remarks>
        public SyntaxTree SyntaxTree
        {
            get
            {
                // We are always holding onto the SyntaxTree object with a ConstantValueSource, so we can just fetch this
                // without any extra work. Unlike normal documents where we don't even have a tree object until we've fetched text and
                // the tree the first time, the generated case we start with a tree and text and then wrap it.
                return GetSyntaxTree(CancellationToken.None);
            }
        }

        protected override TextDocumentState UpdateText(ValueSource<TextAndVersion> newTextSource, PreservationMode mode, bool incremental)
        {
            throw new NotSupportedException(WorkspacesResources.The_contents_of_a_SourceGeneratedDocument_may_not_be_changed);
        }
    }
}

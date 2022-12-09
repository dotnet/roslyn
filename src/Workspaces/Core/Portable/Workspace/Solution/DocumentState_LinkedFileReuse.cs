// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        /// <summary>
        /// Returns a new instance of this document state that points to <paramref name="siblingTextSource"/> as the
        /// text contents of the document, and which will produce a syntax tree that reuses from <paramref
        /// name="siblingTextSource"/> if possible, or which will incrementally parse the current tree to bring it up to
        /// date with <paramref name="siblingTextSource"/> otherwise.
        /// </summary>
        public DocumentState UpdateTextAndTreeContents(ITextAndVersionSource siblingTextSource, ValueSource<TreeAndVersion>? siblingTreeSource)
        {
            if (!SupportsSyntaxTree)
            {
                return new DocumentState(
                    LanguageServices,
                    solutionServices,
                    Services,
                    Attributes,
                    _options,
                    siblingTextSource,
                    LoadTextOptions,
                    treeSource: null);
            }

            Contract.ThrowIfNull(siblingTreeSource);

            // Always pass along the sibling text.  We will always be in sync with that.

            // if a sibling tree source is provided, then we'll want to attempt to use the tree it creates, to share as
            // much memory as possible with linked files.  However, we can't point at that source directly.  If we did,
            // we'd produce the *exact* same tree-reference as another file.  That would be bad as it would break the
            // invariant that each document gets a unique SyntaxTree.  So, instead, we produce a ValueSource that defers
            // to the provided source, gets the tree from it, and then wraps its root in a new tree for us.

            // copy data from this entity, and pass to static helper, so we don't keep this green node alive.

            var filePath = this.Attributes.SyntaxTreeFilePath;
            var languageServices = this.LanguageServices;
            var loadTextOptions = this.LoadTextOptions;
            var parseOptions = this.ParseOptions;
            var textAndVersionSource = this.TextAndVersionSource;
            var treeSource = this.TreeSource;

            var newTreeSource = GetReuseTreeSource(
                filePath, languageServices, loadTextOptions, parseOptions, treeSource, siblingTextSource, siblingTreeSource);

            return new DocumentState(
                LanguageServices,
                solutionServices,
                Services,
                Attributes,
                _options,
                siblingTextSource,
                LoadTextOptions,
                newTreeSource);

            static AsyncLazy<TreeAndVersion> GetReuseTreeSource(
                string filePath,
                HostLanguageServices languageServices,
                LoadTextOptions loadTextOptions,
                ParseOptions parseOptions,
                ValueSource<TreeAndVersion> treeSource,
                ITextAndVersionSource siblingTextSource,
                ValueSource<TreeAndVersion> siblingTreeSource)
            {
                return new AsyncLazy<TreeAndVersion>(
                    cancellationToken => TryReuseSiblingTreeAsync(filePath, languageServices, loadTextOptions, parseOptions, treeSource, siblingTextSource, siblingTreeSource, cancellationToken),
                    cancellationToken => TryReuseSiblingTree(filePath, languageServices, loadTextOptions, parseOptions, treeSource, siblingTextSource, siblingTreeSource, cancellationToken),
                    cacheResult: true);
            }
        }

        private static bool TryReuseSiblingRoot(
            string filePath,
            HostLanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            SyntaxNode siblingRoot,
            VersionStamp siblingVersion,
            [NotNullWhen(true)] out TreeAndVersion? newTreeAndVersion)
        {
            var siblingTree = siblingRoot.SyntaxTree;

            // Look for things that disqualify us from being able to use our sibling's root.
            if (!CanReuseSiblingRoot())
            {
                newTreeAndVersion = null;
                return false;
            }

            var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

            var newTree = treeFactory.CreateSyntaxTree(
                filePath,
                parseOptions,
                siblingTree.Encoding,
                loadTextOptions.ChecksumAlgorithm,
                siblingRoot);

            newTreeAndVersion = new TreeAndVersion(newTree, siblingVersion);
            return true;

            bool CanReuseSiblingRoot()
            {
                var siblingParseOptions = siblingTree.Options;

                var ppSymbolsNames1 = parseOptions.PreprocessorSymbolNames;
                var ppSymbolsNames2 = siblingParseOptions.PreprocessorSymbolNames;

                // If both documents have the same preprocessor directives defined, then they'll always produce the
                // same trees.  So we can trivially reuse the tree from one for the other.
                if (ppSymbolsNames1.SetEquals(ppSymbolsNames2))
                    return true;

                // If the tree contains no `#` directives whatsoever, then you'll parse out the same tree and can reuse it.
                if (!siblingRoot.ContainsDirectives)
                    return true;

                // It's ok to contain directives like #nullable, or #region.  They don't affect parsing.  But if we have a
                // `#if` we can't share as each side might parse this differently.
                var syntaxKinds = languageServices.GetRequiredService<ISyntaxKindsService>();
                if (!siblingRoot.ContainsDirective(syntaxKinds.IfDirectiveTrivia))
                    return true;

                // If the tree contains a #if directive, and the pp-symbol-names are different, then the files
                // absolutely may be parsed differently, and so they should not be shared.
                return false;
            }
        }

        private static async Task<TreeAndVersion> TryReuseSiblingTreeAsync(
            string filePath,
            HostLanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            ValueSource<TreeAndVersion> treeSource,
            ITextAndVersionSource siblingTextSource,
            ValueSource<TreeAndVersion> siblingTreeSource,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = await siblingTreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = await siblingTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            if (TryReuseSiblingRoot(filePath, languageServices, loadTextOptions, parseOptions, siblingRoot, siblingTreeAndVersion.Version, out var newTreeAndVersion))
                return newTreeAndVersion;

            // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
            return await IncrementallyParseTreeAsync(treeSource, siblingTextSource, loadTextOptions, cancellationToken).ConfigureAwait(false);
        }

        private static TreeAndVersion TryReuseSiblingTree(
            string filePath,
            HostLanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            ValueSource<TreeAndVersion> treeSource,
            ITextAndVersionSource siblingTextSource,
            ValueSource<TreeAndVersion> siblingTreeSource,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = siblingTreeSource.GetValue(cancellationToken);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = siblingTree.GetRoot(cancellationToken);

            if (TryReuseSiblingRoot(filePath, languageServices, loadTextOptions, parseOptions, siblingRoot, siblingTreeAndVersion.Version, out var newTreeAndVersion))
                return newTreeAndVersion;

            // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
            return IncrementallyParseTree(treeSource, siblingTextSource, loadTextOptions, cancellationToken);
        }
    }
}

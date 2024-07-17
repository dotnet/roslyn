// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class DocumentState
{
    /// <summary>
    /// <see cref="ITreeAndVersionSource"/> when we're linked to another file (a 'sibling') and will attempt to reuse
    /// that sibling's tree as our own. Note: we won't know if we can actually use the contents of that sibling file
    /// until we actually go and realize it, as it may contains constructs (like pp-directives) that prevent use.  In
    /// that case, we'll fall back to a normal incremental parse between our original <paramref
    /// name="originalTreeSource"/> and the latest <em>text</em> contents of our sibling's file.
    /// </summary>
    private sealed class LinkedFileReuseTreeAndVersionSource(
        ITreeAndVersionSource originalTreeSource,
        AsyncLazy<TreeAndVersion> lazyComputation) : ITreeAndVersionSource
    {
        public readonly ITreeAndVersionSource OriginalTreeSource = originalTreeSource;

        public Task<TreeAndVersion> GetValueAsync(CancellationToken cancellationToken)
            => lazyComputation.GetValueAsync(cancellationToken);

        public TreeAndVersion GetValue(CancellationToken cancellationToken)
            => lazyComputation.GetValue(cancellationToken);

        public bool TryGetValue([NotNullWhen(true)] out TreeAndVersion? value)
            => lazyComputation.TryGetValue(out value);
    }

    /// <summary>
    /// Returns a new instance of this document state that points to <paramref name="siblingTextSource"/> as the
    /// text contents of the document, and which will produce a syntax tree that reuses from <paramref
    /// name="siblingTextSource"/> if possible, or which will incrementally parse the current tree to bring it up to
    /// date with <paramref name="siblingTextSource"/> otherwise.
    /// </summary>
    public DocumentState UpdateTextAndTreeContents(
        ITextAndVersionSource siblingTextSource,
        ITreeAndVersionSource? siblingTreeSource,
        bool forceEvenIfTreesWouldDiffer)
    {
        if (!SupportsSyntaxTree)
        {
            return new DocumentState(
                LanguageServices,
                Services,
                Attributes,
                _options,
                siblingTextSource,
                LoadTextOptions,
                treeSource: null);
        }

        Contract.ThrowIfNull(siblingTreeSource);

        // We don't want to point at a long chain of transformations as our sibling files change, deferring to each next
        // link of the chain to potentially do the work (or potentially failing out). So, if we're about to do this,
        // instead return our original tree-source so that in the case we are unable to use the sibling file's root, we
        // can do a single step incremental parse between our original tree and the final sibling text.
        //
        // We only need to look one deep here as we'll pull that tree source forward to our level.  If another link is
        // later added to us, it will do the same thing.
        var originalTreeSource = this.TreeSource;
        if (originalTreeSource is LinkedFileReuseTreeAndVersionSource linkedFileTreeAndVersionSource)
            originalTreeSource = linkedFileTreeAndVersionSource.OriginalTreeSource;

        // Always pass along the sibling text.  We will always be in sync with that.

        // Defer to static helper to make sure we don't accidentally capture anything else we don't want off of 'this'
        // (like "this.TreeSource").
        return UpdateTextAndTreeContentsWorker(
            this.Attributes, this.LanguageServices, this.Services, this.LoadTextOptions, this.ParseOptions,
            originalTreeSource, siblingTextSource, siblingTreeSource, forceEvenIfTreesWouldDiffer);
    }

    private static DocumentState UpdateTextAndTreeContentsWorker(
        DocumentInfo.DocumentAttributes attributes,
        LanguageServices languageServices,
        IDocumentServiceProvider services,
        LoadTextOptions loadTextOptions,
        ParseOptions parseOptions,
        ITreeAndVersionSource originalTreeSource,
        ITextAndVersionSource siblingTextSource,
        ITreeAndVersionSource siblingTreeSource,
        bool forceEvenIfTreesWouldDiffer)
    {
        // if a sibling tree source is provided, then we'll want to attempt to use the tree it creates, to share as much
        // memory as possible with linked files.  However, we can't point at that source directly.  If we did, we'd
        // produce the *exact* same tree-reference as another file.  That would be bad as it would break the invariant
        // that each document gets a unique SyntaxTree.  So, instead, we produce a tree-source that defers to the
        // provided source, gets the tree from it, and then wraps its root in a new tree for us.

        var lazyComputation = AsyncLazy.Create(
            asynchronousComputeFunction: static (arg, cancellationToken) => TryReuseSiblingTreeAsync(arg.filePath, arg.languageServices, arg.loadTextOptions, arg.parseOptions, arg.originalTreeSource, arg.siblingTextSource, arg.siblingTreeSource, arg.forceEvenIfTreesWouldDiffer, cancellationToken),
            synchronousComputeFunction: static (arg, cancellationToken) => TryReuseSiblingTree(arg.filePath, arg.languageServices, arg.loadTextOptions, arg.parseOptions, arg.originalTreeSource, arg.siblingTextSource, arg.siblingTreeSource, arg.forceEvenIfTreesWouldDiffer, cancellationToken),
            arg: (filePath: attributes.SyntaxTreeFilePath, languageServices, loadTextOptions, parseOptions, originalTreeSource, siblingTextSource, siblingTreeSource, forceEvenIfTreesWouldDiffer));

        var newTreeSource = new LinkedFileReuseTreeAndVersionSource(originalTreeSource, lazyComputation);

        return new DocumentState(
            languageServices, services, attributes, parseOptions, siblingTextSource, loadTextOptions, newTreeSource);

        static bool TryReuseSiblingRoot(
            string filePath,
            LanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            SyntaxNode siblingRoot,
            VersionStamp siblingVersion,
            bool forceEvenIfTreesWouldDiffer,
            [NotNullWhen(true)] out TreeAndVersion? newTreeAndVersion)
        {
            var siblingTree = siblingRoot.SyntaxTree;

            // Look for things that disqualify us from being able to use our sibling's root.
            if (!CanReuseSiblingRoot(forceEvenIfTreesWouldDiffer))
            {
                newTreeAndVersion = null;
                return false;
            }

            var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

            // Note: passing along siblingTree.Encoding is a bit suspect.  Ideally we would only populate this tree
            // with our own data (*except* for the new root).  However, we think it's safe as the encoding really is
            // a property of the file, and that should stay the same even if linked into multiple projects.

            var newTree = treeFactory.CreateSyntaxTree(
                filePath,
                parseOptions,
                siblingTree.Encoding,
                loadTextOptions.ChecksumAlgorithm,
                siblingRoot);

            newTreeAndVersion = new TreeAndVersion(newTree, siblingVersion);
            return true;

            // Determines if the root of a tree from a different project can be used in this project.  The general
            // intuition (explained below) is that files without `#if` directives in them can be reused as the parse
            // trees will be the same.
            //
            // This is *technically* not completely accurate as language-version can affect the parse tree as well.
            // For example, `record X() { }` is a method prior to the addition of records to the language.  However,
            // in practice this should not be an issue.  Specifically, either user code does not have a construct
            // like this, in which case they are not affected by sharing.  *Or*, they do have such a construct, but
            // are being deliberately pathological.  In other words, there are no realistic programs that depend on
            // having one interpretation in one version, and another interpretation in another version.  So we are
            // ok saying we don't care about having that not work in the IDE (it will still work fine in the
            // compiler).
            //
            // Note: we deliberately do not look at language version because it often is different across project
            // flavors.  So we would often get no benefit to sharing if we restricted to only when the lang version
            // is the same.
            bool CanReuseSiblingRoot(bool forceEvenIfTreesWouldDiffer)
            {
                // If we're forcing reuse of a sibling tree, then this always succeeds.
                if (forceEvenIfTreesWouldDiffer)
                    return true;

                Interlocked.Increment(ref s_tryReuseSyntaxTree);
                var siblingParseOptions = siblingTree.Options;

                var ppSymbolsNames1 = parseOptions.PreprocessorSymbolNames;
                var ppSymbolsNames2 = siblingParseOptions.PreprocessorSymbolNames;

                // If both documents have the same preprocessor directives defined, then they'll always produce the
                // same trees.  So we can trivially reuse the tree from one for the other.
                if (ppSymbolsNames1.SetEquals(ppSymbolsNames2))
                {
                    Interlocked.Increment(ref s_couldReuseBecauseOfEqualPPNames);
                    return true;
                }

                // If the tree contains no `#` directives whatsoever, then you'll parse out the same tree and can reuse it.
                if (!siblingRoot.ContainsDirectives)
                {
                    Interlocked.Increment(ref s_couldReuseBecauseOfNoDirectives);
                    return true;
                }

                // It's ok to contain directives like #nullable, or #region.  They don't affect parsing.  But if we have a
                // `#if` we can't share as each side might parse this differently.
                var syntaxKinds = languageServices.GetRequiredService<ISyntaxKindsService>();
                if (!siblingRoot.ContainsDirective(syntaxKinds.IfDirectiveTrivia))
                {
                    Interlocked.Increment(ref s_couldReuseBecauseOfNoPPDirectives);
                    return true;
                }

                // If the tree contains a #if directive, and the pp-symbol-names are different, then the files
                // absolutely may be parsed differently, and so they should not be shared.
                //
                // TODO(cyrusn): We could potentially be smarter here as well.  We can check what pp-symbols the file
                // actually uses. (e.g. 'DEBUG'/'NETCORE'/etc.) and see if the project parse options actually differ
                // for these values.  If not, we could reuse the trees even then.
                Interlocked.Increment(ref s_couldNotReuse);
                return false;
            }
        }

        static async Task<TreeAndVersion> TryReuseSiblingTreeAsync(
            string filePath,
            LanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            ITreeAndVersionSource treeSource,
            ITextAndVersionSource siblingTextSource,
            ITreeAndVersionSource siblingTreeSource,
            bool forceEvenIfTreesWouldDiffer,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = await siblingTreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = await siblingTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            if (TryReuseSiblingRoot(filePath, languageServices, loadTextOptions, parseOptions, siblingRoot, siblingTreeAndVersion.Version, forceEvenIfTreesWouldDiffer, out var newTreeAndVersion))
                return newTreeAndVersion;

            // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
            return await IncrementallyParseTreeAsync(treeSource, siblingTextSource, loadTextOptions, cancellationToken).ConfigureAwait(false);
        }

        static TreeAndVersion TryReuseSiblingTree(
            string filePath,
            LanguageServices languageServices,
            LoadTextOptions loadTextOptions,
            ParseOptions parseOptions,
            ITreeAndVersionSource treeSource,
            ITextAndVersionSource siblingTextSource,
            ITreeAndVersionSource siblingTreeSource,
            bool forceEvenIfTreesWouldDiffer,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = siblingTreeSource.GetValue(cancellationToken);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = siblingTree.GetRoot(cancellationToken);

            if (TryReuseSiblingRoot(filePath, languageServices, loadTextOptions, parseOptions, siblingRoot, siblingTreeAndVersion.Version, forceEvenIfTreesWouldDiffer, out var newTreeAndVersion))
                return newTreeAndVersion;

            // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
            return IncrementallyParseTree(treeSource, siblingTextSource, loadTextOptions, cancellationToken);
        }
    }

    // Values just kept around for benchmark tests.
    private static int s_tryReuseSyntaxTree;
    private static int s_couldNotReuse;
    private static int s_couldReuseBecauseOfEqualPPNames;
    private static int s_couldReuseBecauseOfNoDirectives;
    private static int s_couldReuseBecauseOfNoPPDirectives;

    public struct TestAccessor
    {
        public static int TryReuseSyntaxTree => s_tryReuseSyntaxTree;
        public static int CouldNotReuse => s_couldNotReuse;
        public static int CouldReuseBecauseOfEqualPPNames => s_couldReuseBecauseOfEqualPPNames;
        public static int CouldReuseBecauseOfNoDirectives => s_couldReuseBecauseOfNoDirectives;
        public static int CouldReuseBecauseOfNoPPDirectives => s_couldReuseBecauseOfNoPPDirectives;
    }
}

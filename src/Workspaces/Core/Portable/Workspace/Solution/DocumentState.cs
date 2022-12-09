// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState : TextDocumentState
    {
        private static readonly Func<string?, PreservationMode, string> s_fullParseLog = (path, mode) => $"{path} : {mode}";

        private static readonly ConditionalWeakTable<SyntaxTree, DocumentId> s_syntaxTreeToIdMap = new();

        // properties inherited from the containing project:
        private readonly HostLanguageServices _languageServices;
        private readonly ParseOptions? _options;

        // null if the document doesn't support syntax trees:
        private readonly ValueSource<TreeAndVersion>? _treeSource;

        protected DocumentState(
            HostLanguageServices languageServices,
            HostWorkspaceServices solutionServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions? options,
            ITextAndVersionSource textSource,
            LoadTextOptions loadTextOptions,
            ValueSource<TreeAndVersion>? treeSource)
            : base(solutionServices, documentServiceProvider, attributes, textSource, loadTextOptions)
        {
            Contract.ThrowIfFalse(_options is null == _treeSource is null);

            _languageServices = languageServices;
            _options = options;
            _treeSource = treeSource;
        }

        public DocumentState(
            DocumentInfo info,
            ParseOptions? options,
            LoadTextOptions loadTextOptions,
            HostLanguageServices languageServices,
            HostWorkspaceServices services)
            : base(info, loadTextOptions, services)
        {
            _languageServices = languageServices;
            _options = options;

            // If this is document that doesn't support syntax, then don't even bother holding
            // onto any tree source.  It will never be used to get a tree, and can only hurt us
            // by possibly holding onto data that might cause a slow memory leak.
            if (languageServices.SyntaxTreeFactory == null)
            {
                _treeSource = null;
            }
            else
            {
                Contract.ThrowIfNull(options);
                _treeSource = CreateLazyFullyParsedTree(
                    TextAndVersionSource,
                    LoadTextOptions,
                    info.Attributes.SyntaxTreeFilePath,
                    options,
                    languageServices);
            }
        }

        public ValueSource<TreeAndVersion>? TreeSource => _treeSource;

        [MemberNotNullWhen(true, nameof(_treeSource))]
        [MemberNotNullWhen(true, nameof(TreeSource))]
        [MemberNotNullWhen(true, nameof(_options))]
        [MemberNotNullWhen(true, nameof(ParseOptions))]
        internal bool SupportsSyntaxTree
            => _treeSource != null;

        public HostLanguageServices LanguageServices
            => _languageServices;

        public ParseOptions? ParseOptions
            => _options;

        public SourceCodeKind SourceCodeKind
            => ParseOptions == null ? Attributes.SourceCodeKind : ParseOptions.Kind;

        public bool IsGenerated
            => Attributes.IsGenerated;

        protected static ValueSource<TreeAndVersion> CreateLazyFullyParsedTree(
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode = PreservationMode.PreserveValue)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => FullyParseTreeAsync(newTextSource, loadTextOptions, filePath, options, languageServices, mode, c),
                c => FullyParseTree(newTextSource, loadTextOptions, filePath, options, languageServices, mode, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> FullyParseTreeAsync(
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = await newTextSource.GetValueAsync(loadTextOptions, cancellationToken).ConfigureAwait(false);
                return CreateTreeAndVersion(filePath, options, languageServices, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion FullyParseTree(
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = newTextSource.GetValue(loadTextOptions, cancellationToken);
                return CreateTreeAndVersion(filePath, options, languageServices, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion CreateTreeAndVersion(
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            TextAndVersion textAndVersion,
            CancellationToken cancellationToken)
        {
            var text = textAndVersion.Text;

            var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

            var tree = treeFactory.ParseSyntaxTree(filePath, options, text, cancellationToken);

            Contract.ThrowIfNull(tree);
            CheckTree(tree, text);

            // text version for this document should be unique. use it as a starting point.
            return new TreeAndVersion(tree, textAndVersion.Version);
        }

        private static ValueSource<TreeAndVersion> CreateLazyIncrementallyParsedTree(
            ValueSource<TreeAndVersion> oldTreeSource,
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => IncrementallyParseTreeAsync(oldTreeSource, newTextSource, loadTextOptions, c),
                c => IncrementallyParseTree(oldTreeSource, newTextSource, loadTextOptions, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> IncrementallyParseTreeAsync(
            ValueSource<TreeAndVersion> oldTreeSource,
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Workspace_Document_State_IncrementallyParseSyntaxTree, cancellationToken))
                {
                    var newTextAndVersion = await newTextSource.GetValueAsync(loadTextOptions, cancellationToken).ConfigureAwait(false);
                    var oldTreeAndVersion = await oldTreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    return IncrementallyParse(newTextAndVersion, oldTreeAndVersion, cancellationToken);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static TreeAndVersion IncrementallyParseTree(
            ValueSource<TreeAndVersion> oldTreeSource,
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Workspace_Document_State_IncrementallyParseSyntaxTree, cancellationToken))
                {
                    var newTextAndVersion = newTextSource.GetValue(loadTextOptions, cancellationToken);
                    var oldTreeAndVersion = oldTreeSource.GetValue(cancellationToken);

                    return IncrementallyParse(newTextAndVersion, oldTreeAndVersion, cancellationToken);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static TreeAndVersion IncrementallyParse(
            TextAndVersion newTextAndVersion,
            TreeAndVersion oldTreeAndVersion,
            CancellationToken cancellationToken)
        {
            var newText = newTextAndVersion.Text;
            var oldTree = oldTreeAndVersion.Tree;

            var oldText = oldTree.GetText(cancellationToken);
            var newTree = oldTree.WithChangedText(newText);
            Contract.ThrowIfNull(newTree);
            CheckTree(newTree, newText, oldTree, oldText);

            return MakeNewTreeAndVersion(oldTree, oldText, oldTreeAndVersion.Version, newTree, newText, newTextAndVersion.Version);
        }

        private static TreeAndVersion MakeNewTreeAndVersion(SyntaxTree oldTree, SourceText oldText, VersionStamp oldVersion, SyntaxTree newTree, SourceText newText, VersionStamp newVersion)
        {
            var topLevelChanged = TopLevelChanged(oldTree, oldText, newTree, newText);
            var version = topLevelChanged ? newVersion : oldVersion;
            return new TreeAndVersion(newTree, version);
        }

        private const int MaxTextChangeRangeLength = 1024 * 4;

        private static bool TopLevelChanged(SyntaxTree oldTree, SourceText oldText, SyntaxTree newTree, SourceText newText)
        {
            // ** currently, it doesn't do any text based quick check. we can add them later if current logic is not performant enough for typing case.
            var change = newText.GetEncompassingTextChangeRange(oldText);
            if (change == default)
            {
                // nothing has changed
                return false;
            }

            // if texts are small enough, just use the equivalent to find out whether there was top level edits
            if (oldText.Length < MaxTextChangeRangeLength && newText.Length < MaxTextChangeRangeLength)
            {
                var topLevel = !newTree.IsEquivalentTo(oldTree, topLevel: true);
                return topLevel;
            }

            // okay, text is not small and whole text is changed, then we always treat it as top level edit
            if (change.NewLength == newText.Length)
            {
                return true;
            }

            // if changes are small enough, we use IsEquivalentTo to find out whether there was a top level edit
            if (change.Span.Length < MaxTextChangeRangeLength && change.NewLength < MaxTextChangeRangeLength)
            {
                var topLevel = !newTree.IsEquivalentTo(oldTree, topLevel: true);
                return topLevel;
            }

            // otherwise, we always consider top level change
            return true;
        }

        public bool HasContentChanged(DocumentState oldState)
        {
            return oldState._treeSource != _treeSource
                || HasTextChanged(oldState, ignoreUnchangeableDocument: false);
        }

        [Obsolete("Use TextDocumentState.HasTextChanged")]
        public bool HasTextChanged(DocumentState oldState)
            => HasTextChanged(oldState, ignoreUnchangeableDocument: false);

        public DocumentState UpdateChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        {
            var newLoadTextOptions = new LoadTextOptions(checksumAlgorithm);

            if (LoadTextOptions == newLoadTextOptions)
            {
                return this;
            }

            // To keep the loaded SourceText consistent with the DocumentState,
            // avoid updating the options if the loader can't apply them on the loaded SourceText.
            if (!TextAndVersionSource.CanReloadText)
            {
                return this;
            }

            // TODO: we should be able to reuse the tree root
            var newTreeSource = SupportsSyntaxTree ? CreateLazyFullyParsedTree(
                TextAndVersionSource,
                newLoadTextOptions,
                Attributes.SyntaxTreeFilePath,
                _options,
                _languageServices) : null;

            return new DocumentState(
                LanguageServices,
                LanguageServices.WorkspaceServices,
                Services,
                Attributes,
                _options,
                TextAndVersionSource,
                newLoadTextOptions,
                newTreeSource);
        }

        public DocumentState UpdateParseOptions(ParseOptions options, bool onlyPreprocessorDirectiveChange)
        {
            var originalSourceKind = this.SourceCodeKind;

            var newState = this.SetParseOptions(options, onlyPreprocessorDirectiveChange);
            if (newState.SourceCodeKind != originalSourceKind)
            {
                newState = newState.UpdateSourceCodeKind(originalSourceKind);
            }

            return newState;
        }

        private DocumentState SetParseOptions(ParseOptions options, bool onlyPreprocessorDirectiveChange)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!SupportsSyntaxTree)
            {
                throw new InvalidOperationException();
            }

            ValueSource<TreeAndVersion>? newTreeSource = null;

            // Optimization: if we are only changing preprocessor directives, and we've already parsed the existing tree and it didn't have
            // any, we can avoid a reparse since the tree will be parsed the same.
            if (onlyPreprocessorDirectiveChange &&
                _treeSource.TryGetValue(out var existingTreeAndVersion))
            {
                var existingTree = existingTreeAndVersion.Tree;

                SyntaxTree? newTree = null;

                if (existingTree.TryGetRoot(out var existingRoot) && !existingRoot.ContainsDirectives)
                {
                    var treeFactory = _languageServices.GetRequiredService<ISyntaxTreeFactoryService>();
                    newTree = treeFactory.CreateSyntaxTree(Attributes.SyntaxTreeFilePath, options, existingTree.Encoding, LoadTextOptions.ChecksumAlgorithm, existingRoot);
                }

                if (newTree is not null)
                    newTreeSource = ValueSource.Constant(new TreeAndVersion(newTree, existingTreeAndVersion.Version));
            }

            // If we weren't able to reuse in a smart way, just reparse
            newTreeSource ??= CreateLazyFullyParsedTree(
                TextAndVersionSource,
                LoadTextOptions,
                Attributes.SyntaxTreeFilePath,
                options,
                _languageServices);

            return new DocumentState(
                LanguageServices,
                LanguageServices.WorkspaceServices,
                Services,
                Attributes.With(sourceCodeKind: options.Kind),
                options,
                TextAndVersionSource,
                LoadTextOptions,
                newTreeSource);
        }

        public DocumentState UpdateSourceCodeKind(SourceCodeKind kind)
        {
            if (this.ParseOptions == null || kind == this.SourceCodeKind)
            {
                return this;
            }

            return this.SetParseOptions(this.ParseOptions.WithKind(kind), onlyPreprocessorDirectiveChange: false);
        }

        // TODO: https://github.com/dotnet/roslyn/issues/37125
        // if FilePath is null, then this will change the name of the underlying tree, but we aren't producing a new tree in that case.
        public DocumentState UpdateName(string name)
            => UpdateAttributes(Attributes.With(name: name));

        public DocumentState UpdateFolders(IReadOnlyList<string> folders)
            => UpdateAttributes(Attributes.With(folders: folders));

        private DocumentState UpdateAttributes(DocumentInfo.DocumentAttributes attributes)
        {
            Debug.Assert(attributes != Attributes);

            return new DocumentState(
                _languageServices,
                LanguageServices.WorkspaceServices,
                Services,
                attributes,
                _options,
                TextAndVersionSource,
                LoadTextOptions,
                _treeSource);
        }

        public DocumentState UpdateFilePath(string? filePath)
        {
            var newAttributes = Attributes.With(filePath: filePath);
            Debug.Assert(newAttributes != Attributes);

            // TODO: it's overkill to fully reparse the tree if we had the tree already; all we have to do is update the
            // file path and diagnostic options for that tree.
            var newTreeSource = SupportsSyntaxTree ?
                CreateLazyFullyParsedTree(
                    TextAndVersionSource,
                    LoadTextOptions,
                    newAttributes.SyntaxTreeFilePath,
                    _options,
                    _languageServices) : null;

            return new DocumentState(
                _languageServices,
                solutionServices,
                Services,
                newAttributes,
                _options,
                TextAndVersionSource,
                LoadTextOptions,
                newTreeSource);
        }

        public new DocumentState UpdateText(SourceText newText, PreservationMode mode)
            => (DocumentState)base.UpdateText(newText, mode);

        public new DocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
            => (DocumentState)base.UpdateText(newTextAndVersion, mode);

        public new DocumentState UpdateText(TextLoader loader, PreservationMode mode)
            => (DocumentState)base.UpdateText(loader, mode);

        protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
        {
            ValueSource<TreeAndVersion>? newTreeSource;

            if (!SupportsSyntaxTree)
            {
                newTreeSource = null;
            }
            else if (incremental)
            {
                newTreeSource = CreateLazyIncrementallyParsedTree(_treeSource, newTextSource, LoadTextOptions);
            }
            else
            {
                newTreeSource = CreateLazyFullyParsedTree(
                    newTextSource,
                    LoadTextOptions,
                    Attributes.SyntaxTreeFilePath,
                    _options,
                    _languageServices,
                    mode); // TODO: understand why the mode is given here. If we're preserving text by identity, why also preserve the tree?
            }

            return new DocumentState(
                LanguageServices,
                solutionServices,
                Services,
                Attributes,
                _options,
                textSource: newTextSource,
                LoadTextOptions,
                treeSource: newTreeSource);
        }

        internal DocumentState UpdateTextAndTreeContents(ITextAndVersionSource siblingTextSource, ValueSource<TreeAndVersion>? siblingTreeSource)
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

            // if a tree source is provided, then we'll want to use the tree it creates, to share as much memory as
            // possible with linked files.  However, we can't point at that source directly.  If we did, we'd produce
            // the *exact* same tree-reference as another file.  That would be bad as it would break the invariant that
            // each document gets a unique SyntaxTree.  So, instead, we produce a ValueSource that defers to the
            // provided source, gets the tree from it, and then wraps its root in a new tree for us.

            // copy data from this entity, so we don't keep this green node alive.

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
                treeSource: newTreeSource);

            // Static so we don't accidentally capture "this" green documentstate node in the async lazy.

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

        private static async Task<TreeAndVersion> TryReuseSiblingTreeAsync(
            string filePath,
            HostLanguageServices languageServices,
            LoadTextOptions options,
            ParseOptions parseOptions,
            ValueSource<TreeAndVersion> treeSource,
            ITextAndVersionSource siblingTextSource,
            ValueSource<TreeAndVersion> siblingTreeSource,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = await siblingTreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = await siblingTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxKinds = languageServices.GetRequiredService<ISyntaxKindsService>();
            if (CanReuseSiblingRoot(syntaxKinds.IfDirectiveTrivia, parseOptions, siblingTree.Options, siblingRoot))
            {
                var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

                var newTree = treeFactory.CreateSyntaxTree(
                    filePath,
                    parseOptions,
                    siblingTree.Encoding,
                    options.ChecksumAlgorithm,
                    siblingRoot);

                return new TreeAndVersion(newTree, siblingTreeAndVersion.Version);
            }
            else
            {
                // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
                return await IncrementallyParseTreeAsync(treeSource, siblingTextSource, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private static TreeAndVersion TryReuseSiblingTree(
            string filePath,
            HostLanguageServices languageServices,
            LoadTextOptions options,
            ParseOptions parseOptions,
            ValueSource<TreeAndVersion> treeSource,
            ITextAndVersionSource siblingTextSource,
            ValueSource<TreeAndVersion> siblingTreeSource,
            CancellationToken cancellationToken)
        {
            var siblingTreeAndVersion = siblingTreeSource.GetValue(cancellationToken);
            var siblingTree = siblingTreeAndVersion.Tree;

            var siblingRoot = siblingTree.GetRoot(cancellationToken);

            var syntaxKinds = languageServices.GetRequiredService<ISyntaxKindsService>();
            if (CanReuseSiblingRoot(syntaxKinds.IfDirectiveTrivia, parseOptions, siblingTree.Options, siblingRoot))
            {
                var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

                var newTree = treeFactory.CreateSyntaxTree(
                    filePath,
                    parseOptions,
                    siblingTree.Encoding,
                    options.ChecksumAlgorithm,
                    siblingRoot);

                return new TreeAndVersion(newTree, siblingTreeAndVersion.Version);
            }
            else
            {
                // Couldn't use the sibling file to get the tree contents.  Instead, incrementally parse our tree to the text passed in.
                return IncrementallyParseTree(treeSource, siblingTextSource, options, cancellationToken);
            }
        }

        private static bool CanReuseSiblingRoot(
            int ifDirectiveKind,
            ParseOptions parseOptions,
            ParseOptions siblingParseOptions,
            SyntaxNode siblingRoot)
        {
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
            if (!siblingRoot.ContainsDirective(ifDirectiveKind))
                return true;

            // If the tree contains a #if directive, and the pp-symbol-names are different, then the files
            // absolutely may be parsed differently, and so they should not be shared.
            return false;
        }

#if false
        private static void TryInitializeTreeSourceFromRelatedDocument(
            SolutionState solution, DocumentState document, ValueSource<TreeAndVersion> treeSource)
        {
            s_tryShareSyntaxTreeCount++;
            if (document.FilePath == null)
                return;

            var relatedDocumentIds = solution.GetDocumentIdsWithFilePath(document.FilePath);
            foreach (var docId in relatedDocumentIds)
            {
                // ignore this document when looking at siblings.  We can't initialize ourself with ourself.
                if (docId == document.Id)
                    continue;

                var otherProject = solution.GetProjectState(docId.ProjectId);
                if (otherProject == null)
                    continue;

                var otherDocument = otherProject.DocumentStates.GetState(docId);
                if (otherDocument == null)
                    continue;

                // Now, see if the linked doc actually has its tree readily available.
                if (otherDocument._treeSource == null || !otherDocument._treeSource.TryGetValue(out var otherTreeAndVersion))
                    continue;

                // And see if its root is there as well.  Note: we only need the root to determine if the tree contains
                // pp directives. If this could be stored on the tree itself, that would remove the need for having to have
                // the actual root available.

                var otherTree = otherTreeAndVersion.Tree;
                if (!otherTree.TryGetRoot(out var otherRoot))
                    continue;

                // If the processor directives are not compatible between the other document and this one, we definitely
                // can't reuse the tree.
                if (!HasCompatiblePreprocessorDirectives(document, otherDocument, otherRoot))
                    continue;

                // Note: even if the pp directives are compatible, it may *technically* not be safe to reuse the tree.
                // For example, C# parses some things differently across language version.  like `record Goo() { }` is a
                // method prior to 9.0, and a record from 9.0 onwards.  *However*, code that actually contains
                // constructs that would be parsed differently is considered pathological by us.  e.g. we do not believe
                // it is a realistic scenario that users would genuinely write such a construct and need it to have
                // different syntactic meaning like this across versions.  So we allow for this reuse even though the
                // above it a possibility, since we do not consider it important or relevant to support.

#if false

                // Want to make sure that these two docs are pointing at the same text.  Technically it's possible
                // (though unpleasant) to have linked docs pointing to different text.  This is because our in-memory
                // model doesn't enforce any invariants here.  So it's trivially possible to take two linked documents
                // and do things like `doc1.WithSomeText(text1)` and `doc2.WithSomeText(text2)` and now have them be
                // inconsistent in that regard.  They will eventually become consistent, but there can be periods when
                // they are not.  In this case, we don't want a forked doc to grab a tree from another doc that may be
                // looking at some different text.  So we conservatively only allow for the case where we are certain
                // things are ok.
                //
                //  https://github.com/dotnet/roslyn/issues/65797 tracks a cleaner model where the workspace would
                // enforce that all linked docs would share the same source and we would not need this conservative
                // check here.
                var textsAreEquivalent = (document.TextAndVersionSource, otherDocument.TextAndVersionSource) switch
                {
                    // For constant sources (like what we have that wraps open documents, or explicitly forked docs) we
                    // can reuse if the SourceTexts are clearly identical.
                    (ConstantTextAndVersionSource constant1, ConstantTextAndVersionSource constant2) => constant1.Value.Text == constant2.Value.Text,
                    // For loadable sources (like what we have for docs loaded from disk) we know they should have the
                    // same text since they correspond to the same final physical entity on the machine.  Note: this is
                    // not strictly true as technically it's possible to race here with event notifications where a file
                    // changes, one doc sees it and updates its text loader, and the other linked doc hasn't done this
                    // yet.  However, this race has always existed and we accept that it could cause inconsistencies
                    // anyways.
                    (LoadableTextAndVersionSource loadable1, LoadableTextAndVersionSource loadable2) => loadable1.Loader.FilePath == loadable2.Loader.FilePath,

                    // Anything else, and we presume we can't share this root.
                    _ => false,
                };

                if (!textsAreEquivalent)
                {
                    Console.WriteLine($"Texts are not equivalent: {document.TextAndVersionSource.GetType().Name}-{otherDocument.TextAndVersionSource.GetType().Name}");
                    continue;
                }

                // Console.WriteLine("Texts are equivalent");

#endif

                var factory = document.LanguageServices.GetRequiredService<ISyntaxTreeFactoryService>();
                var newTree = factory.CreateSyntaxTree(
                    document.FilePath, otherTree.Options, otherTree.Encoding, document.LoadTextOptions.ChecksumAlgorithm, otherRoot);

                // Ok, now try to set out value-source to this newly created tree.  This may fail if some other thread
                // beat us here. That's ok, our caller (GetSyntaxTreeAsync) will read the source itself.  So we'll only 
                // ever have one source of truth here.
                treeSource.TrySetValue(new TreeAndVersion(newTree, otherTreeAndVersion.Version));
                s_successfullySharedSyntaxTreeCount++;
                return;
            }

            return;

            static bool HasCompatiblePreprocessorDirectives(DocumentState document1, DocumentState document2, SyntaxNode root)
            {
            }
        }
#endif

        internal DocumentState UpdateTree(SyntaxNode newRoot, PreservationMode mode)
        {
            if (!SupportsSyntaxTree)
            {
                throw new InvalidOperationException();
            }

            var newTextVersion = GetNewerVersion();
            var newTreeVersion = GetNewTreeVersionForUpdatedTree(newRoot, newTextVersion, mode);

            // determine encoding
            Encoding? encoding;

            if (TryGetSyntaxTree(out var priorTree))
            {
                // this is most likely available since UpdateTree is normally called after modifying the existing tree.
                encoding = priorTree.Encoding;
            }
            else if (TryGetText(out var priorText))
            {
                encoding = priorText.Encoding;
            }
            else
            {
                // the existing encoding was never observed so is unknown.
                encoding = null;
            }

            var syntaxTreeFactory = _languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

            Contract.ThrowIfNull(_options);
            var (text, treeAndVersion) = CreateTreeWithLazyText(newRoot, newTextVersion, newTreeVersion, encoding, LoadTextOptions.ChecksumAlgorithm, Attributes, _options, syntaxTreeFactory);

            return new DocumentState(
                LanguageServices,
                solutionServices,
                Services,
                Attributes,
                _options,
                textSource: text,
                LoadTextOptions,
                treeSource: ValueSource.Constant(treeAndVersion));

            // use static method so we don't capture references to this
            static (ITextAndVersionSource, TreeAndVersion) CreateTreeWithLazyText(
                SyntaxNode newRoot,
                VersionStamp textVersion,
                VersionStamp treeVersion,
                Encoding? encoding,
                SourceHashAlgorithm checksumAlgorithm,
                DocumentInfo.DocumentAttributes attributes,
                ParseOptions options,
                ISyntaxTreeFactoryService factory)
            {
                var tree = factory.CreateSyntaxTree(attributes.SyntaxTreeFilePath, options, encoding, checksumAlgorithm, newRoot);

                // its okay to use a strong cached AsyncLazy here because the compiler layer SyntaxTree will also keep the text alive once its built.
                var lazyTextAndVersion = new TreeTextSource(
                    new AsyncLazy<SourceText>(
                        tree.GetTextAsync,
                        tree.GetText,
                        cacheResult: true),
                    textVersion);

                return (lazyTextAndVersion, new TreeAndVersion(tree, treeVersion));
            }
        }

        private VersionStamp GetNewTreeVersionForUpdatedTree(SyntaxNode newRoot, VersionStamp newTextVersion, PreservationMode mode)
        {
            RoslynDebug.Assert(_treeSource != null);

            if (mode != PreservationMode.PreserveIdentity)
            {
                return newTextVersion;
            }

            if (!_treeSource.TryGetValue(out var oldTreeAndVersion) || !oldTreeAndVersion!.Tree.TryGetRoot(out var oldRoot))
            {
                return newTextVersion;
            }

            return oldRoot.IsEquivalentTo(newRoot, topLevel: true) ? oldTreeAndVersion.Version : newTextVersion;
        }

        internal override Task<Diagnostic?> GetLoadDiagnosticAsync(CancellationToken cancellationToken)
        {
            if (TextAndVersionSource is TreeTextSource)
            {
                return SpecializedTasks.Null<Diagnostic>();
            }

            return base.GetLoadDiagnosticAsync(cancellationToken);
        }

        private VersionStamp GetNewerVersion()
        {
            if (TextAndVersionSource.TryGetValue(LoadTextOptions, out var textAndVersion))
            {
                return textAndVersion!.Version.GetNewerVersion();
            }

            if (_treeSource != null && _treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                return treeAndVersion.Version.GetNewerVersion();
            }

            return VersionStamp.Create();
        }

        public bool TryGetSyntaxTree([NotNullWhen(returnValue: true)] out SyntaxTree? syntaxTree)
        {
            syntaxTree = null;
            if (_treeSource != null && _treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                syntaxTree = treeAndVersion.Tree;
                BindSyntaxTreeToId(syntaxTree, Id);
                return true;
            }

            return false;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<SyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken)
        {
            // operation should only be performed on documents that support syntax trees
            RoslynDebug.Assert(_treeSource != null);

            var treeAndVersion = await _treeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // make sure there is an association between this tree and this doc id before handing it out
            BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
            return treeAndVersion.Tree;
        }

        internal SyntaxTree GetSyntaxTree(CancellationToken cancellationToken)
        {
            // operation should only be performed on documents that support syntax trees
            RoslynDebug.Assert(_treeSource != null);

            var treeAndVersion = _treeSource.GetValue(cancellationToken);

            // make sure there is an association between this tree and this doc id before handing it out
            BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
            return treeAndVersion.Tree;
        }

        public bool TryGetTopLevelChangeTextVersion(out VersionStamp version)
        {
            if (_treeSource != null && _treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                version = treeAndVersion.Version;
                return true;
            }
            else
            {
                version = default;
                return false;
            }
        }

        public override async Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
        {
            if (_treeSource == null)
            {
                return await GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                return treeAndVersion.Version;
            }

            treeAndVersion = await _treeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return treeAndVersion.Version;
        }

        private static void BindSyntaxTreeToId(SyntaxTree tree, DocumentId id)
        {
            if (!s_syntaxTreeToIdMap.TryGetValue(tree, out var existingId))
            {
                // Avoid closing over parameter 'id' on the method's fast path
                var localId = id;
                existingId = s_syntaxTreeToIdMap.GetValue(tree, t => localId);
            }

            Contract.ThrowIfFalse(existingId == id);
        }

        public static DocumentId? GetDocumentIdForTree(SyntaxTree tree)
        {
            s_syntaxTreeToIdMap.TryGetValue(tree, out var id);
            return id;
        }

        private static void CheckTree(
            SyntaxTree newTree,
            SourceText newText,
            SyntaxTree? oldTree = null,
            SourceText? oldText = null)
        {
            // this should be always true
            if (newTree.Length == newText.Length)
            {
                return;
            }

            var newTreeContent = newTree.GetRoot().ToFullString();
            var newTextContent = newText.ToString();

            var oldTreeContent = oldTree?.GetRoot().ToFullString();
            var oldTextContent = oldText?.ToString();

            // we time to time see (incremental) parsing bug where text <-> tree round tripping is broken.
            // send NFW for those cases, since we'll be in a very broken state at that point
            FatalError.ReportAndCatch(new Exception($"tree and text has different length {newTree.Length} vs {newText.Length}"), ErrorSeverity.Critical);

            // this will make sure that these variables are not thrown away in the dump
            GC.KeepAlive(newTreeContent);
            GC.KeepAlive(newTextContent);
            GC.KeepAlive(oldTreeContent);
            GC.KeepAlive(oldTextContent);

            GC.KeepAlive(newTree);
            GC.KeepAlive(newText);
            GC.KeepAlive(oldTree);
            GC.KeepAlive(oldText);
        }
    }
}

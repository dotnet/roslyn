// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState : TextDocumentState
    {
        private static readonly Func<string, PreservationMode, string> s_fullParseLog = (path, mode) => $"{path} : {mode}";

        private readonly HostLanguageServices _languageServices;
        private readonly ParseOptions _options;
        private readonly ValueSource<AnalyzerConfigSet> _analyzerConfigSetSource;
        private readonly ValueSource<TreeAndVersion> _treeSource;

        private DocumentState(
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IDocumentServiceProvider documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ValueSource<AnalyzerConfigSet> analyzerConfigSetSource,
            SourceText sourceTextOpt,
            ValueSource<TextAndVersion> textSource,
            ValueSource<TreeAndVersion> treeSource)
            : base(solutionServices, documentServiceProvider, attributes, sourceTextOpt, textSource)
        {
            _languageServices = languageServices;
            _options = options;
            _analyzerConfigSetSource = analyzerConfigSetSource;

            // If this is document that doesn't support syntax, then don't even bother holding
            // onto any tree source.  It will never be used to get a tree, and can only hurt us
            // by possibly holding onto data that might cause a slow memory leak.
            _treeSource = this.SupportsSyntaxTree
                ? treeSource
                : ValueSource<TreeAndVersion>.Empty;
        }

        internal bool SupportsSyntaxTree
        {
            get
            {
                return _languageServices.SyntaxTreeFactory != null;
            }
        }

        public DocumentState(
            DocumentInfo info,
            ParseOptions options,
            ValueSource<AnalyzerConfigSet> analyzerConfigSetSource,
            HostLanguageServices languageServices,
            SolutionServices services)
            : base(info, services)
        {
            _languageServices = languageServices;
            _options = options;
            _analyzerConfigSetSource = analyzerConfigSetSource;

            // If this is document that doesn't support syntax, then don't even bother holding
            // onto any tree source.  It will never be used to get a tree, and can only hurt us
            // by possibly holding onto data that might cause a slow memory leak.
            if (!this.SupportsSyntaxTree)
            {
                _treeSource = ValueSource<TreeAndVersion>.Empty;
            }
            else
            {
                _treeSource = CreateLazyFullyParsedTree(
                    base.TextAndVersionSource,
                    info.Id.ProjectId,
                    GetSyntaxTreeFilePath(info.Attributes),
                    options,
                    analyzerConfigSetSource,
                    languageServices,
                    services);
            }
        }

        // This is the string used to represent the FilePath property on a SyntaxTree object.
        // if the document does not yet have a file path, use the document's name instead in regular code
        // or an empty string in script code.
        private static string GetSyntaxTreeFilePath(DocumentInfo.DocumentAttributes info)
        {
            if (info.FilePath != null)
            {
                return info.FilePath;
            }
            return info.SourceCodeKind == SourceCodeKind.Regular
                ? info.Name
                : "";
        }

        private static ValueSource<TreeAndVersion> CreateLazyFullyParsedTree(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            ValueSource<AnalyzerConfigSet> analyzerConfigSetValueSource,
            HostLanguageServices languageServices,
            PreservationMode mode = PreservationMode.PreserveValue)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => FullyParseTreeAsync(newTextSource, cacheKey, filePath, options, analyzerConfigSetValueSource, languageServices, mode, c),
                c => FullyParseTree(newTextSource, cacheKey, filePath, options, analyzerConfigSetValueSource, languageServices, mode, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> FullyParseTreeAsync(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            ValueSource<AnalyzerConfigSet> analyzerConfigSetValueSource,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = await newTextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var analyzerConfigSet = await analyzerConfigSetValueSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, analyzerConfigSet, languageServices, mode, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion FullyParseTree(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            ValueSource<AnalyzerConfigSet> analyzerConfigSetValueSource,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = newTextSource.GetValue(cancellationToken);
                var analyzerConfigSet = analyzerConfigSetValueSource.GetValue(cancellationToken);
                return CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, analyzerConfigSet, languageServices, mode, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion CreateTreeAndVersion(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            AnalyzerConfigSet analyzerConfigSet,
            HostLanguageServices languageServices,
            PreservationMode mode,
            TextAndVersion textAndVersion,
            CancellationToken cancellationToken)
        {
            var text = textAndVersion.Text;

            var treeFactory = languageServices.GetService<ISyntaxTreeFactoryService>();

            var treeDiagnosticOptions = filePath != null ? analyzerConfigSet.GetOptionsForSourcePath(filePath).TreeOptions : null;

            var tree = treeFactory.ParseSyntaxTree(filePath, options, text, treeDiagnosticOptions, cancellationToken);

            var root = tree.GetRoot(cancellationToken);
            if (mode == PreservationMode.PreserveValue && treeFactory.CanCreateRecoverableTree(root))
            {
                tree = treeFactory.CreateRecoverableTree(cacheKey, tree.FilePath, tree.Options, newTextSource, text.Encoding, root, tree.DiagnosticOptions);
            }

            Contract.ThrowIfNull(tree);
            CheckTree(tree, text);

            // text version for this document should be unique. use it as a starting point.
            return TreeAndVersion.Create(tree, textAndVersion.Version);
        }

        private static ValueSource<TreeAndVersion> CreateLazyIncrementallyParsedTree(
            ValueSource<TreeAndVersion> oldTreeSource,
            ValueSource<TextAndVersion> newTextSource)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => IncrementallyParseTreeAsync(oldTreeSource, newTextSource, c),
                c => IncrementallyParseTree(oldTreeSource, newTextSource, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> IncrementallyParseTreeAsync(
            ValueSource<TreeAndVersion> oldTreeSource,
            ValueSource<TextAndVersion> newTextSource,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Workspace_Document_State_IncrementallyParseSyntaxTree, cancellationToken))
                {
                    var newTextAndVersion = await newTextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var oldTreeAndVersion = await oldTreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    return IncrementallyParse(newTextAndVersion, oldTreeAndVersion, cancellationToken);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static TreeAndVersion IncrementallyParseTree(
            ValueSource<TreeAndVersion> oldTreeSource,
            ValueSource<TextAndVersion> newTextSource,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Workspace_Document_State_IncrementallyParseSyntaxTree, cancellationToken))
                {
                    var newTextAndVersion = newTextSource.GetValue(cancellationToken);
                    var oldTreeAndVersion = oldTreeSource.GetValue(cancellationToken);

                    return IncrementallyParse(newTextAndVersion, oldTreeAndVersion, cancellationToken);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
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
            return TreeAndVersion.Create(newTree, version);
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

        /// <summary>
        /// True if the content (text/tree) has changed.
        /// </summary>
        public bool HasContentChanged(DocumentState oldState)
        {
            return oldState._treeSource != this._treeSource
                || oldState.sourceTextOpt != this.sourceTextOpt
                || oldState.TextAndVersionSource != this.TextAndVersionSource;
        }

        /// <summary>
        /// True if the Text has changed
        /// </summary>
        public bool HasTextChanged(DocumentState oldState)
        {
            return (oldState.sourceTextOpt != this.sourceTextOpt
                || oldState.TextAndVersionSource != this.TextAndVersionSource);
        }

        public DocumentState UpdateParseOptions(ParseOptions options)
        {
            var originalSourceKind = this.SourceCodeKind;

            var newState = this.SetParseOptions(options);
            if (newState.SourceCodeKind != originalSourceKind)
            {
                newState = newState.UpdateSourceCodeKind(originalSourceKind);
            }

            return newState;
        }

        private DocumentState SetParseOptions(ParseOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var newTreeSource = CreateLazyFullyParsedTree(
                this.TextAndVersionSource,
                this.Id.ProjectId,
                GetSyntaxTreeFilePath(this.Attributes),
                options,
                _analyzerConfigSetSource,
                _languageServices);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(sourceCodeKind: options.Kind),
                options,
                _analyzerConfigSetSource,
                this.sourceTextOpt,
                this.TextAndVersionSource,
                newTreeSource);
        }

        public DocumentState UpdateSourceCodeKind(SourceCodeKind kind)
        {
            if (this.ParseOptions == null || kind == this.SourceCodeKind)
            {
                return this;
            }

            return this.SetParseOptions(this.ParseOptions.WithKind(kind));
        }

        public DocumentState UpdateName(string name)
        {
            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(name: name),
                _options,
                _analyzerConfigSetSource,
                this.sourceTextOpt,
                this.TextAndVersionSource,
                _treeSource);
        }

        public DocumentState UpdateFolders(IList<string> folders)
        {
            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(folders: folders),
                _options,
                _analyzerConfigSetSource,
                this.sourceTextOpt,
                this.TextAndVersionSource,
                _treeSource);
        }

        public DocumentState UpdateFilePath(string filePath)
        {
            var newAttributes = this.Attributes.With(filePath: filePath);

            // TODO: it's overkill to fully reparse the tree if we had the tree already; all we have to do is update the
            // file path and diagnostic options for that tree.
            var newTreeSource = CreateLazyFullyParsedTree(
                this.TextAndVersionSource,
                this.Id.ProjectId,
                GetSyntaxTreeFilePath(newAttributes),
                _options,
                _analyzerConfigSetSource,
                _languageServices);

            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                newAttributes,
                _options,
                _analyzerConfigSetSource,
                this.sourceTextOpt,
                this.TextAndVersionSource,
                newTreeSource);
        }

        public DocumentState UpdateAnalyzerConfigSet(ValueSource<AnalyzerConfigSet> newAnalyzerConfigSet)
        {
            // TODO: it's overkill to fully reparse the tree if we had the tree already; all we have to do is update the
            // file path and diagnostic options for that tree.
            var newTreeSource = CreateLazyFullyParsedTree(
                this.TextAndVersionSource,
                this.Id.ProjectId,
                GetSyntaxTreeFilePath(this.Attributes),
                _options,
                newAnalyzerConfigSet,
                _languageServices);

            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                newAnalyzerConfigSet,
                this.sourceTextOpt,
                this.TextAndVersionSource,
                newTreeSource);
        }

        public new DocumentState UpdateText(SourceText newText, PreservationMode mode)
        {
            return (DocumentState)base.UpdateText(newText, mode);
        }

        public new DocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        {
            return (DocumentState)base.UpdateText(newTextAndVersion, mode);
        }

        protected override TextDocumentState UpdateText(ValueSource<TextAndVersion> newTextSource, PreservationMode mode, bool incremental)
        {
            ValueSource<TreeAndVersion> newTreeSource;

            if (!this.SupportsSyntaxTree)
            {
                newTreeSource = ValueSource<TreeAndVersion>.Empty;
            }
            else if (incremental)
            {
                newTreeSource = CreateLazyIncrementallyParsedTree(_treeSource, newTextSource);
            }
            else
            {
                newTreeSource = CreateLazyFullyParsedTree(
                    newTextSource,
                    this.Id.ProjectId,
                    GetSyntaxTreeFilePath(this.Attributes),
                    _options,
                    _analyzerConfigSetSource,
                    _languageServices,
                    mode); // TODO: understand why the mode is given here. If we're preserving text by identity, why also preserve the tree?
            }

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                _analyzerConfigSetSource,
                sourceTextOpt: null,
                textSource: newTextSource,
                treeSource: newTreeSource);
        }

        public new DocumentState UpdateText(TextLoader loader, PreservationMode mode)
        {
            return UpdateText(loader, textOpt: null, mode: mode);
        }

        internal DocumentState UpdateText(TextLoader loader, SourceText textOpt, PreservationMode mode)
        {
            var documentState = (DocumentState)base.UpdateText(loader, mode);

            // If we are given a SourceText directly, fork it since we didn't pass that into the base.
            // TODO: understand why this is being called this way at all. It seems we only have a textOpt in a specific case
            // when we are opening a file, when it seems this could have just called the other overload that took a
            // TextAndVersion that could have just pinned the object directly.
            if (textOpt != null)
            {
                return new DocumentState(
                    this.LanguageServices,
                    this.solutionServices,
                    this.Services,
                    this.Attributes,
                    _options,
                    _analyzerConfigSetSource,
                    sourceTextOpt: textOpt,
                    textSource: documentState.TextAndVersionSource,
                    treeSource: documentState._treeSource);
            }

            return documentState;
        }

        internal DocumentState UpdateTree(SyntaxNode newRoot, PreservationMode mode)
        {
            if (newRoot == null)
            {
                throw new ArgumentNullException(nameof(newRoot));
            }

            var newTextVersion = this.GetNewerVersion();
            var newTreeVersion = GetNewTreeVersionForUpdatedTree(newRoot, newTextVersion, mode);

            // determine encoding
            Encoding encoding;
            ImmutableDictionary<string, ReportDiagnostic> treeDiagnosticReportingOptions = null;

            if (this.TryGetSyntaxTree(out var priorTree))
            {
                // this is most likely available since UpdateTree is normally called after modifying the existing tree.
                encoding = priorTree.Encoding;
                treeDiagnosticReportingOptions = priorTree.DiagnosticOptions;
            }
            else if (this.TryGetText(out var priorText))
            {
                encoding = priorText.Encoding;
            }
            else
            {
                // the existing encoding was never observed so is unknown.
                encoding = null;
            }

            var syntaxTreeFactory = _languageServices.GetService<ISyntaxTreeFactoryService>();

            var filePath = GetSyntaxTreeFilePath(this.Attributes);

            if (treeDiagnosticReportingOptions == null && filePath != null)
            {
                // Ideally we'd pass a cancellation token here but we don't have one to pass as the operation previously didn't take a cancellation token.
                // In practice, I don't suspect it will matter: GetValue will only do work if we haven't already computed the AnalyzerConfigSet for this project,
                // which would only happen if no tree was observed for any file in this project. Arbitrarily replacing trees without ever looking at the
                // original one is possible but unlikely.
                treeDiagnosticReportingOptions = _analyzerConfigSetSource.GetValue(CancellationToken.None).GetOptionsForSourcePath(filePath).TreeOptions;
            }

            var result = CreateRecoverableTextAndTree(newRoot, filePath, newTextVersion, newTreeVersion, encoding, this.Attributes, _options, treeDiagnosticReportingOptions, syntaxTreeFactory, mode);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                _analyzerConfigSetSource,
                sourceTextOpt: null,
                textSource: result.Item1,
                treeSource: new ConstantValueSource<TreeAndVersion>(result.Item2));
        }

        private VersionStamp GetNewTreeVersionForUpdatedTree(SyntaxNode newRoot, VersionStamp newTextVersion, PreservationMode mode)
        {
            if (mode != PreservationMode.PreserveIdentity)
            {
                return newTextVersion;
            }

            if (!_treeSource.TryGetValue(out var oldTreeAndVersion) || !oldTreeAndVersion.Tree.TryGetRoot(out var oldRoot))
            {
                return newTextVersion;
            }

            return oldRoot.IsEquivalentTo(newRoot, topLevel: true) ? oldTreeAndVersion.Version : newTextVersion;
        }

        // use static method so we don't capture references to this
        private static Tuple<ValueSource<TextAndVersion>, TreeAndVersion> CreateRecoverableTextAndTree(
            SyntaxNode newRoot,
            string filePath,
            VersionStamp textVersion,
            VersionStamp treeVersion,
            Encoding encoding,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ImmutableDictionary<string, ReportDiagnostic> treeDiagnosticReportingOptionsOpt,
            ISyntaxTreeFactoryService factory,
            PreservationMode mode)
        {
            SyntaxTree tree = null;
            ValueSource<TextAndVersion> lazyTextAndVersion = null;

            if ((mode == PreservationMode.PreserveIdentity) || !factory.CanCreateRecoverableTree(newRoot))
            {
                // its okay to use a strong cached AsyncLazy here because the compiler layer SyntaxTree will also keep the text alive once its built.
                lazyTextAndVersion = new TreeTextSource(
                    new AsyncLazy<SourceText>(
                        c => tree.GetTextAsync(c),
                        c => tree.GetText(c),
                        cacheResult: true),
                    textVersion,
                    filePath);

                tree = factory.CreateSyntaxTree(filePath, options, encoding, newRoot, treeDiagnosticReportingOptionsOpt);
            }
            else
            {
                // uses CachedWeakValueSource so the document and tree will return the same SourceText instance across multiple accesses as long
                // as the text is referenced elsewhere.
                lazyTextAndVersion = new TreeTextSource(
                    new CachedWeakValueSource<SourceText>(
                        new AsyncLazy<SourceText>(
                            c => BuildRecoverableTreeTextAsync(tree, encoding, c),
                            c => BuildRecoverableTreeText(tree, encoding, c),
                            cacheResult: false)),
                    textVersion,
                    filePath);

                tree = factory.CreateRecoverableTree(attributes.Id.ProjectId, filePath, options, lazyTextAndVersion, encoding, newRoot, treeDiagnosticReportingOptionsOpt);
            }

            return Tuple.Create(lazyTextAndVersion, TreeAndVersion.Create(tree, treeVersion));
        }

        private static SourceText BuildRecoverableTreeText(SyntaxTree tree, Encoding encoding, CancellationToken cancellationToken)
        {
            // build text from root, so recoverable tree won't cycle.
            return tree.GetRoot(cancellationToken).GetText(encoding);
        }

        private static async Task<SourceText> BuildRecoverableTreeTextAsync(SyntaxTree tree, Encoding encoding, CancellationToken cancellationToken)
        {
            // build text from root, so recoverable tree won't cycle.
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            return root.GetText(encoding);
        }

        private VersionStamp GetNewerVersion()
        {
            if (this.TextAndVersionSource.TryGetValue(out var textAndVersion))
            {
                return textAndVersion.Version.GetNewerVersion();
            }

            if (_treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                return treeAndVersion.Version.GetNewerVersion();
            }

            return VersionStamp.Create();
        }

        public bool TryGetSyntaxTree(out SyntaxTree syntaxTree)
        {
            syntaxTree = default;
            if (_treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                syntaxTree = treeAndVersion.Tree;
                BindSyntaxTreeToId(syntaxTree, this.Id);
                return true;
            }

            return false;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<SyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken)
        {
            var treeAndVersion = await _treeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // make sure there is an association between this tree and this doc id before handing it out
            BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
            return treeAndVersion.Tree;
        }

        internal SyntaxTree GetSyntaxTree(CancellationToken cancellationToken)
        {
            var treeAndVersion = _treeSource.GetValue(cancellationToken);

            // make sure there is an association between this tree and this doc id before handing it out
            BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
            return treeAndVersion.Tree;
        }

        public bool TryGetTopLevelChangeTextVersion(out VersionStamp version)
        {
            if (_treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
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
            if (!this.SupportsSyntaxTree)
            {
                return await this.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_treeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
            {
                return treeAndVersion.Version;
            }

            treeAndVersion = await _treeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return treeAndVersion.Version;
        }

        public async Task<ImmutableDictionary<string, string>> GetAnalyzerOptionsAsync(string projectFilePath, CancellationToken cancellationToken)
        {
            // We need to work out path to this document. Documents may not have a "real" file path if they're something created
            // as a part of a code action, but haven't been written to disk yet.
            string effectiveFilePath;

            if (FilePath != null)
            {
                effectiveFilePath = FilePath;
            }
            else if (Name != null && projectFilePath != null)
            {
                effectiveFilePath = PathUtilities.CombinePathsUnchecked(PathUtilities.GetDirectoryName(projectFilePath), Name);
            }
            else
            {
                // Really no idea where this is going, so bail
                // TODO: use AnalyzerConfigOptions.EmptyDictionary, since we don't have a public dictionary
                return ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer);
            }

            var analyzerConfigSet = await _analyzerConfigSetSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

            return analyzerConfigSet.GetOptionsForSourcePath(effectiveFilePath).AnalyzerOptions;
        }

        private static readonly ConditionalWeakTable<SyntaxTree, DocumentId> s_syntaxTreeToIdMap =
            new ConditionalWeakTable<SyntaxTree, DocumentId>();

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

        public static DocumentId GetDocumentIdForTree(SyntaxTree tree)
        {
            s_syntaxTreeToIdMap.TryGetValue(tree, out var id);
            return id;
        }

        private static void CheckTree(
            SyntaxTree newTree,
            SourceText newText,
            SyntaxTree oldTree = null,
            SourceText oldText = null)
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
            // send NFW for those cases
            FatalError.ReportWithoutCrash(new Exception($"tree and text has different length {newTree.Length} vs {newText.Length}"));

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

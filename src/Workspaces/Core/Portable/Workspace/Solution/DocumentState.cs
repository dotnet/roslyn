﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState : TextDocumentState
    {
        private static readonly Func<string?, PreservationMode, string> s_fullParseLog = (path, mode) => $"{path} : {mode}";

        private static readonly ConditionalWeakTable<SyntaxTree, DocumentId> s_syntaxTreeToIdMap =
            new();

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
            SourceText? sourceText,
            ITextAndVersionSource textSource,
            LoadTextOptions loadTextOptions,
            ValueSource<TreeAndVersion>? treeSource)
            : base(solutionServices, documentServiceProvider, attributes, sourceText, textSource, loadTextOptions)
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
                    info.Id.ProjectId,
                    info.Attributes.SyntaxTreeFilePath,
                    options,
                    languageServices);
            }
        }

        [MemberNotNullWhen(true, nameof(_treeSource))]
        [MemberNotNullWhen(true, nameof(_options))]
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
            ProjectId cacheKey,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode = PreservationMode.PreserveValue)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => FullyParseTreeAsync(newTextSource, loadTextOptions, cacheKey, filePath, options, languageServices, mode, c),
                c => FullyParseTree(newTextSource, loadTextOptions, cacheKey, filePath, options, languageServices, mode, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> FullyParseTreeAsync(
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            ProjectId cacheKey,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = await newTextSource.GetValueAsync(loadTextOptions, cancellationToken).ConfigureAwait(false);
                var treeAndVersion = CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, languageServices, mode, textAndVersion, cancellationToken);

                // The tree may be a RecoverableSyntaxTree. In its initial state, the RecoverableSyntaxTree keeps a
                // strong reference to the root SyntaxNode, and only transitions to a weak reference backed by temporary
                // storage after the first time GetRoot (or GetRootAsync) is called. Since we know we are creating a
                // RecoverableSyntaxTree for the purpose of avoiding problematic memory overhead, we call GetRoot
                // immediately to force the object to weakly hold its data from the start.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1307180
                await treeAndVersion.Tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                return treeAndVersion;
            }
        }

        private static TreeAndVersion FullyParseTree(
            ITextAndVersionSource newTextSource,
            LoadTextOptions loadTextOptions,
            ProjectId cacheKey,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = newTextSource.GetValue(loadTextOptions, cancellationToken);
                var treeAndVersion = CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, languageServices, mode, textAndVersion, cancellationToken);

                // The tree may be a RecoverableSyntaxTree. In its initial state, the RecoverableSyntaxTree keeps a
                // strong reference to the root SyntaxNode, and only transitions to a weak reference backed by temporary
                // storage after the first time GetRoot (or GetRootAsync) is called. Since we know we are creating a
                // RecoverableSyntaxTree for the purpose of avoiding problematic memory overhead, we call GetRoot
                // immediately to force the object to weakly hold its data from the start.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1307180
                treeAndVersion.Tree.GetRoot(cancellationToken);

                return treeAndVersion;
            }
        }

        private static TreeAndVersion CreateTreeAndVersion(
            ITextAndVersionSource newTextSource,
            ProjectId cacheKey,
            string? filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            PreservationMode mode,
            TextAndVersion textAndVersion,
            CancellationToken cancellationToken)
        {
            var text = textAndVersion.Text;

            var treeFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();

            var tree = treeFactory.ParseSyntaxTree(filePath, options, text, cancellationToken);

            var root = tree.GetRoot(cancellationToken);
            if (mode == PreservationMode.PreserveValue && treeFactory.CanCreateRecoverableTree(root))
            {
                tree = treeFactory.CreateRecoverableTree(cacheKey, tree.FilePath, tree.Options, newTextSource, new LoadTextOptions(text.ChecksumAlgorithm), text.Encoding, root);
            }

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
                Id.ProjectId,
                Attributes.SyntaxTreeFilePath,
                _options,
                _languageServices) : null;

            return new DocumentState(
                LanguageServices,
                LanguageServices.WorkspaceServices,
                Services,
                Attributes,
                _options,
                sourceText,
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

                if (existingTree is IRecoverableSyntaxTree recoverableTree &&
                    !recoverableTree.ContainsDirectives)
                {
                    // It's a recoverable tree, so we can try to reuse without even having to need the root
                    newTree = recoverableTree.WithOptions(options);
                }
                else if (existingTree.TryGetRoot(out var existingRoot) && !existingRoot.ContainsDirectives)
                {
                    var treeFactory = _languageServices.GetRequiredService<ISyntaxTreeFactoryService>();
                    newTree = treeFactory.CreateSyntaxTree(Attributes.SyntaxTreeFilePath, options, existingTree.Encoding, LoadTextOptions.ChecksumAlgorithm, existingRoot);
                }

                if (newTree is not null)
                    newTreeSource = new ConstantValueSource<TreeAndVersion>(new TreeAndVersion(newTree, existingTreeAndVersion.Version));
            }

            // If we weren't able to reuse in a smart way, just reparse
            newTreeSource ??= CreateLazyFullyParsedTree(
                TextAndVersionSource,
                LoadTextOptions,
                Id.ProjectId,
                Attributes.SyntaxTreeFilePath,
                options,
                _languageServices);

            return new DocumentState(
                LanguageServices,
                LanguageServices.WorkspaceServices,
                Services,
                Attributes.With(sourceCodeKind: options.Kind),
                options,
                sourceText,
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
                sourceText,
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
                    Id.ProjectId,
                    newAttributes.SyntaxTreeFilePath,
                    _options,
                    _languageServices) : null;

            return new DocumentState(
                _languageServices,
                solutionServices,
                Services,
                newAttributes,
                _options,
                sourceText,
                TextAndVersionSource,
                LoadTextOptions,
                newTreeSource);
        }

        public new DocumentState UpdateText(SourceText newText, PreservationMode mode)
            => (DocumentState)base.UpdateText(newText, mode);

        public new DocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
            => (DocumentState)base.UpdateText(newTextAndVersion, mode);

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
                    Id.ProjectId,
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
                sourceText: null,
                textSource: newTextSource,
                LoadTextOptions,
                treeSource: newTreeSource);
        }

        internal DocumentState UpdateText(TextLoader loader, SourceText? text, PreservationMode mode)
        {
            var documentState = (DocumentState)UpdateText(loader, mode);

            // If we are given a SourceText directly, fork it since we didn't pass that into the base.
            // TODO: understand why this is being called this way at all. It seems we only have a text in a specific case
            // when we are opening a file, when it seems this could have just called the other overload that took a
            // TextAndVersion that could have just pinned the object directly.
            if (text == null)
            {
                return documentState;
            }

            return new DocumentState(
                LanguageServices,
                solutionServices,
                Services,
                Attributes,
                _options,
                sourceText: text,
                textSource: documentState.TextAndVersionSource,
                LoadTextOptions,
                treeSource: documentState._treeSource);
        }

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
            var (text, treeAndVersion) = CreateRecoverableTextAndTree(newRoot, newTextVersion, newTreeVersion, encoding, LoadTextOptions.ChecksumAlgorithm, Attributes, _options, syntaxTreeFactory, mode);

            return new DocumentState(
                LanguageServices,
                solutionServices,
                Services,
                Attributes,
                _options,
                sourceText: null,
                textSource: text,
                LoadTextOptions,
                treeSource: new ConstantValueSource<TreeAndVersion>(treeAndVersion));
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

        // use static method so we don't capture references to this
        private static (ITextAndVersionSource, TreeAndVersion) CreateRecoverableTextAndTree(
            SyntaxNode newRoot,
            VersionStamp textVersion,
            VersionStamp treeVersion,
            Encoding? encoding,
            SourceHashAlgorithm checksumAlgorithm,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ISyntaxTreeFactoryService factory,
            PreservationMode mode)
        {
            SyntaxTree tree;
            ITextAndVersionSource lazyTextAndVersion;

            if (mode == PreservationMode.PreserveIdentity || !factory.CanCreateRecoverableTree(newRoot))
            {
                tree = factory.CreateSyntaxTree(attributes.SyntaxTreeFilePath, options, encoding, checksumAlgorithm, newRoot);

                // its okay to use a strong cached AsyncLazy here because the compiler layer SyntaxTree will also keep the text alive once its built.
                lazyTextAndVersion = new TreeTextSource(
                    new AsyncLazy<SourceText>(
                        tree.GetTextAsync,
                        tree.GetText,
                        cacheResult: true),
                    textVersion);
            }
            else
            {
                // There is a strange circularity here: the creation of lazyTextAndVersion reads this local, but will see it as non-null since it
                // only uses it through a lambda that won't have ran. The assignment exists to placate the definite-assignment analysis (which is
                // right to be suspicious of this).
                tree = null!;

                // Uses CachedWeakValueSource so the document and tree will return the same SourceText instance across multiple accesses as long
                // as the text is referenced elsewhere.
                lazyTextAndVersion = new TreeTextSource(
                    new WeaklyCachedValueSource<SourceText>(
                        new AsyncLazy<SourceText>(
                            // Build text from root, so recoverable tree won't cycle.
                            async cancellationToken => (await tree.GetRootAsync(cancellationToken).ConfigureAwait(false)).GetText(encoding, checksumAlgorithm),
                            cancellationToken => tree.GetRoot(cancellationToken).GetText(encoding, checksumAlgorithm),
                            cacheResult: false)),
                    textVersion);

                tree = factory.CreateRecoverableTree(attributes.Id.ProjectId, attributes.SyntaxTreeFilePath, options, lazyTextAndVersion, new LoadTextOptions(checksumAlgorithm), encoding, newRoot);
            }

            return (lazyTextAndVersion, new TreeAndVersion(tree, treeVersion));
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

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
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

        private readonly ValueSource<TreeAndVersion> _treeSource;

        private DocumentState(
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IDocumentServiceProvider documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            SourceText sourceTextOpt,
            ValueSource<TextAndVersion> textSource,
            ValueSource<TreeAndVersion> treeSource,
            ValueSource<DocumentStateChecksums> lazyChecksums)
            : base(solutionServices, documentServiceProvider, attributes, sourceTextOpt, textSource, lazyChecksums)
        {
            _languageServices = languageServices;
            _options = options;

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

        public static DocumentState Create(
            DocumentInfo info,
            ParseOptions options,
            HostLanguageServices language,
            SolutionServices services)
        {
            var textSource = info.TextLoader != null
                ? CreateRecoverableText(info.TextLoader, info.Id, services, reportInvalidDataException: true)
                : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, info.FilePath));

            var treeSource = CreateLazyFullyParsedTree(
                textSource,
                info.Id.ProjectId,
                GetSyntaxTreeFilePath(info.Attributes),
                options,
                language,
                services);

            return new DocumentState(
                languageServices: language,
                documentServiceProvider: info.DocumentServiceProvider,
                solutionServices: services,
                attributes: info.Attributes,
                options: options,
                sourceTextOpt: null,
                textSource: textSource,
                treeSource: treeSource,
                lazyChecksums: null);
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
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            PreservationMode mode = PreservationMode.PreserveValue)
        {
            return new AsyncLazy<TreeAndVersion>(
                c => FullyParseTreeAsync(newTextSource, cacheKey, filePath, options, languageServices, solutionServices, mode, c),
                c => FullyParseTree(newTextSource, cacheKey, filePath, options, languageServices, solutionServices, mode, c),
                cacheResult: true);
        }

        private static async Task<TreeAndVersion> FullyParseTreeAsync(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = await newTextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, languageServices, mode, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion FullyParseTree(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey,
            string filePath,
            ParseOptions options,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            PreservationMode mode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_State_FullyParseSyntaxTree, s_fullParseLog, filePath, mode, cancellationToken))
            {
                var textAndVersion = newTextSource.GetValue(cancellationToken);
                return CreateTreeAndVersion(newTextSource, cacheKey, filePath, options, languageServices, mode, textAndVersion, cancellationToken);
            }
        }

        private static TreeAndVersion CreateTreeAndVersion(
            ValueSource<TextAndVersion> newTextSource,
            ProjectId cacheKey, string filePath,
            ParseOptions options, HostLanguageServices languageServices,
            PreservationMode mode, TextAndVersion textAndVersion,
            CancellationToken cancellationToken)
        {
            var text = textAndVersion.Text;

            var treeFactory = languageServices.GetService<ISyntaxTreeFactoryService>();

            var tree = treeFactory.ParseSyntaxTree(filePath, options, text, cancellationToken);

            var root = tree.GetRoot(cancellationToken);
            if (mode == PreservationMode.PreserveValue && treeFactory.CanCreateRecoverableTree(root))
            {
                tree = treeFactory.CreateRecoverableTree(cacheKey, tree.FilePath, tree.Options, newTextSource, text.Encoding, root);
            }

            Contract.ThrowIfNull(tree);

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
                || oldState.textAndVersionSource != this.textAndVersionSource;
        }

        /// <summary>
        /// True if the Text has changed
        /// </summary>
        public bool HasTextChanged(DocumentState oldState)
        {
            return (oldState.sourceTextOpt != this.sourceTextOpt
                || oldState.textAndVersionSource != this.textAndVersionSource);
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
                this.textAndVersionSource,
                this.Id.ProjectId,
                GetSyntaxTreeFilePath(this.Attributes),
                options,
                _languageServices,
                this.solutionServices);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(sourceCodeKind: options.Kind),
                options,
                this.sourceTextOpt,
                this.textAndVersionSource,
                newTreeSource,
                lazyChecksums: null);
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
                this.sourceTextOpt,
                this.textAndVersionSource,
                _treeSource,
                lazyChecksums: null);
        }

        public DocumentState UpdateFolders(IList<string> folders)
        {
            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(folders: folders),
                _options,
                this.sourceTextOpt,
                this.textAndVersionSource,
                _treeSource,
                lazyChecksums: null);
        }

        public DocumentState UpdateFilePath(string filePath)
        {
            return new DocumentState(
                _languageServices,
                this.solutionServices,
                this.Services,
                this.Attributes.With(filePath: filePath),
                _options,
                this.sourceTextOpt,
                this.textAndVersionSource,
                _treeSource,
                lazyChecksums: null);
        }

        public new DocumentState UpdateText(SourceText newText, PreservationMode mode)
        {
            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            var newVersion = this.GetNewerVersion();
            var newTextAndVersion = TextAndVersion.Create(newText, newVersion, this.FilePath);

            return this.UpdateText(newTextAndVersion, mode);
        }

        public new DocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        {
            if (newTextAndVersion == null)
            {
                throw new ArgumentNullException(nameof(newTextAndVersion));
            }

            var newTextSource = mode == PreservationMode.PreserveIdentity
                ? CreateStrongText(newTextAndVersion)
                : CreateRecoverableText(newTextAndVersion, this.solutionServices);

            // always chain incremental parsing request, it will internally put
            // appropriate request such as full parsing request if there are too many pending
            // incremental parsing requests hanging around.
            //
            // However, don't bother with the chaining if this is a document that doesn't support
            // syntax trees.  The chaining will keep old data alive (like the old tree source,
            // which itself is keeping an old tree source which itself is keeping a ... alive),
            // causing a slow memory leak.
            var newTreeSource = !this.SupportsSyntaxTree
                ? ValueSource<TreeAndVersion>.Empty
                : CreateLazyIncrementallyParsedTree(_treeSource, newTextSource);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                sourceTextOpt: null,
                textSource: newTextSource,
                treeSource: newTreeSource,
                lazyChecksums: null);
        }

        public new DocumentState UpdateText(TextLoader loader, PreservationMode mode)
        {
            return UpdateText(loader, textOpt: null, mode: mode);
        }

        internal DocumentState UpdateText(TextLoader loader, SourceText textOpt, PreservationMode mode)
        {
            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            var newTextSource = mode == PreservationMode.PreserveIdentity
                ? CreateStrongText(loader, this.Id, this.solutionServices, reportInvalidDataException: true)
                : CreateRecoverableText(loader, this.Id, this.solutionServices, reportInvalidDataException: true);

            // Only create the ValueSource for creating the SyntaxTree if this is a Document that
            // supports SyntaxTrees.  There's no point in creating the async lazy and holding onto
            // this data otherwise.
            var newTreeSource = !this.SupportsSyntaxTree
                ? ValueSource<TreeAndVersion>.Empty
                : CreateLazyFullyParsedTree(
                    newTextSource,
                    this.Id.ProjectId,
                    GetSyntaxTreeFilePath(this.Attributes),
                    _options,
                    _languageServices,
                    this.solutionServices,
                    mode);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                sourceTextOpt: textOpt,
                textSource: newTextSource,
                treeSource: newTreeSource,
                lazyChecksums: null);
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
            if (this.TryGetSyntaxTree(out var priorTree))
            {
                // this is most likely available since UpdateTree is normally called after modifying the existing tree.
                encoding = priorTree.Encoding;
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

            var result = CreateRecoverableTextAndTree(newRoot, newTextVersion, newTreeVersion, encoding, this.Attributes, _options, syntaxTreeFactory, mode, this.solutionServices);

            return new DocumentState(
                this.LanguageServices,
                this.solutionServices,
                this.Services,
                this.Attributes,
                _options,
                sourceTextOpt: null,
                textSource: result.Item1,
                treeSource: new ConstantValueSource<TreeAndVersion>(result.Item2),
                lazyChecksums: null);
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
            SyntaxNode newRoot, VersionStamp textVersion, VersionStamp treeVersion, Encoding encoding,
            DocumentInfo.DocumentAttributes attributes, ParseOptions options, ISyntaxTreeFactoryService factory, PreservationMode mode, SolutionServices solutionServices)
        {
            string filePath = attributes.FilePath;
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

                tree = factory.CreateSyntaxTree(GetSyntaxTreeFilePath(attributes), options, encoding, newRoot);
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

                tree = factory.CreateRecoverableTree(attributes.Id.ProjectId, GetSyntaxTreeFilePath(attributes), options, lazyTextAndVersion, encoding, newRoot);
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
            if (this.textAndVersionSource.TryGetValue(out var textAndVersion))
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
    }
}

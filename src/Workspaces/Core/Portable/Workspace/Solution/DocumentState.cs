// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace Microsoft.CodeAnalysis;

internal partial class DocumentState : TextDocumentState
{
    private static readonly Func<string?, PreservationMode, string> s_fullParseLog = (path, mode) => $"{path} : {mode}";

    private static readonly ConditionalWeakTable<SyntaxTree, DocumentId> s_syntaxTreeToIdMap = new();

    // properties inherited from the containing project:
    public LanguageServices LanguageServices { get; }
    public ParseOptions? ParseOptions { get; }

    // null if the document doesn't support syntax trees:
    public ITreeAndVersionSource? TreeSource { get; }

    private ImmutableArray<byte> _contentHash;

    protected DocumentState(
        LanguageServices languageServices,
        IDocumentServiceProvider? documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textSource,
        LoadTextOptions loadTextOptions,
        ParseOptions? options,
        ITreeAndVersionSource? treeSource)
        : base(languageServices.SolutionServices, documentServiceProvider, attributes, textSource, loadTextOptions)
    {
        Contract.ThrowIfFalse(ParseOptions is null == TreeSource is null);

        LanguageServices = languageServices;
        ParseOptions = options;
        TreeSource = treeSource;
    }

    public static DocumentState Create(
        LanguageServices languageServices,
        DocumentInfo info,
        ParseOptions? options,
        LoadTextOptions loadTextOptions)
    {
        var textSource = CreateTextAndVersionSource(languageServices.SolutionServices, info, loadTextOptions);

        // If this is document that doesn't support syntax, then don't even bother holding
        // onto any tree source.  It will never be used to get a tree, and can only hurt us
        // by possibly holding onto data that might cause a slow memory leak.
        ITreeAndVersionSource? treeSource;
        if (languageServices.GetService<ISyntaxTreeFactoryService>() == null)
        {
            treeSource = null;
        }
        else
        {
            Contract.ThrowIfNull(options);
            treeSource = CreateLazyFullyParsedTree(
                textSource,
                loadTextOptions,
                info.Attributes.SyntaxTreeFilePath,
                options,
                languageServices);
        }

        return new DocumentState(
            languageServices,
            info.DocumentServiceProvider,
            info.Attributes,
            textSource,
            loadTextOptions,
            options,
            treeSource);
    }

    [MemberNotNullWhen(true, nameof(TreeSource))]
    [MemberNotNullWhen(true, nameof(ParseOptions))]
    internal bool SupportsSyntaxTree
        => TreeSource != null;

    public SourceCodeKind SourceCodeKind
        => ParseOptions?.Kind ?? Attributes.SourceCodeKind;

    public bool IsGenerated
        => Attributes.IsGenerated;

    public async ValueTask<ImmutableArray<byte>> GetContentHashAsync(CancellationToken cancellationToken)
    {
        if (_contentHash.IsDefault)
        {
            var text = await this.GetTextAsync(cancellationToken).ConfigureAwait(false);
            ImmutableInterlocked.InterlockedCompareExchange(ref _contentHash, text.GetContentHash(), default);
        }

        return _contentHash;
    }

    protected static ITreeAndVersionSource CreateLazyFullyParsedTree(
        ITextAndVersionSource newTextSource,
        LoadTextOptions loadTextOptions,
        string? filePath,
        ParseOptions options,
        LanguageServices languageServices,
        PreservationMode mode = PreservationMode.PreserveValue)
    {
        return SimpleTreeAndVersionSource.Create(
            static (arg, c) => FullyParseTreeAsync(arg.newTextSource, arg.loadTextOptions, arg.filePath, arg.options, arg.languageServices, arg.mode, c),
            static (arg, c) => FullyParseTree(arg.newTextSource, arg.loadTextOptions, arg.filePath, arg.options, arg.languageServices, arg.mode, c),
            arg: (newTextSource, loadTextOptions, filePath, options, languageServices, mode));
    }

    private static async Task<TreeAndVersion> FullyParseTreeAsync(
        ITextAndVersionSource newTextSource,
        LoadTextOptions loadTextOptions,
        string? filePath,
        ParseOptions options,
        LanguageServices languageServices,
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
        LanguageServices languageServices,
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
        LanguageServices languageServices,
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

    private static ITreeAndVersionSource CreateLazyIncrementallyParsedTree(
        ITreeAndVersionSource oldTreeSource,
        ITextAndVersionSource newTextSource,
        LoadTextOptions loadTextOptions)
    {
        return SimpleTreeAndVersionSource.Create(
            static (arg, c) => IncrementallyParseTreeAsync(arg.oldTreeSource, arg.newTextSource, arg.loadTextOptions, c),
            static (arg, c) => IncrementallyParseTree(arg.oldTreeSource, arg.newTextSource, arg.loadTextOptions, c),
            arg: (oldTreeSource, newTextSource, loadTextOptions));
    }

    private static async Task<TreeAndVersion> IncrementallyParseTreeAsync(
        ITreeAndVersionSource oldTreeSource,
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
        ITreeAndVersionSource oldTreeSource,
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
        return oldState.TreeSource != TreeSource
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
            ParseOptions,
            LanguageServices) : null;

        return new DocumentState(
            LanguageServices,
            DocumentServiceProvider,
            Attributes,
            TextAndVersionSource,
            newLoadTextOptions,
            ParseOptions,
            newTreeSource);
    }

    public DocumentState UpdateParseOptionsAndSourceCodeKind(ParseOptions options, bool onlyPreprocessorDirectiveChange)
    {
        Contract.ThrowIfFalse(SupportsSyntaxTree);

        ITreeAndVersionSource? newTreeSource = null;

        // Optimization: if we are only changing preprocessor directives, and we've already parsed the existing tree
        // and it didn't have any, we can avoid a reparse since the tree will be parsed the same.
        //
        // We only need to care about `#if` directives as those are the only sorts of directives that can affect how
        // a tree is parsed.
        if (onlyPreprocessorDirectiveChange &&
            TreeSource.TryGetValue(out var existingTreeAndVersion))
        {
            var existingTree = existingTreeAndVersion.Tree;

            SyntaxTree? newTree = null;

            var syntaxKinds = LanguageServices.GetRequiredService<ISyntaxKindsService>();
            if (existingTree.TryGetRoot(out var existingRoot) && !existingRoot.ContainsDirective(syntaxKinds.IfDirectiveTrivia))
            {
                var treeFactory = LanguageServices.GetRequiredService<ISyntaxTreeFactoryService>();
                newTree = treeFactory.CreateSyntaxTree(Attributes.SyntaxTreeFilePath, options, existingTree.Encoding, LoadTextOptions.ChecksumAlgorithm, existingRoot);
            }

            if (newTree is not null)
                newTreeSource = SimpleTreeAndVersionSource.Create(new TreeAndVersion(newTree, existingTreeAndVersion.Version));
        }

        // If we weren't able to reuse in a smart way, just reparse
        newTreeSource ??= CreateLazyFullyParsedTree(
            TextAndVersionSource,
            LoadTextOptions,
            Attributes.SyntaxTreeFilePath,
            options,
            LanguageServices);

        return new DocumentState(
            LanguageServices,
            DocumentServiceProvider,
            Attributes.With(sourceCodeKind: options.Kind),
            TextAndVersionSource,
            LoadTextOptions,
            options,
            newTreeSource);
    }

    public DocumentState UpdateSourceCodeKind(SourceCodeKind kind)
    {
        if (kind == SourceCodeKind)
        {
            return this;
        }

        if (ParseOptions != null)
        {
            return UpdateParseOptionsAndSourceCodeKind(ParseOptions.WithKind(kind), onlyPreprocessorDirectiveChange: false);
        }

        return WithAttributes(Attributes.With(sourceCodeKind: kind));
    }

    protected override TextDocumentState UpdateAttributes(DocumentInfo.DocumentAttributes newAttributes)
    {
        Contract.ThrowIfTrue(ReferenceEquals(newAttributes, Attributes));
        ITreeAndVersionSource? newTreeSource;

        if (newAttributes.SyntaxTreeFilePath != Attributes.SyntaxTreeFilePath)
        {
            // TODO: it's overkill to fully reparse the tree if we had the tree already; all we have to do is update the
            // file path and diagnostic options for that tree.
            newTreeSource = SupportsSyntaxTree ?
                CreateLazyFullyParsedTree(
                    TextAndVersionSource,
                    LoadTextOptions,
                    newAttributes.SyntaxTreeFilePath,
                    ParseOptions,
                    LanguageServices) : null;
        }
        else
        {
            newTreeSource = TreeSource;
        }

        return new DocumentState(
            LanguageServices,
            DocumentServiceProvider,
            newAttributes,
            TextAndVersionSource,
            LoadTextOptions,
            ParseOptions,
            newTreeSource);
    }

    public new DocumentState WithAttributes(DocumentInfo.DocumentAttributes newAttributes)
        => (DocumentState)base.WithAttributes(newAttributes);

    public new DocumentState UpdateText(SourceText newText, PreservationMode mode)
        => (DocumentState)base.UpdateText(newText, mode);

    public new DocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        => (DocumentState)base.UpdateText(newTextAndVersion, mode);

    public new DocumentState UpdateText(TextLoader loader, PreservationMode mode)
        => (DocumentState)base.UpdateText(loader, mode);

    protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
    {
        ITreeAndVersionSource? newTreeSource;

        if (!SupportsSyntaxTree)
        {
            newTreeSource = null;
        }
        else if (incremental)
        {
            newTreeSource = CreateLazyIncrementallyParsedTree(TreeSource, newTextSource, LoadTextOptions);
        }
        else
        {
            newTreeSource = CreateLazyFullyParsedTree(
                newTextSource,
                LoadTextOptions,
                Attributes.SyntaxTreeFilePath,
                ParseOptions,
                LanguageServices,
                mode); // TODO: understand why the mode is given here. If we're preserving text by identity, why also preserve the tree?
        }

        return new DocumentState(
            LanguageServices,
            DocumentServiceProvider,
            Attributes,
            textSource: newTextSource,
            LoadTextOptions,
            ParseOptions,
            treeSource: newTreeSource);
    }

    internal DocumentState UpdateTree(SyntaxNode newRoot, PreservationMode mode)
    {
        if (!SupportsSyntaxTree)
        {
            throw new InvalidOperationException();
        }

        var newTextVersion = GetNewerVersion();
        var newTreeVersion = GetNewTreeVersionForUpdatedTree(newRoot, newTextVersion, mode);

        // use the encoding that we get from the new root
        var encoding = newRoot.SyntaxTree.Encoding;

        var syntaxTreeFactory = LanguageServices.GetRequiredService<ISyntaxTreeFactoryService>();

        Contract.ThrowIfNull(ParseOptions);
        var (text, treeAndVersion) = CreateTreeWithLazyText(newRoot, newTextVersion, newTreeVersion, encoding, LoadTextOptions.ChecksumAlgorithm, Attributes, ParseOptions, syntaxTreeFactory);

        return new DocumentState(
            LanguageServices,
            DocumentServiceProvider,
            Attributes,
            textSource: text,
            LoadTextOptions,
            ParseOptions,
            treeSource: SimpleTreeAndVersionSource.Create(treeAndVersion));

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
                AsyncLazy.Create(
                    static (tree, c) => tree.GetTextAsync(c),
                    static (tree, c) => tree.GetText(c),
                    arg: tree),
                textVersion);

            return (lazyTextAndVersion, new TreeAndVersion(tree, treeVersion));
        }
    }

    private VersionStamp GetNewTreeVersionForUpdatedTree(SyntaxNode newRoot, VersionStamp newTextVersion, PreservationMode mode)
    {
        RoslynDebug.Assert(TreeSource != null);

        if (mode != PreservationMode.PreserveIdentity)
        {
            return newTextVersion;
        }

        if (!TreeSource.TryGetValue(out var oldTreeAndVersion) || !oldTreeAndVersion!.Tree.TryGetRoot(out var oldRoot))
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

        if (TreeSource != null && TreeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
        {
            return treeAndVersion.Version.GetNewerVersion();
        }

        return VersionStamp.Create();
    }

    public bool TryGetSyntaxTree([NotNullWhen(returnValue: true)] out SyntaxTree? syntaxTree)
    {
        syntaxTree = null;
        if (TreeSource != null && TreeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
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
        RoslynDebug.Assert(TreeSource != null);

        var treeAndVersion = await TreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);

        // make sure there is an association between this tree and this doc id before handing it out
        BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
        return treeAndVersion.Tree;
    }

    internal SyntaxTree GetSyntaxTree(CancellationToken cancellationToken)
    {
        // operation should only be performed on documents that support syntax trees
        RoslynDebug.Assert(TreeSource != null);

        var treeAndVersion = TreeSource.GetValue(cancellationToken);

        // make sure there is an association between this tree and this doc id before handing it out
        BindSyntaxTreeToId(treeAndVersion.Tree, this.Id);
        return treeAndVersion.Tree;
    }

    public bool TryGetTopLevelChangeTextVersion(out VersionStamp version)
    {
        if (TreeSource != null && TreeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
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

    public override async ValueTask<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
    {
        if (TreeSource == null)
        {
            return await GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        }

        if (TreeSource.TryGetValue(out var treeAndVersion) && treeAndVersion != null)
        {
            return treeAndVersion.Version;
        }

        treeAndVersion = await TreeSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
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

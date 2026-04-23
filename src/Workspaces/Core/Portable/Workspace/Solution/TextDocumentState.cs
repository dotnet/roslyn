// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal abstract partial class TextDocumentState
{
    public readonly SolutionServices SolutionServices;
    public readonly IDocumentServiceProvider DocumentServiceProvider;
    public readonly DocumentInfo.DocumentAttributes Attributes;
    public readonly ITextAndVersionSource TextAndVersionSource;
    public readonly LoadTextOptions LoadTextOptions;

    // Checksums for this solution state
    private readonly AsyncLazy<DocumentStateChecksums> _lazyChecksums;

    protected TextDocumentState(
        SolutionServices solutionServices,
        IDocumentServiceProvider? documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textAndVersionSource,
        LoadTextOptions loadTextOptions)
    {
        SolutionServices = solutionServices;
        DocumentServiceProvider = documentServiceProvider ?? DefaultTextDocumentServiceProvider.Instance;
        Attributes = attributes;
        TextAndVersionSource = textAndVersionSource;
        LoadTextOptions = loadTextOptions;

        // This constructor is called whenever we're creating a new TextDocumentState from another
        // TextDocumentState, and so we populate all the fields from the inputs. We will always create
        // a new AsyncLazy to compute the checksum though, and that's because there's no practical way for
        // the newly created TextDocumentState to have the same checksum as a previous TextDocumentState:
        // if we're creating a new state, it's because something changed, and we'll have to create a new checksum.
        _lazyChecksums = AsyncLazy.Create(static (self, cancellationToken) => self.ComputeChecksumsAsync(cancellationToken), arg: this);
    }

    public DocumentId Id => Attributes.Id;
    public string? FilePath => Attributes.FilePath;
    public IReadOnlyList<string> Folders => Attributes.Folders;
    public string Name => Attributes.Name;

    public TextDocumentState WithDocumentInfo(DocumentInfo info)
        => WithAttributes(info.Attributes)
          .WithDocumentServiceProvider(info.DocumentServiceProvider)
          .WithTextLoader(info.TextLoader, PreservationMode.PreserveValue);

    public TextDocumentState WithAttributes(DocumentInfo.DocumentAttributes newAttributes)
        => ReferenceEquals(newAttributes, Attributes) ? this : UpdateAttributes(newAttributes);

    public TextDocumentState WithDocumentServiceProvider(IDocumentServiceProvider? newProvider)
        => ReferenceEquals(newProvider, DocumentServiceProvider) ? this : UpdateDocumentServiceProvider(newProvider);

    public TextDocumentState WithTextLoader(TextLoader? loader, PreservationMode mode)
        => ReferenceEquals(loader, TextAndVersionSource.TextLoader) ? this : UpdateText(loader, mode);

    protected abstract TextDocumentState UpdateAttributes(DocumentInfo.DocumentAttributes newAttributes);
    protected abstract TextDocumentState UpdateDocumentServiceProvider(IDocumentServiceProvider? newProvider);
    protected abstract TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental);

    private static ConstantTextAndVersionSource CreateStrongText(TextAndVersion text)
        => new(text);

    private static RecoverableTextAndVersion CreateRecoverableText(TextAndVersion text, SolutionServices services)
        => new(new ConstantTextAndVersionSource(text), services);

    public ITemporaryStorageTextHandle? StorageHandle
        => (TextAndVersionSource as RecoverableTextAndVersion)?.StorageHandle;

    public bool TryGetText([NotNullWhen(returnValue: true)] out SourceText? text)
    {
        if (this.TextAndVersionSource.TryGetValue(LoadTextOptions, out var textAndVersion))
        {
            text = textAndVersion.Text;
            return true;
        }
        else
        {
            text = null;
            return false;
        }
    }

    public bool TryGetTextVersion(out VersionStamp version)
        => TextAndVersionSource.TryGetVersion(LoadTextOptions, out version);

    public bool TryGetTextAndVersion([NotNullWhen(true)] out TextAndVersion? textAndVersion)
        => TextAndVersionSource.TryGetValue(LoadTextOptions, out textAndVersion);

    public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        if (TryGetText(out var text))
            return text;

        var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
        return textAndVersion.Text;
    }

    public SourceText GetTextSynchronously(CancellationToken cancellationToken)
    {
        var textAndVersion = this.TextAndVersionSource.GetValue(LoadTextOptions, cancellationToken);
        return textAndVersion.Text;
    }

    public VersionStamp GetTextVersionSynchronously(CancellationToken cancellationToken)
    {
        var textAndVersion = this.TextAndVersionSource.GetValue(LoadTextOptions, cancellationToken);
        return textAndVersion.Version;
    }

    public async ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        // try fast path first
        if (TryGetTextVersion(out var version))
        {
            return version;
        }

        var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
        return textAndVersion.Version;
    }

    public TextDocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        => UpdateText(mode == PreservationMode.PreserveIdentity
                ? CreateStrongText(newTextAndVersion)
                : CreateRecoverableText(newTextAndVersion, SolutionServices),
            mode,
            incremental: true);

    public TextDocumentState UpdateText(SourceText newText, PreservationMode mode)
    {
        var newVersion = GetNewerVersion();
        var newTextAndVersion = TextAndVersion.Create(newText, newVersion, FilePath);

        return UpdateText(newTextAndVersion, mode);
    }

    public TextDocumentState UpdateText(TextLoader? loader, PreservationMode mode)
    {
        // don't blow up on non-text documents.
        var newTextSource = CreateTextAndVersionSource(SolutionServices, loader, FilePath, LoadTextOptions, mode);

        return UpdateText(newTextSource, mode, incremental: false);
    }

    protected static ITextAndVersionSource CreateTextAndVersionSource(SolutionServices solutionServices, TextLoader? loader, string? filePath, LoadTextOptions loadTextOptions, PreservationMode mode = PreservationMode.PreserveValue)
        => loader != null
            ? CreateTextFromLoader(solutionServices, loader, mode)
            : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, encoding: null, loadTextOptions.ChecksumAlgorithm), VersionStamp.Default, filePath));

    private static ITextAndVersionSource CreateTextFromLoader(SolutionServices solutionServices, TextLoader loader, PreservationMode mode)
    {
        // If the caller is explicitly stating that identity must be preserved, then we created a source that will load
        // from the loader the first time, but then cache that result so that hte same result is *always* returned.
        if (mode == PreservationMode.PreserveIdentity)
            return new LoadableTextAndVersionSource(loader, cacheResult: true);

        // If the loader asks us to always hold onto it strongly, then we do not want to create a recoverable text
        // source here.  Instead, we'll go back to the loader each time to get the text.  This is useful for when the
        // loader knows it can always reconstitute the snapshot exactly as it was before.  For example, if the loader
        // points at the contents of a memory mapped file in another process.
        if (loader.AlwaysHoldStrongly)
            return new LoadableTextAndVersionSource(loader, cacheResult: false);

        // Otherwise, we just want to hold onto this loader by value.  So we create a loader that will load the
        // contents, but not hold onto them strongly, and we wrap it in a recoverable-text that will then take those
        // contents and dump it into a memory-mapped-file in this process so that snapshot semantics can be preserved.
        return new RecoverableTextAndVersion(new LoadableTextAndVersionSource(loader, cacheResult: false), solutionServices);
    }

    private async ValueTask<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
    {
        if (this.TextAndVersionSource.TryGetValue(LoadTextOptions, out var textAndVersion))
        {
            return textAndVersion;
        }
        else
        {
            return await TextAndVersionSource.GetValueAsync(LoadTextOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<string?> GetFailedToLoadExceptionMessageAsync(CancellationToken cancellationToken)
    {
        if (TextAndVersionSource is TreeTextSource)
            return null;

        var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
        return textAndVersion.ExceptionMessage;
    }

    private VersionStamp GetNewerVersion()
    {
        if (this.TextAndVersionSource.TryGetValue(LoadTextOptions, out var textAndVersion))
        {
            return textAndVersion.Version.GetNewerVersion();
        }

        return VersionStamp.Create();
    }

    public virtual ValueTask<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
        => this.TextAndVersionSource.GetVersionAsync(LoadTextOptions, cancellationToken);

    /// <summary>
    /// Only checks if the source of the text has changed, no content check is done.
    /// </summary>
    public bool HasTextChanged(TextDocumentState oldState, bool ignoreUnchangeableDocument)
    {
        if (ignoreUnchangeableDocument && !oldState.CanApplyChange())
        {
            return false;
        }

        return oldState.TextAndVersionSource != TextAndVersionSource;
    }

    public bool HasInfoChanged(TextDocumentState oldState)
        => oldState.Attributes != Attributes;
}

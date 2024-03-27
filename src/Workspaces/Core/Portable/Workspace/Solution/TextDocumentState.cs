// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class TextDocumentState
{
    protected readonly SolutionServices solutionServices;

    internal ITextAndVersionSource TextAndVersionSource { get; }
    public readonly LoadTextOptions LoadTextOptions;

    // Checksums for this solution state
    private readonly AsyncLazy<DocumentStateChecksums> _lazyChecksums;

    public DocumentInfo.DocumentAttributes Attributes { get; }

    /// <summary>
    /// A <see cref="IDocumentServiceProvider"/> associated with this document
    /// </summary>
    public IDocumentServiceProvider Services { get; }

    protected TextDocumentState(
        SolutionServices solutionServices,
        IDocumentServiceProvider? documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textAndVersionSource,
        LoadTextOptions loadTextOptions)
    {
        this.solutionServices = solutionServices;

        this.LoadTextOptions = loadTextOptions;
        TextAndVersionSource = textAndVersionSource;

        Attributes = attributes;
        Services = documentServiceProvider ?? DefaultTextDocumentServiceProvider.Instance;

        // This constructor is called whenever we're creating a new TextDocumentState from another
        // TextDocumentState, and so we populate all the fields from the inputs. We will always create
        // a new AsyncLazy to compute the checksum though, and that's because there's no practical way for
        // the newly created TextDocumentState to have the same checksum as a previous TextDocumentState:
        // if we're creating a new state, it's because something changed, and we'll have to create a new checksum.
        _lazyChecksums = AsyncLazy.Create(static (self, cancellationToken) => self.ComputeChecksumsAsync(cancellationToken), arg: this);
    }

    public TextDocumentState(SolutionServices solutionServices, DocumentInfo info, LoadTextOptions loadTextOptions)
        : this(solutionServices,
               info.DocumentServiceProvider,
               info.Attributes,
               textAndVersionSource: info.TextLoader != null
                ? CreateRecoverableText(info.TextLoader, solutionServices)
                : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, encoding: null, loadTextOptions.ChecksumAlgorithm), VersionStamp.Default, info.FilePath)),
               loadTextOptions)
    {
    }

    public DocumentId Id => Attributes.Id;
    public string? FilePath => Attributes.FilePath;
    public IReadOnlyList<string> Folders => Attributes.Folders;
    public string Name => Attributes.Name;

    private static ITextAndVersionSource CreateStrongText(TextAndVersion text)
        => new ConstantTextAndVersionSource(text);

    private static ITextAndVersionSource CreateStrongText(TextLoader loader)
        => new LoadableTextAndVersionSource(loader, cacheResult: true);

    private static ITextAndVersionSource CreateRecoverableText(TextAndVersion text, SolutionServices services)
    {
        var service = services.GetRequiredService<IWorkspaceConfigurationService>();
        var options = service.Options;

        return options.DisableRecoverableText
            ? CreateStrongText(text)
            : new RecoverableTextAndVersion(new ConstantTextAndVersionSource(text), services);
    }

    private static ITextAndVersionSource CreateRecoverableText(TextLoader loader, SolutionServices services)
    {
        var service = services.GetRequiredService<IWorkspaceConfigurationService>();
        var options = service.Options;

        return options.DisableRecoverableText
            ? CreateStrongText(loader)
            : new RecoverableTextAndVersion(new LoadableTextAndVersionSource(loader, cacheResult: false), services);
    }

    public ITemporaryTextStorageInternal? Storage
        => (TextAndVersionSource as RecoverableTextAndVersion)?.Storage;

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

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        if (TryGetText(out var text))
        {
            return new ValueTask<SourceText>(text);
        }

        return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
            static (self, cancellationToken) => self.GetTextAndVersionAsync(cancellationToken),
            static (textAndVersion, _) => textAndVersion.Text,
            this,
            cancellationToken);
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

    public async Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
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
    {
        var newTextSource = mode == PreservationMode.PreserveIdentity
            ? CreateStrongText(newTextAndVersion)
            : CreateRecoverableText(newTextAndVersion, solutionServices);

        return UpdateText(newTextSource, mode, incremental: true);
    }

    public TextDocumentState UpdateText(SourceText newText, PreservationMode mode)
    {
        var newVersion = GetNewerVersion();
        var newTextAndVersion = TextAndVersion.Create(newText, newVersion, FilePath);

        return UpdateText(newTextAndVersion, mode);
    }

    public TextDocumentState UpdateText(TextLoader loader, PreservationMode mode)
    {
        // don't blow up on non-text documents.
        var newTextSource = mode == PreservationMode.PreserveIdentity
            ? CreateStrongText(loader)
            : CreateRecoverableText(loader, solutionServices);

        return UpdateText(newTextSource, mode, incremental: false);
    }

    protected virtual TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
    {
        return new TextDocumentState(
            solutionServices,
            this.Services,
            this.Attributes,
            textAndVersionSource: newTextSource,
            LoadTextOptions);
    }

    private ValueTask<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
    {
        if (this.TextAndVersionSource.TryGetValue(LoadTextOptions, out var textAndVersion))
        {
            return new ValueTask<TextAndVersion>(textAndVersion);
        }
        else
        {
            return new ValueTask<TextAndVersion>(TextAndVersionSource.GetValueAsync(LoadTextOptions, cancellationToken));
        }
    }

    internal virtual async Task<Diagnostic?> GetLoadDiagnosticAsync(CancellationToken cancellationToken)
        => (await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false)).LoadDiagnostic;

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

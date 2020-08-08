// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        protected readonly SolutionServices solutionServices;

        /// <summary>
        /// A direct reference to our source text.  This is only kept around in specialized scenarios.
        /// Specifically, we keep this around when a document is opened.  By providing this we can allow
        /// clients to easily get to the text of the document in a non-blocking fashion if that's all
        /// that they need.
        ///
        /// Note: this facility does not extend to getting the version as well.  That's because the
        /// version of a document depends on both the current source contents and the contents from 
        /// the previous version of the document.  (i.e. if the contents are the same, then we will
        /// preserve the same version, otherwise we'll move the version forward).  Because determining
        /// the version depends on comparing text, and because getting the old text may block, we 
        /// do not have the ability to know the version of the document up front, and instead can
        /// only retrieve is asynchronously through <see cref="TextAndVersionSource"/>.
        /// </summary> 
        protected readonly SourceText? sourceText;
        protected ValueSource<TextAndVersion> TextAndVersionSource { get; }

        // Checksums for this solution state
        private readonly ValueSource<DocumentStateChecksums> _lazyChecksums;

        public DocumentInfo.DocumentAttributes Attributes { get; }

        /// <summary>
        /// A <see cref="IDocumentServiceProvider"/> associated with this document
        /// </summary>
        public IDocumentServiceProvider Services { get; }

        protected TextDocumentState(
            SolutionServices solutionServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            SourceText? sourceText,
            ValueSource<TextAndVersion> textAndVersionSource)
        {
            this.solutionServices = solutionServices;
            this.sourceText = sourceText;
            this.TextAndVersionSource = textAndVersionSource;

            Attributes = attributes;
            Services = documentServiceProvider ?? DefaultTextDocumentServiceProvider.Instance;

            // This constructor is called whenever we're creating a new TextDocumentState from another
            // TextDocumentState, and so we populate all the fields from the inputs. We will always create
            // a new AsyncLazy to compute the checksum though, and that's because there's no practical way for
            // the newly created TextDocumentState to have the same checksum as a previous TextDocumentState:
            // if we're creating a new state, it's because something changed, and we'll have to create a new checksum.
            _lazyChecksums = new AsyncLazy<DocumentStateChecksums>(ComputeChecksumsAsync, cacheResult: true);
        }

        public TextDocumentState(DocumentInfo info, SolutionServices services)

            : this(services,
                   info.DocumentServiceProvider,
                   info.Attributes,
                   sourceText: null,
                   textAndVersionSource: info.TextLoader != null
                    ? CreateRecoverableText(info.TextLoader, info.Id, services)
                    : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, info.FilePath)))
        {
        }

        public DocumentId Id => Attributes.Id;
        public string? FilePath => Attributes.FilePath;
        public IReadOnlyList<string> Folders => Attributes.Folders;
        public string Name => Attributes.Name;

        protected static ValueSource<TextAndVersion> CreateStrongText(TextAndVersion text)
            => new ConstantValueSource<TextAndVersion>(text);

        protected static ValueSource<TextAndVersion> CreateStrongText(TextLoader loader, DocumentId documentId, SolutionServices services)
        {
            return new AsyncLazy<TextAndVersion>(
                asynchronousComputeFunction: cancellationToken => loader.LoadTextAsync(services.Workspace, documentId, cancellationToken),
                synchronousComputeFunction: cancellationToken => loader.LoadTextSynchronously(services.Workspace, documentId, cancellationToken),
                cacheResult: true);
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextAndVersion text, SolutionServices services)
        {
            var result = new RecoverableTextAndVersion(CreateStrongText(text), services.TemporaryStorage);

            // This RecoverableTextAndVersion is created directly from a TextAndVersion instance. In its initial state,
            // the RecoverableTextAndVersion keeps a strong reference to the initial TextAndVersion, and only
            // transitions to a weak reference backed by temporary storage after the first time GetValue (or
            // GetValueAsync) is called. Since we know we are creating a RecoverableTextAndVersion for the purpose of
            // avoiding problematic address space overhead, we call GetValue immediately to force the object to weakly
            // hold its data from the start.
            result.GetValue();

            return result;
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextLoader loader, DocumentId documentId, SolutionServices services)
        {
            return new RecoverableTextAndVersion(
                new AsyncLazy<TextAndVersion>(
                    asynchronousComputeFunction: cancellationToken => loader.LoadTextAsync(services.Workspace, documentId, cancellationToken),
                    synchronousComputeFunction: cancellationToken => loader.LoadTextSynchronously(services.Workspace, documentId, cancellationToken),
                    cacheResult: false),
                services.TemporaryStorage);
        }

        public ITemporaryTextStorage? Storage
        {
            get
            {
                var recoverableText = this.TextAndVersionSource as RecoverableTextAndVersion;
                if (recoverableText == null)
                {
                    return null;
                }

                return recoverableText.Storage;
            }
        }

        public bool TryGetText([NotNullWhen(returnValue: true)] out SourceText? text)
        {
            if (this.sourceText != null)
            {
                text = sourceText;
                return true;
            }

            if (this.TextAndVersionSource.TryGetValue(out var textAndVersion))
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
        {
            // try fast path first
            if (this.TextAndVersionSource is ITextVersionable versionable)
            {
                return versionable.TryGetTextVersion(out version);
            }

            if (this.TextAndVersionSource.TryGetValue(out var textAndVersion))
            {
                version = textAndVersion.Version;
                return true;
            }
            else
            {
                version = default;
                return false;
            }
        }

        public bool TryGetTextAndVersion(out TextAndVersion? textAndVersion)
            => TextAndVersionSource.TryGetValue(out textAndVersion);

        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            if (sourceText != null)
            {
                return sourceText;
            }

            if (TryGetText(out var text))
            {
                return text;
            }

            var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Text;
        }

        public SourceText GetTextSynchronously(CancellationToken cancellationToken)
        {
            var textAndVersion = this.TextAndVersionSource.GetValue(cancellationToken);
            return textAndVersion.Text;
        }

        public VersionStamp GetTextVersionSynchronously(CancellationToken cancellationToken)
        {
            var textAndVersion = this.TextAndVersionSource.GetValue(cancellationToken);
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
                : CreateRecoverableText(newTextAndVersion, this.solutionServices);

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
                ? CreateStrongText(loader, Id, solutionServices)
                : CreateRecoverableText(loader, Id, solutionServices);

            return UpdateText(newTextSource, mode, incremental: false);
        }

        protected virtual TextDocumentState UpdateText(ValueSource<TextAndVersion> newTextSource, PreservationMode mode, bool incremental)
        {
            return new TextDocumentState(
                this.solutionServices,
                this.Services,
                this.Attributes,
                sourceText: null,
                textAndVersionSource: newTextSource);
        }

        private async Task<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
        {
            if (this.TextAndVersionSource.TryGetValue(out var textAndVersion))
            {
                return textAndVersion;
            }
            else
            {
                return await this.TextAndVersionSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        internal virtual async Task<Diagnostic?> GetLoadDiagnosticAsync(CancellationToken cancellationToken)
            => (await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false)).LoadDiagnostic;

        private VersionStamp GetNewerVersion()
        {
            if (this.TextAndVersionSource.TryGetValue(out var textAndVersion))
            {
                return textAndVersion.Version.GetNewerVersion();
            }

            return VersionStamp.Create();
        }

        public virtual async Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
        {
            var textAndVersion = await this.TextAndVersionSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Version;
        }

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
}

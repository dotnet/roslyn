// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

        public DocumentInfo.DocumentAttributes Attributes { get; }

        /// <summary>
        /// A <see cref="IDocumentServiceProvider"/> associated with this document
        /// </summary>
        public IDocumentServiceProvider Services { get; }

        public DocumentId Id
        {
            get { return Attributes.Id; }
        }

        public string? FilePath
        {
            get { return Attributes.FilePath; }
        }

        public IReadOnlyList<string> Folders
        {
            get { return Attributes.Folders; }
        }

        public string Name
        {
            get { return this.Attributes.Name; }
        }

        public TextDocumentState(DocumentInfo info, SolutionServices services)
            : this(
                  services,
                  info.DocumentServiceProvider,
                  info.Attributes,
                  sourceText: null,
                  textAndVersionSource: info.TextLoader != null
                    ? CreateRecoverableText(info.TextLoader, info.Id, services, reportInvalidDataException: false)
                    : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, info.FilePath)))
        {
        }

        protected static ValueSource<TextAndVersion> CreateStrongText(TextAndVersion text)
        {
            return new ConstantValueSource<TextAndVersion>(text);
        }

        protected static ValueSource<TextAndVersion> CreateStrongText(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException)
        {
            return new AsyncLazy<TextAndVersion>(
                asynchronousComputeFunction: c => LoadTextAsync(loader, documentId, services, reportInvalidDataException, c),
                synchronousComputeFunction: c => LoadTextSynchronously(loader, documentId, services, reportInvalidDataException, c),
                cacheResult: true);
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextAndVersion text, SolutionServices services)
        {
            return new RecoverableTextAndVersion(CreateStrongText(text), services.TemporaryStorage);
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException)
        {
            return new RecoverableTextAndVersion(
                new AsyncLazy<TextAndVersion>(
                    asynchronousComputeFunction: c => LoadTextAsync(loader, documentId, services, reportInvalidDataException, c),
                    synchronousComputeFunction: c => LoadTextSynchronously(loader, documentId, services, reportInvalidDataException, c),
                    cacheResult: false),
                services.TemporaryStorage);
        }

        private const double MaxDelaySecs = 1.0;
        private const int MaxRetries = 5;
        internal static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(MaxDelaySecs / MaxRetries);

        protected static async Task<TextAndVersion> LoadTextAsync(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException, CancellationToken cancellationToken)
        {
            var retries = 0;

            while (true)
            {
                try
                {
                    return await loader.LoadTextAndVersionAsync(services.Workspace, documentId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                }
                catch (OperationCanceledException)
                {
                    // if load text is failed due to a cancellation, make sure we propagate it out to the caller
                    throw;
                }
                catch (IOException e)
                {
                    if (++retries > MaxRetries)
                    {
                        services.Workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                        return TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, documentId.GetDebuggerDisplay());
                    }

                    // fall out to try again
                }
                catch (InvalidDataException e)
                {
                    // TODO: Adjust this behavior in the future if we add support for non-text additional files
                    if (reportInvalidDataException)
                    {
                        services.Workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                    }

                    return TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, documentId.GetDebuggerDisplay());
                }

                // try again after a delay
                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        protected static TextAndVersion LoadTextSynchronously(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException, CancellationToken cancellationToken)
        {
            var retries = 0;

            while (true)
            {
                try
                {
                    return loader.LoadTextAndVersionSynchronously(services.Workspace, documentId, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // if load text is failed due to a cancellation, make sure we propagate it out to the caller
                    throw;
                }
                catch (IOException e)
                {
                    if (++retries > MaxRetries)
                    {
                        services.Workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                        return TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, documentId.GetDebuggerDisplay());
                    }

                    // fall out to try again
                }
                catch (InvalidDataException e)
                {
                    // TODO: Adjust this behavior in the future if we add support for non-text additional files
                    if (reportInvalidDataException)
                    {
                        services.Workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                    }

                    return TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, documentId.GetDebuggerDisplay());
                }

                // try again after a delay
                Thread.Sleep(RetryDelay);
            }
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

        public async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
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
            if (newTextAndVersion == null)
            {
                throw new ArgumentNullException(nameof(newTextAndVersion));
            }

            var newTextSource = mode == PreservationMode.PreserveIdentity
                ? CreateStrongText(newTextAndVersion)
                : CreateRecoverableText(newTextAndVersion, this.solutionServices);

            return UpdateText(newTextSource, mode, incremental: true);
        }

        public virtual TextDocumentState UpdateFilePath(string filePath)
        {
            var newAttributes = this.Attributes.With(filePath: filePath);

            return new TextDocumentState(
                this.solutionServices,
                this.Services,
                newAttributes,
                sourceTextOpt: this.sourceTextOpt,
                textAndVersionSource: this.TextAndVersionSource);
        }

        public TextDocumentState UpdateText(SourceText newText, PreservationMode mode)
        {
            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            var newVersion = this.GetNewerVersion();
            var newTextAndVersion = TextAndVersion.Create(newText, newVersion, this.FilePath);

            var newState = this.UpdateText(newTextAndVersion, mode);
            return newState;
        }

        public TextDocumentState UpdateText(TextLoader loader, PreservationMode mode)
        {
            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            // don't blow up on non-text documents.
            var newTextSource = mode == PreservationMode.PreserveIdentity
                ? CreateStrongText(loader, this.Id, this.solutionServices, reportInvalidDataException: false)
                : CreateRecoverableText(loader, this.Id, this.solutionServices, reportInvalidDataException: false);

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
    }
}

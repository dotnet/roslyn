// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        /// only retrieve is asynchronously through <see cref="textAndVersionSource"/>.
        /// </summary> 
        protected readonly SourceText sourceTextOpt;
        protected ValueSource<TextAndVersion> textAndVersionSource;

        // Checksums for this solution state
        private readonly ValueSource<DocumentStateChecksums> _lazyChecksums;

        protected TextDocumentState(
            SolutionServices solutionServices,
            IDocumentServiceProvider documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            SourceText sourceTextOpt,
            ValueSource<TextAndVersion> textAndVersionSource,
            ValueSource<DocumentStateChecksums> lazyChecksums)
        {
            this.solutionServices = solutionServices;
            this.sourceTextOpt = sourceTextOpt;
            this.textAndVersionSource = textAndVersionSource;

            Attributes = attributes;
            Services = documentServiceProvider ?? DefaultTextDocumentServiceProvider.Instance;

            // for now, let it re-calculate if anything changed.
            // TODO: optimize this so that we only re-calcuate checksums that are actually changed
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

        public string FilePath
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

        public static TextDocumentState Create(DocumentInfo info, SolutionServices services)
        {
            var textSource = info.TextLoader != null
                ? CreateRecoverableText(info.TextLoader, info.Id, services, reportInvalidDataException: false)
                : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, info.FilePath));

            return new TextDocumentState(
                solutionServices: services,
                documentServiceProvider: info.DocumentServiceProvider,
                attributes: info.Attributes,
                sourceTextOpt: null,
                textAndVersionSource: textSource,
                lazyChecksums: null);
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
            int retries = 0;

            while (true)
            {
                try
                {
                    using (ExceptionHelpers.SuppressFailFast())
                    {
                        var result = await loader.LoadTextAndVersionAsync(services.Workspace, documentId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                        return result;
                    }
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
            int retries = 0;

            while (true)
            {
                try
                {
                    using (ExceptionHelpers.SuppressFailFast())
                    {
                        var result = loader.LoadTextAndVersionSynchronously(services.Workspace, documentId, cancellationToken);
                        return result;
                    }
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

        public ITemporaryTextStorage Storage
        {
            get
            {
                var recoverableText = this.textAndVersionSource as RecoverableTextAndVersion;
                if (recoverableText == null)
                {
                    return null;
                }

                return recoverableText.Storage;
            }
        }

        public bool TryGetText(out SourceText text)
        {
            if (this.sourceTextOpt != null)
            {
                text = sourceTextOpt;
                return true;
            }

            if (this.textAndVersionSource.TryGetValue(out var textAndVersion))
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
            if (this.textAndVersionSource is ITextVersionable versionable)
            {
                return versionable.TryGetTextVersion(out version);
            }

            if (this.textAndVersionSource.TryGetValue(out var textAndVersion))
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
            if (sourceTextOpt != null)
            {
                return sourceTextOpt;
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
            var textAndVersion = this.textAndVersionSource.GetValue(cancellationToken);
            return textAndVersion.Text;
        }

        public VersionStamp GetTextVersionSynchronously(CancellationToken cancellationToken)
        {
            var textAndVersion = this.textAndVersionSource.GetValue(cancellationToken);
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

            return new TextDocumentState(
                this.solutionServices,
                this.Services,
                this.Attributes,
                sourceTextOpt: null,
                textAndVersionSource: newTextSource,
                lazyChecksums: null);
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

            return new TextDocumentState(
                this.solutionServices,
                this.Services,
                this.Attributes,
                sourceTextOpt: null,
                textAndVersionSource: newTextSource,
                lazyChecksums: null);
        }

        private async Task<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
        {
            if (this.textAndVersionSource.TryGetValue(out var textAndVersion))
            {
                return textAndVersion;
            }
            else
            {
                return await this.textAndVersionSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private VersionStamp GetNewerVersion()
        {
            if (this.textAndVersionSource.TryGetValue(out var textAndVersion))
            {
                return textAndVersion.Version.GetNewerVersion();
            }

            return VersionStamp.Create();
        }

        public virtual async Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
        {
            TextAndVersion textAndVersion = await this.textAndVersionSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Version;
        }
    }
}

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        protected SolutionServices solutionServices;
        protected DocumentInfo info;

        protected ValueSource<TextAndVersion> textSource;

        protected TextDocumentState(
            SolutionServices solutionServices,
            DocumentInfo info,
            ValueSource<TextAndVersion> textSource)
        {
            this.solutionServices = solutionServices;
            this.info = info;
            this.textSource = textSource;
        }

        public DocumentId Id
        {
            get { return this.info.Id; }
        }

        public string FilePath
        {
            get { return this.info.FilePath; }
        }

        public DocumentInfo Info
        {
            get { return this.info; }
        }

        public IReadOnlyList<string> Folders
        {
            get { return this.info.Folders; }
        }

        public string Name
        {
            get { return this.info.Name; }
        }

        public static TextDocumentState Create(DocumentInfo info, SolutionServices services)
        {
            var textSource = info.TextLoader != null
                ? CreateRecoverableText(info.TextLoader, info.Id, services, reportInvalidDataException: false)
                : CreateStrongText(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), VersionStamp.Default, info.FilePath));

            // remove any initial loader so we don't keep source alive
            info = info.WithTextLoader(null);

            return new TextDocumentState(
                solutionServices: services,
                info: info,
                textSource: textSource);
        }

        protected static ValueSource<TextAndVersion> CreateStrongText(TextAndVersion text)
        {
            return new ConstantValueSource<TextAndVersion>(text);
        }

        protected static ValueSource<TextAndVersion> CreateStrongText(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException)
        {
            return new AsyncLazy<TextAndVersion>(c => LoadTextAsync(loader, documentId, services, reportInvalidDataException, c), cacheResult: true);
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextAndVersion text, SolutionServices services)
        {
            return new RecoverableTextAndVersion(CreateStrongText(text), services.TemporaryStorage);
        }

        protected static ValueSource<TextAndVersion> CreateRecoverableText(TextLoader loader, DocumentId documentId, SolutionServices services, bool reportInvalidDataException)
        {
            return new RecoverableTextAndVersion(
                new AsyncLazy<TextAndVersion>(c => LoadTextAsync(loader, documentId, services, reportInvalidDataException, c), cacheResult: false),
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
                await Task.Delay(RetryDelay).ConfigureAwait(false);
            }
        }

        public bool TryGetText(out SourceText text)
        {
            TextAndVersion textAndVersion;
            if (this.textSource.TryGetValue(out textAndVersion))
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
            var versionable = this.textSource as ITextVersionable;
            if (versionable != null)
            {
                return versionable.TryGetTextVersion(out version);
            }

            TextAndVersion textAndVersion;
            if (this.textSource.TryGetValue(out textAndVersion))
            {
                version = textAndVersion.Version;
                return true;
            }
            else
            {
                version = default(VersionStamp);
                return false;
            }
        }

        public async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            var textAndVersion = await this.textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Text;
        }

        public SourceText GetText(CancellationToken cancellationToken)
        {
            var textAndVersion = this.textSource.GetValue(cancellationToken);
            return textAndVersion.Text;
        }

        public async Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        {
            // try fast path first
            VersionStamp version;
            if (TryGetTextVersion(out version))
            {
                return version;
            }

            TextAndVersion textAndVersion;
            if (this.textSource.TryGetValue(out textAndVersion))
            {
                return textAndVersion.Version;
            }
            else
            {
                textAndVersion = await this.textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return textAndVersion.Version;
            }
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
                this.info,
                newTextSource);
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
            var newTextSource = (mode == PreservationMode.PreserveIdentity)
                ? CreateStrongText(loader, this.Id, this.solutionServices, reportInvalidDataException: false)
                : CreateRecoverableText(loader, this.Id, this.solutionServices, reportInvalidDataException: false);

            return new TextDocumentState(
                this.solutionServices,
                this.info,
                textSource: newTextSource);
        }

        private VersionStamp GetNewerVersion()
        {
            TextAndVersion textAndVersion;
            if (this.textSource.TryGetValue(out textAndVersion))
            {
                return textAndVersion.Version.GetNewerVersion();
            }

            return VersionStamp.Create();
        }

        public virtual async Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken)
        {
            TextAndVersion textAndVersion = await this.textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Version;
        }
    }
}

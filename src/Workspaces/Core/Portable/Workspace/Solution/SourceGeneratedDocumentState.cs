// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceGeneratedDocumentState : DocumentState
    {
        private readonly AsyncLazy<Checksum> _lazyTextChecksum;

        public SourceGeneratedDocumentIdentity Identity { get; }

        public string HintName => Identity.HintName;

        public static SourceGeneratedDocumentState Create(
            SourceGeneratedDocumentIdentity documentIdentity,
            SourceText generatedSourceText,
            ParseOptions parseOptions,
            LanguageServices languageServices)
        {
            var loadTextOptions = new LoadTextOptions(generatedSourceText.ChecksumAlgorithm);
            var textAndVersion = TextAndVersion.Create(generatedSourceText, VersionStamp.Create());
            var textSource = new ConstantTextAndVersionSource(textAndVersion);
            var treeSource = CreateLazyFullyParsedTree(
                textSource,
                loadTextOptions,
                documentIdentity.FilePath,
                parseOptions,
                languageServices);

            return new SourceGeneratedDocumentState(
                documentIdentity,
                languageServices,
                documentServiceProvider: SourceGeneratedTextDocumentServiceProvider.Instance,
                new DocumentInfo.DocumentAttributes(
                    documentIdentity.DocumentId,
                    name: documentIdentity.HintName,
                    folders: SpecializedCollections.EmptyReadOnlyList<string>(),
                    parseOptions.Kind,
                    filePath: documentIdentity.FilePath,
                    isGenerated: true,
                    designTimeOnly: false),
                parseOptions,
                textSource,
                loadTextOptions,
                treeSource);
        }

        private SourceGeneratedDocumentState(
            SourceGeneratedDocumentIdentity documentIdentity,
            LanguageServices languageServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ITextAndVersionSource textSource,
            LoadTextOptions loadTextOptions,
            AsyncLazy<TreeAndVersion> treeSource)
            : base(languageServices, documentServiceProvider, attributes, options, textSource, loadTextOptions, treeSource)
        {
            Identity = documentIdentity;
            _lazyTextChecksum = AsyncLazy.Create(async cancellationToken =>
            {
                var text = await this.GetTextAsync(cancellationToken).ConfigureAwait(false);
                return Checksum.From(text.GetChecksum());
            });
        }

        // The base allows for parse options to be null for non-C#/VB languages, but we'll always have parse options
        public new ParseOptions ParseOptions => base.ParseOptions!;

        public Task<Checksum> GetTextChecksumAsync(CancellationToken cancellationToken)
            => _lazyTextChecksum.GetValueAsync(cancellationToken);

        protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
            => throw new NotSupportedException(WorkspacesResources.The_contents_of_a_SourceGeneratedDocument_may_not_be_changed);

        private bool TryGetTextChecksum([NotNullWhen(true)] out Checksum? checksum)
        {
            if (_lazyTextChecksum.TryGetValue(out checksum))
                return true;

            if (this.TryGetText(out var existingText))
            {
                checksum = Checksum.From(existingText.GetChecksum());
                return true;
            }

            return false;
        }

        public SourceGeneratedDocumentState WithUpdatedGeneratedContent(SourceText sourceText, ParseOptions parseOptions)
        {
            if (TryGetTextChecksum(out var existingChecksum) &&
                existingChecksum == Checksum.From(sourceText.GetChecksum()) &&
                ParseOptions.Equals(parseOptions))
            {
                // We can reuse this instance directly
                return this;
            }

            return Create(
                Identity,
                sourceText,
                parseOptions,
                LanguageServices);
        }

        /// <summary>
        /// This is modeled after <see cref="DefaultTextDocumentServiceProvider"/>, but sets
        /// <see cref="IDocumentOperationService.CanApplyChange"/> to <see langword="false"/> for source generated
        /// documents.
        /// </summary>
        internal sealed class SourceGeneratedTextDocumentServiceProvider : IDocumentServiceProvider
        {
            public static readonly SourceGeneratedTextDocumentServiceProvider Instance = new();

            private SourceGeneratedTextDocumentServiceProvider()
            {
            }

            public TService? GetService<TService>()
                where TService : class, IDocumentService
            {
                if (SourceGeneratedDocumentOperationService.Instance is TService documentOperationService)
                {
                    return documentOperationService;
                }

                if (DocumentPropertiesService.Default is TService documentPropertiesService)
                {
                    return documentPropertiesService;
                }

                return null;
            }

            private class SourceGeneratedDocumentOperationService : IDocumentOperationService
            {
                public static readonly SourceGeneratedDocumentOperationService Instance = new();

                public bool CanApplyChange => false;
                public bool SupportDiagnostics => true;
            }
        }
    }
}

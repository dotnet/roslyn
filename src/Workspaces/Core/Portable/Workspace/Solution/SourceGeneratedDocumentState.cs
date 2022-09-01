// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceGeneratedDocumentState : DocumentState
    {
        public SourceGeneratedDocumentIdentity Identity { get; }
        public string HintName => Identity.HintName;
        public string SourceGeneratorAssemblyName => Identity.GeneratorAssemblyName;
        public string SourceGeneratorTypeName => Identity.GeneratorTypeName;

        public static SourceGeneratedDocumentState Create(
            SourceGeneratedDocumentIdentity documentIdentity,
            SourceText generatedSourceText,
            ParseOptions parseOptions,
            HostLanguageServices languageServices,
            SolutionServices solutionServices)
        {
            var textAndVersion = TextAndVersion.Create(generatedSourceText, VersionStamp.Create());
            var textSource = new ConstantValueSource<TextAndVersion>(textAndVersion);
            var treeSource = CreateLazyFullyParsedTree(
                textSource,
                documentIdentity.DocumentId.ProjectId,
                documentIdentity.FilePath,
                parseOptions,
                languageServices);

            return new SourceGeneratedDocumentState(
                documentIdentity,
                languageServices,
                solutionServices,
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
                treeSource);
        }

        private SourceGeneratedDocumentState(
            SourceGeneratedDocumentIdentity documentIdentity,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ValueSource<TextAndVersion> textSource,
            ValueSource<TreeAndVersion> treeSource)
            : base(languageServices, solutionServices, documentServiceProvider, attributes, options, sourceText: null, textSource, treeSource)
        {
            Identity = documentIdentity;
        }

        // The base allows for parse options to be null for non-C#/VB languages, but we'll always have parse options
        public new ParseOptions ParseOptions => base.ParseOptions!;

        protected override TextDocumentState UpdateText(ValueSource<TextAndVersion> newTextSource, PreservationMode mode, bool incremental)
        {
            throw new NotSupportedException(WorkspacesResources.The_contents_of_a_SourceGeneratedDocument_may_not_be_changed);
        }

        public SourceGeneratedDocumentState WithUpdatedGeneratedContent(SourceText sourceText, ParseOptions parseOptions)
        {
            if (TryGetText(out var existingText) &&
                Checksum.From(existingText.GetChecksum()) == Checksum.From(sourceText.GetChecksum()) &&
                ParseOptions.Equals(parseOptions))
            {
                // We can reuse this instance directly
                return this;
            }

            return Create(
                Identity,
                sourceText,
                parseOptions,
                this.LanguageServices,
                this.solutionServices);
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

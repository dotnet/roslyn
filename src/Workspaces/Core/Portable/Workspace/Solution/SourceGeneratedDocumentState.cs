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
        public string HintName { get; }
        public ISourceGenerator SourceGenerator { get; }

        public static SourceGeneratedDocumentState Create(
            string hintName,
            SourceText generatedSourceText,
            DocumentId documentId,
            string? filePath,
            ParseOptions parseOptions,
            ISourceGenerator sourceGenerator,
            HostLanguageServices languageServices,
            SolutionServices solutionServices)
        {
            var textAndVersion = TextAndVersion.Create(generatedSourceText, VersionStamp.Create());
            var textSource = new ConstantValueSource<TextAndVersion>(textAndVersion);
            var treeSource = CreateLazyFullyParsedTree(
                textSource,
                documentId.ProjectId,
                filePath,
                parseOptions,
                languageServices);

            return new SourceGeneratedDocumentState(
                languageServices,
                solutionServices,
                documentServiceProvider: null,
                new DocumentInfo.DocumentAttributes(
                    documentId,
                    name: hintName,
                    folders: SpecializedCollections.EmptyReadOnlyList<string>(),
                    parseOptions.Kind,
                    filePath: filePath,
                    isGenerated: true,
                    designTimeOnly: false),
                parseOptions,
                textSource,
                treeSource,
                sourceGenerator,
                hintName);
        }

        private SourceGeneratedDocumentState(
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IDocumentServiceProvider? documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            ParseOptions options,
            ValueSource<TextAndVersion> textSource,
            ValueSource<TreeAndVersion> treeSource,
            ISourceGenerator sourceGenerator,
            string hintName)
            : base(languageServices, solutionServices, documentServiceProvider, attributes, options, sourceText: null, textSource, treeSource)
        {
            SourceGenerator = sourceGenerator;
            HintName = hintName;
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
                this.HintName,
                sourceText,
                this.Id,
                this.FilePath,
                ParseOptions,
                this.SourceGenerator,
                this.LanguageServices,
                this.solutionServices);
        }
    }
}
